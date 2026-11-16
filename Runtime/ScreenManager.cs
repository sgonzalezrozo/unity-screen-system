using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScreenSystem
{
    /// <summary>The entry point for screen navigation.</summary>
    [RequireComponent(typeof(ScreenSystemBootstrap))]
    public class ScreenManager : MonoBehaviour, IScreenNavigationService
    {
        private static ScreenManager instance;

        // Completed with the manager once it's initialized, or with null if it failed or isn't set up.
        private static TaskCompletionSource<ScreenManager> readySource = new();

        // With Enter Play Mode Options and domain reload off, statics survive between Play sessions. Start each one clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStatics()
        {
            instance = null;
            readySource = new TaskCompletionSource<ScreenManager>();
        }

        /// <summary>
        /// Waits until Screen System is ready to navigate and returns the manager. Returns null if initialization
        /// failed or Screen System isn't set up (the error is logged). Safe to call from any Awake or Start, and
        /// any number of times:
        /// <code>
        /// ScreenManager screens = await ScreenManager.WhenReadyAsync();
        /// if (screens != null)
        ///     await screens.SetRoot("MainMenu");
        /// </code>
        /// </summary>
        public static async Task<ScreenManager> WhenReadyAsync(CancellationToken ct = default)
        {
            Task<ScreenManager> ready = readySource.Task;
            if (ready.IsCompleted || !ct.CanBeCanceled)
                return await ready;

            TaskCompletionSource<bool> cancelled = new();
            using (ct.Register(() => cancelled.TrySetResult(true)))
            {
                if (await Task.WhenAny(ready, cancelled.Task) != ready)
                    throw new OperationCanceledException(ct);
            }

            return await ready;
        }

        internal static void ReportReady(ScreenManager manager)
            => readySource.TrySetResult(manager);

        /// <summary>Called when Screen System can't start, so WhenReadyAsync returns null instead of waiting forever.</summary>
        internal static void ReportNotAvailable()
            => readySource.TrySetResult(null);

        [Header("Quit")] [SerializeField] private string quitConfirmationModalId = "QuitConfirmation";

        [Header("Splash Screen")] [SerializeField]
        private string splashScreenId = "SplashMain";

        private ScreenServiceInitializer serviceInitializer;
        private ScreenInputHandler inputHandler;
        private ScreenIdResolver idResolver;
        private ScreenNavigationService navigationService;
        private ScreenDisplayService displayService;
        private bool isInitialized;
        private string initializationError;
        private TaskCompletionSource<bool> initializationCompletionSource = new();
        private CancellationTokenSource initializationCts;

        /// <summary>
        /// The active manager. Set in Awake: the ScreenSystem prefab is created before the first scene loads, so it's
        /// already set when scene objects run their Awake. It may not be initialized yet; use
        /// <see cref="WhenReadyAsync"/> to wait for both.
        /// </summary>
        public static ScreenManager Instance
            => instance;

        /// <summary>True once the manager is ready to navigate.</summary>
        public bool IsInitialized
            => isInitialized;

        /// <summary>Waits until the manager is ready to navigate.</summary>
        public Task<bool> WaitForInitializationAsync()
            => initializationCompletionSource.Task;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        private async void Start()
        {
            if (instance != this)
                return;

            try
            {
                await InitializeScreenSystemAsync();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                ReportNotAvailable();
            }
        }

        private async Task InitializeScreenSystemAsync()
        {
            try
            {
                serviceInitializer = new ScreenServiceInitializer(transform);
                initializationCts = new CancellationTokenSource();

                bool initSuccess = await serviceInitializer.InitializeAsync(
                    initializationCts.Token,
                    quitConfirmationModalId);

                if (!initSuccess)
                {
                    isInitialized = false;
                    initializationError = serviceInitializer.InitializationError;
                    if (Debug.isDebugBuild)
                        Debug.LogError($"[ScreenManager] Initialization failed: {initializationError}");
                    initializationCompletionSource.TrySetResult(false);
                    ReportNotAvailable();
                    return;
                }

                navigationService = serviceInitializer.NavigationService;
                displayService = serviceInitializer.DisplayService;
                idResolver = new ScreenIdResolver(serviceInitializer.Registry);

                // The splash and the quit confirmation are always kept on scene changes, like Persist Over Scenes screens.
                List<string> specialScreenIds = new();
                foreach (string special in new[] { splashScreenId, quitConfirmationModalId })
                {
                    ScreenDefinition definition = string.IsNullOrEmpty(special) ? null : serviceInitializer.Registry.GetScreen(special);
                    if (definition != null)
                        specialScreenIds.Add(definition.ScreenId);
                }

                navigationService.SetAlwaysKeptScreens(specialScreenIds);
                displayService.SetAlwaysKeptScreens(specialScreenIds);
                inputHandler = new ScreenInputHandler(this);

                if (!string.IsNullOrEmpty(splashScreenId))
                    await ShowInitialSplash();

                SceneManager.sceneLoaded += OnSceneLoaded;
                isInitialized = true;
                initializationCompletionSource.TrySetResult(true);
                ReportReady(this);
            }
            catch (Exception ex)
            {
                isInitialized = false;
                initializationError = $"Exception: {ex.Message}";

                if (Debug.isDebugBuild)
                    Debug.LogError($"[ScreenManager] Initialization exception: {ex.Message}\n{ex.StackTrace}");

                initializationCompletionSource.TrySetResult(false);
                ReportNotAvailable();
            }
        }

        private async Task ShowInitialSplash()
        {
            try
            {
                ScreenNavigationService nav = navigationService;

                if (nav == null)
                {
                    Debug.LogWarning("[ScreenManager] navigationService is null, cannot show splash");
                    return;
                }

                bool success = await nav.GoToAsync(splashScreenId, CancellationToken.None);

                if (!success)
                    Debug.Log($"[ScreenManager] Splash screen '{splashScreenId}' not found, skipping");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ScreenManager] Failed to show initial splash: {ex.Message}");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            serviceInitializer?.SceneLifecycleManager?.OnSceneLoaded(scene, mode);
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            serviceInitializer?.SceneLifecycleManager?.Tick();
            inputHandler?.CheckForInput(CancellationToken.None);
        }


        // Navigation before initialization logs a clear error and returns false, instead of throwing.
        private bool IsReady(string call)
        {
            if (isInitialized && navigationService != null && idResolver != null)
                return true;

            string reason = initializationError != null ? $"initialization failed: {initializationError}" : "it isn't initialized yet. Await ScreenManager.WhenReadyAsync() first";
            Debug.LogError($"[ScreenManager] {call} ignored: {reason}.");
            return false;
        }

        /// <summary>Opens a screen by name or ID. The result converts to bool; its Status says why a call didn't happen.</summary>
        public Task<NavigationResult> GoTo(string screenId, CancellationToken ct = default)
            => GoTo(screenId, NavigationHistory.Push, ct);

        /// <summary>
        /// Opens a screen by name or ID. With <see cref="NavigationHistory.ClearToRoot"/>, every screen above the
        /// root is removed first, as a tab bar does.
        /// </summary>
        public Task<NavigationResult> GoTo(string screenId, NavigationHistory history, CancellationToken ct = default)
            => GoToWithParameter(screenId, history, null, ct);

        /// <summary>
        /// Opens a screen and passes it a value. Every component on the screen's layers that implements
        /// <see cref="IScreenParameter{T}"/> gets it once, before the layers are shown. GoBack and PopTo don't pass
        /// it again, and Screen System doesn't keep it.
        /// </summary>
        public Task<NavigationResult> GoTo<T>(string screenId, T parameter, CancellationToken ct = default)
            => GoToWithParameter(screenId, NavigationHistory.Push, ScreenParameter.Create(parameter), ct);

        /// <summary>Opens a screen, passes it a value, and with ClearToRoot removes every screen above the root first.</summary>
        public Task<NavigationResult> GoTo<T>(string screenId, T parameter, NavigationHistory history, CancellationToken ct = default)
            => GoToWithParameter(screenId, history, ScreenParameter.Create(parameter), ct);

        private async Task<NavigationResult> GoToWithParameter(string screenId, NavigationHistory history, ScreenParameter parameter, CancellationToken ct)
        {
            if (!IsReady(nameof(GoTo)))
                return new NavigationResult(NavigationStatus.NotReady, screenId);

            if (!idResolver.TryResolveScreenId(screenId, nameof(GoTo), out string resolvedId))
                return new NavigationResult(NavigationStatus.NotFound, screenId);

            return Result(await navigationService.GoToAsync(resolvedId, history, parameter, ct), screenId);
        }

        /// <summary>
        /// Closes the open modal or returns to the previous screen. At the root, starts the quit flow unless
        /// Quit On Back At Root is off. With no screen open, does nothing, so your own UI keeps the back key.
        /// </summary>
        public async Task<NavigationResult> GoBack(CancellationToken ct = default)
        {
            if (!IsReady(nameof(GoBack)))
                return new NavigationResult(NavigationStatus.NotReady);

            // A call ignored during a transition also returns false at the root. That must not start the quit flow.
            if (navigationService.IsNavigating)
            {
                Debug.LogWarning("[ScreenManager] GoBack ignored: a navigation call is still running.");
                return new NavigationResult(NavigationStatus.Transitioning);
            }

            // Nothing open: the back key belongs to UI outside Screen System, as in a project still migrating.
            if (string.IsNullOrEmpty(navigationService.GetCurrentScreen()) && !navigationService.CanGoBack)
                return new NavigationResult(NavigationStatus.NothingToClose);

            NavigationResult result = Result(await navigationService.GoBackAsync(ct), null);

            if (!result && !navigationService.CanGoBack && QuitsOnBackAtRoot)
                await serviceInitializer?.QuitManager?.HandleQuitRequestAsync(ct);

            return result;
        }

        /// <summary>Returns to a screen that is already in the stack.</summary>
        public async Task<NavigationResult> PopTo(string screenId, CancellationToken ct = default)
        {
            if (!IsReady(nameof(PopTo)))
                return new NavigationResult(NavigationStatus.NotReady, screenId);

            if (!idResolver.TryResolveScreenId(screenId, nameof(PopTo), out string resolvedId))
                return new NavigationResult(NavigationStatus.NotFound, screenId);

            return Result(await navigationService.PopToAsync(resolvedId, ct), screenId);
        }

        /// <summary>Replaces the root screen.</summary>
        public async Task<NavigationResult> SetRoot(string screenId, CancellationToken ct = default)
        {
            if (!IsReady(nameof(SetRoot)))
                return new NavigationResult(NavigationStatus.NotReady, screenId);

            if (!idResolver.TryResolveScreenId(screenId, nameof(SetRoot), out string resolvedId))
                return new NavigationResult(NavigationStatus.NotFound, screenId);

            return Result(await navigationService.SetRootAsync(resolvedId, ct), screenId);
        }

        /// <summary>
        /// Hides every screen, modal and alert and empties the stack. Use it to return to UI that isn't managed by
        /// Screen System, for example from NavigationEvents.OnBackAtRoot while migrating. Screens stay loaded until
        /// the next scene change, so opening them again is fast.
        /// </summary>
        public async Task<NavigationResult> CloseAll(CancellationToken ct = default)
        {
            if (!IsReady(nameof(CloseAll)))
                return new NavigationResult(NavigationStatus.NotReady);

            return Result(await navigationService.CloseAllAsync(ct), null);
        }

        private NavigationResult Result(bool succeeded, string screen)
            => new(succeeded ? NavigationStatus.Succeeded : FailureStatus(), screen);

        // The guard reports Transitioning; otherwise the navigator's reason. Never Succeeded for a false result.
        private NavigationStatus FailureStatus()
        {
            NavigationStatus status = navigationService.LastStatus;
            return status == NavigationStatus.Succeeded ? NavigationStatus.Failed : status;
        }

        private static bool QuitsOnBackAtRoot
        {
            get
            {
                ScreenSystemSettings settings = ScreenSystemSettings.Instance;
                return settings == null || settings.QuitOnBackAtRoot;
            }
        }

        /// <summary>Closes the open alert. NothingToClose if there's none; Transitioning during another call.</summary>
        public async Task<NavigationResult> DismissAlert(CancellationToken ct = default)
        {
            if (!IsReady(nameof(DismissAlert)))
                return new NavigationResult(NavigationStatus.NotReady);

            return Result(await navigationService.DismissAlertAsync(ct), null);
        }

        /// <summary>True while an alert is open. Navigation is blocked until it's dismissed.</summary>
        public bool IsAlertActive
            => navigationService?.IsAlertActive ?? false;

        /// <summary>True while a navigation call is running. Calls made meanwhile are ignored and return false.</summary>
        [UnityEngine.Scripting.Preserve]
        public bool IsTransitioning
            => navigationService?.IsNavigating ?? false;

        /// <summary>True when there is a screen or modal to go back from.</summary>
        public bool CanGoBack
            => navigationService?.CanGoBack ?? false;

        /// <summary>Gets the current screen ID.</summary>
        public string GetCurrentScreen()
            => navigationService?.GetCurrentScreen();

        /// <summary>Gets the root screen ID.</summary>
        public string GetRootScreen()
            => navigationService?.GetRootScreen();

        /// <summary>Gets the ID of the topmost visible screen.</summary>
        public string GetTopScreen()
            => displayService?.GetTopScreen();

        /// <summary>Gets the IDs of the active screens.</summary>
        public List<string> GetActiveScreenIds()
        {
            return displayService?.GetActiveScreenIds() ?? new List<string>();
        }

        /// <summary>Gets the layers of a screen.</summary>
        public List<LayerComponent> GetScreenLayers(string screenId)
        {
            if (idResolver == null || !idResolver.TryResolveScreenId(screenId, nameof(GetScreenLayers), out string resolvedId))
                return new List<LayerComponent>();

            return displayService?.GetScreenLayers(resolvedId) ?? new List<LayerComponent>();
        }

        /// <summary>Clears the navigation history. Not needed for normal scene loads, because single scene loads clean up automatically.</summary>
        public void PrepareForSceneChange()
        {
            navigationService?.ClearNavigationHistoryOnSceneChange();
        }

#if UNITY_EDITOR
        public ScreenRegistry GetRegistryForEditor()
            => serviceInitializer?.Registry;

        public ScreenCacheManager GetCacheManagerForEditor()
            => serviceInitializer?.DisplayService?.GetCacheManager();

        public List<string> GetNavigationHistoryForEditor()
            => serviceInitializer?.NavigationService?.GetHistoryForEditor() ?? new List<string>();

        public List<string> GetModalHistoryForEditor()
            => serviceInitializer?.NavigationService?.GetModalHistoryForEditor() ?? new List<string>();

        public string GetAlertForEditor()
        {
            List<string> alerts = serviceInitializer?.NavigationService?.GetAlertHistoryForEditor();
            return alerts != null && alerts.Count > 0 ? alerts[0] : null;
        }

        public string GetRunningOperationForEditor()
            => serviceInitializer?.NavigationService?.CurrentOperationForEditor;

        public bool IsSnapshotBackdropVisibleForEditor
            => serviceInitializer?.DisplayService?.GetBlurServiceForEditor()?.IsSnapshotVisible ?? false;

        public bool IsDimBackdropVisibleForEditor
            => serviceInitializer?.DisplayService?.GetBlurServiceForEditor()?.IsDimVisible ?? false;

        public bool QuitsOnBackAtRootForEditor
            => QuitsOnBackAtRoot;

        /// <summary>The quit confirmation modal's name, when the registry has it; otherwise null and back at the root quits.</summary>
        public string GetQuitConfirmationForEditor()
            => !string.IsNullOrEmpty(quitConfirmationModalId) && serviceInitializer?.Registry?.GetScreen(quitConfirmationModalId) != null
                ? quitConfirmationModalId
                : null;
#endif

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            initializationCts?.Cancel();
            initializationCts?.Dispose();
            serviceInitializer?.SceneLifecycleManager?.Dispose();
            serviceInitializer?.Cleanup();

            // The screens go with this object, so release every Addressables handle they held.
            if (instance == this)
            {
                LayerPrefabLoader.ReleaseAllLoadedPrefabs();
                LayerPrefabLoader.ReleaseAllBundles();
            }
        }
    }
}

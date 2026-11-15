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

        // With Enter Play Mode Options and domain reload off, statics survive between Play sessions. Start each one clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
            => instance = null;

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

        /// <summary>The active manager. It is null until Start() has run, so wait for it before navigating.</summary>
        public static ScreenManager Instance
            => instance;

        /// <summary>True once the manager is ready to navigate.</summary>
        public bool IsInitialized
            => isInitialized;

        /// <summary>Waits until the manager is ready to navigate.</summary>
        public Task<bool> WaitForInitializationAsync()
            => initializationCompletionSource.Task;

        private async void Start()
        {
            try
            {
                if (instance != null && instance != this)
                {
                    Destroy(gameObject);
                    return;
                }

                instance = this;


                await InitializeScreenSystemAsync();
            }
            catch (Exception)
            {
                throw;
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
            }
            catch (Exception ex)
            {
                isInitialized = false;
                initializationError = $"Exception: {ex.Message}";

                if (Debug.isDebugBuild)
                    Debug.LogError($"[ScreenManager] Initialization exception: {ex.Message}\n{ex.StackTrace}");

                initializationCompletionSource.TrySetResult(false);
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

            string reason = initializationError != null ? $"initialization failed: {initializationError}" : "it isn't initialized yet. Await WaitForInitializationAsync() first";
            Debug.LogError($"[ScreenManager] {call} ignored: {reason}.");
            return false;
        }

        /// <summary>Opens a screen by name or ID. Returns false if the call is ignored or blocked.</summary>
        public async Task<bool> GoTo(string screenId, CancellationToken ct = default)
        {
            if (!IsReady(nameof(GoTo)))
                return false;

            if (!idResolver.TryResolveScreenId(screenId, nameof(GoTo), out string resolvedId))
            {
                Debug.LogError($"[ScreenManager] TryResolveScreenId FAILED for: {screenId}");
                return false;
            }

            bool result = await navigationService.GoToAsync(resolvedId, ct);
            return result;
        }

        /// <summary>Closes the open modal or returns to the previous screen. At the root, starts the quit flow.</summary>
        public async Task<bool> GoBack(CancellationToken ct = default)
        {
            if (!IsReady(nameof(GoBack)))
                return false;

            // A call ignored during a transition also returns false at the root. That must not start the quit flow.
            if (navigationService.IsNavigating)
            {
                Debug.LogWarning("[ScreenManager] GoBack ignored: a navigation call is still running.");
                return false;
            }

            bool result = await navigationService.GoBackAsync(ct);

            if (!result && !navigationService.CanGoBack)
                await serviceInitializer?.QuitManager?.HandleQuitRequestAsync(ct);

            return result;
        }

        /// <summary>Returns to a screen that is already in the stack.</summary>
        public async Task<bool> PopTo(string screenId, CancellationToken ct = default)
        {
            if (!IsReady(nameof(PopTo)))
                return false;

            if (!idResolver.TryResolveScreenId(screenId, nameof(PopTo), out string resolvedId))
                return false;

            return await navigationService.PopToAsync(resolvedId, ct);
        }

        /// <summary>Replaces the root screen.</summary>
        public async Task<bool> SetRoot(string screenId, CancellationToken ct = default)
        {
            if (!IsReady(nameof(SetRoot)))
                return false;

            if (!idResolver.TryResolveScreenId(screenId, nameof(SetRoot), out string resolvedId))
            {
                Debug.LogError($"[ScreenManager] TryResolveScreenId FAILED for SetRoot: {screenId}");
                return false;
            }

            return await navigationService.SetRootAsync(resolvedId, ct);
        }

        /// <summary>Closes the open alert. Returns false if there's none, or during a transition.</summary>
        public async Task<bool> DismissAlert(CancellationToken ct = default)
        {
            if (!IsReady(nameof(DismissAlert)))
                return false;

            return await navigationService.DismissAlertAsync(ct);
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

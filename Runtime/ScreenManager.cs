using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tools.ScreenSystem.Scripts
{
    [RequireComponent(typeof(ScreenSystemBootstrap))]
    public class ScreenManager : MonoBehaviour, IScreenNavigationService
    {
        private static ScreenManager instance;

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

        public static ScreenManager Instance
            => instance;

        public bool IsInitialized
            => isInitialized;

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

            inputHandler?.CheckForInput(CancellationToken.None);
        }


        private void EnsureInitialized()
        {
            if (!isInitialized)
                throw new InvalidOperationException($"[ScreenManager] Initialization failed: {initializationError ?? "Unknown error"}");

            if (navigationService == null)
                throw new InvalidOperationException("[ScreenManager] navigationService is null - initialization incomplete");
        }

        public async Task<bool> GoTo(string screenId, CancellationToken ct = default)
        {
            if (!idResolver.TryResolveScreenId(screenId, nameof(GoTo), out string resolvedId))
            {
                Debug.LogError($"[ScreenManager] TryResolveScreenId FAILED for: {screenId}");
                return false;
            }

            EnsureInitialized();

            bool result = await navigationService.GoToAsync(resolvedId, ct);
            return result;
        }

        public async Task<bool> GoBack(CancellationToken ct = default)
        {
            EnsureInitialized();
            bool result = await navigationService.GoBackAsync(ct);

            if (!result && navigationService != null && !navigationService.CanGoBack)
                await serviceInitializer?.QuitManager?.HandleQuitRequestAsync(ct);

            return result;
        }

        public async Task<bool> PopTo(string screenId, CancellationToken ct = default)
        {
            if (!idResolver.TryResolveScreenId(screenId, nameof(PopTo), out string resolvedId))
                return false;

            EnsureInitialized();
            return await navigationService.PopToAsync(resolvedId, ct);
        }

        public async Task<bool> SetRoot(string screenId, CancellationToken ct = default)
        {
            if (!idResolver.TryResolveScreenId(screenId, nameof(SetRoot), out string resolvedId))
            {
                Debug.LogError($"[ScreenManager] TryResolveScreenId FAILED for SetRoot: {screenId}");
                return false;
            }

            EnsureInitialized();
            return await navigationService.SetRootAsync(resolvedId, ct);
        }

        public bool CanGoBack
            => navigationService?.CanGoBack ?? false;

        public string GetCurrentScreen()
            => navigationService?.GetCurrentScreen();

        public string GetRootScreen()
            => navigationService?.GetRootScreen();

        public string GetTopScreen()
            => displayService?.GetTopScreen();

        public List<string> GetActiveScreenIds()
        {
            return displayService?.GetActiveScreenIds() ?? new List<string>();
        }

        public List<LayerComponent> GetScreenLayers(string screenId)
        {
            if (!idResolver.TryResolveScreenId(screenId, nameof(GetScreenLayers), out string resolvedId))
                return new List<LayerComponent>();

            return displayService?.GetScreenLayers(resolvedId) ?? new List<LayerComponent>();
        }

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
            serviceInitializer?.Cleanup();
        }
    }
}

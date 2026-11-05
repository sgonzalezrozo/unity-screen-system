using System;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem.Services
{
    public class ScreenInitializer : IScreenInitializer
    {
        private const string SETUP_REQUIRED_MESSAGE =
            "╔════════════════════════════════════════════════════════════╗\n" +
            "║  SCREEN SYSTEM NOT SET UP                                  ║\n" +
            "╠════════════════════════════════════════════════════════════╣\n" +
            "║  ScreenManager cannot initialize!                          ║\n" +
            "║                                                            ║\n" +
            "║  REQUIRED ACTION:                                          ║\n" +
            "║     In Unity Editor, click:                                ║\n" +
            "║     Tools → Screen System → Setup Project                  ║\n" +
            "║                                                            ║\n" +
            "║  This is a ONE-TIME setup (30 seconds)                     ║\n" +
            "╚════════════════════════════════════════════════════════════╝";

        private readonly Transform screenSystemRoot;
        private ScreenRegistry screenRegistry;
        private ScreenDisplayService displayService;
        private bool initialized;
        private string initializationError;

        public bool IsInitialized => initialized;
        public string InitializationError => initializationError;

        public ScreenInitializer(Transform screenSystemRoot)
        {
            this.screenSystemRoot = screenSystemRoot ?? throw new ArgumentNullException(nameof(screenSystemRoot));
        }

        public async Task<bool> InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                if (!ScreenSystemSettings.IsValid())
                {
                    Debug.LogError(SETUP_REQUIRED_MESSAGE);
                    initializationError = "ScreenSystemSettings not valid. Run setup.";
                    return false;
                }

                ScreenSystemSettings settings = ScreenSystemSettings.Instance;
                screenRegistry = settings.ScreenRegistry;

                if (screenRegistry == null)
                {
                    Debug.LogError("[ScreenInitializer] ScreenRegistry not found in ScreenSystemSettings! Run setup.");
                    initializationError = "ScreenRegistry not found.";
                    return false;
                }

                screenRegistry.Initialize();
                await Task.Yield();

                ComponentDiscovery componentDiscovery = new(screenSystemRoot);

                Transform screenContainer = componentDiscovery.DiscoverOrCreateScreenContainer();
                await Task.Yield();

                BlurBackgroundLayer blurBackground = DiscoverBlurBackground();
                await Task.Yield();

                displayService = new ScreenDisplayService(screenRegistry, screenContainer, blurBackground);

                initialized = true;

                return true;
            }
            catch (OperationCanceledException)
            {
                initializationError = "Initialization cancelled";
                Debug.LogWarning("[ScreenInitializer] Initialization cancelled");
                return false;
            }
            catch (Exception ex)
            {
                initialized = false;
                initializationError = ex.Message;
                Debug.LogError($"[ScreenInitializer] Fatal error: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public ScreenRegistry GetRegistry()
            => screenRegistry;

        public ScreenDisplayService GetDisplayService()
            => displayService;

        private BlurBackgroundLayer DiscoverBlurBackground()
        {
            Transform blurTransform = screenSystemRoot.Find("BlurBackground");
            if (blurTransform == null)
                return null;

            BlurBackgroundLayer blurLayer = blurTransform.GetComponent<BlurBackgroundLayer>();
            if (blurLayer != null)
            {
                if (!blurTransform.gameObject.activeSelf)
                    return blurLayer;

                Debug.LogWarning("[ScreenInitializer] BlurBackground was active on startup! Setting inactive now. Update prefab to start inactive.");
                blurTransform.gameObject.SetActive(false);

                return blurLayer;
            }

            Debug.LogWarning("[ScreenInitializer] BlurBackground found but missing BlurBackgroundLayer component. Blur effect will be disabled.");
            return null;
        }
    }
}

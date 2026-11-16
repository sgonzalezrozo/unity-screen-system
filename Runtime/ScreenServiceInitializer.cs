using System;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using ScreenSystem.Services;
using UnityEngine;

namespace ScreenSystem
{
    internal class ScreenServiceInitializer
    {
        private readonly Transform managerTransform;
        private IScreenInitializer screenInitializer;

        public bool IsInitialized { get; private set; }
        public string InitializationError { get; private set; }

        public ScreenRegistry Registry { get; private set; }
        public ScreenDisplayService DisplayService { get; private set; }
        public ScreenNavigationService NavigationService { get; private set; }
        public SceneLifecycleManager SceneLifecycleManager { get; private set; }
        public BackButtonNavigationHandler BackButtonHandler { get; private set; }
        public QuitManager QuitManager { get; private set; }
        public LayerFactory LayerFactory { get; private set; }

        public ScreenServiceInitializer(Transform managerTransform)
        {
            this.managerTransform = managerTransform;
        }

        public async Task<bool> InitializeAsync(
            CancellationToken ct,
            string quitConfirmationModalId)
        {
            try
            {
                screenInitializer = new ScreenInitializer(managerTransform);
                bool initSuccess = await screenInitializer.InitializeAsync(ct);

                if (!initSuccess)
                {
                    IsInitialized = false;
                    InitializationError = screenInitializer.InitializationError;
                    return false;
                }

                Registry = screenInitializer.GetRegistry();
                DisplayService = screenInitializer.GetDisplayService();
                LayerFactory = DisplayService.GetLayerFactory();

                NavigationService = new ScreenNavigationService(Registry, DisplayService);
                SceneLifecycleManager = new SceneLifecycleManager(DisplayService, NavigationService, Registry);

                BackButtonHandler = new BackButtonNavigationHandler(NavigationService, DisplayService, Registry);
                QuitManager = new QuitManager(NavigationService, quitConfirmationModalId);

                IsInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                IsInitialized = false;
                InitializationError = ex.Message;
                Debug.LogError($"[ScreenServiceInitializer] Initialization failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public void Cleanup()
        {
            // Cleanup handled by QuitManager lifecycle
        }
    }
}


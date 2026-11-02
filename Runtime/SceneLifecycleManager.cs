using System.Collections.Generic;
using ScreenSystem.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScreenSystem
{
    public class SceneLifecycleManager
    {
        private readonly ScreenDisplayService displayService;
        private readonly ScreenNavigationService navigationService;
        private readonly ScreenRegistry screenRegistry;

        public SceneLifecycleManager(ScreenDisplayService displayService, ScreenNavigationService navigationService, ScreenRegistry screenRegistry)
        {
            this.displayService = displayService;
            this.navigationService = navigationService;
            this.screenRegistry = screenRegistry;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
                return;

            OnSceneChange();
        }

        public void OnDestroy()
        {
            displayService.RemoveNonPersistentScreens();
            displayService.ClearActiveScreens();
        }

        private void OnSceneChange()
        {
            navigationService.ClearNavigationHistoryOnSceneChange();

            displayService.HidePersistentScreenLayers();
            displayService.RemoveNonPersistentScreens();
            displayService.ClearActiveScreens();

            // Clear cached resources to prevent unbounded memory growth
            displayService.ClearAllCachedResources();

            ShowSplashAfterSceneChange();
            Resources.UnloadUnusedAssets();
        }

        private void ShowSplashAfterSceneChange()
        {
            Dictionary<string, List<LayerComponent>> cachedScreens = displayService.GetCachedScreens();

            foreach (KeyValuePair<string, List<LayerComponent>> kvp in cachedScreens)
            {
                string screenId = kvp.Key;
                ScreenDefinition screen = screenRegistry.GetScreen(screenId);

                bool isPersistent = IsScreenPersistent(screen);

                if (!isPersistent)
                    continue;

                List<LayerComponent> layers = kvp.Value;
                foreach (LayerComponent layer in layers)
                {
                    if (layer == null)
                        continue;

                    Canvas canvas = layer.GetComponent<Canvas>();
                    if (canvas != null)
                        canvas.enabled = true;

                    if (layer.GraphicRaycaster != null)
                        layer.GraphicRaycaster.enabled = true;
                }
            }

            LayerFactory layerFactory = displayService.GetLayerFactory();
            if (layerFactory == null)
                return;

            LayerInitializer layerInitializer = layerFactory.GetLayerInitializer();
            if (layerInitializer == null)
                return;

            Dictionary<string, GameObject> allLayerObjects = layerInitializer.GetAllCachedLayers();
            if (allLayerObjects == null)
                return;

            foreach (KeyValuePair<string, GameObject> kvp in allLayerObjects)
            {
                GameObject layerObj = kvp.Value;

                // Skip if layer GameObject was destroyed
                if (layerObj == null)
                    continue;

                // Get LayerComponent from the GameObject
                LayerComponent layer = layerObj.GetComponent<LayerComponent>();
                if (layer == null)
                    continue;

                // Check if this layer belongs to a persistent screen
                // Shared layers maintain their CurrentScreenId (NOT empty after fix)
                bool isPersistent = false;

                if (!string.IsNullOrEmpty(layer.CurrentScreenId))
                {
                    ScreenDefinition layerScreen = screenRegistry.GetScreen(layer.CurrentScreenId);
                    isPersistent = IsScreenPersistent(layerScreen);
                }

                if (!isPersistent)
                    continue;

                Canvas canvas = layer.GetComponent<Canvas>();
                if (canvas == null)
                    continue;

                bool wasDisabled = !canvas.enabled;

                if (!wasDisabled)
                    continue;

                canvas.enabled = true;

                if (layer.GraphicRaycaster != null)
                    layer.GraphicRaycaster.enabled = true;
            }
        }

        private bool IsScreenPersistent(ScreenDefinition screen)
        {
            return screen != null && screen.PersistOverScenes;
        }
    }
}


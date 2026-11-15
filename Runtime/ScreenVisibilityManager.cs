using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem
{
	public class ScreenVisibilityManager
	{
        private readonly ScreenCacheManager cacheManager;

        public ScreenVisibilityManager(ScreenCacheManager cacheManager = null)
        {
            this.cacheManager = cacheManager;
        }

        public void ActivateLayers(List<LayerComponent> layers, string screenId = null)
        {
            if (layers == null)
                return;

            // First, hide all layers from OTHER screens that aren't being preserved
            if (!string.IsNullOrEmpty(screenId) && cacheManager != null)
            {
                Dictionary<string, List<LayerComponent>> allCachedScreens = cacheManager.GetAllCachedScreens();
                foreach (KeyValuePair<string, List<LayerComponent>> screenCache in allCachedScreens)
                {
                    if (screenCache.Key == screenId)
                        continue;

                    List<LayerComponent> otherScreenLayers = screenCache.Value;
                    if (otherScreenLayers == null)
                        continue;

                    foreach (LayerComponent layer in otherScreenLayers)
                    {
                        if (layer == null)
                            continue;

                        // If this layer is NOT in the current screen's layers, it might need to be hidden
                        if (!layers.Contains(layer) && layer.CurrentScreenId != screenId)
                        {
                            // Only hide if not assigned to another active screen
                            bool isAssignedToActiveScreen = false;
                            IReadOnlyList<string> activeScreenIds = cacheManager.GetActiveScreens();
                            for (int i = 0; i < activeScreenIds.Count; i++)
                            {
                                if (activeScreenIds[i] == screenId)
                                    continue;

                                if (layer.CurrentScreenId == activeScreenIds[i])
                                {
                                    isAssignedToActiveScreen = true;
                                    break;
                                }
                            }

                            if (!isAssignedToActiveScreen && layer.IsCanvasEnabled)
                            {
                                Canvas canvas = layer.GetComponent<Canvas>();
                                if (canvas != null)
                                    canvas.enabled = false;
                                if (layer.GraphicRaycaster != null)
                                    layer.GraphicRaycaster.enabled = false;
                            }
                        }
                    }
                }
            }

            // Now activate the current screen's layers
            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                // Show all layers in the current screen's layers list
                // For shared layers (e.g., TopBar), CurrentScreenId might not match during reassignment,
                // but if they're in this layers list, they should be shown for this screen
                Canvas canvas = layer.GetComponent<Canvas>();
                bool isAlreadyVisible = IsLayerCurrentlyVisibleOnScreen(layer);

                if (isAlreadyVisible)
                    continue;

                if (canvas != null)
                    canvas.enabled = true;

                if (layer.GraphicRaycaster != null)
                    layer.GraphicRaycaster.enabled = true;

                layer.Show();
            }
        }

        // A layer already visible, such as a top bar shared with the previous screen, isn't shown again, so its
        // OnScreenShown doesn't run again. IsVisible covers uGUI and UI Toolkit layers.
        private bool IsLayerCurrentlyVisibleOnScreen(LayerComponent layer)
        {
            if (layer == null)
                return false;

            return layer.gameObject.activeInHierarchy && layer.IsVisible;
        }
    }
}

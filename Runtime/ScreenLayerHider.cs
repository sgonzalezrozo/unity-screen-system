using System.Collections.Generic;

namespace ScreenSystem
{
    internal class ScreenLayerHider
    {
        private readonly ScreenCacheManager cacheManager;

        public ScreenLayerHider(ScreenCacheManager cacheManager = null)
        {
            this.cacheManager = cacheManager;
        }

        public void HideLayersInstantly(List<LayerComponent> layers, string screenId, List<LayerComponent> preserveLayers = null)
        {
            if (layers == null)
                return;

            NavigationEvents.NotifyTransitionOutStarted(screenId);

            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                if (preserveLayers != null && preserveLayers.Contains(layer))
                    continue;

                layer.UnassignFromScreen(screenId);

                // Check if layer is still assigned to ANY active screen
                bool isAssignedToActiveScreen = false;
                if (cacheManager != null)
                {
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
                }

                // If still assigned to an active screen via SharedLayerManager, don't hide yet
                if (isAssignedToActiveScreen)
                    continue;

                // No active screen assignment, safe to hide
                if (layer.CurrentScreenId != null && cacheManager != null)
                {
                    // If CurrentScreenId is not null but not in active screens, also hide
                    IReadOnlyList<string> activeScreens = cacheManager.GetActiveScreens();
                    bool foundInActive = false;
                    for (int i = 0; i < activeScreens.Count; i++)
                    {
                        if (activeScreens[i] == layer.CurrentScreenId)
                        {
                            foundInActive = true;
                            break;
                        }
                    }
                    if (!foundInActive)
                        layer.Hide();
                }
                else
                {
                    layer.Hide();
                }
            }

            NavigationEvents.NotifyTransitionOutCompleted(screenId);
        }

        public void DisableRaycasters(List<LayerComponent> layers)
        {
            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                if (layer.GraphicRaycaster == null)
                    continue;

                if (layer.CurrentScreenId != null)
                    continue;

                layer.GraphicRaycaster.enabled = false;
            }
        }
    }
}

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;

namespace ScreenSystem
{
    public class ScreenRelationshipHider
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly ScreenCacheManager cacheManager;
        private readonly ScreenLayerHider layerHider;
        private readonly ScreenHideService hideService;

        public ScreenRelationshipHider(
            ScreenRegistry screenRegistry,
            ScreenCacheManager cacheManager,
            ScreenLayerHider layerHider,
            ScreenHideService hideService = null)
        {
            this.screenRegistry = screenRegistry;
            this.cacheManager = cacheManager;
            this.layerHider = layerHider;
            this.hideService = hideService;
        }

        public void HideOtherScreensForType(string currentScreenId, ScreenType screenType)
        {
            if (screenType == ScreenType.fullscreen)
                HideAllScreensExcept(currentScreenId);
            else if (screenType == ScreenType.modal)
                HideOtherModalsExcept(currentScreenId);
        }

        public async Task HideOtherScreensAsync(string currentScreenId, ScreenType screenType, CancellationToken ct, List<LayerComponent> preserveLayers = null)
        {
            if (screenType == ScreenType.fullscreen)
                await HideAllOtherScreens(currentScreenId, preserveLayers);
            else if (screenType == ScreenType.modal)
                await HideOtherModals(currentScreenId, ct, preserveLayers);
        }

        private void HideAllScreensExcept(string currentScreenId)
        {
            IReadOnlyList<string> activeScreens = cacheManager.GetActiveScreens();

            for (int i = 0; i < activeScreens.Count; i++)
            {
                string activeScreenId = activeScreens[i];
                if (activeScreenId == currentScreenId)
                    continue;

                if (!cacheManager.TryGetCachedLayers(activeScreenId, out List<LayerComponent> oldLayers))
                    continue;

                layerHider.HideLayersInstantly(oldLayers, activeScreenId);
            }
        }

        private void HideOtherModalsExcept(string currentScreenId)
        {
            IReadOnlyList<string> activeScreens = cacheManager.GetActiveScreens();

            for (int i = 0; i < activeScreens.Count; i++)
            {
                string activeScreenId = activeScreens[i];
                if (activeScreenId == currentScreenId)
                    continue;

                if (!cacheManager.TryGetCachedLayers(activeScreenId, out List<LayerComponent> oldLayers))
                    continue;

                ScreenDefinition activeScreen = screenRegistry.GetScreen(activeScreenId);

                if (activeScreen is not { screenType: ScreenType.modal })
                    continue;

                cacheManager.UnregisterActiveScreen(activeScreenId);
                layerHider.HideLayersInstantly(oldLayers, activeScreenId);
            }
        }

        private Task HideAllOtherScreens(string currentScreenId, List<LayerComponent> preserveLayers = null)
        {
            using Unity.Profiling.ProfilerMarker.AutoScope _ = ScreenSystemMarkers.HideLayers.Auto();
            IReadOnlyList<string> activeScreens = cacheManager.GetActiveScreens();

            for (int i = 0; i < activeScreens.Count; i++)
            {
                string activeScreenId = activeScreens[i];
                if (activeScreenId == currentScreenId)
                    continue;

                if (!cacheManager.IsScreenCached(activeScreenId))
                    continue;

                if (!cacheManager.TryGetCachedLayers(activeScreenId, out List<LayerComponent> layers))
                    continue;

                layerHider.HideLayersInstantly(layers, activeScreenId, preserveLayers);
            }

            return Task.CompletedTask;
        }

        private async Task HideOtherModals(string currentScreenId, CancellationToken ct, List<LayerComponent> preserveLayers = null)
        {
            IReadOnlyList<string> activeScreens = cacheManager.GetActiveScreens();

            for (int i = 0; i < activeScreens.Count; i++)
            {
                string activeScreenId = activeScreens[i];
                if (activeScreenId == currentScreenId || !cacheManager.IsScreenCached(activeScreenId))
                    continue;

                ScreenDefinition activeScreen = screenRegistry.GetScreen(activeScreenId);

                if (activeScreen == null || activeScreen.screenType != ScreenType.modal)
                    continue;

                if (hideService != null)
                    await hideService.HideScreenAsync(activeScreenId, null, ct);
            }
        }
    }
}


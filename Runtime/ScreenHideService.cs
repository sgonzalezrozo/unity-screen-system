using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;

namespace ScreenSystem
{
    internal class ScreenHideService : IScreenHideService
    {
        private readonly ScreenHider screenHider;
        private readonly ScreenLayerHider layerHider;
        private readonly ScreenRelationshipHider relationshipHider;

        public ScreenHideService(
            ScreenRegistry screenRegistry,
            ScreenCacheManager cacheManager,
            BlurBackgroundService blurService = null)
        {
            layerHider = new ScreenLayerHider(cacheManager);
            screenHider = new ScreenHider(screenRegistry, cacheManager, blurService, layerHider);
            relationshipHider = new ScreenRelationshipHider(screenRegistry, cacheManager, layerHider, this);
        }

        public async Task HideScreenAsync(string screenId, string preserveLayersForScreenId = null, CancellationToken ct = default)
        {
            if (ct.IsCancellationRequested)
                ct.ThrowIfCancellationRequested();

            await screenHider.HideAsync(screenId, preserveLayersForScreenId, ct);
        }

        public void HideOtherScreensForType(string currentScreenId, ScreenType screenType)
        {
            relationshipHider.HideOtherScreensForType(currentScreenId, screenType);
        }

        public async Task HideOtherScreensAsync(string currentScreenId, ScreenType screenType, CancellationToken ct, List<LayerComponent> preserveLayers = null)
        {
            await relationshipHider.HideOtherScreensAsync(currentScreenId, screenType, ct, preserveLayers);
        }
    }
}

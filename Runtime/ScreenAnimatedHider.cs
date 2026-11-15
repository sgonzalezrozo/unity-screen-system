using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenAnimatedHider
    {
        private const string LOG_PREFIX = "[ScreenHide]";

        private readonly ScreenRegistry screenRegistry;
        private readonly ScreenCacheManager cacheManager;
        private readonly BlurBackgroundService blurService;
        private readonly ScreenLayerHider layerHider;

        public ScreenAnimatedHider(
            ScreenRegistry screenRegistry,
            ScreenCacheManager cacheManager,
            BlurBackgroundService blurService,
            ScreenLayerHider layerHider)
        {
            this.screenRegistry = screenRegistry;
            this.cacheManager = cacheManager;
            this.blurService = blurService;
            this.layerHider = layerHider;
        }

        public async Task HideScreenAsyncInternal(string screenId, string preserveLayersForScreenId = null, CancellationToken ct = default)
        {
            try
            {
                if (!cacheManager.TryGetCachedLayers(screenId, out List<LayerComponent> layers))
                    return;

                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();

                ScreenDefinition screen = screenRegistry.GetScreen(screenId);
                if (screen != null)
                {
                    if (screen.screenType == ScreenType.modal && blurService != null)
                        await blurService.HideBlurForModalAsync(screenId, ct);

                    cacheManager.UnregisterActiveScreen(screenId);
                }

                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();

                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();

                List<LayerComponent> preserveLayers = null;
                if (!string.IsNullOrEmpty(preserveLayersForScreenId) && cacheManager.TryGetCachedLayers(preserveLayersForScreenId, out List<LayerComponent> preserveScreenLayers))
                    preserveLayers = preserveScreenLayers;

                using (ScreenSystemMarkers.HideLayers.Auto())
                {
                    layerHider.HideLayersInstantly(layers, screenId, preserveLayers);
                    layerHider.DisableRaycasters(layers);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} Error while hiding screen '{screenId}': {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}


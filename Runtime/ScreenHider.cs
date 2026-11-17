using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    /// <summary>Hides a screen's layers at once, except those shared with the screen being shown.</summary>
    internal class ScreenHider
    {
        private const string LOG_PREFIX = "[ScreenHide]";

        private readonly ScreenRegistry screenRegistry;
        private readonly ScreenCacheManager cacheManager;
        private readonly BlurBackgroundService blurService;
        private readonly ScreenLayerHider layerHider;

        public ScreenHider(
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

        public async Task HideAsync(string screenId, string preserveLayersForScreenId = null, CancellationToken ct = default)
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
                    // The screen the overlay's snapshot replaced renders again.
                    if ((screen.ScreenType == ScreenType.Modal || screen.ScreenType == ScreenType.Alert) && blurService != null)
                        blurService.Remove(screenId);

                    cacheManager.UnregisterActiveScreen(screenId);
                }

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


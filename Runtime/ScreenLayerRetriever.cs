using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenLayerRetriever
    {
        private readonly LayerFactory layerFactory;
        private readonly ScreenCacheManager cacheManager;

        public ScreenLayerRetriever(LayerFactory layerFactory, ScreenCacheManager cacheManager)
        {
            this.layerFactory = layerFactory;
            this.cacheManager = cacheManager;
        }

        public async Task<List<LayerComponent>> GetOrCreateLayersAsync(
            string screenId,
            ScreenDefinition screen,
            string logId,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            bool isCached = cacheManager.TryGetCachedLayers(screenId, out List<LayerComponent> existingLayers);

            if (isCached && existingLayers != null && existingLayers.Count > 0)
            {
                ct.ThrowIfCancellationRequested();

                layerFactory.ReassignLayersToScreen(existingLayers, screenId, screen, logId);
                return existingLayers;
            }

            ct.ThrowIfCancellationRequested();

            List<LayerComponent> layers = await layerFactory.GetOrInstantiateLayers(screenId, screen, logId, ct);
            ct.ThrowIfCancellationRequested();

            if (layers == null || layers.Count == 0)
            {
                Debug.Log($"[ScreenLayerRetriever] Failed to create layers for screen: {logId}");
                return null;
            }

            cacheManager.CacheLayers(screenId, layers);
            return layers;
        }

        public void RegisterActiveScreen(string screenId)
        {
            cacheManager.RegisterActiveScreen(screenId);
        }
    }
}


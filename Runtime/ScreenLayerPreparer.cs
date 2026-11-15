using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenLayerPreparer
    {
        private readonly ScreenCacheManager cacheManager;

        public ScreenLayerPreparer(ScreenCacheManager cacheManager = null)
        {
            this.cacheManager = cacheManager;
        }

        public async Task PrepareLayersForDisplayAsync(List<LayerComponent> layers, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using (ScreenSystemMarkers.PrepareLayers.Auto())
                PrepareCanvasState(layers);
            await Task.Yield();

            ct.ThrowIfCancellationRequested();

            foreach (LayerComponent layer in layers)
            {
                if (layer != null)
                    layer.SetNavigationToken(ct);
            }
        }

        public void ActivateLayers(List<LayerComponent> layers, string screenId = null)
        {
            ScreenVisibilityManager visibilityManager = new(cacheManager);
            visibilityManager.ActivateLayers(layers, screenId);
        }

        private void PrepareCanvasState(List<LayerComponent> layers)
        {
            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                Canvas canvas = layer.GetComponent<Canvas>();
                if (canvas == null)
                    continue;

                bool canvasEnabled = canvas.enabled;
                bool gameObjectActive = layer.gameObject.activeInHierarchy;
                bool wasVisibleBefore = canvasEnabled && gameObjectActive;

                if (!wasVisibleBefore && canvas.enabled)
                    canvas.enabled = false;
            }
        }
    }
}


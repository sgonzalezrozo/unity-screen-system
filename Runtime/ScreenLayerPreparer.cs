using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    internal class ScreenLayerPreparer
    {
        private readonly ScreenCacheManager cacheManager;

        public ScreenLayerPreparer(ScreenCacheManager cacheManager = null)
        {
            this.cacheManager = cacheManager;
        }

        // Synchronous: a yield here made every show that ran after an await (boot, chained navigation) wait a whole
        // frame before its layers could activate.
        public Task PrepareLayersForDisplayAsync(List<LayerComponent> layers, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            using (ScreenSystemMarkers.PrepareLayers.Auto())
                PrepareCanvasState(layers);
            StartupTimeline.MarkOnce("show.first-prepare");

            foreach (LayerComponent layer in layers)
            {
                if (layer != null)
                    layer.SetNavigationToken(ct);
            }

            return Task.CompletedTask;
        }

        public void ActivateLayers(List<LayerComponent> layers, string screenId = null)
        {
            ScreenVisibilityManager visibilityManager = new(cacheManager);
            visibilityManager.ActivateLayers(layers, screenId);
            StartupTimeline.MarkOnce("show.first-activate");
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


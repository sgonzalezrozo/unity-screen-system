using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
    /// <summary>
    /// Puts the backdrop behind modals and alerts, so at most three windows render:
    /// - A modal, or an alert with no modal under it: a blurred snapshot of the screen replaces it, and the screen's
    ///   layers stop rendering until the overlay closes. Layers the overlay shares (a navigation bar) keep rendering.
    /// - An alert over a modal: a flat dim between them. The modal stays visible.
    /// Without a backdrop in the ScreenSystem prefab, overlays open over the screen as before.
    /// </summary>
    public class BlurBackgroundService
    {
        private sealed class Overlay
        {
            public bool IsDim;
            public bool HasSnapshot;
            public readonly List<LayerComponent> Suspended = new();
        }

        private readonly BlurBackgroundLayer backdrop;
        private readonly ScreenCacheManager cacheManager;
        private readonly Dictionary<string, Overlay> overlays = new();
        private readonly HashSet<LayerComponent> overlayLayerSet = new();

        public BlurBackgroundService(BlurBackgroundLayer backdrop, ScreenCacheManager cacheManager = null)
        {
            this.backdrop = backdrop;
            this.cacheManager = cacheManager;
        }

        public bool HasBackdrop
            => backdrop != null;

        /// <summary>True while the blurred snapshot replaces the screen behind a modal or alert.</summary>
        public bool IsSnapshotVisible
            => backdrop != null && backdrop.IsSnapshotVisible;

        /// <summary>True while the dim sits between a modal and the alert over it.</summary>
        public bool IsDimVisible
            => backdrop != null && backdrop.IsDimVisible;

        /// <summary>Number of open overlays (modals and alerts) with a backdrop.</summary>
        public int OverlayCount
            => overlays.Count;

        /// <summary>Layers currently suspended behind a snapshot. Read by tests and the benchmark.</summary>
        public int SuspendedLayerCount
        {
            get
            {
                int count = 0;
                foreach (Overlay overlay in overlays.Values)
                    count += overlay.Suspended.Count;
                return count;
            }
        }

        private bool HasSnapshotOverlay
        {
            get
            {
                foreach (Overlay overlay in overlays.Values)
                {
                    if (overlay.HasSnapshot)
                        return true;
                }

                return false;
            }
        }

        private bool HasModalOverlay(string excludeScreenId)
        {
            foreach (KeyValuePair<string, Overlay> pair in overlays)
            {
                if (pair.Key != excludeScreenId && !pair.Value.IsDim)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Called before an overlay's layers become visible. Captures the screen when the overlay will get a snapshot,
        /// so the capture holds only what's underneath.
        /// </summary>
        public async Task PrepareAsync(string screenId, ScreenType type, CancellationToken ct)
        {
            if (backdrop == null || overlays.ContainsKey(screenId))
                return;

            if (type == ScreenType.alert && HasModalOverlay(screenId))
                return;

            await backdrop.CaptureAsync(ct);
        }

        /// <summary>Called once the overlay's layers are visible. Places the backdrop just below them.</summary>
        public void Apply(string screenId, ScreenType type, List<LayerComponent> overlayLayers)
        {
            if (backdrop == null || overlays.ContainsKey(screenId))
                return;

            int sortingOrder = LowestZOrder(overlayLayers) - 1;
            Overlay overlay = new();
            overlays[screenId] = overlay;

            if (type == ScreenType.alert && HasModalOverlay(screenId))
            {
                overlay.IsDim = true;
                backdrop.ShowDim(sortingOrder);
                return;
            }

            backdrop.ShowSnapshot(sortingOrder);
            if (!backdrop.IsSnapshotVisible)
                return;

            overlay.HasSnapshot = true;
            SuspendLayersUnder(overlayLayers, overlay);
        }

        /// <summary>Called when an overlay hides. Brings back what its snapshot replaced.</summary>
        public void Remove(string screenId)
        {
            if (backdrop == null || !overlays.TryGetValue(screenId, out Overlay overlay))
                return;

            overlays.Remove(screenId);

            if (overlay.IsDim)
            {
                backdrop.HideDim();
                return;
            }

            foreach (LayerComponent layer in overlay.Suspended)
            {
                if (layer != null)
                    layer.ResumeRendering();
            }

            if (!HasSnapshotOverlay)
                backdrop.HideSnapshot();
        }

        /// <summary>Called on scene changes. Resumes every suspended layer that still exists and hides the backdrop.</summary>
        public void Cleanup()
        {
            foreach (Overlay overlay in overlays.Values)
            {
                foreach (LayerComponent layer in overlay.Suspended)
                {
                    if (layer != null)
                        layer.ResumeRendering();
                }
            }

            overlays.Clear();
            if (backdrop != null)
            {
                backdrop.HideSnapshot();
                backdrop.HideDim();
            }
        }

        private void SuspendLayersUnder(List<LayerComponent> overlayLayers, Overlay overlay)
        {
            if (cacheManager == null)
                return;

            // Reused set and a non-copying view of the cache, so opening a modal allocates as little as possible.
            overlayLayerSet.Clear();
            if (overlayLayers != null)
            {
                foreach (LayerComponent layer in overlayLayers)
                    overlayLayerSet.Add(layer);
            }

            foreach (List<LayerComponent> layers in cacheManager.CachedLayerLists)
            {
                if (layers == null)
                    continue;

                for (int i = 0; i < layers.Count; i++)
                {
                    LayerComponent layer = layers[i];
                    if (layer == null || overlayLayerSet.Contains(layer) || !layer.IsVisible || overlay.Suspended.Contains(layer))
                        continue;

                    layer.SuspendRendering();
                    overlay.Suspended.Add(layer);
                }
            }

            overlayLayerSet.Clear();
        }

        private static int LowestZOrder(List<LayerComponent> layers)
        {
            int lowest = int.MaxValue;
            if (layers != null)
            {
                foreach (LayerComponent layer in layers)
                {
                    if (layer != null && layer.CurrentZOrder < lowest)
                        lowest = layer.CurrentZOrder;
                }
            }

            return lowest == int.MaxValue ? 0 : lowest;
        }
    }
}

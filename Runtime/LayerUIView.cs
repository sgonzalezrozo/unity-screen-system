using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem
{
    public class LayerUIView : ILayerUIView
    {
        private Canvas canvas;
        private GraphicRaycaster raycaster;
        private CanvasGroup canvasGroup;

        public Canvas Canvas => canvas;
        public GraphicRaycaster GraphicRaycaster => raycaster;

        public LayerUIView(Canvas canvas, GraphicRaycaster raycaster)
        {
            this.canvas = canvas;
            this.raycaster = raycaster;
            canvasGroup = canvas?.GetComponent<CanvasGroup>();
            if (canvasGroup == null && canvas != null)
                canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }

        public void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
                canvasGroup.alpha = alpha;
        }

        public float GetAlpha()
        {
            return canvasGroup != null ? canvasGroup.alpha : 1f;
        }

        public void Show()
        {
            if (canvas != null)
                canvas.enabled = true;

            if (raycaster != null)
                raycaster.enabled = true;
        }

        public void Hide()
        {
            if (canvas != null)
                canvas.enabled = false;
        }

        public void SetZOrder(int zOrder)
        {
            if (canvas != null)
                canvas.sortingOrder = zOrder;
        }

        public void SetRaycastTarget(bool enabled)
        {
            if (raycaster != null)
                raycaster.enabled = enabled;
        }
    }
}


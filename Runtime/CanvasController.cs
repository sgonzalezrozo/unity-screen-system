using ScreenSystem.ScriptableObjects;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem
{
    internal class CanvasController
    {
        private const float BASE_PLANE_DISTANCE = 100f;
        private const float DEPTH_PER_ZORDER = 0.01f;

        private readonly Canvas canvas;
        private readonly GraphicRaycaster graphicRaycaster;
        private readonly string canvasName;

        public Canvas Canvas
            => canvas;

        public GraphicRaycaster GraphicRaycaster
            => graphicRaycaster;

        public bool IsCanvasEnabled
            => canvas != null && canvas.enabled;

        public CanvasController(GameObject gameObject)
        {
            canvasName = gameObject.name;
            canvas = gameObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>();
            if (graphicRaycaster == null)
                graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();

            canvas.overrideSorting = true;

            if (graphicRaycaster == null)
                Debug.LogError($"[CanvasController] Failed to add GraphicRaycaster to {canvasName}");
        }

		public void ApplyZOrder(int zOrder)
		{
			if (canvas == null)
				return;

			canvas.overrideSorting = true;
			canvas.sortingOrder = zOrder;

			ScreenSystemSettings screenSystemSettings = ScreenSystemSettings.Instance;
			if (screenSystemSettings == null)
			{
				Debug.LogError("[ScreenLayersManager] ScreenSystemSettings is null");
				return;
			}

			if (screenSystemSettings.UIFramework == UIFramework.UIToolkit)
				return;

			canvas.renderMode = screenSystemSettings.CanvasRenderMode;

			if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
				canvas.planeDistance = BASE_PLANE_DISTANCE - (zOrder * DEPTH_PER_ZORDER);
		}

		public void Show()
		{
			if (canvas == null)
			{
				Debug.LogWarning($"[CanvasController] Show failed - canvas is null for {canvasName}");
				return;
			}

			canvas.enabled = true;
			ResortCanvas();

			if (graphicRaycaster != null)
				graphicRaycaster.enabled = true;
		}

		// The sorting order is assigned while the canvas is still disabled, and Unity doesn't always move a disabled
		// root canvas in its draw list when the order changes. Enabled, it could keep a stale position and draw behind
		// layers with a lower order until something else re-sorted the canvases. Setting the order again re-sorts it.
		private void ResortCanvas()
		{
			int order = canvas.sortingOrder;
			canvas.sortingOrder = order == int.MaxValue ? order - 1 : order + 1;
			canvas.sortingOrder = order;
		}

		public void Hide()
		{
			if (canvas == null)
			{
				Debug.LogWarning($"[CanvasController] Hide failed - canvas is null for {canvasName}");
				return;
			}

			canvas.enabled = false;
		}

#if UNITY_EDITOR
        public void ConfigureForEditor()
        {
            if (canvas == null)
                return;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.overrideSorting = true;
            canvas.sortingLayerName = "Default";
            canvas.sortingOrder = 0;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;
        }
#endif
    }
}

using UnityEngine;

namespace ScreenSystem
{
    public class ComponentDiscovery
    {
        private const float UI_CAMERA_ORTHOGRAPHIC_SIZE = 5f;
        private const int UI_CAMERA_DEPTH = 10;
        private const float UI_CAMERA_NEAR_CLIP = 0.3f;
        private const float UI_CAMERA_FAR_CLIP = 1000f;
        private const float CANVAS_SCALER_REFERENCE_RESOLUTION = 1080f;

        private readonly Transform rootTransform;

        public ComponentDiscovery(Transform rootTransform)
        {
            this.rootTransform = rootTransform;
        }

        public Camera DiscoverOrCreateUICamera()
        {
            Camera uiCamera = FindUICamera();

            if (uiCamera != null)
                return uiCamera;

            uiCamera = Object.FindFirstObjectByType<Camera>();

            if (uiCamera != null)
                return uiCamera;

            Debug.LogWarning("[ComponentDiscovery] UICamera not found. Creating default UICamera.");
            uiCamera = CreateDefaultUICamera();

            return uiCamera;
        }

        public Transform DiscoverOrCreateScreenContainer()
        {
            Transform container = FindScreenContainer();

            if (container == null)
                container = CreateDefaultScreenContainer();

            return container;
        }

        private Camera FindUICamera()
        {
            Transform uiCameraTransform = rootTransform.Find("UICamera");
            if (uiCameraTransform == null)
                return rootTransform.GetComponentInChildren<Camera>();

            Camera camera = uiCameraTransform.GetComponent<Camera>();
            if (camera != null)
                return camera;

            return rootTransform.GetComponentInChildren<Camera>();
        }

        private Transform FindScreenContainer()
        {
            Transform container = rootTransform.Find("ScreenContainer");

            if (container == null)
                container = rootTransform.Find("UICanvas");

            return container;
        }

        private Camera CreateDefaultUICamera()
        {
            GameObject cameraObj = new("UICamera");
            cameraObj.transform.SetParent(rootTransform);
            Camera camera = cameraObj.AddComponent<Camera>();

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = UI_CAMERA_ORTHOGRAPHIC_SIZE;
            camera.depth = UI_CAMERA_DEPTH;
            camera.cullingMask = LayerMask.GetMask("UI");
            camera.nearClipPlane = UI_CAMERA_NEAR_CLIP;
            camera.farClipPlane = UI_CAMERA_FAR_CLIP;

            cameraObj.transform.position = new Vector3(0, 0, -10);
            cameraObj.transform.rotation = Quaternion.identity;

            return camera;
        }

        private Transform CreateDefaultScreenContainer()
        {
            // Create GameObject with Canvas component
            GameObject containerObj = new("ScreenContainer");
            containerObj.layer = LayerMask.NameToLayer("UI");
            containerObj.transform.SetParent(rootTransform);

            // Setup RectTransform (required for Canvas)
            RectTransform rectTransform = containerObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // // Add Canvas component
            // Canvas canvas = containerObj.AddComponent<Canvas>();
            // canvas.renderMode = RenderMode.ScreenSpaceCamera;
            // canvas.worldCamera = DiscoverOrCreateUICamera();
            //
            // // Add CanvasScaler for responsive UI
            // CanvasScaler canvasScaler = containerObj.AddComponent<CanvasScaler>();
            // canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // canvasScaler.referenceResolution = new Vector2(CANVAS_SCALER_REFERENCE_RESOLUTION, CANVAS_SCALER_REFERENCE_RESOLUTION);
            //
            // // Add GraphicRaycaster for input handling
            // containerObj.AddComponent<GraphicRaycaster>();

            return containerObj.transform;
        }
    }
}


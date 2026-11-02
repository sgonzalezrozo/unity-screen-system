using Tools.ScreenSystem.Scripts;
using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.ScreenSystem.Editor
{
    public static class BlurBackgroundLayerMenu
    {
        [MenuItem("GameObject/UI/Blur Background Layer", false, 10)]
        private static void CreateBlurBackgroundLayer(MenuCommand menuCommand)
        {
            GameObject selectedObject = menuCommand.context as GameObject;
            Transform parent = selectedObject?.transform;

            GameObject blurLayer = CreateBlurBackgroundGameObject();

            if (parent != null)
                blurLayer.transform.SetParent(parent, false);

            Undo.RegisterCreatedObjectUndo(blurLayer, "Create Blur Background Layer");
            Selection.activeObject = blurLayer;

            Debug.Log($"[BlurBackgroundLayerMenu] Created BlurBackground layer{(parent != null ? $" under '{parent.name}'" : "")}");
        }

        [MenuItem("CONTEXT/Transform/Add BlurBackground to ScreenSystem", true)]
        private static bool ValidateAddBlurBackgroundToScreenSystem(MenuCommand menuCommand)
        {
            Transform transform = menuCommand.context as Transform;
            return transform != null && transform.name.Contains("ScreenSystem");
        }

        [MenuItem("CONTEXT/Transform/Add BlurBackground to ScreenSystem")]
        private static void AddBlurBackgroundToScreenSystem(MenuCommand menuCommand)
        {
            Transform screenSystem = menuCommand.context as Transform;
            if (screenSystem == null)
                return;

            Transform existing = screenSystem.Find("BlurBackground");
            if (existing != null)
            {
                EditorUtility.DisplayDialog(
                    "BlurBackground Already Exists",
                    $"A BlurBackground already exists under '{screenSystem.name}'.\n\nSelect it to configure settings.",
                    "OK"
                );
                Selection.activeObject = existing.gameObject;
                return;
            }

            GameObject blurLayer = CreateBlurBackgroundGameObject();
            blurLayer.transform.SetParent(screenSystem, false);

            Canvas canvas = blurLayer.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 0;
            }

            blurLayer.SetActive(false);

            Undo.RegisterCreatedObjectUndo(blurLayer, "Add BlurBackground to ScreenSystem");
            Selection.activeObject = blurLayer;

            Debug.Log($"[BlurBackgroundLayerMenu] Created shared BlurBackground under ScreenSystem. This will be used for ALL modal screens.");
        }

        private static GameObject CreateBlurBackgroundGameObject()
        {
            GameObject blurLayer = new GameObject("BlurBackground");

            Canvas canvas = blurLayer.AddComponent<Canvas>();
            // CRITICAL: Use Screen Space - Camera for GrabPass to work
            canvas.renderMode = RenderMode.ScreenSpaceCamera;

            // Get UICamera from ScreenSystemSettings
            ScreenSystemSettings settings = ScreenSystemSettings.Instance;

            blurLayer.AddComponent<GraphicRaycaster>();

            RectTransform rectTransform = blurLayer.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            GameObject imageObject = new GameObject("BlurImage");
            imageObject.transform.SetParent(blurLayer.transform, false);

            RectTransform imageRect = imageObject.AddComponent<RectTransform>();
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            Image blurImage = imageObject.AddComponent<Image>();

            // Use blur settings from ScreenSystemSettings
            if (settings != null)
            {
                blurImage.color = settings.BlurTintColor;
            }
            else
            {
                blurImage.color = new Color(0f, 0f, 0f, 0.5f);
            }

            blurImage.raycastTarget = true;

            CanvasGroup canvasGroup = blurLayer.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;

            blurLayer.AddComponent<BlurBackgroundLayer>();

            // Disable canvas on creation (BlurBackgroundLayer will enable it when needed)
            canvas.enabled = false;

            return blurLayer;
        }
    }
}


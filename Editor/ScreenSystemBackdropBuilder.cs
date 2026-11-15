using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem.Editor
{
    /// <summary>Builds the backdrop behind modals and alerts inside the ScreenSystem prefab.</summary>
    public static class ScreenSystemBackdropBuilder
    {
        public const string BackdropName = "BlurBackground";
        private const string ShaderName = "Hidden/ScreenSystem/SnapshotBlur";
        private const string PrefabPath = "Assets/Resources/Tools/Screen System/Prefabs/ScreenSystem.prefab";

        /// <summary>Replaces any backdrop under <paramref name="parent"/> with a new one.</summary>
        public static BlurBackgroundLayer Build(Transform parent)
        {
            Transform existing = parent.Find(BackdropName);
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            GameObject root = new(BackdropName, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.layer = UILayer();
            Stretch(root.GetComponent<RectTransform>());

            (Canvas snapshotCanvas, GameObject snapshotObject) = CreateCanvas(root.transform, "Snapshot");
            RawImage snapshot = snapshotObject.AddComponent<RawImage>();
            snapshot.raycastTarget = true;

            (Canvas dimCanvas, GameObject dimObject) = CreateCanvas(root.transform, "Dim");
            Image dim = dimObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.5f);
            dim.raycastTarget = true;

            BlurBackgroundLayer backdrop = root.AddComponent<BlurBackgroundLayer>();
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
                Debug.LogWarning($"[ScreenSystem Setup] Shader '{ShaderName}' not found. Modals will open without a blurred snapshot.");

            backdrop.Configure(shader, snapshotCanvas, snapshot, dimCanvas, dim);
            return backdrop;
        }

        /// <summary>Adds or rebuilds the backdrop in the existing ScreenSystem prefab. Returns false if there's no prefab.</summary>
        public static bool EnsureInPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                return false;

            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Build(contents.transform);
                PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            Debug.Log("[ScreenSystem Setup] Modal backdrop added to the ScreenSystem prefab.");
            return true;
        }

        [MenuItem("Tools/Screen System/Rebuild Modal Backdrop", priority = 3)]
        private static void RebuildFromMenu()
        {
            if (!EnsureInPrefab())
                EditorUtility.DisplayDialog("Modal Backdrop", "Run Tools > Screen System > Setup first. There's no ScreenSystem prefab yet.", "OK");
        }

        private static (Canvas canvas, GameObject gameObject) CreateCanvas(Transform parent, string name)
        {
            GameObject child = new(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            child.transform.SetParent(parent, false);
            child.layer = UILayer();
            Stretch(child.GetComponent<RectTransform>());

            Canvas canvas = child.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.enabled = false;
            return (canvas, child);
        }

        private static int UILayer()
        {
            int layer = LayerMask.NameToLayer("UI");
            return layer >= 0 ? layer : 0;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

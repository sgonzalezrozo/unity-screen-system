using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Assets > Screen System > New Layer: asks for a name and creates a uGUI layer prefab in the selected folder.
    /// <code>
    /// Name.prefab    Canvas, GraphicRaycaster, CanvasGroup, LayerComponent (stretched to full screen)
    /// └── Background Image, stretched to full screen
    /// </code>
    /// The prefab is marked Addressable with its file name as the address, so screens can list it as a layer.
    /// </summary>
    public static class LayerPrefabCreator
    {
        private const string DEFAULT_NAME = "NewLayer";

        [MenuItem("Assets/Screen System/New Layer", false, 21)]
        public static void NewLayer()
        {
            string folder = FeatureCreatorWizard.SelectedFolder();
            NamePromptWindow.Show(
                "New Layer",
                "Prefab Name",
                DEFAULT_NAME,
                $"Creates {folder}/<Name>.prefab with a Canvas and a full-screen Image, marked Addressable as <Name>.",
                name => Create(folder, name));
        }

        /// <summary>Creates the layer prefab in <paramref name="folder"/> and selects it. Returns null if one exists already.</summary>
        public static GameObject Create(string folder, string name)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                folder = "Assets";

            string path = $"{folder}/{name}.prefab";
            if (File.Exists(path))
            {
                EditorUtility.DisplayDialog("Screen System", $"'{path}' already exists.", "OK");
                return null;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            GameObject root = new(name, typeof(RectTransform));
            FeatureCreatorWizard.SetUpCanvas(root);
            root.AddComponent<LayerComponent>();

            GameObject background = new("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform rect = (RectTransform)background.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            if (uiLayer >= 0)
            {
                root.layer = uiLayer;
                background.layer = uiLayer;
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            if (prefab == null)
                return null;

            string address = ScreenSystemAssets.EnsureLoadable(prefab);
            AssetDatabase.SaveAssets();

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            Debug.Log($"[ScreenSystem] Layer created: {path} (address '{address}').");
            return prefab;
        }
    }
}

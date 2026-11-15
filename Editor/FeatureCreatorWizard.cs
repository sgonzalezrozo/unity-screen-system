using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Creates screens that work on the first GoTo. For each screen, in the feature folder:
    /// <code>
    /// Feature/
    /// ├── ScreenName/ScreenName.prefab    (+ ScreenName.uxml for UI Toolkit)
    /// └── Services/ScreenName.asset       ScreenDefinition, ID "Feature_ScreenName"
    /// </code>
    /// The prefab is marked Addressable (address = its name) and the definition is added to the registry.
    /// </summary>
    public class FeatureCreatorWizard : EditorWindow
    {
        public const string ServicesFolder = "Services";

        private class ScreenItem
        {
            public string Name;
            public ScreenType Type = ScreenType.fullscreen;
            public bool PersistOverScenes;
        }

        private enum WizardMode
        {
            CreateNewFeature,
            AddScreenToFeature
        }

        private string featureName = "NewFeature";
        private List<ScreenItem> screens = new() { new ScreenItem { Name = "MainScreen" } };
        private Vector2 scrollPos;
        private string targetFolderPath = "Assets";
        private UIFramework selectedFramework = UIFramework.uGUI;
        private WizardMode mode = WizardMode.CreateNewFeature;

        [MenuItem("Assets/Screen System/New Screen Feature", false, 20)]
        public static void NewScreenFeature()
            => Open(WizardMode.CreateNewFeature);

        [MenuItem("Assets/Screen System/Add Screen to Feature", false, 21)]
        public static void AddScreenToFeature()
            => Open(WizardMode.AddScreenToFeature);

        private static void Open(WizardMode wizardMode)
        {
            string path = "Assets";
            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!AssetDatabase.IsValidFolder(path))
                    path = Path.GetDirectoryName(path);
            }

            FeatureCreatorWizard window = GetWindow<FeatureCreatorWizard>("New Screens");
            window.minSize = new Vector2(460, 300);
            window.mode = wizardMode;
            window.SetTarget(path?.Replace('\\', '/'));
            window.Show();
        }

        private void SetTarget(string path)
        {
            targetFolderPath = string.IsNullOrEmpty(path) ? "Assets" : path;

            if (mode == WizardMode.AddScreenToFeature)
            {
                targetFolderPath = FindFeatureFolder(targetFolderPath);
                featureName = Path.GetFileName(targetFolderPath);
                screens = new() { new ScreenItem { Name = $"{featureName}NewScreen" } };
                return;
            }

            string folderName = Path.GetFileName(targetFolderPath);
            if (!string.IsNullOrEmpty(folderName) && folderName != "Assets")
            {
                featureName = folderName;
                targetFolderPath = Path.GetDirectoryName(targetFolderPath)?.Replace('\\', '/');
                if (screens.Count > 0)
                    screens[0].Name = $"{folderName}Main";
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(mode == WizardMode.CreateNewFeature ? "New feature with screens" : "Add screens to a feature", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");

            if (mode == WizardMode.CreateNewFeature)
                featureName = EditorGUILayout.TextField("Feature", featureName);
            else
                EditorGUILayout.LabelField("Feature", featureName);

            selectedFramework = (UIFramework)EditorGUILayout.EnumPopup("UI Framework", selectedFramework);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(mode == WizardMode.CreateNewFeature ? "Parent Folder" : "Feature Folder", targetFolderPath);
            if (GUILayout.Button("Select", GUILayout.Width(70)))
                SelectFolder();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Screens", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, "box", GUILayout.ExpandHeight(true));
            for (int i = 0; i < screens.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                screens[i].Name = EditorGUILayout.TextField(screens[i].Name);
                screens[i].Type = (ScreenType)EditorGUILayout.EnumPopup(screens[i].Type, GUILayout.Width(85));
                screens[i].PersistOverScenes = EditorGUILayout.ToggleLeft("Persist", screens[i].PersistOverScenes, GUILayout.Width(60));

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    screens.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Screen"))
                screens.Add(new ScreenItem { Name = $"{featureName}Screen{screens.Count + 1}" });

            EditorGUILayout.EndScrollView();

            string featureDir = FeatureDirectory();
            EditorGUILayout.HelpBox(
                $"Creates {featureDir}/<Screen>/<Screen>.prefab and {featureDir}/{ServicesFolder}/<Screen>.asset, " +
                "marks the prefab Addressable and adds the screen to the registry. Open it with GoTo(\"<Screen>\").",
                MessageType.None);

            GUI.enabled = screens.Count > 0 && !string.IsNullOrWhiteSpace(featureName);
            if (GUILayout.Button("Create", GUILayout.Height(36)))
                CreateAll();
            GUI.enabled = true;
        }

        private void SelectFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Select folder", targetFolderPath, "");
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("Screen System", "Pick a folder inside Assets.", "OK");
                return;
            }

            string assetPath = "Assets" + path.Substring(Application.dataPath.Length);
            if (mode == WizardMode.AddScreenToFeature)
                SetTarget(assetPath);
            else
                targetFolderPath = assetPath;
        }

        private string FeatureDirectory()
            => mode == WizardMode.AddScreenToFeature ? targetFolderPath : $"{targetFolderPath}/{featureName.Replace(" ", "")}";

        private void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(targetFolderPath))
            {
                EditorUtility.DisplayDialog("Screen System", $"'{targetFolderPath}' isn't a folder.", "OK");
                return;
            }

            string featureDir = FeatureDirectory();
            string feature = Path.GetFileName(featureDir);
            List<string> created = new();
            List<string> skipped = new();

            foreach (ScreenItem item in screens)
            {
                string screenName = item.Name?.Replace(" ", "");
                if (string.IsNullOrEmpty(screenName))
                    continue;

                ScreenDefinition definition = CreateScreen(featureDir, feature, screenName, item.Type, selectedFramework, item.PersistOverScenes);
                if (definition != null)
                    created.Add(screenName);
                else
                    skipped.Add(screenName);
            }

            string message = created.Count > 0 ? $"Created: {string.Join(", ", created)}." : "Nothing created.";
            if (skipped.Count > 0)
                message += $"\nSkipped, already exist: {string.Join(", ", skipped)}.";

            EditorUtility.DisplayDialog("Screen System", message, "OK");
            if (created.Count > 0)
                Close();
        }

        /// <summary>
        /// Creates one screen: its prefab, its ScreenDefinition and its registry entry. Returns null if a screen
        /// with that name already exists in the feature.
        /// </summary>
        public static ScreenDefinition CreateScreen(string featureDir, string feature, string screenName, ScreenType type, UIFramework framework, bool persistOverScenes = false)
        {
            string screenDir = $"{featureDir}/{screenName}";
            string servicesDir = $"{featureDir}/{ServicesFolder}";
            string prefabPath = $"{screenDir}/{screenName}.prefab";
            string definitionPath = $"{servicesDir}/{screenName}.asset";

            if (AssetDatabase.LoadAssetAtPath<ScreenDefinition>(definitionPath) != null)
                return null;

            EnsureFolder(featureDir);
            EnsureFolder(screenDir);
            EnsureFolder(servicesDir);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                prefab = CreatePrefab(screenDir, screenName, framework);

            ScreenDefinition definition = CreateInstance<ScreenDefinition>();
            definition.ScreenName = screenName;
            definition.ScreenId = ScreenDefinition.MakeScreenId(feature, screenName);
            definition.screenType = type;
            definition.Framework = framework;
            definition.PersistOverScenes = persistOverScenes;
            definition.AssetPath = ScreenSystemAssets.EnsureLoadable(prefab);
            AssetDatabase.CreateAsset(definition, definitionPath);
            AssetDatabase.SaveAssets();

            ScreenRegistrySync.Sync(logChanges: false);
            return definition;
        }

        private static GameObject CreatePrefab(string screenDir, string screenName, UIFramework framework)
        {
            GameObject root = new(screenName) { layer = LayerMask.NameToLayer("UI") };

            if (framework == UIFramework.UIToolkit)
                SetUpUIDocument(root, screenDir, screenName);
            else
                SetUpCanvas(root);

            root.AddComponent<LayerDefinition>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{screenDir}/{screenName}.prefab");
            DestroyImmediate(root);
            return prefab;
        }

        private static void SetUpCanvas(GameObject root)
        {
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = false;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<CanvasGroup>();

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void SetUpUIDocument(GameObject root, string screenDir, string screenName)
        {
            UIDocument document = root.AddComponent<UIDocument>();
            document.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(ScreenSystemAssets.PanelSettingsPath);
            if (document.panelSettings == null)
                Debug.LogWarning($"[ScreenSystem] No PanelSettings at {ScreenSystemAssets.PanelSettingsPath}. Run Tools > Screen System > Setup, then assign it to {screenName}.");

            string uxmlPath = $"{screenDir}/{screenName}.uxml";
            if (!File.Exists(uxmlPath))
            {
                File.WriteAllText(uxmlPath,
                    "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\" xmlns:uie=\"UnityEditor.UIElements\">\n" +
                    $"    <ui:VisualElement name=\"{screenName}\" style=\"flex-grow: 1;\" />\n" +
                    "</ui:UXML>\n");
                AssetDatabase.ImportAsset(uxmlPath);
            }

            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        }

        /// <summary>
        /// The feature folder for a selected path: the selection itself, or its parent when the selection is a
        /// feature's Services folder, a screen folder next to Services, or a legacy Screens folder.
        /// </summary>
        public static string FindFeatureFolder(string path)
        {
            string name = Path.GetFileName(path);
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');

            if (name == ServicesFolder || name == "Screens" || name == "Prefabs")
                return parent;

            if (!AssetDatabase.IsValidFolder($"{path}/{ServicesFolder}") && parent != null && AssetDatabase.IsValidFolder($"{parent}/{ServicesFolder}"))
                return parent;

            return path;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

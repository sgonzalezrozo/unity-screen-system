using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Assets > Screen System > New Screen: creates screens that work on the first GoTo, in the folder you
    /// right-clicked. That folder is the feature: its name prefixes the screen IDs. For each screen:
    /// <code>
    /// PlayerHealth/                                the folder you picked
    /// ├── PlayerHealthMain/PlayerHealthMain.prefab  (+ .uxml for UI Toolkit)
    /// └── Services/PlayerHealthMain.asset           ScreenDefinition, ID "PlayerHealth_PlayerHealthMain"
    /// </code>
    /// The prefab is marked Addressable (address = its name) and the definition is added to the registry. Opened on a
    /// feature that already has screens (or from a ScreenDefinition's Add Screen to Feature… button), it adds to it.
    /// </summary>
    public class FeatureCreatorWizard : EditorWindow
    {
        public const string ServicesFolder = "Services";

        private const float WIDTH = 500f;
        private const float BASE_HEIGHT = 196f;
        private const float ROW_HEIGHT = 22f;
        private const float EXISTING_HEIGHT = 34f;
        private const int MAX_VISIBLE_ROWS = 10;
        private const float TYPE_WIDTH = 96f;
        private const float PERSIST_WIDTH = 64f;
        private const float REMOVE_WIDTH = 22f;

        private class ScreenItem
        {
            public string Name;
            public ScreenType Type = ScreenType.Fullscreen;
            public bool PersistOverScenes;
        }

        private readonly List<ScreenItem> screens = new();
        private readonly List<string> existingScreens = new();
        private Vector2 scrollPos;
        private string featureFolder = "Assets";
        private UIFramework selectedFramework = UIFramework.uGUI;

        private string Feature => Path.GetFileName(featureFolder);

        /// <summary>The project root isn't a feature: its name would prefix every screen ID with "Assets".</summary>
        private bool HasFeatureFolder => AssetDatabase.IsValidFolder(featureFolder) && featureFolder != "Assets";

        [MenuItem("Assets/Screen System/New Screen", false, 20)]
        public static void NewScreen()
            => Open(SelectedFolder());

        /// <summary>
        /// Opens the wizard to add screens to the feature that holds <paramref name="path"/> (the feature folder, its
        /// Services folder or a screen folder). The ScreenDefinition Inspector's Add Screen to Feature button uses it.
        /// </summary>
        public static void AddScreenToFeature(string path)
            => Open(path);

        private static void Open(string path)
        {
            FeatureCreatorWizard window = GetWindow<FeatureCreatorWizard>(true, "New Screens", true);
            window.SetTarget(path?.Replace('\\', '/'));
            window.ShowUtility();
        }

        private void SetTarget(string path)
        {
            featureFolder = FindFeatureFolder(string.IsNullOrEmpty(path) ? "Assets" : path);
            ReadExistingScreens();
            screens.Clear();
            screens.Add(new ScreenItem { Name = NextScreenName() });
            FitToContent();
        }

        /// <summary>The screens the feature already has: their ScreenDefinitions in its Services folder.</summary>
        private void ReadExistingScreens()
        {
            existingScreens.Clear();
            string services = $"{featureFolder}/{ServicesFolder}";
            if (!AssetDatabase.IsValidFolder(services))
                return;

            foreach (string guid in AssetDatabase.FindAssets("t:ScreenDefinition", new[] { services }))
                existingScreens.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
            existingScreens.Sort();
        }

        /// <summary>FeatureMain for the first screen, then FeatureScreen2, FeatureScreen3...: names GoTo can tell apart.</summary>
        private string NextScreenName()
        {
            string feature = HasFeatureFolder ? Feature.Replace(" ", "") : "New";
            if (existingScreens.Count == 0 && screens.Count == 0)
                return $"{feature}Main";

            int number = existingScreens.Count + screens.Count + 1;
            string name = $"{feature}Screen{number}";
            while (existingScreens.Contains(name) || screens.Exists(item => item.Name == name))
                name = $"{feature}Screen{++number}";
            return name;
        }

        /// <summary>Sizes the window to its rows, so a short list doesn't leave an empty panel; a long one scrolls.</summary>
        private void FitToContent()
        {
            int rows = Mathf.Clamp(screens.Count, 1, MAX_VISIBLE_ROWS);
            float height = BASE_HEIGHT + rows * ROW_HEIGHT + (existingScreens.Count > 0 ? EXISTING_HEIGHT : 0f);
            Vector2 size = new(WIDTH, height);
            minSize = new Vector2(WIDTH, height);
            maxSize = new Vector2(WIDTH * 2f, height);
            Rect current = position;
            position = new Rect(current.x, current.y, Mathf.Max(current.width, WIDTH), size.y);
        }

        /// <summary>
        /// The folder the Assets menu was opened on: the selected folder, the folder of the selected asset, or
        /// the folder open in the Project window when nothing is selected.
        /// </summary>
        internal static string SelectedFolder()
        {
            string path = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
            if (!string.IsNullOrEmpty(path))
                return AssetDatabase.IsValidFolder(path) ? path : Path.GetDirectoryName(path)?.Replace('\\', '/');

            // ProjectWindowUtil.TryGetActiveFolderPath is internal.
            System.Reflection.MethodInfo method = typeof(ProjectWindowUtil).GetMethod(
                "TryGetActiveFolderPath",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
            {
                object[] args = { null };
                if ((bool)method.Invoke(null, args) && args[0] is string active && AssetDatabase.IsValidFolder(active))
                    return active;
            }

            return "Assets";
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Folder", "The feature's folder. Its name prefixes the screen IDs."), new GUIContent(featureFolder, featureFolder));
            if (GUILayout.Button("Select", GUILayout.Width(64)))
                SelectFolder();
            EditorGUILayout.EndHorizontal();

            selectedFramework = (UIFramework)EditorGUILayout.EnumPopup("UI Framework", selectedFramework);

            if (existingScreens.Count > 0)
                EditorGUILayout.HelpBox($"Already in {Feature}: {string.Join(", ", existingScreens)}. New screens are added next to them.", MessageType.None);

            EditorGUILayout.Space(6);
            DrawScreenRows();

            EditorGUILayout.Space(4);
            if (HasFeatureFolder)
            {
                EditorGUILayout.HelpBox(
                    $"Each screen gets <Screen>/<Screen>.prefab and {ServicesFolder}/<Screen>.asset in {featureFolder}, ID {Feature.Replace(" ", "")}_<Screen>. " +
                    "The prefab is marked Addressable and the screen added to the registry. Open it with GoTo(\"<Screen>\").",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Pick the feature's folder (right-click it, or Select): its name prefixes the screen IDs. Assets itself isn't a feature.", MessageType.Warning);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Cancel", GUILayout.Width(90)))
                Close();

            using (new EditorGUI.DisabledScope(!HasFeatureFolder || !screens.Exists(item => !string.IsNullOrWhiteSpace(item.Name))))
            {
                if (GUILayout.Button("Create", GUILayout.Width(90)))
                    CreateAll();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(8);
        }

        private void DrawScreenRows()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Screens", EditorStyles.boldLabel);
            GUILayout.Label("Type", EditorStyles.miniLabel, GUILayout.Width(TYPE_WIDTH));
            GUILayout.Label("", GUILayout.Width(PERSIST_WIDTH + REMOVE_WIDTH));
            EditorGUILayout.EndHorizontal();

            bool scrolls = screens.Count > MAX_VISIBLE_ROWS;
            if (scrolls)
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(MAX_VISIBLE_ROWS * ROW_HEIGHT));

            int removeAt = -1;
            for (int i = 0; i < screens.Count; i++)
            {
                ScreenItem item = screens[i];
                EditorGUILayout.BeginHorizontal();
                item.Name = EditorGUILayout.TextField(item.Name);
                item.Type = (ScreenType)EditorGUILayout.EnumPopup(item.Type, GUILayout.Width(TYPE_WIDTH));
                item.PersistOverScenes = EditorGUILayout.ToggleLeft(new GUIContent("Persist", "Stays open across scene changes"), item.PersistOverScenes, GUILayout.Width(PERSIST_WIDTH));
                using (new EditorGUI.DisabledScope(screens.Count == 1))
                {
                    if (GUILayout.Button(new GUIContent("✕", "Remove this screen"), EditorStyles.miniButton, GUILayout.Width(REMOVE_WIDTH)))
                        removeAt = i;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (scrolls)
                EditorGUILayout.EndScrollView();

            if (removeAt >= 0)
            {
                screens.RemoveAt(removeAt);
                FitToContent();
            }

            if (GUILayout.Button("+ Add Screen"))
            {
                screens.Add(new ScreenItem { Name = NextScreenName() });
                FitToContent();
            }
        }

        private void SelectFolder()
        {
            string path = EditorUtility.OpenFolderPanel("Feature folder", featureFolder, "");
            if (string.IsNullOrEmpty(path))
                return;

            if (!path.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("Screen System", "Pick a folder inside Assets.", "OK");
                return;
            }

            SetTarget("Assets" + path.Substring(Application.dataPath.Length));
        }

        private void CreateAll()
        {
            if (!HasFeatureFolder)
                return;

            string feature = Feature.Replace(" ", "");
            List<string> created = new();
            List<string> skipped = new();

            foreach (ScreenItem item in screens)
            {
                string screenName = item.Name?.Replace(" ", "");
                if (string.IsNullOrEmpty(screenName))
                    continue;

                ScreenDefinition definition = CreateScreen(featureFolder, feature, screenName, item.Type, selectedFramework, item.PersistOverScenes);
                if (definition != null)
                    created.Add(screenName);
                else
                    skipped.Add(screenName);
            }

            if (skipped.Count > 0)
                EditorUtility.DisplayDialog("Screen System", $"Already exist, skipped: {string.Join(", ", skipped)}." + (created.Count > 0 ? $"\nCreated: {string.Join(", ", created)}." : string.Empty), "OK");
            else
                Debug.Log($"[ScreenSystem] Created in {featureFolder}: {string.Join(", ", created)}.");

            if (created.Count > 0)
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>($"{featureFolder}/{created[0]}/{created[0]}.prefab");
                Close();
            }
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
            ProjectAssemblySetup.PrepareFolder(featureDir);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                prefab = CreatePrefab(screenDir, screenName, framework);

            ScreenDefinition definition = CreateInstance<ScreenDefinition>();
            definition.ScreenName = screenName;
            definition.ScreenId = ScreenDefinition.MakeScreenId(feature, screenName);
            definition.ScreenType = type;
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

            // The LayerDefinition inspector adds the SCREEN separator the first time it shows the prefab and marks it
            // dirty, which makes Unity ask to save or reload it. The new screen is saved with it, so opening it changes nothing.
            root.AddComponent<LayerDefinition>().EnsureScreenSeparatorExists();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{screenDir}/{screenName}.prefab");
            DestroyImmediate(root);
            return prefab;
        }

        internal static void SetUpCanvas(GameObject root)
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

        internal static void SetUpUIDocument(GameObject root, string screenDir, string screenName)
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

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);

            // A folder already on disk that Unity hasn't imported yet (another tool wrote files into it a moment ago) is
            // imported, not created again: CreateFolder would add a "Name 1" folder next to it.
            if (Directory.Exists(path))
            {
                AssetDatabase.ImportAsset(path);
                if (AssetDatabase.IsValidFolder(path))
                    return;
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}

using System.Collections.Generic;
using System.IO;
using ScreenSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace ScreenSystem.Editor
{
    public class FeatureCreatorWizard : EditorWindow
    {
        private class ScreenItem
        {
            public string Name;
            public bool PersistOverScenes;
        }

        private enum WizardMode
        {
            CreateNewFeature,
            AddScreenToFeature
        }

        private string featureCategory = "NewFeature";
        private List<ScreenItem> screens = new() { new ScreenItem { Name = "MainScreen" } };
        private Vector2 scrollPos;
        private string targetFolderPath = "Assets";
        private UIFramework selectedFramework = UIFramework.uGUI;
        private static FeatureCreatorWizard window;
        private WizardMode mode = WizardMode.CreateNewFeature;

        [MenuItem("Assets/Screen System/New Screen Feature", false, 20)]
        public static void NewScreenFeature()
        {
            InitializeWindow(WizardMode.CreateNewFeature);
        }

        [MenuItem("Assets/Screen System/Add Screen to Feature", false, 21)]
        public static void AddScreenToFeature()
        {
            InitializeWindow(WizardMode.AddScreenToFeature);
        }

        private static void InitializeWindow(WizardMode wizardMode)
        {
            string path = "Assets";
            if (Selection.activeObject != null)
            {
                path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!AssetDatabase.IsValidFolder(path))
                    path = Path.GetDirectoryName(path);
            }

            window = GetWindow<FeatureCreatorWizard>("Feature Creator Wizard");
            window.minSize = new Vector2(400, 300);
            window.targetFolderPath = path?.Replace('\\', '/');
            window.mode = wizardMode;

            if (wizardMode == WizardMode.AddScreenToFeature)
            {
                if (string.IsNullOrEmpty(window.targetFolderPath) || !AssetDatabase.IsValidFolder(window.targetFolderPath))
                {
                    EditorUtility.DisplayDialog("Error", "Please select a valid folder.", "OK");
                    window.Close();
                    return;
                }

                string folderName = Path.GetFileName(window.targetFolderPath);
                window.featureCategory = folderName == "Screens"
                    ? Path.GetFileName(Path.GetDirectoryName(window.targetFolderPath))
                    : folderName;
                window.screens = new() { new ScreenItem { Name = $"{window.featureCategory}NewScreen" } };
            }
            else
            {
                if (!string.IsNullOrEmpty(window.targetFolderPath))
                {
                    string folderName = Path.GetFileName(window.targetFolderPath);
                    if (!string.IsNullOrEmpty(folderName) && folderName != "Assets")
                    {
                        window.featureCategory = folderName;
                        if (window.screens.Count > 0)
                            window.screens[0].Name = $"{folderName}Main";
                    }
                }
            }

            window.Show();
        }


        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            string windowTitle = mode == WizardMode.CreateNewFeature
                ? "Create Multiple Screens for a Feature"
                : "Add Screens to Existing Feature";

            EditorGUILayout.LabelField(windowTitle, EditorStyles.boldLabel);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical("box");

            if (mode == WizardMode.CreateNewFeature)
                featureCategory = EditorGUILayout.TextField("Feature Category", featureCategory);
            else
                EditorGUILayout.LabelField("Feature:", featureCategory);

            selectedFramework = (UIFramework)EditorGUILayout.EnumPopup("UI Framework", selectedFramework);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Target Folder:", targetFolderPath);
            if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
            {
                string folderType = mode == WizardMode.CreateNewFeature ? "parent folder" : "folder";
                string path = EditorUtility.OpenFolderPanel($"Select {folderType}", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        targetFolderPath = "Assets" + path.Substring(Application.dataPath.Length);

                        if (mode == WizardMode.AddScreenToFeature)
                        {
                            string folderName = Path.GetFileName(targetFolderPath);
                            featureCategory = folderName == "Screens"
                                ? Path.GetFileName(Path.GetDirectoryName(targetFolderPath))
                                : folderName;
                            if (screens.Count > 0)
                                screens[0].Name = $"{featureCategory}NewScreen";
                        }
                        else
                        {
                            string folderName = Path.GetFileName(targetFolderPath);
                            if (!string.IsNullOrEmpty(folderName) && folderName != "Assets")
                            {
                                featureCategory = folderName;
                                if (screens.Count > 0)
                                    screens[0].Name = $"{folderName}Main";
                            }
                        }
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Please select a folder inside the Assets directory.", "OK");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Screens to Create", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, "box", GUILayout.ExpandHeight(true));
            for (int i = 0; i < screens.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                screens[i].Name = EditorGUILayout.TextField($"Screen {i + 1}", screens[i].Name);
                screens[i].PersistOverScenes = EditorGUILayout.ToggleLeft("Persist", screens[i].PersistOverScenes, GUILayout.Width(65));

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    screens.RemoveAt(i);
                    i--;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Screen"))
                screens.Add(new ScreenItem { Name = $"NewScreen{screens.Count + 1}" });

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            GUI.enabled = screens.Count > 0 && !string.IsNullOrEmpty(featureCategory);

            string buttonText = mode == WizardMode.CreateNewFeature
                ? "Create Feature Screens & Prefabs"
                : "Add Screens to Feature";

            if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                CreateFeature();

            GUI.enabled = true;
        }

        private void CreateFeature()
        {
            string screensDir;
            string prefabsDir;

            if (mode == WizardMode.AddScreenToFeature)
            {
                if (string.IsNullOrEmpty(targetFolderPath) || !AssetDatabase.IsValidFolder(targetFolderPath))
                {
                    EditorUtility.DisplayDialog("Error", "Target folder is invalid.", "OK");
                    return;
                }

                string folderName = Path.GetFileName(targetFolderPath);

                if (folderName == "Screens")
                {
                    screensDir = targetFolderPath;
                    string parentDir = Path.GetDirectoryName(targetFolderPath);
                    prefabsDir = parentDir + "/Prefabs";
                }
                else
                {
                    screensDir = targetFolderPath;
                    prefabsDir = targetFolderPath + "/Prefabs";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(targetFolderPath) || !AssetDatabase.IsValidFolder(targetFolderPath))
                {
                    EditorUtility.DisplayDialog("Error", "Target folder is invalid.", "OK");
                    return;
                }

                string featureDir = targetFolderPath + "/" + featureCategory;
                if (!AssetDatabase.IsValidFolder(featureDir))
                    AssetDatabase.CreateFolder(targetFolderPath, featureCategory);

                screensDir = featureDir + "/Screens";
                prefabsDir = featureDir + "/Prefabs";
            }

            if (!AssetDatabase.IsValidFolder(prefabsDir))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(prefabsDir), "Prefabs");

            if (!AssetDatabase.IsValidFolder(screensDir))
                AssetDatabase.CreateFolder(Path.GetDirectoryName(screensDir), "Screens");

            int createdCount = 0;

            foreach (ScreenItem screen in screens)
            {
                string screenName = screen.Name;

                if (string.IsNullOrWhiteSpace(screenName))
                    continue;

                string id = $"{featureCategory.Replace(" ", "")}_{screenName.Replace(" ", "")}";

                string assetPath = $"{screensDir}/{screenName}.asset";
                if (AssetDatabase.LoadAssetAtPath<ScreenDefinition>(assetPath) == null)
                {
                    ScreenDefinition def = CreateInstance<ScreenDefinition>();
                    def.ScreenName = screenName;
                    def.ScreenId = id;
                    def.screenType = ScreenType.fullscreen;
                    def.Framework = selectedFramework;

                    if (screen.PersistOverScenes)
                        def.PersistOverScenes = true;

                    AssetDatabase.CreateAsset(def, assetPath);
                    createdCount++;
                }

                string prefabPath = $"{prefabsDir}/{screenName}.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                    continue;

                GameObject screenObj = new(screenName)
                {
                    layer = LayerMask.NameToLayer("UI")
                };

                if (selectedFramework == UIFramework.uGUI)
                    CreateuGUIPrefab(screenObj);
                else if (selectedFramework == UIFramework.UIToolkit)
                    CreateUIToolkitPrefab(screenObj);

                screenObj.AddComponent<LayerDefinition>();

                PrefabUtility.SaveAsPrefabAsset(screenObj, prefabPath);
                DestroyImmediate(screenObj);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message = mode == WizardMode.AddScreenToFeature
                ? $"Added {createdCount} screens and prefabs to feature '{featureCategory}'."
                : $"Created {createdCount} screens and prefabs for feature '{featureCategory}'.";

            EditorUtility.DisplayDialog("Success", message, "OK");

            window.Close();
        }

        private void CreateuGUIPrefab(GameObject screenObj)
        {
            Canvas screenCanvas = screenObj.AddComponent<Canvas>();
            screenCanvas.overrideSorting = false;

            screenObj.AddComponent<GraphicRaycaster>();

            CanvasGroup canvasGroup = screenObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            RectTransform screenRect = screenObj.GetComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;
            screenRect.anchoredPosition = Vector2.zero;
            screenRect.localScale = Vector3.one;
        }

        private void CreateUIToolkitPrefab(GameObject screenObj)
        {
            screenObj.AddComponent<UIDocument>();

            Debug.LogWarning("[FeatureCreatorWizard] UIDocument created. Please manually assign:\n" +
                             "1. Source Asset (UXML): Your UXML file\n" +
                             "2. Panel Settings: Assets/Settings/UI/GlobalPanelSettings.asset");
        }
    }
}

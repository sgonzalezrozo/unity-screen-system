using ScreenSystem;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    [CustomEditor(typeof(ScreenDefinition))]
    public class ScreenDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty assetPathProp;
        private SerializedProperty screenNameProp;
        private SerializedProperty screenIdProp;
        private SerializedProperty screenTypeProp;
        private SerializedProperty persistOverScenesProp;
        private SerializedProperty ignoreNavigationRulesProp;
        private string loadSourceFor;
        private string loadSource;

        private void OnEnable()
        {
            assetPathProp = serializedObject.FindProperty("AssetPath");
            screenNameProp = serializedObject.FindProperty("ScreenName");
            screenIdProp = serializedObject.FindProperty("ScreenId");
            screenTypeProp = serializedObject.FindProperty("ScreenType");
            persistOverScenesProp = serializedObject.FindProperty("PersistOverScenes");
            ignoreNavigationRulesProp = serializedObject.FindProperty("IgnoreNavigationRules");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            ScreenDefinition screenDef = (ScreenDefinition)target;

            EditorGUILayout.Space(10);

            Rect dropRect = GUILayoutUtility.GetRect(0f, 60f, GUILayout.ExpandWidth(true));
            GUIStyle style = new(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
            GUI.Box(dropRect, "Drag & Drop Screen Prefab Here (made Addressable if needed)", style);

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropRect.Contains(evt.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (Object draggedObject in DragAndDrop.objectReferences)
                        {
                            if (draggedObject is not GameObject go || !PrefabUtility.IsPartOfPrefabAsset(go))
                                continue;

                            // Marks the prefab Addressable when it isn't in Resources, so the screen loads on its first GoTo.
                            string finalPath = ScreenSystemAssets.EnsureLoadable(go);
                            if (string.IsNullOrEmpty(finalPath))
                                continue;

                            assetPathProp.stringValue = finalPath;
                            serializedObject.ApplyModifiedProperties();

                            screenDef.SyncWithPrefab();
                            EditorUtility.SetDirty(screenDef);
                        }
                    }

                    Event.current.Use();
                    break;
            }

            if (string.IsNullOrEmpty(screenDef.AssetPath))
                EditorGUILayout.HelpBox("Missing Asset Path! Please assign a prefab.", MessageType.Error);
            else
            {
                if (loadSourceFor != screenDef.AssetPath)
                {
                    loadSourceFor = screenDef.AssetPath;
                    loadSource = Resources.Load<GameObject>(screenDef.AssetPath) != null ? "Resources" : "Addressables";
                }

                string pathType = loadSource;
                EditorGUILayout.HelpBox($"Loading from: {pathType}\nPath: {screenDef.AssetPath}", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Screen Properties", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.PropertyField(assetPathProp);

            GUI.enabled = false;
            EditorGUILayout.PropertyField(screenNameProp);
            EditorGUILayout.PropertyField(screenIdProp);
            GUI.enabled = true;

            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(screenTypeProp);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (persistOverScenesProp is not null)
                EditorGUILayout.PropertyField(persistOverScenesProp);

            if (ignoreNavigationRulesProp is not null)
                EditorGUILayout.PropertyField(ignoreNavigationRulesProp);

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();

            DrawEditSection(screenDef);
        }

        /// <summary>Buttons to open the screen's prefab and to add more screens to its feature.</summary>
        private static void DrawEditSection(ScreenDefinition screenDef)
        {
            string definitionPath = AssetDatabase.GetAssetPath(screenDef);
            if (string.IsNullOrEmpty(definitionPath))
                return;

            string featureFolder = FeatureCreatorWizard.FindFeatureFolder(System.IO.Path.GetDirectoryName(definitionPath)?.Replace('\\', '/'));
            GameObject prefab = FindPrefab(screenDef);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Edit", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Feature", System.IO.Path.GetFileName(featureFolder));

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = prefab != null;
            if (GUILayout.Button(new GUIContent("Edit Prefab", prefab != null ? AssetDatabase.GetAssetPath(prefab) : "No prefab found for this Asset Path")))
                AssetDatabase.OpenAsset(prefab);
            GUI.enabled = true;

            if (GUILayout.Button(new GUIContent("Add Screen to Feature…", $"Adds screens to {featureFolder}")))
                FeatureCreatorWizard.AddScreenToFeature(featureFolder);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        /// <summary>The prefab the definition loads: from Resources, or the Addressable entry with that address.</summary>
        private static GameObject FindPrefab(ScreenDefinition screenDef)
        {
            if (string.IsNullOrEmpty(screenDef.AssetPath))
                return null;

            GameObject fromResources = Resources.Load<GameObject>(screenDef.AssetPath);
            if (fromResources != null)
                return fromResources;

            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return null;

            foreach (UnityEditor.AddressableAssets.Settings.AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (UnityEditor.AddressableAssets.Settings.AddressableAssetEntry entry in group.entries)
                {
                    if (entry.address == screenDef.AssetPath)
                        return AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                }
            }

            return null;
        }
    }
}

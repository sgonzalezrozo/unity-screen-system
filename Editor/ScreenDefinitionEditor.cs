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
            screenTypeProp = serializedObject.FindProperty("screenType");
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
        }
    }
}

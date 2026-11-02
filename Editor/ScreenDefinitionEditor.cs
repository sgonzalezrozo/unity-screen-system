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

        private void OnEnable()
        {
            assetPathProp = serializedObject.FindProperty("AssetPath");
            screenNameProp = serializedObject.FindProperty("ScreenName");
            screenIdProp = serializedObject.FindProperty("ScreenId");
            screenTypeProp = serializedObject.FindProperty("screenType");
            persistOverScenesProp = serializedObject.FindProperty("PersistOverScenes");
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
            GUI.Box(dropRect, "Drag & Drop Screen Prefab Here (Resources or Addressable)", style);

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

                            string assetPath = AssetDatabase.GetAssetPath(go);
                            if (string.IsNullOrEmpty(assetPath))
                                continue;

                            string finalPath = DetectAssetPath(assetPath);

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
                string pathType = screenDef.AssetPath.Contains("/Resources/") ? "Resources" : "Addressables";
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

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private string DetectAssetPath(string assetPath)
        {
            int resourcesIndex = assetPath.IndexOf("/Resources/");
            if (resourcesIndex >= 0)
            {
                string resourcesPath = assetPath.Substring(resourcesIndex + "/Resources/".Length);
                resourcesPath = System.IO.Path.ChangeExtension(resourcesPath, null);
                Debug.Log($"[ScreenDefinitionEditor] Detected Resources path: {resourcesPath}");
                return resourcesPath;
            }

            string prefabName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            Debug.Log($"[ScreenDefinitionEditor] Using Addressables name: {prefabName}");
            return prefabName;
        }
    }
}

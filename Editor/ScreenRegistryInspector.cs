using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Tools.ScreenSystem.Editor
{
    [CustomEditor(typeof(ScreenRegistry))]
    public class ScreenRegistryInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("🔨 Rebuild Registry from Assets", GUILayout.Height(30)))
            {
                ScreenRegistry registry = target as ScreenRegistry;
                if (registry != null)
                    ScreenRegistryCacheBuilder.RebuildCache();
            }

            EditorGUILayout.HelpBox(
                "This scans all ScreenDefinition assets in the project and rebuilds the registry cache. Use this whenever you create or delete screen assets.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            ScreenRegistry reg = target as ScreenRegistry;
            if (reg != null)
                EditorGUILayout.LabelField($"Total Screens: {reg.Screens?.Count ?? 0}", EditorStyles.boldLabel);
        }
    }
}

using Tools.ScreenSystem.Scripts;
using UnityEditor;
using UnityEngine;

namespace Tools.ScreenSystem.Editor
{
    [CustomEditor(typeof(BlurBackgroundLayer))]
    public class BlurBackgroundLayerInspector : UnityEditor.Editor
    {
        private SerializedProperty blurStrengthProp;
        private SerializedProperty tintColorProp;
        private SerializedProperty fadeDurationProp;
        private SerializedProperty blurImageProp;
        private SerializedProperty canvasGroupProp;

        private void OnEnable()
        {
            blurStrengthProp = serializedObject.FindProperty("blurStrength");
            tintColorProp = serializedObject.FindProperty("tintColor");
            fadeDurationProp = serializedObject.FindProperty("fadeDuration");
            blurImageProp = serializedObject.FindProperty("blurImage");
            canvasGroupProp = serializedObject.FindProperty("canvasGroup");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "🎨 SHARED BLUR BACKGROUND (Real Gaussian Blur)\n\n" +
                "This is a SINGLE SHARED instance used for ALL modal screens.\n\n" +
                "• Place as child of ScreenSystem GameObject\n" +
                "• Name it 'BlurBackground' (exact name)\n" +
                "• Uses Screen Space - Camera for real blur effect\n" +
                "• Settings apply to all modals automatically",
                MessageType.Info);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Blur Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(blurStrengthProp, new GUIContent("Blur Strength", "Strength of Gaussian blur (0-10). Higher = more blur."));
            EditorGUILayout.PropertyField(tintColorProp, new GUIContent("Tint Color", "Semi-transparent overlay color applied over blur."));
            EditorGUILayout.PropertyField(fadeDurationProp, new GUIContent("Fade Duration", "Duration in seconds for fade in/out animation."));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(blurImageProp);
            EditorGUILayout.PropertyField(canvasGroupProp);

            EditorGUILayout.Space();

            BlurBackgroundLayer blurLayer = (BlurBackgroundLayer)target;

            if (GUILayout.Button("Test Show Blur"))
            {
                if (Application.isPlaying)
                    _ = blurLayer.ShowAsync();
                else
                    EditorUtility.DisplayDialog("Test Blur", "Enter Play Mode to test blur effect", "OK");
            }

            if (GUILayout.Button("Test Hide Blur"))
            {
                if (Application.isPlaying)
                    _ = blurLayer.HideAsync();
                else
                    EditorUtility.DisplayDialog("Test Blur", "Enter Play Mode to test blur effect", "OK");
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "📋 SETUP INSTRUCTIONS:\n\n" +
                "1. This GameObject should be a child of ScreenSystem\n" +
                "2. Name it exactly 'BlurBackground'\n" +
                "3. Canvas will auto-configure to Screen Space - Camera\n" +
                "4. Requires Main Camera tagged as 'MainCamera'\n" +
                "5. It will be automatically discovered by ScreenManager\n" +
                "6. All modals will share this blur instance\n\n" +
                "🌟 Real Gaussian blur using GrabPass!\n" +
                "✨ Changes here affect ALL modal screens!",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}


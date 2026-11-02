using UnityEditor;
using UnityEditor.SceneManagement;
using LayerDefinitionClass = ScreenSystem.LayerDefinition;

namespace ScreenSystem.Editor.LayerDefinitionEditor
{
    /// <summary>
    /// Main editor window for LayerDefinition ScriptableObject.
    /// Single responsibility: Orchestrate the inspector UI and event lifecycle.
    /// </summary>
    [CustomEditor(typeof(LayerDefinitionClass))]
    public class LayerDefinitionEditorWindow : UnityEditor.Editor
    {
        private LayerDefinitionView view;
        private LayerDefinitionPreviewManager previewManager;

        private void OnEnable()
        {
            previewManager = new LayerDefinitionPreviewManager();
            view = new LayerDefinitionView(serializedObject, previewManager);

            LayerDefinitionClass layerDef = (LayerDefinitionClass)target;
            if (layerDef.EnsureScreenSeparatorExists())
            {
                EditorUtility.SetDirty(layerDef);
                serializedObject.Update();
            }

            // Register event handlers
            EditorApplication.update -= UpdatePreviews;
            SceneView.duringSceneGui -= OnSceneGUI;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

            EditorApplication.update += UpdatePreviews;
            SceneView.duringSceneGui += OnSceneGUI;
            PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            // Unregister event handlers
            EditorApplication.update -= UpdatePreviews;
            SceneView.duringSceneGui -= OnSceneGUI;
            PrefabStage.prefabStageClosing -= OnPrefabStageClosing;
            PrefabStage.prefabStageOpened -= OnPrefabStageOpened;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        public override void OnInspectorGUI()
        {
            view.DrawInspector();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            LayerDefinitionClass layerDef = (LayerDefinitionClass)target;
            if (layerDef != null)
                previewManager.DrawSceneIndicators(layerDef);
        }

        private void UpdatePreviews()
        {
            LayerDefinitionClass layerDef = (LayerDefinitionClass)target;
            if (layerDef != null)
                previewManager.Update(layerDef);
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
                previewManager.Cleanup();
        }

        private void OnPrefabStageClosing(PrefabStage stage)
        {
            previewManager.Cleanup();
        }

        private void OnPrefabStageOpened(PrefabStage stage)
        {
            previewManager.Cleanup();
        }
    }
}

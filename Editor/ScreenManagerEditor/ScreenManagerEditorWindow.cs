using Tools.ScreenSystem.Scripts;
using UnityEditor;
using UnityEngine;

namespace Tools.ScreenSystem.Editor.ScreenManagerEditor
{
    public class ScreenManagerEditorWindow : EditorWindow
    {
        private const double REPAINT_INTERVAL = 0.5;

        private ScreenManager screenManager;
        private Vector2 windowScrollPosition;
        private double lastRepaintTime;

        private ScreenHierarchyPanel hierarchyPanel;
        private ScreenMetricsPanel metricsPanel;
        private GUIStyle headerStyle;

        [MenuItem("Tools/Screen System/Screen Manager Window")]
        public static void ShowWindow()
        {
            ScreenManagerEditorWindow window = GetWindow<ScreenManagerEditorWindow>("Screen Manager");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            hierarchyPanel = new ScreenHierarchyPanel();
            metricsPanel = new ScreenMetricsPanel();
            lastRepaintTime = EditorApplication.timeSinceStartup;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            screenManager = null;
            hierarchyPanel?.Reset();
            metricsPanel?.Reset();
            Repaint();
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("Enter Play Mode to see live Screen Manager data.", MessageType.Info);
                return;
            }

            if (screenManager == null)
                screenManager = ScreenManager.Instance;

            if (screenManager == null)
            {
                EditorGUILayout.Space(20);
                EditorGUILayout.HelpBox("No ScreenManager instance found in the scene.", MessageType.Warning);
                return;
            }

            windowScrollPosition = EditorGUILayout.BeginScrollView(windowScrollPosition);

            metricsPanel.DrawDebugToolsSection(screenManager);
            EditorGUILayout.Space(5);
            metricsPanel.DrawLegend();
            EditorGUILayout.Space(5);
            hierarchyPanel.DrawScreenStackSection(screenManager, screenManager.GetRegistryForEditor());
            EditorGUILayout.Space(10);
            DrawMetricsFoldout();

            EditorGUILayout.EndScrollView();

            ScheduleRepaint();
        }

        private void InitializeStyles()
        {
            if (headerStyle != null)
                return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            hierarchyPanel.Initialize(headerStyle);
            metricsPanel.Initialize();
        }

        private void DrawMetricsFoldout()
        {
            bool metricsFoldout = EditorGUILayout.Foldout(true, "PERFORMANCE METRICS", true, headerStyle);
            if (metricsFoldout)
                metricsPanel.DrawMetricsSection();
        }

        private void ScheduleRepaint()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastRepaintTime < REPAINT_INTERVAL)
                return;

            lastRepaintTime = currentTime;
            Repaint();
        }
    }
}


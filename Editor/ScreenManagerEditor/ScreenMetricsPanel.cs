using Tools.ScreenSystem.Scripts;
using UnityEditor;
using UnityEngine;

namespace Tools.ScreenSystem.Editor.ScreenManagerEditor
{
    /// <summary>
    /// Handles rendering of debug tools, legend, and performance metrics.
    /// Single responsibility: Display debug information and metrics only.
    /// </summary>
    public class ScreenMetricsPanel
    {
        private GUIStyle headerStyle;
        private bool metricsInitialized;

        public void Initialize()
        {
            if (metricsInitialized)
                return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            metricsInitialized = true;
        }

        public void DrawDebugToolsSection(ScreenManager screenManager)
        {
            EditorGUILayout.BeginVertical("box");

            DrawSectionHeader("DEBUG TOOLS");
            EditorGUILayout.Space(3);

            EditorGUILayout.EndVertical();
        }

        public void DrawLegend()
        {
            EditorGUILayout.BeginVertical("box");

            GUIStyle legendTitleStyle = new(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            EditorGUILayout.LabelField("Legend", legendTitleStyle);

            EditorGUILayout.BeginHorizontal();

            DrawLegendItem(new Color(1f, 0.8f, 0f), "\u25CF Active");
            DrawLegendItem(new Color(0.4f, 0.9f, 0.4f), "\u25CF Visible");
            DrawLegendItem(new Color(0.5f, 0.5f, 0.5f), "\u25CB Hidden");

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        public void DrawMetricsSection()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("PERFORMANCE METRICS", headerStyle);
            EditorGUILayout.Space(3);

            var allMetrics = ScreenMetrics.Instance.GetAllMetrics();

            if (allMetrics.Count > 0)
            {
                float totalAvg = 0f;
                int count = 0;

                foreach (ScreenMetrics.ScreenMetricEntry entry in allMetrics.Values)
                {
                    if (entry.LoadCount <= 0)
                        continue;

                    totalAvg += entry.AverageLoadTimeMs;
                    count++;
                }

                if (count > 0)
                {
                    EditorGUILayout.LabelField($"Overall Average Load Time: {totalAvg / count:F2}ms", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Total Screens Tracked: {allMetrics.Count}");
                }
            }

            if (GUILayout.Button("Clear Metrics", GUILayout.Height(25)))
                ScreenMetrics.Instance.Clear();

            EditorGUILayout.EndVertical();
        }

        public void DrawScreenPerformance(string screenId)
        {
            ScreenMetrics.ScreenMetricEntry metrics = ScreenMetrics.Instance.GetScreenMetrics(screenId);

            if (metrics == null || metrics.LoadCount <= 0)
                return;

            EditorGUILayout.LabelField(
                $"Load: {metrics.AverageLoadTimeMs:F1}ms avg, {metrics.LastLoadTimeMs:F1}ms last {(metrics.IsCached ? "(cached)" : "(cold)")}",
                EditorStyles.miniLabel);
        }

        public void Reset()
        {
            metricsInitialized = false;
        }

        private void DrawLegendItem(Color color, string label)
        {
            GUIStyle style = new(EditorStyles.miniLabel)
            {
                normal = { textColor = color },
                margin = new RectOffset(4, 8, 0, 0)
            };
            EditorGUILayout.LabelField(label, style, GUILayout.Width(90));
        }

        private void DrawSectionHeader(string label)
        {
            GUIStyle subHeaderStyle = new(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField(label, subHeaderStyle);
        }
    }
}


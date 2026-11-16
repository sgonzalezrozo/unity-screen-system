using ScreenSystem;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor.ScreenManagerEditor
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

        private void DrawSectionHeader(string label)
        {
            GUIStyle subHeaderStyle = new(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField(label, subHeaderStyle);
        }
    }
}


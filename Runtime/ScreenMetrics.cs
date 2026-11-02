using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenMetrics
    {
        private static ScreenMetrics instance;

        private Dictionary<string, ScreenMetricEntry> screenMetrics = new();
        private Dictionary<string, List<float>> loadTimeHistory = new();
        private Stopwatch currentOperation;
        private string currentOperationId;

        public static ScreenMetrics Instance
        {
            get
            {
                if (instance == null)
                    instance = new ScreenMetrics();
                return instance;
            }
        }

        public class ScreenMetricEntry
        {
            public string ScreenId;
            public string ScreenName;
            public DateTime FirstLoadTime;
            public DateTime LastAccessTime;
            public int LoadCount;
            public float LastLoadTimeMs;
            public float AverageLoadTimeMs;
            public float MinLoadTimeMs;
            public float MaxLoadTimeMs;
            public long MemoryBytes;
            public int LayerCount;
            public bool IsCached;
            public ScreenStatus Status;
        }

        public enum ScreenStatus
        {
            Unloaded,
            Loading,
            Loaded,
            Active,
            Hidden,
            Destroyed
        }

        public class OperationMetric
        {
            public string OperationName;
            public float DurationMs;
            public DateTime Timestamp;
            public Dictionary<string, object> Metadata;
        }

        private ScreenMetrics()
        {
            screenMetrics = new Dictionary<string, ScreenMetricEntry>();
            loadTimeHistory = new Dictionary<string, List<float>>();
        }

        public void BeginOperation(string operationId)
        {
            currentOperationId = operationId;
            currentOperation = Stopwatch.StartNew();
        }

        public float EndOperation(string screenId = null)
        {
            if (currentOperation == null)
            {
                UnityEngine.Debug.LogWarning("[ScreenMetrics] No operation in progress");
                return 0f;
            }

            currentOperation.Stop();
            float durationMs = (float)currentOperation.Elapsed.TotalMilliseconds;

            // UnityEngine.Debug.Log($"[ScreenMetrics] {currentOperationId}: {durationMs:F2}ms");

            if (!string.IsNullOrEmpty(screenId))
            {
                RecordLoadTime(screenId, durationMs);
            }

            currentOperation = null;
            currentOperationId = null;

            return durationMs;
        }

        public float EndOperationSilent(string screenId = null)
        {
            if (currentOperation == null)
                return 0f;

            currentOperation.Stop();
            float durationMs = (float)currentOperation.Elapsed.TotalMilliseconds;

            if (!string.IsNullOrEmpty(screenId))
            {
                RecordLoadTime(screenId, durationMs);
            }

            currentOperation = null;
            currentOperationId = null;

            return durationMs;
        }

        public void RecordScreenLoad(string screenId, string screenName, float loadTimeMs, int layerCount, bool fromCache)
        {
            if (!screenMetrics.ContainsKey(screenId))
            {
                screenMetrics[screenId] = new ScreenMetricEntry
                {
                    ScreenId = screenId,
                    ScreenName = screenName,
                    FirstLoadTime = DateTime.Now,
                    MinLoadTimeMs = float.MaxValue,
                    MaxLoadTimeMs = 0f,
                    LoadCount = 0
                };
            }

            ScreenMetricEntry entry = screenMetrics[screenId];
            entry.LastAccessTime = DateTime.Now;
            entry.LoadCount++;
            entry.LastLoadTimeMs = loadTimeMs;
            entry.LayerCount = layerCount;
            entry.IsCached = fromCache;
            entry.Status = ScreenStatus.Loaded;

            RecordLoadTime(screenId, loadTimeMs);

            entry.MinLoadTimeMs = Mathf.Min(entry.MinLoadTimeMs, loadTimeMs);
            entry.MaxLoadTimeMs = Mathf.Max(entry.MaxLoadTimeMs, loadTimeMs);

            if (loadTimeHistory.ContainsKey(screenId))
            {
                List<float> history = loadTimeHistory[screenId];
                float sum = 0f;
                foreach (float time in history)
                    sum += time;
                entry.AverageLoadTimeMs = sum / history.Count;
            }
        }

        public void UpdateScreenStatus(string screenId, ScreenStatus status)
        {
            if (screenMetrics.TryGetValue(screenId, out ScreenMetricEntry entry))
            {
                entry.Status = status;
                entry.LastAccessTime = DateTime.Now;
            }
        }

        public void RecordMemoryUsage(string screenId, long memoryBytes)
        {
            if (screenMetrics.TryGetValue(screenId, out ScreenMetricEntry entry))
            {
                entry.MemoryBytes = memoryBytes;
            }
        }

        public ScreenMetricEntry GetScreenMetrics(string screenId)
        {
            return screenMetrics.TryGetValue(screenId, out ScreenMetricEntry entry) ? entry : null;
        }

        public Dictionary<string, ScreenMetricEntry> GetAllMetrics()
        {
            return new Dictionary<string, ScreenMetricEntry>(screenMetrics);
        }

        public List<float> GetLoadTimeHistory(string screenId)
        {
            return loadTimeHistory.TryGetValue(screenId, out List<float> history)
                ? new List<float>(history)
                : new List<float>();
        }

        public void Clear()
        {
            screenMetrics.Clear();
            loadTimeHistory.Clear();
        }

        public string GenerateReport()
        {
            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== SCREEN SYSTEM PERFORMANCE REPORT ===");
            report.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Total Screens Tracked: {screenMetrics.Count}");
            report.AppendLine();

            if (screenMetrics.Count == 0)
            {
                report.AppendLine("No metrics recorded yet.");
                return report.ToString();
            }

            report.AppendLine("Screen Load Times:");
            report.AppendLine("─────────────────────────────────────────────────────────────");

            foreach (KeyValuePair<string, ScreenMetricEntry> kvp in screenMetrics)
            {
                ScreenMetricEntry entry = kvp.Value;
                report.AppendLine($"Screen: {entry.ScreenName} ({entry.ScreenId})");
                report.AppendLine($"  Status: {entry.Status}");
                report.AppendLine($"  Load Count: {entry.LoadCount}");
                report.AppendLine($"  Last Load: {entry.LastLoadTimeMs:F2}ms {(entry.IsCached ? "(cached)" : "(cold)")}");
                report.AppendLine($"  Average: {entry.AverageLoadTimeMs:F2}ms");
                report.AppendLine($"  Min: {entry.MinLoadTimeMs:F2}ms | Max: {entry.MaxLoadTimeMs:F2}ms");
                report.AppendLine($"  Layers: {entry.LayerCount}");
                report.AppendLine($"  Memory: {FormatBytes(entry.MemoryBytes)}");
                report.AppendLine($"  First Load: {entry.FirstLoadTime:HH:mm:ss}");
                report.AppendLine($"  Last Access: {entry.LastAccessTime:HH:mm:ss}");
                report.AppendLine();
            }

            float totalAverage = 0f;
            int count = 0;
            foreach (ScreenMetricEntry entry in screenMetrics.Values)
            {
                if (entry.LoadCount > 0)
                {
                    totalAverage += entry.AverageLoadTimeMs;
                    count++;
                }
            }

            if (count > 0)
            {
                report.AppendLine("─────────────────────────────────────────────────────────────");
                report.AppendLine($"Overall Average Load Time: {totalAverage / count:F2}ms");
            }

            return report.ToString();
        }

        public string ExportToJson()
        {
            List<ScreenMetricEntry> entries = new List<ScreenMetricEntry>(screenMetrics.Values);
            return JsonUtility.ToJson(new { metrics = entries }, prettyPrint: true);
        }

        private void RecordLoadTime(string screenId, float loadTimeMs)
        {
            if (!loadTimeHistory.ContainsKey(screenId))
            {
                loadTimeHistory[screenId] = new List<float>();
            }

            loadTimeHistory[screenId].Add(loadTimeMs);

            const int MAX_HISTORY = 20;
            if (loadTimeHistory[screenId].Count > MAX_HISTORY)
            {
                loadTimeHistory[screenId].RemoveAt(0);
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F2} KB";
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}


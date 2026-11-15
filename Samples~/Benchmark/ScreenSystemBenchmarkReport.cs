using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ScreenSystem.Benchmark
{
    [Serializable]
    public sealed class BenchmarkMetric
    {
        public string name;
        public double value;
        public string unit;
        public string aggregation;
        public int samples;
    }

    [Serializable]
    public sealed class BenchmarkSession
    {
        public string deviceModel;
        public string osVersion;
        public string unityVersion;
        public string gitCommit;
        public string buildMode;
        public string scriptingBackend;
        public bool isEditor;
        public bool validForPublishing;
    }

    [Serializable]
    public sealed class BenchmarkResult
    {
        public string tool = "screen-system";
        public string schemaVersion = "1.0.0";
        public string timestampUtc;
        public BenchmarkSession session;
        public List<string> fullscreenScreens = new();
        public List<string> modalScreens = new();
        public List<string> alertScreens = new();
        public List<string> persistentScreens = new();
        public List<string> scenes = new();
        public List<BenchmarkMetric> metrics = new();
        public List<string> notes = new();
    }

    /// <summary>Writes the result as JSON and a Markdown summary. Called after each phase, so an aborted run keeps what it measured.</summary>
    public sealed class ScreenSystemBenchmarkReport
    {
        private readonly string fileStem = $"screen-system-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

        public ScreenSystemBenchmarkReport()
        {
#if UNITY_EDITOR
            Folder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "ScreenSystemBenchmarks");
#else
            Folder = Path.Combine(Application.persistentDataPath, "ScreenSystemBenchmarks");
#endif
            Directory.CreateDirectory(Folder);
        }

        public string Folder { get; }

        public string JsonPath
            => Path.Combine(Folder, fileStem + ".json");

        public void Save(BenchmarkResult result)
        {
            result.timestampUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(JsonPath, JsonUtility.ToJson(result, true));
            File.WriteAllText(Path.Combine(Folder, fileStem + ".md"), ToMarkdown(result));
        }

        public static BenchmarkSession CreateSession(string commit)
        {
            bool isEditor = Application.isEditor;
            return new BenchmarkSession
            {
                deviceModel = SystemInfo.deviceModel,
                osVersion = SystemInfo.operatingSystem,
                unityVersion = Application.unityVersion,
                gitCommit = string.IsNullOrWhiteSpace(commit) ? "unknown" : commit.Trim(),
                buildMode = Debug.isDebugBuild ? "Development" : "Release",
#if ENABLE_IL2CPP
                scriptingBackend = "IL2CPP",
#else
                scriptingBackend = "Mono",
#endif
                isEditor = isEditor,
                validForPublishing = !isEditor && !string.IsNullOrWhiteSpace(commit)
            };
        }

        public static string ToMarkdown(BenchmarkResult result)
        {
            StringBuilder sb = new();
            sb.AppendLine("# Screen System benchmark");
            sb.AppendLine();
            BenchmarkSession s = result.session;
            sb.AppendLine($"- Commit: `{s.gitCommit}` - Unity {s.unityVersion} - {s.scriptingBackend} {s.buildMode}{(s.isEditor ? " (Editor)" : "")}");
            sb.AppendLine($"- Device: {s.deviceModel}, {s.osVersion}");
            sb.AppendLine($"- Valid for publishing: {(s.validForPublishing ? "yes" : "no")}");
            sb.AppendLine($"- Fullscreen: {string.Join(", ", result.fullscreenScreens)}; modals: {string.Join(", ", result.modalScreens)}; persistent: {string.Join(", ", result.persistentScreens)}; scenes: {string.Join(", ", result.scenes)}");
            sb.AppendLine();
            sb.AppendLine("| Metric | Value | Unit | Aggregation | Samples |");
            sb.AppendLine("|---|---:|---|---|---:|");
            foreach (BenchmarkMetric m in result.metrics)
                sb.AppendLine($"| {m.name} | {m.value:0.###} | {m.unit} | {m.aggregation} | {m.samples} |");

            if (result.notes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("## Notes");
                foreach (string note in result.notes)
                    sb.AppendLine($"- {note}");
            }

            return sb.ToString();
        }
    }
}

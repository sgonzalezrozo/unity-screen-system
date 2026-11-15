using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor.Profiling;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;

namespace ScreenSystem.Benchmark.Editor
{
    /// <summary>
    /// Reads the Profiler frames recorded during the warm pass and splits the main thread's GC allocations by
    /// marker, so it shows whether they come from the package (ScreenSystem.* markers), UI rebuilds or elsewhere.
    /// Editor allocations differ from IL2CPP, so use it to find where allocations happen, not how many bytes a device sees.
    /// </summary>
    internal static class ScreenSystemBenchmarkAllocationReport
    {
        public static string Write(string folder)
        {
            int first = ProfilerDriver.firstFrameIndex;
            int last = ProfilerDriver.lastFrameIndex;
            if (first < 0 || last < first)
            {
                Debug.LogWarning("[ScreenSystemBenchmark] No Profiler frames were recorded, so there's no allocation report.");
                return null;
            }

            Dictionary<string, double> selfByMarker = new();
            Dictionary<string, double> selfByParentAndMarker = new();
            double total = 0;
            int frames = 0;
            List<int> children = new();

            for (int frame = first; frame <= last; frame++)
            {
                using HierarchyFrameDataView view = ProfilerDriver.GetHierarchyFrameDataView(
                    frame, 0, HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnGcMemory, false);
                if (view == null || !view.valid)
                    continue;

                frames++;
                Walk(view, view.GetRootItemID(), "", children, selfByMarker, selfByParentAndMarker, ref total);
            }

            StringBuilder sb = new();
            sb.AppendLine("# Warm show allocations by marker (Editor)");
            sb.AppendLine();
            sb.AppendLine($"Frames analyzed: {frames}. Main thread GC allocations in those frames: {total / 1024:0.0} KB.");
            sb.AppendLine();
            sb.AppendLine("Self allocations: bytes allocated directly in a marker, not in its children.");
            sb.AppendLine();
            AppendTable(sb, "## By marker", selfByMarker, total);
            AppendTable(sb, "## By parent > marker", selfByParentAndMarker, total);

            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"allocations-{System.DateTime.UtcNow:yyyyMMdd-HHmmss}.md");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[ScreenSystemBenchmark] Allocation report: {path}");
            return path;
        }

        private static void Walk(HierarchyFrameDataView view, int id, string parentName, List<int> children,
            Dictionary<string, double> byMarker, Dictionary<string, double> byPair, ref double total)
        {
            string name = view.GetItemName(id);
            double itemTotal = view.GetItemColumnDataAsSingle(id, HierarchyFrameDataView.columnGcMemory);

            children.Clear();
            view.GetItemChildren(id, children);
            int[] childIds = children.ToArray();

            double childTotal = 0;
            foreach (int child in childIds)
                childTotal += view.GetItemColumnDataAsSingle(child, HierarchyFrameDataView.columnGcMemory);

            double self = itemTotal - childTotal;
            if (self > 0 && id != view.GetRootItemID())
            {
                total += self;
                byMarker.TryGetValue(name, out double marker);
                byMarker[name] = marker + self;
                string pair = $"{parentName} > {name}";
                byPair.TryGetValue(pair, out double pairTotal);
                byPair[pair] = pairTotal + self;
            }

            foreach (int child in childIds)
                Walk(view, child, name, children, byMarker, byPair, ref total);
        }

        private static void AppendTable(StringBuilder sb, string title, Dictionary<string, double> values, double total)
        {
            sb.AppendLine(title);
            sb.AppendLine();
            sb.AppendLine("| Marker | KB | Share |");
            sb.AppendLine("|---|---:|---:|");
            foreach (KeyValuePair<string, double> pair in values.OrderByDescending(p => p.Value).Take(30))
                sb.AppendLine($"| {pair.Key} | {pair.Value / 1024:0.0} | {(total > 0 ? pair.Value / total : 0):P1} |");
            sb.AppendLine();
        }
    }
}

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    public static class RenderTextureLeakInspector
    {
        private static readonly string[] EDITOR_RT_NAME_PREFIXES =
        {
            "ProfilerScreenshot",
            "GUIViewHDRRT",
            "GameView RT",
            "SceneView",
            "LDRSceneTarget",
            "UIR Dynamic Atlas",
            "EditorAtlasMonitorRT",
            "PreviewTexture",
            "Preview Camera",
            "AssetPreview",
            "SceneCamera",
            "Handles",
            "Inspector",
            "PlayModeView",
        };

        private static readonly HashSet<int> snapshotInstanceIds = new();
        private static int snapshotCount;
        private static long snapshotBytes;

        public static void DumpAll()
            => DumpAllInternal(false);

        public static void DumpAllBuildRelevant()
            => DumpAllInternal(true);

        private static void DumpAllInternal(bool filterEditor)
        {
            RenderTexture[] all = Resources.FindObjectsOfTypeAll<RenderTexture>();

            long totalBytes = 0;
            int shown = 0;
            int skipped = 0;
            StringBuilder sb = new();
            string suffix = filterEditor ? " (build-relevant only)" : "";
            sb.AppendLine($"[RTLeak] Live RenderTextures: {all.Length}{suffix}");

            for (int i = 0; i < all.Length; i++)
            {
                RenderTexture rt = all[i];
                if (rt == null)
                    continue;

                if (filterEditor && IsEditorOwned(rt))
                {
                    skipped++;
                    continue;
                }

                long bytes = EstimateBytes(rt);
                totalBytes += bytes;
                shown++;

                sb.AppendLine(FormatRenderTexture(rt, bytes));
            }

            if (filterEditor)
                sb.AppendLine($"[RTLeak] Shown: {shown}  Filtered editor RTs: {skipped}");
            sb.AppendLine($"[RTLeak] Estimated total: {FormatBytes(totalBytes)}");
            Debug.Log(sb.ToString());
        }

        public static void DumpGrouped()
            => DumpGroupedInternal(false);

        public static void DumpGroupedBuildRelevant()
            => DumpGroupedInternal(true);

        private static void DumpGroupedInternal(bool filterEditor)
        {
            RenderTexture[] all = Resources.FindObjectsOfTypeAll<RenderTexture>();
            Dictionary<string, int> countsByName = new();
            Dictionary<string, long> bytesByName = new();

            long totalBytes = 0;
            int skipped = 0;
            for (int i = 0; i < all.Length; i++)
            {
                RenderTexture rt = all[i];
                if (rt == null)
                    continue;

                if (filterEditor && IsEditorOwned(rt))
                {
                    skipped++;
                    continue;
                }

                string key = string.IsNullOrEmpty(rt.name) ? "<unnamed>" : rt.name;
                long bytes = EstimateBytes(rt);
                totalBytes += bytes;

                if (!countsByName.ContainsKey(key))
                {
                    countsByName[key] = 0;
                    bytesByName[key] = 0;
                }

                countsByName[key]++;
                bytesByName[key] += bytes;
            }

            List<KeyValuePair<string, int>> sorted = new(countsByName);
            sorted.Sort(CompareByCountDesc);

            StringBuilder sb = new();
            string suffix = filterEditor ? $" (build-relevant only, {skipped} editor RTs filtered)" : "";
            sb.AppendLine($"[RTLeak] Grouped RenderTextures: {all.Length - skipped} total, {FormatBytes(totalBytes)}{suffix}");
            sb.AppendLine("count | bytes | name");
            for (int i = 0; i < sorted.Count; i++)
            {
                string key = sorted[i].Key;
                sb.AppendLine($"{sorted[i].Value,5} | {FormatBytes(bytesByName[key]),10} | {key}");
            }

            Debug.Log(sb.ToString());
        }

        public static void TakeSnapshot()
        {
            snapshotInstanceIds.Clear();
            snapshotBytes = 0;

            RenderTexture[] all = Resources.FindObjectsOfTypeAll<RenderTexture>();
            for (int i = 0; i < all.Length; i++)
            {
                RenderTexture rt = all[i];
                if (rt == null)
                    continue;

                snapshotInstanceIds.Add(rt.GetInstanceID());
                snapshotBytes += EstimateBytes(rt);
            }

            snapshotCount = snapshotInstanceIds.Count;
            Debug.Log($"[RTLeak] Snapshot taken: {snapshotCount} RTs, {FormatBytes(snapshotBytes)}. " +
                     $"Now switch scenes and run 'Compare Against Snapshot'.");
        }

        public static void CompareAgainstSnapshot()
            => CompareAgainstSnapshotInternal(false);

        public static void CompareAgainstSnapshotBuildRelevant()
            => CompareAgainstSnapshotInternal(true);

        private static void CompareAgainstSnapshotInternal(bool filterEditor)
        {
            if (snapshotCount == 0)
            {
                Debug.LogWarning("[RTLeak] No snapshot taken yet. Use 'Take Snapshot' first.");
                return;
            }

            RenderTexture[] all = Resources.FindObjectsOfTypeAll<RenderTexture>();
            Dictionary<string, int> newByName = new();
            Dictionary<string, long> newBytesByName = new();

            long currentTotalBytes = 0;
            long newBytes = 0;
            int newCount = 0;
            int totalShown = 0;
            int editorSkipped = 0;

            for (int i = 0; i < all.Length; i++)
            {
                RenderTexture rt = all[i];
                if (rt == null)
                    continue;

                if (filterEditor && IsEditorOwned(rt))
                {
                    editorSkipped++;
                    continue;
                }

                long bytes = EstimateBytes(rt);
                currentTotalBytes += bytes;
                totalShown++;

                if (snapshotInstanceIds.Contains(rt.GetInstanceID()))
                    continue;

                newCount++;
                newBytes += bytes;

                string key = string.IsNullOrEmpty(rt.name) ? "<unnamed>" : rt.name;
                if (!newByName.ContainsKey(key))
                {
                    newByName[key] = 0;
                    newBytesByName[key] = 0;
                }
                newByName[key]++;
                newBytesByName[key] += bytes;
            }

            List<KeyValuePair<string, int>> sorted = new(newByName);
            sorted.Sort(CompareByCountDesc);

            StringBuilder sb = new();
            string suffix = filterEditor ? " (build-relevant only)" : "";
            sb.AppendLine($"[RTLeak] Snapshot was: {snapshotCount} RTs, {FormatBytes(snapshotBytes)}");
            sb.AppendLine($"[RTLeak] Current is:   {totalShown} RTs, {FormatBytes(currentTotalBytes)}{suffix}");
            if (filterEditor)
                sb.AppendLine($"[RTLeak] Filtered editor RTs: {editorSkipped}");
            sb.AppendLine($"[RTLeak] NEW since snapshot: {newCount} RTs, {FormatBytes(newBytes)}");
            sb.AppendLine("count | bytes | name (NEW RTs only)");
            for (int i = 0; i < sorted.Count; i++)
            {
                string key = sorted[i].Key;
                sb.AppendLine($"{sorted[i].Value,5} | {FormatBytes(newBytesByName[key]),10} | {key}");
            }

            if (newCount == 0)
                Debug.Log(sb.ToString());
            else
                Debug.LogWarning(sb.ToString());
        }

        public static void ForceUnload()
        {
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            Debug.Log("[RTLeak] Ran UnloadUnusedAssets + GC. Re-run 'Compare Against Snapshot' to see what actually leaked.");
        }

        private static int CompareByCountDesc(KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            => b.Value.CompareTo(a.Value);

        private static bool IsEditorOwned(RenderTexture rt)
        {
            if (rt == null)
                return true;

            string name = rt.name;
            if (string.IsNullOrEmpty(name))
                return false;

            for (int i = 0; i < EDITOR_RT_NAME_PREFIXES.Length; i++)
            {
                if (name.StartsWith(EDITOR_RT_NAME_PREFIXES[i]))
                    return true;
            }

            return false;
        }

        private static string FormatRenderTexture(RenderTexture rt, long bytes)
        {
            string name = string.IsNullOrEmpty(rt.name) ? "<unnamed>" : rt.name;
            string flags = (rt.hideFlags & HideFlags.DontSave) != 0 ? " [DontSave]" : "";
            return $"  {rt.width}x{rt.height} {rt.format} depth={rt.depth} msaa={rt.antiAliasing} " +
                   $"{FormatBytes(bytes)}{flags} name='{name}' id={rt.GetInstanceID()}";
        }

        private static long EstimateBytes(RenderTexture rt)
        {
            int bpp = BytesPerPixel(rt.format);
            long pixels = (long)rt.width * rt.height;
            long color = pixels * bpp * Mathf.Max(1, rt.antiAliasing);
            long depth = rt.depth > 0 ? pixels * (rt.depth / 8) : 0;
            return color + depth;
        }

        private static int BytesPerPixel(RenderTextureFormat format)
        {
            switch (format)
            {
                case RenderTextureFormat.R8:
                case RenderTextureFormat.R16:
                    return 1;
                case RenderTextureFormat.RG16:
                case RenderTextureFormat.RGB565:
                case RenderTextureFormat.ARGB4444:
                case RenderTextureFormat.ARGB1555:
                    return 2;
                case RenderTextureFormat.ARGB32:
                case RenderTextureFormat.BGRA32:
                case RenderTextureFormat.Default:
                case RenderTextureFormat.RGB111110Float:
                case RenderTextureFormat.RG32:
                case RenderTextureFormat.RFloat:
                    return 4;
                case RenderTextureFormat.ARGBHalf:
                case RenderTextureFormat.RGHalf:
                case RenderTextureFormat.RGFloat:
                    return 8;
                case RenderTextureFormat.ARGBFloat:
                    return 16;
                default:
                    return 4;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:0.0} KB";
            return $"{bytes / (1024f * 1024f):0.00} MB";
        }
    }
}

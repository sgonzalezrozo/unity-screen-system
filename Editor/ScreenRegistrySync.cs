using System.Collections.Generic;
using System.Linq;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Keeps the ScreenRegistry in step with the ScreenDefinition assets in the project. A definition created,
    /// deleted, moved or renamed updates the registry on its own, so Rebuild Registry Cache is only a repair tool.
    /// </summary>
    public class ScreenRegistrySync : AssetPostprocessor
    {
        private static bool syncQueued;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!TouchesAssets(imported) && !TouchesAssets(deleted) && !TouchesAssets(moved))
                return;

            if (syncQueued)
                return;

            // Runs once after the import finishes, however many assets changed.
            syncQueued = true;
            EditorApplication.delayCall += () =>
            {
                syncQueued = false;
                Sync(logChanges: true);
                ScreenIdsGenerator.WriteIfChanged();
            };
        }

        private static bool TouchesAssets(string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i].EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>Makes the registry list every ScreenDefinition in the project. Returns true if it changed.</summary>
        public static bool Sync(bool logChanges)
        {
            ScreenRegistry registry = ScreenSystemAssets.LoadRegistry();
            if (registry == null)
                return false;

            List<ScreenDefinition> found = new();
            foreach (string guid in AssetDatabase.FindAssets("t:ScreenDefinition"))
            {
                ScreenDefinition definition = AssetDatabase.LoadAssetAtPath<ScreenDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null)
                    found.Add(definition);
            }

            List<ScreenDefinition> merged = Merge(registry.Screens, found, out int added, out int removed);
            if (added == 0 && removed == 0)
            {
                WarnAboutDuplicates(registry.Screens);
                return false;
            }

            registry.Screens.Clear();
            registry.Screens.AddRange(merged);
            registry.ForceReinitialize();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssetIfDirty(registry);

            if (logChanges)
                Debug.Log($"[ScreenSystem] Registry updated: {added} added, {removed} removed, {merged.Count} screens.");

            WarnAboutDuplicates(merged);
            return true;
        }

        /// <summary>
        /// The registry's order is kept for screens that still exist; missing or deleted entries are dropped and
        /// new definitions are appended.
        /// </summary>
        public static List<ScreenDefinition> Merge(IList<ScreenDefinition> current, IEnumerable<ScreenDefinition> found, out int added, out int removed)
        {
            HashSet<ScreenDefinition> foundSet = new(found.Where(d => d != null));
            List<ScreenDefinition> merged = new();
            HashSet<ScreenDefinition> seen = new();
            removed = 0;

            foreach (ScreenDefinition definition in current)
            {
                if (definition == null || !foundSet.Contains(definition) || !seen.Add(definition))
                {
                    removed++;
                    continue;
                }

                merged.Add(definition);
            }

            added = 0;
            foreach (ScreenDefinition definition in foundSet)
            {
                if (seen.Add(definition))
                {
                    merged.Add(definition);
                    added++;
                }
            }

            return merged;
        }

        /// <summary>Screen names and IDs used by more than one screen. GoTo by a shared name opens the first one.</summary>
        public static List<string> FindDuplicates(IEnumerable<ScreenDefinition> screens)
        {
            List<string> problems = new();
            foreach (IGrouping<string, ScreenDefinition> group in screens.Where(s => s != null && !string.IsNullOrEmpty(s.ScreenName)).GroupBy(s => s.ScreenName))
            {
                if (group.Count() > 1)
                    problems.Add($"Screen name '{group.Key}' is used by {group.Count()} screens");
            }

            foreach (IGrouping<string, ScreenDefinition> group in screens.Where(s => s != null && !string.IsNullOrEmpty(s.ScreenId)).GroupBy(s => s.ScreenId))
            {
                if (group.Count() > 1)
                    problems.Add($"Screen ID '{group.Key}' is used by {group.Count()} screens");
            }

            return problems;
        }

        private static void WarnAboutDuplicates(IEnumerable<ScreenDefinition> screens)
        {
            foreach (string problem in FindDuplicates(screens))
                Debug.LogWarning($"[ScreenSystem] {problem}. Rename one so GoTo opens the right screen.");
        }
    }
}

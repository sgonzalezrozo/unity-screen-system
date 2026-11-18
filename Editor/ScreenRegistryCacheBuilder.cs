using System.Collections.Generic;
using System.Diagnostics;
using ScreenSystem;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace ScreenSystem.Editor
{
    public static class ScreenRegistryCacheBuilder
    {
        public static void RebuildCache()
        {
            ScreenRegistry registry = FindScreenRegistry();
            if (registry == null)
                return;

            string[] guids = AssetDatabase.FindAssets("t:ScreenDefinition");

            List<ScreenDefinition> loadedScreens = new();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                ScreenDefinition screen = AssetDatabase.LoadAssetAtPath<ScreenDefinition>(assetPath);

                if (screen != null)
                {
#if UNITY_EDITOR
                    screen.SyncWithPrefab();
#endif
                    loadedScreens.Add(screen);
                }
            }

            registry.Screens.Clear();
            registry.Screens.AddRange(loadedScreens);

            Stopwatch timer = Stopwatch.StartNew();
            registry.ForceReinitialize();
            timer.Stop();

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            string validationMessage = "";

            if (registry.Validate(out List<string> errors))
                validationMessage = "Registry is valid and has no errors.";
            else
                validationMessage = "Registry has errors:\n- " + string.Join("\n- ", errors);

            EditorUtility.DisplayDialog(
                "Registry Rebuilt",
                $"ScreenRegistry rebuilt successfully!\n\n" +
                $"Screens loaded: {loadedScreens.Count}\n" +
                $"Time: {timer.ElapsedMilliseconds}ms\n\n" +
                $"{validationMessage}\n\n" +
                $"All ScreenDefinition assets have been loaded into the registry.",
                "OK");

            Debug.Log($"[ScreenRegistryCacheBuilder] Success. Loaded {loadedScreens.Count} screens.");
        }

        private static ScreenRegistry FindScreenRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:ScreenRegistry");

            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "ScreenRegistry not found in project!", "OK");
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            ScreenRegistry registry = AssetDatabase.LoadAssetAtPath<ScreenRegistry>(path);

            if (registry == null)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load ScreenRegistry from {path}", "OK");
                return null;
            }

            return registry;
        }

    }
}

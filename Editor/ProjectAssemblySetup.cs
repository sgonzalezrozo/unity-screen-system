using System.IO;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Prepares the assembly a new screen or layer lives in, so code next to it compiles: the .asmdef that compiles the
    /// folder gets ScreenSystem and ToolsCore, and can see the generated ScreenIds. An .asmdef can't reference
    /// Assembly-CSharp, so ScreenIds in a folder with no .asmdef (the default, Assets/ScreenSystem) is moved next to
    /// that .asmdef; ScreenIds under another .asmdef gets a reference to it instead.
    /// </summary>
    internal static class ProjectAssemblySetup
    {
        private const string SCREEN_IDS_FILE = "ScreenIds.cs";

        public static void PrepareFolder(string folder)
        {
            AssemblyReferences.Ensure(folder, "ScreenSystem", "ToolsCore");
            MakeScreenIdsVisibleFrom(folder);
        }

        internal static void MakeScreenIdsVisibleFrom(string folder)
        {
            string folderAsmdef = AssemblyReferences.FindOwningAsmdef(folder);
            ScreenSystemSettings settings = ScreenSystemAssets.LoadSettings();
            if (folderAsmdef == null || settings == null || !settings.GenerateScreenIds)
                return;

            string idsPath = string.IsNullOrWhiteSpace(settings.ScreenIdsPath) ? ScreenSystemSettings.DefaultScreenIdsPath : settings.ScreenIdsPath;
            string idsAsmdef = AssemblyReferences.FindOwningAsmdef(Path.GetDirectoryName(idsPath));
            if (idsAsmdef == folderAsmdef)
                return;

            if (idsAsmdef != null)
            {
                AssemblyReferences.Ensure(folder, AssemblyReferences.ReadAssemblyName(File.ReadAllText(idsAsmdef)));
                return;
            }

            MoveScreenIds(settings, idsPath, $"{Path.GetDirectoryName(folderAsmdef).Replace('\\', '/')}/{SCREEN_IDS_FILE}");
        }

        private static void MoveScreenIds(ScreenSystemSettings settings, string from, string to)
        {
            if (File.Exists(from) && !File.Exists(to))
            {
                string error = AssetDatabase.MoveAsset(from, to);
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogWarning($"[ScreenSystem] Couldn't move {from} to {to}: {error}. Set Screen IDs Path in ScreenSystemSettings to a folder of that assembly.");
                    return;
                }
            }

            settings.ScreenIdsPath = to;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ScreenSystem] ScreenIds moved to {to}, so code under that assembly definition can use it (an .asmdef can't see Assembly-CSharp).");
        }
    }
}

using System.IO;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ScreenSystem.Editor
{
    /// <summary>Editor helpers shared by Setup, the screen wizard, the layer editor and the registry sync.</summary>
    public static class ScreenSystemAssets
    {
        public const string SettingsPath = "Assets/Resources/Tools/Screen System/Scriptables/ScreenSystemSettings.asset";
        public const string PanelSettingsPath = "Assets/Resources/Tools/Screen System/Prefabs/GlobalPanelSettings.asset";

        public static ScreenSystemSettings LoadSettings()
            => AssetDatabase.LoadAssetAtPath<ScreenSystemSettings>(SettingsPath);

        public static ScreenRegistry LoadRegistry()
        {
            ScreenSystemSettings settings = LoadSettings();
            if (settings != null && settings.ScreenRegistry != null)
                return settings.ScreenRegistry;

            string[] guids = AssetDatabase.FindAssets("t:ScreenRegistry");
            return guids.Length > 0 ? AssetDatabase.LoadAssetAtPath<ScreenRegistry>(AssetDatabase.GUIDToAssetPath(guids[0])) : null;
        }

        /// <summary>
        /// Returns the name Screen System loads a prefab by: its path inside a Resources folder, or its Addressables
        /// address. A prefab that is neither is marked Addressable, with its file name as the address.
        /// </summary>
        public static string EnsureLoadable(GameObject prefab)
        {
            if (prefab == null)
                return null;

            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string resourcesPath = ResourcesPath(assetPath);
            if (resourcesPath != null)
                return resourcesPath;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogWarning($"[ScreenSystem] Addressables isn't set up, so '{assetPath}' can't be loaded. Move it into a Resources folder or set up Addressables.");
                return null;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry != null)
                return entry.address;

            entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            string address = Path.GetFileNameWithoutExtension(assetPath);
            if (AddressTaken(settings, address, guid))
                Debug.LogWarning($"[ScreenSystem] Another Addressable already uses the address '{address}'. Rename '{assetPath}' so screens and layers load the right prefab.");

            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true);
            return address;
        }

        /// <summary>The path Resources.Load uses for an asset inside a Resources folder, or null.</summary>
        public static string ResourcesPath(string assetPath)
        {
            int index = assetPath.IndexOf("/Resources/", System.StringComparison.Ordinal);
            if (index < 0)
                return null;

            string relative = assetPath.Substring(index + "/Resources/".Length);
            return Path.ChangeExtension(relative, null).Replace('\\', '/');
        }

        private static bool AddressTaken(AddressableAssetSettings settings, string address, string guid)
        {
            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry entry in group.entries)
                {
                    if (entry.guid != guid && entry.address == address)
                        return true;
                }
            }

            return false;
        }
    }
}

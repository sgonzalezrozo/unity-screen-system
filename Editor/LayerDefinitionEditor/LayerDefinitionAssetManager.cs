using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Tools.ScreenSystem.Editor.LayerDefinitionEditor
{
    /// <summary>
    /// Manages asset and addressable operations for layer definitions.
    /// Single responsibility: Handle prefab lookup and addressable name resolution.
    /// </summary>
    public class LayerDefinitionAssetManager
    {
        /// <summary>
        /// Get the addressable name for a prefab, if it's registered in Addressables.
        /// </summary>
        public string GetAddressableNameForPrefab(GameObject prefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return null;

            AddressableAssetEntry entry = settings.FindAssetEntry(guid);
            if (entry != null)
                return entry.address;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry e in group.entries)
                {
                    if (e.guid == guid)
                        return e.address;
                }
            }

            return null;
        }

        /// <summary>
        /// Validate a prefab can be used (must be Addressable or in Resources folder).
        /// </summary>
        public bool ValidatePrefabCanBeAdded(GameObject prefab)
        {
            if (prefab == null)
                return false;

            string addressableName = GetAddressableNameForPrefab(prefab);
            if (!string.IsNullOrEmpty(addressableName))
                return true;

            string assetPath = AssetDatabase.GetAssetPath(prefab);
            return assetPath.Contains("/Resources/");
        }

        /// <summary>
        /// Get usable name for prefab (addressable name or prefab name).
        /// </summary>
        public string GetLayerNameForPrefab(GameObject prefab)
        {
            string addressableName = GetAddressableNameForPrefab(prefab);
            return !string.IsNullOrEmpty(addressableName) ? addressableName : prefab.name;
        }
    }
}


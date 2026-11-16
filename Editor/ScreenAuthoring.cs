using System;
using System.Collections.Generic;
using System.IO;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ScreenSystem.Editor
{
    /// <summary>Where a layer goes in a screen's draw order.</summary>
    public enum LayerPlacement
    {
        /// <summary>Behind everything: first in the list. A full background.</summary>
        Back,

        /// <summary>Right behind the screen prefab.</summary>
        BehindScreen,

        /// <summary>Right in front of the screen prefab.</summary>
        InFrontOfScreen,

        /// <summary>In front of everything: last in the list. A top bar or a navigation bar.</summary>
        Front
    }

    /// <summary>
    /// Builds screens from code, doing what the wizard and the LayerDefinition Inspector do. For editor scripts and
    /// for AI agents with Unity editor tools (an MCP server or <c>-executeMethod</c>):
    /// <code>
    /// ScreenDefinition shop = ScreenAuthoring.CreateScreen("Assets/Client/Shop", "ShopMain");
    /// GameObject topBar = ScreenAuthoring.CreateLayer("Assets/Client/Shared", "TopBar");
    /// ScreenAuthoring.AddLayer(shop, topBar, LayerPlacement.Front);
    /// </code>
    /// Prefabs that aren't in Resources are marked Addressable, and the registry updates itself.
    /// </summary>
    public static class ScreenAuthoring
    {
        public const string ScreenEntryName = "SCREEN";

        /// <summary>
        /// Creates a screen in <paramref name="featureDir"/>: <c>Feature/Screen/Screen.prefab</c> and
        /// <c>Feature/Services/Screen.asset</c>, loadable and registered. Returns the existing definition if the
        /// screen already exists.
        /// </summary>
        public static ScreenDefinition CreateScreen(string featureDir, string screenName, ScreenType type = ScreenType.fullscreen, UIFramework framework = UIFramework.uGUI, bool persistOverScenes = false)
        {
            featureDir = featureDir?.TrimEnd('/');
            string feature = Path.GetFileName(featureDir);
            ScreenDefinition created = FeatureCreatorWizard.CreateScreen(featureDir, feature, screenName, type, framework, persistOverScenes);
            return created != null
                ? created
                : AssetDatabase.LoadAssetAtPath<ScreenDefinition>($"{featureDir}/{FeatureCreatorWizard.ServicesFolder}/{screenName}.asset");
        }

        /// <summary>
        /// Creates a layer prefab at <c>folder/layerName.prefab</c> with its own Canvas (uGUI) or UIDocument (UI
        /// Toolkit), marked loadable. Returns the existing prefab if there's one at that path.
        /// </summary>
        public static GameObject CreateLayer(string folder, string layerName, UIFramework framework = UIFramework.uGUI)
        {
            folder = folder?.TrimEnd('/');
            string path = $"{folder}/{layerName}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                ScreenSystemAssets.EnsureLoadable(existing);
                return existing;
            }

            FeatureCreatorWizard.EnsureFolder(folder);
            GameObject root = new(layerName) { layer = LayerMask.NameToLayer("UI") };
            if (framework == UIFramework.UIToolkit)
                FeatureCreatorWizard.SetUpUIDocument(root, folder, layerName);
            else
                FeatureCreatorWizard.SetUpCanvas(root);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            ScreenSystemAssets.EnsureLoadable(prefab);
            return prefab;
        }

        /// <summary>
        /// Adds a layer to a screen's LayerDefinition at <paramref name="placement"/>. The layer prefab is made
        /// loadable first. Adding a layer the screen already lists does nothing and returns true.
        /// </summary>
        public static bool AddLayer(ScreenDefinition screen, GameObject layerPrefab, LayerPlacement placement = LayerPlacement.Front)
        {
            if (screen == null || layerPrefab == null)
            {
                Debug.LogError("[ScreenAuthoring] AddLayer needs a screen and a layer prefab.");
                return false;
            }

            string screenPath = FindScreenPrefabPath(screen);
            if (screenPath == null)
                return false;

            if (AssetDatabase.GetAssetPath(layerPrefab) == screenPath)
            {
                Debug.LogError($"[ScreenAuthoring] '{screen.ScreenName}' can't be a layer of itself.");
                return false;
            }

            string layerName = ScreenSystemAssets.EnsureLoadable(layerPrefab);
            if (string.IsNullOrEmpty(layerName))
                return false;

            return EditScreenLayers(screenPath, definition =>
            {
                if (IndexOf(definition, layerName) >= 0)
                    return false;

                definition.AddLayer(layerName);
                definition.MoveLayer(definition.LayerCount - 1, TargetIndex(definition, placement));
                return true;
            });
        }

        /// <summary>Adds a layer to the screen with that name or ID.</summary>
        public static bool AddLayer(string screen, GameObject layerPrefab, LayerPlacement placement = LayerPlacement.Front)
            => AddLayer(FindScreen(screen), layerPrefab, placement);

        /// <summary>Removes a layer from a screen's LayerDefinition. Returns false if the screen doesn't list it.</summary>
        public static bool RemoveLayer(ScreenDefinition screen, string layerName)
        {
            string screenPath = FindScreenPrefabPath(screen);
            if (screenPath == null || string.IsNullOrEmpty(layerName) || layerName == ScreenEntryName)
                return false;

            bool removed = false;
            EditScreenLayers(screenPath, definition =>
            {
                int index = IndexOf(definition, layerName);
                if (index < 0)
                    return false;

                definition.RemoveLayer(index);
                removed = true;
                return true;
            });

            return removed;
        }

        /// <summary>
        /// The screen's layers from back to front, with <see cref="ScreenEntryName"/> where the screen prefab draws.
        /// Empty if the screen has no LayerDefinition.
        /// </summary>
        public static List<string> GetLayers(ScreenDefinition screen)
        {
            List<string> names = new();
            string screenPath = FindScreenPrefabPath(screen);
            GameObject prefab = screenPath != null ? AssetDatabase.LoadAssetAtPath<GameObject>(screenPath) : null;
            LayerDefinition definition = prefab != null ? prefab.GetComponent<LayerDefinition>() : null;
            if (definition == null)
                return names;

            for (int i = 0; i < definition.LayerCount; i++)
            {
                LayerEntry entry = definition.GetLayer(i);
                names.Add(entry.isScreenSeparator ? ScreenEntryName : entry.layerName);
            }

            if (!names.Contains(ScreenEntryName))
                names.Insert(Mathf.Clamp(definition.screenSeparatorIndex, 0, names.Count), ScreenEntryName);

            return names;
        }

        /// <summary>The ScreenDefinition with that name or ID, from the registry or the project.</summary>
        public static ScreenDefinition FindScreen(string nameOrId)
        {
            if (string.IsNullOrEmpty(nameOrId))
                return null;

            ScreenRegistry registry = ScreenSystemAssets.LoadRegistry();
            if (registry != null)
            {
                foreach (ScreenDefinition screen in registry.Screens)
                {
                    if (screen != null && (screen.ScreenName == nameOrId || screen.ScreenId == nameOrId))
                        return screen;
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:ScreenDefinition"))
            {
                ScreenDefinition screen = AssetDatabase.LoadAssetAtPath<ScreenDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (screen != null && (screen.ScreenName == nameOrId || screen.ScreenId == nameOrId))
                    return screen;
            }

            Debug.LogError($"[ScreenAuthoring] No screen named '{nameOrId}'.");
            return null;
        }

        /// <summary>The asset path of the prefab a ScreenDefinition loads, from its Addressables address or Resources path.</summary>
        public static string FindScreenPrefabPath(ScreenDefinition screen)
        {
            if (screen == null || string.IsNullOrEmpty(screen.AssetPath))
            {
                Debug.LogError($"[ScreenAuthoring] '{screen?.name}' has no asset path. Drag its prefab onto the ScreenDefinition.");
                return null;
            }

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings != null)
            {
                foreach (AddressableAssetGroup group in settings.groups)
                {
                    if (group == null)
                        continue;

                    foreach (AddressableAssetEntry entry in group.entries)
                    {
                        if (entry.address == screen.AssetPath && entry.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                            return entry.AssetPath;
                    }
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (ScreenSystemAssets.ResourcesPath(path) == screen.AssetPath)
                    return path;
            }

            Debug.LogError($"[ScreenAuthoring] No prefab is loadable as '{screen.AssetPath}' for screen '{screen.ScreenName}'.");
            return null;
        }

        // -executeMethod entry points, for agents without an editor connection:
        //   Unity -batchmode -quit -projectPath . -executeMethod ScreenSystem.Editor.ScreenAuthoring.CreateScreenFromCommandLine -feature Assets/Client/Shop -screen ShopMain [-type modal] [-framework UIToolkit]
        //   ... ScreenSystem.Editor.ScreenAuthoring.CreateLayerFromCommandLine -folder Assets/Client/Shared -layer TopBar [-framework UIToolkit]
        //   ... ScreenSystem.Editor.ScreenAuthoring.AddLayerFromCommandLine -screen ShopMain -layer Assets/Client/Shared/TopBar.prefab [-placement Front]
        public static void CreateScreenFromCommandLine()
        {
            ScreenType type = ParseEnum(Argument("-type"), ScreenType.fullscreen);
            UIFramework framework = ParseEnum(Argument("-framework"), UIFramework.uGUI);
            ExitWith(CreateScreen(Argument("-feature"), Argument("-screen"), type, framework) != null);
        }

        public static void CreateLayerFromCommandLine()
        {
            UIFramework framework = ParseEnum(Argument("-framework"), UIFramework.uGUI);
            ExitWith(CreateLayer(Argument("-folder"), Argument("-layer"), framework) != null);
        }

        public static void AddLayerFromCommandLine()
        {
            GameObject layer = AssetDatabase.LoadAssetAtPath<GameObject>(Argument("-layer") ?? "");
            LayerPlacement placement = ParseEnum(Argument("-placement"), LayerPlacement.Front);
            ExitWith(AddLayer(Argument("-screen"), layer, placement));
        }

        private static bool EditScreenLayers(string screenPath, Func<LayerDefinition, bool> edit)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(screenPath);
            try
            {
                LayerDefinition definition = root.GetComponent<LayerDefinition>();
                if (definition == null)
                    definition = root.AddComponent<LayerDefinition>();

                bool addedSeparator = definition.EnsureScreenSeparatorExists();
                bool changed = edit(definition) | addedSeparator;
                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, screenPath);

                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static int TargetIndex(LayerDefinition definition, LayerPlacement placement)
        {
            int last = definition.LayerCount - 1;
            int separator = IndexOf(definition, null);
            return placement switch
            {
                LayerPlacement.Back => 0,
                LayerPlacement.BehindScreen => separator,
                LayerPlacement.InFrontOfScreen => separator + 1,
                _ => last
            };
        }

        // The index of the layer with that name, or of the SCREEN entry when name is null. -1 if missing.
        private static int IndexOf(LayerDefinition definition, string layerName)
        {
            for (int i = 0; i < definition.LayerCount; i++)
            {
                LayerEntry entry = definition.GetLayer(i);
                if (layerName == null ? entry.isScreenSeparator : !entry.isScreenSeparator && entry.layerName == layerName)
                    return i;
            }

            return -1;
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }

            return null;
        }

        private static T ParseEnum<T>(string value, T fallback) where T : struct
            => !string.IsNullOrEmpty(value) && Enum.TryParse(value, true, out T parsed) ? parsed : fallback;

        private static void ExitWith(bool success)
        {
            AssetDatabase.SaveAssets();
            if (Application.isBatchMode)
                EditorApplication.Exit(success ? 0 : 1);
        }
    }
}

using System.IO;
using ScreenSystem;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;

namespace ScreenSystem.Editor
{
    public static class ScreenSystemSetup
    {
        private const string RESOURCES_PATH = "Assets/Resources/Tools/Screen System";
        private const string SCRIPTABLES_PATH = "Assets/Resources/Tools/Screen System/Scriptables";
        private const string PREFABS_PATH = "Assets/Resources/Tools/Screen System/Prefabs";
        private const string SETTINGS_FILE = "ScreenSystemSettings.asset";
        private const string REGISTRY_FILE = "ScreenRegistry.asset";

        [MenuItem("Tools/Screen System/Setup", priority = 1)]
        public static void SetupProject()
        {
            Debug.Log("[ScreenSystem Setup] Starting ScreenSystem setup...");
            SetupCommonFiles();

            // An existing prefab is kept, but gets the modal backdrop if it's missing or outdated.
            if (!CreateScreenSystemPrefab_UIToolkit())
                ScreenSystemBackdropBuilder.EnsureInPrefab();
            CreateAddressablesLabel();
            AssetDatabase.Refresh();
            ShowCompletionDialog("UIToolkit");
        }

        private static void SetupCommonFiles()
        {
            // Create Resources folder structure
            CreateFolderStructure();

            // Create or move settings
            CreateOrMoveSettings();

            // Create or move registry
            CreateOrMoveRegistry();

            // Validate the setup completed successfully
            ValidateSetup();
        }

         private static void ShowCompletionDialog(string framework)
         {
             EditorUtility.DisplayDialog(
                 "Setup Complete!",
                 $"Screen System {framework} has been set up successfully!\n\n" +
                 "Files created in:\nAssets/Resources/Tools/Screen System/Scriptables/\n\n" +
                 "- ScreenSystem.prefab created in Prefabs folder\n" +
                 "- ScreenRegistry linked to ScreenSystemSettings\n\n" +
                 "Next steps:\n" +
                 "1. The ScreenSystem prefab loads automatically\n" +
                 "2. Create layer prefabs with required components:\n" +
                 (framework == "uGUI" ? "   - Canvas + GraphicRaycaster + RectTransform + LayerComponent\n" : "   - UIDocument + PanelSettings + LayerComponent\n") +
                 "3. Create your first feature: Assets > Screen System > New Screen Feature\n" +
                 "4. Set Framework in ScreenDefinition assets",
                 "OK");

             Debug.Log($"[ScreenSystem Setup] Setup complete for {framework}! Files created in: Assets/Resources/Tools/Screen System/");

             // Select settings file
             ScreenSystemSettings settings = AssetDatabase.LoadAssetAtPath<ScreenSystemSettings>($"{SCRIPTABLES_PATH}/{SETTINGS_FILE}");
             if (settings != null)
                 Selection.activeObject = settings;
         }

        private static void CreateFolderStructure()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Tools");
            EnsureFolder("Assets/Resources/Tools", "Screen System");
            EnsureFolder(RESOURCES_PATH, "Scriptables");
            EnsureFolder(RESOURCES_PATH, "Prefabs");
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = $"{parent}/{folderName}";

            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void CreateOrMoveSettings()
        {
            string targetPath = $"{SCRIPTABLES_PATH}/{SETTINGS_FILE}";

            // Check if settings already exist
            ScreenSystemSettings existingSettings = AssetDatabase.LoadAssetAtPath<ScreenSystemSettings>(targetPath);

            if (existingSettings == null)
            {
                // Search for existing settings elsewhere
                string[] guids = AssetDatabase.FindAssets("t:ScreenSystemSettings");

                if (guids.Length > 0)
                {
                    string oldPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    string error = AssetDatabase.MoveAsset(oldPath, targetPath);

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.LogWarning($"[ScreenSystem Setup] Could not move settings: {error}");
                        CreateNewSettings(targetPath);
                    }
                }
                else
                    CreateNewSettings(targetPath);
            }
        }

        private static void CreateNewSettings(string path)
        {
            ScreenSystemSettings settings = ScriptableObject.CreateInstance<ScreenSystemSettings>();
            settings.DefaultLogLevel = LogLevel.Info;

            AssetDatabase.CreateAsset(settings, path);
        }

        private static void CreateOrMoveRegistry()
        {
            string targetPath = $"{SCRIPTABLES_PATH}/{REGISTRY_FILE}";

            // Check if registry already exists at target
            ScreenRegistry existingRegistry = AssetDatabase.LoadAssetAtPath<ScreenRegistry>(targetPath);

            if (existingRegistry == null)
            {
                // Search for existing registry
                string[] guids = AssetDatabase.FindAssets("t:ScreenRegistry");

                if (guids.Length > 0)
                {
                    string oldPath = AssetDatabase.GUIDToAssetPath(guids[0]);

                    if (oldPath != targetPath)
                    {
                        string error = AssetDatabase.MoveAsset(oldPath, targetPath);

                        if (!string.IsNullOrEmpty(error))
                        {
                            Debug.LogWarning($"[ScreenSystem Setup] Could not move registry: {error}");
                            CreateNewRegistry(targetPath);
                        }
                    }
                }
                else
                    CreateNewRegistry(targetPath);
            }

            // Link to settings
            LinkRegistryToSettings(targetPath);
        }

        private static void CreateNewRegistry(string path)
        {
            ScreenRegistry registry = ScriptableObject.CreateInstance<ScreenRegistry>();
            AssetDatabase.CreateAsset(registry, path);
        }

        private static void LinkRegistryToSettings(string registryPath)
        {
            ScreenSystemSettings settings = AssetDatabase.LoadAssetAtPath<ScreenSystemSettings>($"{SCRIPTABLES_PATH}/{SETTINGS_FILE}");
            ScreenRegistry registry = AssetDatabase.LoadAssetAtPath<ScreenRegistry>(registryPath);

            if (settings != null && registry != null && settings.ScreenRegistry != registry)
            {
                settings.ScreenRegistry = registry;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }
        }

        private static void ValidateSetup()
        {
            ScreenSystemSettings settings = AssetDatabase.LoadAssetAtPath<ScreenSystemSettings>($"{SCRIPTABLES_PATH}/{SETTINGS_FILE}");
            ScreenRegistry registry = AssetDatabase.LoadAssetAtPath<ScreenRegistry>($"{SCRIPTABLES_PATH}/{REGISTRY_FILE}");

            bool isValid = ScreenSystemSettings.IsValid();

            if (!isValid)
            {
                if (settings == null)
                    Debug.LogError("[ScreenSystem Setup] ScreenSystemSettings not found after setup!");
                else if (registry == null)
                    Debug.LogError("[ScreenSystem Setup] ScreenRegistry not found after setup!");
                else if (settings.ScreenRegistry == null)
                    Debug.LogError("[ScreenSystem Setup] ScreenRegistry field not linked to ScreenSystemSettings!");
            }
            else
            {
                Debug.Log($"[ScreenSystem Setup] Validation passed:\n" +
                         $"  • ScreenSystemSettings: {settings.name}\n" +
                         $"  • ScreenRegistry linked: {registry.name}\n" +
                         $"  • System ready to use!");
            }
        }

        private static bool CreateScreenSystemPrefab_UIToolkit()
        {
            string prefabPath = $"{PREFABS_PATH}/ScreenSystem.prefab";

            if (File.Exists(prefabPath))
            {
                Debug.LogWarning("[ScreenSystem Setup] ScreenSystem.prefab already exists. Delete it to regenerate with updated settings.");
                return false;
            }

            // Create root GameObject with ScreenManager
            GameObject root = new GameObject("ScreenSystem");
            root.AddComponent<ScreenManager>();

            // Create UICamera child
            GameObject uiCameraObj = new GameObject("UICamera");
            uiCameraObj.transform.SetParent(root.transform);
            Camera uiCamera = uiCameraObj.AddComponent<Camera>();

            // Configure camera for UIToolkit rendering
            // CRITICAL: Use Depth flag, not SolidColor - allows UIToolkit to render on top
            uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.backgroundColor = Color.clear;
            uiCamera.cullingMask = LayerMask.GetMask("UI");
            uiCamera.orthographic = true;
            uiCamera.orthographicSize = 5f;
            uiCamera.depth = 10;
            uiCamera.nearClipPlane = 0.3f;
             uiCamera.farClipPlane = 1000f;

             // NOTE: ShellTemplate has been removed. Layer prefabs now contain all components directly.
             // Each UIToolkit layer prefab must include: UIDocument + PanelSettings + LayerComponent
             // Create DarkOverlay for UIToolkit (instead of blur)
            CreateDarkOverlayForUIToolkit(root);

            // The blurred snapshot and the dim behind modals and alerts.
            ScreenSystemBackdropBuilder.Build(root.transform);

            // Create Global PanelSettings for UIToolkit
            CreateGlobalPanelSettings();

             // Save as prefab
             GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);

             if (prefabAsset != null)
             {
                 LinkUICameraToSettings(prefabAsset);
                 Debug.Log("[ScreenSystem Setup] ScreenSystem.prefab created (no shell templates)");
                 Debug.Log("[ScreenSystem Setup] Layer prefabs must include all components: UIDocument + PanelSettings + LayerComponent");
             }

            Object.DestroyImmediate(root);

            return true;
        }

         private static void CreateDarkOverlayForUIToolkit(GameObject parent)
         {
             GameObject overlayObj = new GameObject("DarkOverlay");
             overlayObj.transform.SetParent(parent.transform);
             // Do NOT set UI layer - would auto-add Canvas

             // DarkOverlay for UIToolkit is a bare container (no components)
             // Individual UIToolkit layer prefabs handle their own overlay/modal presentation

             overlayObj.SetActive(false);

             Debug.Log("[ScreenSystem Setup] DarkOverlay created for UIToolkit (bare Transform, inactive)");
         }

        private static void CreateGlobalPanelSettings()
        {
            string panelSettingsPath = $"{PREFABS_PATH}/GlobalPanelSettings.asset";

            // Check if GlobalPanelSettings already exists
            if (File.Exists(panelSettingsPath))
            {
                Debug.Log("[ScreenSystem Setup] GlobalPanelSettings already exists at: " + panelSettingsPath);
                return;
            }

            try
            {
                // Create a new PanelSettings asset
                PanelSettings panelSettings = ScriptableObject.CreateInstance<PanelSettings>();

                if (panelSettings == null)
                {
                    Debug.LogError("[ScreenSystem Setup] Failed to create PanelSettings instance");
                    return;
                }

                // Configure PanelSettings for mobile UI
                panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
                panelSettings.scale = 1f;
                panelSettings.referenceDpi = 96;

                // Save as asset
                AssetDatabase.CreateAsset(panelSettings, panelSettingsPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[ScreenSystem Setup] GlobalPanelSettings created at: {panelSettingsPath}");
                Debug.Log("[ScreenSystem Setup] Assign this to all UIDocument components in UIToolkit screens");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ScreenSystem Setup] Failed to create GlobalPanelSettings: {ex.Message}");
            }
        }

        private static void LinkUICameraToSettings(GameObject prefabAsset)
        {
            ScreenSystemSettings settings = AssetDatabase.LoadAssetAtPath<ScreenSystemSettings>($"{SCRIPTABLES_PATH}/{SETTINGS_FILE}");
            if (settings != null)
            {
                // Load the prefab contents to find UICamera
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefabAsset));

                Transform uiCameraTransform = prefabContents.transform.Find("UICamera");
                if (uiCameraTransform != null)
                {
                    Camera uiCamera = uiCameraTransform.GetComponent<Camera>();
                    if (uiCamera != null)
                    {
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                        Debug.Log("[ScreenSystem Setup] UICamera linked to ScreenSystemSettings");
                    }
                    else
                        Debug.LogError("[ScreenSystem Setup] UICamera GameObject found but missing Camera component!");
                }
                else
                    Debug.LogError("[ScreenSystem Setup] UICamera not found in ScreenSystem prefab!");

                settings.BlurStrength = 8.0f;
                settings.BlurTintColor = new Color(0f, 0f, 0f, 0.4f);
                settings.BlurFadeDuration = 0.2f;

                PrefabUtility.UnloadPrefabContents(prefabContents);
            }
        }

        private static void CreateAddressablesLabel()
        {
            try
            {
                // Use reflection to avoid compile dependency on Addressables
                System.Type settingsType = System.Type.GetType("UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject, Unity.Addressables.Editor");

                if (settingsType == null)
                {
                    Debug.LogWarning("[ScreenSystem Setup] Addressables not found. Label 'ScreenContent' will not be created. Install Addressables package for better content management.");
                    return;
                }

                System.Reflection.PropertyInfo settingsProperty = settingsType.GetProperty("Settings");
                object addressableSettings = settingsProperty?.GetValue(null);

                if (addressableSettings == null)
                {
                    Debug.LogWarning("[ScreenSystem Setup] Addressables Settings not initialized. Please configure Addressables in your project.");
                    return;
                }

                // Get labels collection
                System.Reflection.MethodInfo getLabelTableMethod = addressableSettings.GetType().GetMethod("GetLabels");
                if (getLabelTableMethod == null)
                {
                    Debug.LogWarning("[ScreenSystem Setup] Could not access Addressables labels.");
                    return;
                }

                object labels = getLabelTableMethod.Invoke(addressableSettings, null);
                if (labels == null)
                {
                    Debug.LogWarning("[ScreenSystem Setup] Addressables labels collection is null.");
                    return;
                }

                // Check if label already exists
                System.Reflection.MethodInfo containsMethod = labels.GetType().GetMethod("Contains", new[] { typeof(string) });
                if (containsMethod != null)
                {
                    bool hasLabel = (bool)(containsMethod.Invoke(labels, new object[] { "ScreenContent" }) ?? false);

                    if (hasLabel)
                    {
                        Debug.Log("[ScreenSystem Setup] Addressables label 'ScreenContent' already exists");
                        return;
                    }
                }

                // Add label
                System.Reflection.MethodInfo addLabelMethod = addressableSettings.GetType().GetMethod("AddLabel", new[] { typeof(string), typeof(bool) });
                if (addLabelMethod != null)
                {
                    addLabelMethod.Invoke(addressableSettings, new object[] { "ScreenContent", true });
                    Debug.Log("[ScreenSystem Setup] Created Addressables label 'ScreenContent'");
                }
                else
                    Debug.LogWarning("[ScreenSystem Setup] Could not add Addressables label. You may need to add 'ScreenContent' label manually.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ScreenSystem Setup] Could not create Addressables label: {ex.Message}\nYou can add 'ScreenContent' label manually in Addressables settings.");
            }
        }

        private static void RemoveComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                // Use force=true to ignore dependencies and remove immediately
                Object.DestroyImmediate(component, true);
                Debug.Log($"[ScreenSystem Setup] Removed {typeof(T).Name} from {gameObject.name}");
            }
        }
    }
}

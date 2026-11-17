using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Creates the two screens Screen System handles by name: the splash, shown while the game boots, and the quit
    /// confirmation, opened by back at the root. They're plain uGUI starting points, meant to be redesigned. A screen
    /// that already exists, or whose ID is empty on the ScreenManager, is left alone, so running it again is safe.
    /// </summary>
    public static class SpecialScreensCreator
    {
        public const string DefaultFeatureFolder = "Assets/Boot";
        public const string DefaultSplashId = "SplashMain";
        public const string DefaultQuitConfirmationId = "QuitConfirmation";

        private const string ScreenSystemPrefabPath = "Assets/Resources/Tools/Screen System/Prefabs/ScreenSystem.prefab";
        private const string BuiltinFont = "LegacyRuntime.ttf";
        private static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
        private static readonly Color Background = new(0.08f, 0.09f, 0.11f, 1f);
        private static readonly Color Panel = new(0.15f, 0.16f, 0.19f, 1f);
        private static readonly Color ButtonColor = new(0.26f, 0.28f, 0.33f, 1f);
        private static readonly Color ConfirmColor = new(0.78f, 0.27f, 0.25f, 1f);

        [MenuItem("Tools/Screen System/Create Special Screens", priority = 2)]
        public static void CreateFromMenu()
        {
            List<string> created = Create();
            string message = created.Count == 0
                ? "The splash and quit confirmation screens already exist."
                : $"Created {string.Join(" and ", created)} in {DefaultFeatureFolder}. Redesign them as you like; keep their names, or change the IDs on the ScreenManager.";
            EditorUtility.DisplayDialog("Screen System", message, "OK");
        }

        /// <summary>The names of the special screens that don't exist yet. Empty when both exist or are turned off.</summary>
        public static List<string> Missing()
        {
            List<string> missing = new();
            ReadIds(out string splashId, out string quitId);
            if (!string.IsNullOrEmpty(splashId) && !Exists(splashId))
                missing.Add(splashId);
            if (!string.IsNullOrEmpty(quitId) && !Exists(quitId))
                missing.Add(quitId);
            return missing;
        }

        /// <summary>Creates the special screens that are missing in <paramref name="featureFolder"/>. Returns the names it created.</summary>
        public static List<string> Create(string featureFolder = DefaultFeatureFolder)
        {
            ReadIds(out string splashId, out string quitId);
            return Create(featureFolder, splashId, quitId);
        }

        /// <summary>Creates the named screens that are missing; an empty name is skipped. Returns the names it created.</summary>
        public static List<string> Create(string featureFolder, string splashId, string quitId)
        {
            List<string> created = new();

            if (!string.IsNullOrEmpty(splashId) && !Exists(splashId) && CreateSplash(featureFolder, splashId))
                created.Add(splashId);

            if (!string.IsNullOrEmpty(quitId) && !Exists(quitId) && CreateQuitConfirmation(featureFolder, quitId))
                created.Add(quitId);

            if (created.Count > 0)
            {
                AssetDatabase.SaveAssets();
                ScreenRegistrySync.Sync(logChanges: false);
                Debug.Log($"[ScreenSystem] Created {string.Join(", ", created)} in {featureFolder}.");
            }

            return created;
        }

        // The IDs set on the ScreenSystem prefab's ScreenManager, or the defaults when there's no prefab yet.
        internal static void ReadIds(out string splashId, out string quitId)
        {
            splashId = DefaultSplashId;
            quitId = DefaultQuitConfirmationId;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenSystemPrefabPath);
            ScreenManager manager = prefab != null ? prefab.GetComponentInChildren<ScreenManager>(true) : null;
            if (manager == null)
                return;

            SerializedObject serialized = new(manager);
            splashId = serialized.FindProperty("splashScreenId")?.stringValue ?? splashId;
            quitId = serialized.FindProperty("quitConfirmationModalId")?.stringValue ?? quitId;
        }

        internal static bool Exists(string nameOrId)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ScreenDefinition"))
            {
                ScreenDefinition definition = AssetDatabase.LoadAssetAtPath<ScreenDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null && (definition.ScreenName == nameOrId || definition.ScreenId == nameOrId))
                    return true;
            }

            return false;
        }

        private static bool CreateSplash(string featureFolder, string screenName)
        {
            ScreenDefinition definition = ScreenAuthoring.CreateScreen(featureFolder, screenName, ScreenType.Fullscreen);
            if (definition == null)
                return false;

            // It shows over anything while the game boots.
            definition.IgnoreNavigationRules = true;
            EditorUtility.SetDirty(definition);

            return EditPrefab(definition, root =>
            {
                AddScaler(root);
                Stretch(AddImage(root.transform, "Background", Background).rectTransform);
                Text title = AddText(root.transform, "Title", PlayerSettings.productName, 72);
                Stretch(title.rectTransform);
            });
        }

        private static bool CreateQuitConfirmation(string featureFolder, string screenName)
        {
            ScreenDefinition definition = ScreenAuthoring.CreateScreen(featureFolder, screenName, ScreenType.Modal);
            if (definition == null)
                return false;

            return EditPrefab(definition, root =>
            {
                AddScaler(root);
                QuitConfirmationLayer layer = root.AddComponent<QuitConfirmationLayer>();

                Image panel = AddImage(root.transform, "Panel", Panel);
                panel.rectTransform.sizeDelta = new Vector2(820f, 460f);

                Text question = AddText(panel.transform, "Question", "Quit the game?", 60);
                Place(question.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 1f));

                Button cancel = AddButton(panel.transform, "Cancel", "Cancel", ButtonColor);
                Place(cancel.GetComponent<RectTransform>(), new Vector2(0.06f, 0.1f), new Vector2(0.47f, 0.42f));
                UnityEventTools.AddVoidPersistentListener(cancel.onClick, layer.Cancel);

                Button quit = AddButton(panel.transform, "Quit", "Quit", ConfirmColor);
                Place(quit.GetComponent<RectTransform>(), new Vector2(0.53f, 0.1f), new Vector2(0.94f, 0.42f));
                UnityEventTools.AddVoidPersistentListener(quit.onClick, layer.Quit);
            });
        }

        private static bool EditPrefab(ScreenDefinition definition, System.Action<GameObject> edit)
        {
            string path = ScreenAuthoring.FindScreenPrefabPath(definition);
            if (path == null)
                return false;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                edit(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddScaler(GameObject root)
        {
            if (!root.TryGetComponent(out CanvasScaler scaler))
                scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static Image AddImage(Transform parent, string name, Color color)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Image)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text AddText(Transform parent, string name, string value, int size)
        {
            GameObject go = new(name, typeof(RectTransform), typeof(Text)) { layer = parent.gameObject.layer };
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>(BuiltinFont);
            text.text = value;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button AddButton(Transform parent, string name, string label, Color color)
        {
            Image image = AddImage(parent, name, color);
            Button button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Stretch(AddText(image.transform, "Label", label, 48).rectTransform);
            return button;
        }

        private static void Stretch(RectTransform rect)
            => Place(rect, Vector2.zero, Vector2.one);

        private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

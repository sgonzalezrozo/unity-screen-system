using ScreenSystem.Attributes;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenDefinition : ScriptableObject, ISerializationCallbackReceiver
    {
        // Older assets stored these flags in a nested list. They're copied to the fields below on load.
        [SerializeField, HideInInspector]
        private System.Collections.Generic.List<ScreenBehavior> Behaviors = new();

        [ReadOnly]
        [Tooltip("Path to prefab: Resources path (e.g., 'Splash/SplashMain') or Addressable path/label. Resources takes priority.")]
        public string AssetPath;

        [ReadOnly]
        [Tooltip("Display name for this screen (auto-assigned from prefab)")]
        public string ScreenName;

        [ReadOnly]
        [Tooltip("Unique screen ID. New screens get Feature_ScreenName from their folder")]
        public string ScreenId;

        [Tooltip("Screen display type: fullscreen, modal, or alert")]
        public ScreenType screenType = ScreenType.fullscreen;

        [Tooltip("UI framework for this screen: uGUI or UIToolkit")]
        public UIFramework Framework = UIFramework.uGUI;

        [Tooltip("Persist over scene changes")]
        public bool PersistOverScenes;

        [Tooltip("Open over active modals and alerts, for splash screens and critical errors")]
        public bool IgnoreNavigationRules;

#if UNITY_EDITOR
        private void OnValidate()
        {
            SyncWithPrefab();
        }

        public void SyncWithPrefab()
        {
            if (string.IsNullOrEmpty(AssetPath))
                return;

            string prefabName = System.IO.Path.GetFileNameWithoutExtension(AssetPath);
            bool changed = false;

            if (string.IsNullOrEmpty(ScreenName) || ScreenName != prefabName)
            {
                ScreenName = prefabName;
                changed = true;
            }

            if (string.IsNullOrEmpty(ScreenId))
            {
                string id = DefaultScreenId(UnityEditor.AssetDatabase.GetAssetPath(this), ScreenName);
                ScreenId = string.IsNullOrEmpty(id) ? System.Guid.NewGuid().ToString() : id;
                changed = true;
            }

            if (changed)
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>The ID the screen wizard gives a screen: "Feature_ScreenName", without spaces.</summary>
        public static string MakeScreenId(string feature, string screenName)
            => $"{feature?.Replace(" ", "")}_{screenName?.Replace(" ", "")}";

        /// <summary>
        /// "Feature_ScreenName" for a definition at <paramref name="definitionPath"/>. The feature is the folder that
        /// holds the Services (or legacy Screens) folder the definition sits in; otherwise its own folder.
        /// Returns null when the path or name is empty.
        /// </summary>
        public static string DefaultScreenId(string definitionPath, string screenName)
        {
            if (string.IsNullOrEmpty(definitionPath) || string.IsNullOrEmpty(screenName))
                return null;

            string folder = System.IO.Path.GetDirectoryName(definitionPath)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(folder);
            if (folderName == "Services" || folderName == "Screens")
                folderName = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(folder));

            return string.IsNullOrEmpty(folderName) ? null : MakeScreenId(folderName, screenName);
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (Behaviors == null || Behaviors.Count == 0)
                return;

            foreach (ScreenBehavior behavior in Behaviors)
            {
                if (behavior == null)
                    continue;

                PersistOverScenes |= behavior.PersistOverScenes;
                IgnoreNavigationRules |= behavior.IgnoreNavigationRules;
            }

            Behaviors.Clear();
        }

        public override string ToString()
            => $"{ScreenName}";
    }
}

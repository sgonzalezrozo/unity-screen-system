using ScreenSystem.Attributes;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenDefinition : ScriptableObject
    {
        [ReadOnly]
        [Tooltip("Path to prefab: Resources path (e.g., 'Splash/SplashMain') or Addressable path/label. Resources takes priority.")]
        public string AssetPath;

        [ReadOnly]
        [Tooltip("Display name for this screen (auto-assigned from prefab)")]
        public string ScreenName;

        [ReadOnly]
        [Tooltip("Unique screen ID (auto-generated GUID)")]
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
                ScreenId = System.Guid.NewGuid().ToString();
                changed = true;
            }

            if (changed)
                UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        public override string ToString()
            => $"{ScreenName}";
    }
}

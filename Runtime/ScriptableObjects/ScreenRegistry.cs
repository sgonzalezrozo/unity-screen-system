using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem.ScriptableObjects
{
    public class ScreenRegistry : ScriptableObject
    {
        public List<ScreenDefinition> Screens = new();

        [NonSerialized] private ScreenIndex screenIndex;
        [NonSerialized] private bool isInitialized;

        public void Initialize()
        {
            if (isInitialized)
                return;

            screenIndex = new ScreenIndex();
            screenIndex.Build(Screens);
            isInitialized = true;
        }

        public void ForceReinitialize()
        {
            isInitialized = false;
            screenIndex = null;
            Initialize();
        }

        public ScreenDefinition GetScreen(string screenId)
        {
            if (!isInitialized)
                Initialize();

            return screenIndex?.GetScreen(screenId);
        }

        public List<ScreenDefinition> GetAllScreens()
            => Screens;


#if UNITY_EDITOR
        public bool Validate(out List<string> errors)
        {
            errors = new List<string>();

            if (Screens == null || Screens.Count == 0)
            {
                errors.Add("No screens defined");
                return false;
            }

            HashSet<string> screenIds = new();
            HashSet<string> assetPaths = new();

            foreach (ScreenDefinition screen in Screens)
            {
                if (screen == null)
                    continue;

                if (string.IsNullOrEmpty(screen.ScreenId))
                {
                    errors.Add($"Screen Asset '{screen.name}' has empty ScreenId");
                    continue;
                }

                if (screenIds.Contains(screen.ScreenId))
                    errors.Add($"Duplicate ScreenId: '{screen.ScreenId}'");
                else
                    screenIds.Add(screen.ScreenId);

                if (string.IsNullOrEmpty(screen.AssetPath))
                    errors.Add($"Screen Asset '{screen.name}' is missing AssetPath");
                else if (!assetPaths.Add(screen.AssetPath))
                    errors.Add($"Duplicate AssetPath used in '{screen.ScreenId}': '{screen.AssetPath}'");
            }

            return errors.Count == 0;
        }
#endif
    }
}

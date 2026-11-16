using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem
{

    internal class ScreenIndex
    {
        private readonly Dictionary<string, ScreenDefinition> screenDefinitions;

        public ScreenIndex()
        {
            screenDefinitions = new Dictionary<string, ScreenDefinition>();
        }

        public void Build(List<ScreenDefinition> screens)
        {
            screenDefinitions.Clear();

            if (screens == null || screens.Count == 0)
                return;

            for (int i = 0; i < screens.Count; i++)
            {
                ScreenDefinition screen = screens[i];

                if (screen == null)
                    continue;

                if (string.IsNullOrEmpty(screen.ScreenId))
                {
                    Debug.LogWarning($"[ScreenIndex] Screen has empty ScreenId (Name: {screen.ScreenName})");
                    continue;
                }

                screenDefinitions[screen.ScreenId] = screen;
            }
        }

        public ScreenDefinition GetScreen(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return null;

            if (screenDefinitions.TryGetValue(identifier, out ScreenDefinition screen))
                return screen;

            foreach (KeyValuePair<string, ScreenDefinition> kvp in screenDefinitions)
            {
                if (kvp.Value != null && kvp.Value.ScreenName == identifier)
                    return kvp.Value;
            }

            return null;
        }
    }
}

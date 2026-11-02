using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem
{
    [Serializable]
    public class ScreenBehavior
    {
        [Tooltip("Keep visible after navigate away")]
        public bool KeepVisibleAfterNavigate;

        [Tooltip("Keep raycast enabled when not top screen")]
        public bool KeepRaycastEnabledWhenNotTopScreen;

        [Tooltip("Persist over scene changes")]
        public bool PersistOverScenes;

        [Tooltip("Can show over active modals (splash screens, critical errors)")]
        public bool IgnoreNavigationRules;

        public override string ToString()
        {
            List<string> behaviors = new();
            if (KeepVisibleAfterNavigate)
                behaviors.Add("KeepVisible");

            if (KeepRaycastEnabledWhenNotTopScreen)
                behaviors.Add("KeepRaycast");

            if (PersistOverScenes)
                behaviors.Add("PersistOverScenes");

            if (IgnoreNavigationRules)
                behaviors.Add("IgnoreNavigationRules");

            return behaviors.Count > 0 ? string.Join(", ", behaviors) : "None";
        }
    }
}

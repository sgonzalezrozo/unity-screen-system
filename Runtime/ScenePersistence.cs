using System.Collections.Generic;

namespace ScreenSystem
{
    /// <summary>
    /// Decides which screens survive a single scene load: those with Persist Over Scenes, plus the special screens
    /// (the splash and the quit confirmation modal), which are always kept. Everything else is unloaded.
    /// </summary>
    internal static class ScenePersistence
    {
        public static bool IsKept(ScreenDefinition screen, ICollection<string> alwaysKeptScreenIds)
        {
            if (screen == null)
                return false;

            if (screen.PersistOverScenes)
                return true;

            return alwaysKeptScreenIds != null && alwaysKeptScreenIds.Contains(screen.ScreenId);
        }
    }
}

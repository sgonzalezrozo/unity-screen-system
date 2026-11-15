using System.Collections.Generic;
using ScreenSystem.ScriptableObjects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScreenSystem
{
    public class SceneLifecycleManager
    {
        private readonly ScreenDisplayService displayService;
        private readonly ScreenNavigationService navigationService;
        private readonly ScreenRegistry screenRegistry;

        public SceneLifecycleManager(ScreenDisplayService displayService, ScreenNavigationService navigationService, ScreenRegistry screenRegistry)
        {
            this.displayService = displayService;
            this.navigationService = navigationService;
            this.screenRegistry = screenRegistry;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
                return;

            OnSceneChange();
        }

        public void OnDestroy()
        {
            displayService.RemoveNonPersistentScreens();
            displayService.ClearActiveScreens();
        }

        // Unloads every screen without Persist Over Scenes. Persistent screens keep their layers, their
        // loaded prefab and their visibility, so a splash shown before the load stays up until the new
        // scene opens its first fullscreen screen, which hides it.
        private void OnSceneChange()
        {
            // A load still running from the old scene would finish after the cleanup and leave its handle behind.
            navigationService.AbandonRunningOperationForSceneChange();
            displayService.CancelActiveOperations();

            List<string> visiblePersistent = displayService.GetVisiblePersistentScreenIds();

            navigationService.ClearNavigationHistoryOnSceneChange();
            displayService.RemoveNonPersistentScreens();
            displayService.ClearActiveScreens();

            for (int i = 0; i < visiblePersistent.Count; i++)
                displayService.RegisterActiveScreen(visiblePersistent[i]);

            displayService.ClearAllCachedResources();
            Resources.UnloadUnusedAssets();
        }
    }
}

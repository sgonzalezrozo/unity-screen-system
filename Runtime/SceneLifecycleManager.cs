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

        // Frames to wait for the new scene's first screen before unloading unused assets anyway.
        private const int MAX_UNLOAD_DELAY_FRAMES = 60;

        private bool unloadPending;
        private int unloadPendingSinceFrame;
        private int firstScreenShownFrame = -1;

        public SceneLifecycleManager(ScreenDisplayService displayService, ScreenNavigationService navigationService, ScreenRegistry screenRegistry)
        {
            this.displayService = displayService;
            this.navigationService = navigationService;
            this.screenRegistry = screenRegistry;
            NavigationEvents.OnTransitionInCompleted += OnScreenShown;
        }

        public void Dispose()
            => NavigationEvents.OnTransitionInCompleted -= OnScreenShown;

        private void OnScreenShown(string screenId)
        {
            if (unloadPending && firstScreenShownFrame < 0)
                firstScreenShownFrame = Time.frameCount;
        }

        /// <summary>
        /// Called every frame. Unloads the previous scene's unused assets once the new scene's first screen has been
        /// drawn for a frame. Unloading runs a full GC and blocks asset loads, so doing it during the scene change
        /// made the first screen wait about 60 ms on a Pixel 5.
        /// </summary>
        public void Tick()
        {
            if (!unloadPending)
                return;

            bool firstScreenDrawn = firstScreenShownFrame >= 0 && Time.frameCount > firstScreenShownFrame;
            bool waitedTooLong = Time.frameCount - unloadPendingSinceFrame > MAX_UNLOAD_DELAY_FRAMES;
            if (!firstScreenDrawn && !waitedTooLong)
                return;

            unloadPending = false;
            Resources.UnloadUnusedAssets();
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
            using Unity.Profiling.ProfilerMarker.AutoScope _ = ScreenSystemMarkers.SceneCleanup.Auto();
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

            // The released assets are unloaded later, in Tick, so the new scene's first screen loads first.
            unloadPending = true;
            unloadPendingSinceFrame = Time.frameCount;
            firstScreenShownFrame = -1;
        }
    }
}

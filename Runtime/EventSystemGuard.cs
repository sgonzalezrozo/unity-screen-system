using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ScreenSystem
{
    /// <summary>
    /// Buttons need an EventSystem. When there is none, Screen System turns on its own, under the ScreenSystem
    /// object, and turns it off as soon as another one is active, so there is never more than one.
    /// </summary>
    internal sealed class EventSystemGuard
    {
        // How often to look for another EventSystem while ours is on. Scene loads check on the next frame.
        private const int CHECK_INTERVAL_FRAMES = 30;

        private readonly Transform owner;
        private ScreenSystemEventSystem ownEventSystem;
        private int framesUntilCheck;

        public EventSystemGuard(Transform owner)
        {
            this.owner = owner;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => framesUntilCheck = 0;

        private void OnSceneUnloaded(Scene scene) => framesUntilCheck = 0;

        /// <summary>Called once per frame by ScreenManager.</summary>
        public void Tick()
        {
            if (owner == null)
                return;

            // Nothing handles input: a cheap check, every frame.
            if (EventSystem.current == null)
            {
                TurnOnOwn();
                framesUntilCheck = CHECK_INTERVAL_FRAMES;
                return;
            }

            if (ownEventSystem == null || !ownEventSystem.isActiveAndEnabled)
                return;

            if (--framesUntilCheck > 0)
                return;

            framesUntilCheck = CHECK_INTERVAL_FRAMES;
            if (AnotherIsActive())
                ownEventSystem.gameObject.SetActive(false);
        }

        private bool AnotherIsActive()
        {
            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (eventSystem != ownEventSystem && eventSystem.isActiveAndEnabled)
                    return true;
            }

            return false;
        }

        private void TurnOnOwn()
        {
            if (ownEventSystem == null)
            {
                GameObject go = new GameObject("EventSystem");
                go.SetActive(false);
                go.transform.SetParent(owner, false);
                ownEventSystem = go.AddComponent<ScreenSystemEventSystem>();
#if ENABLE_INPUT_SYSTEM
                go.AddComponent<InputSystemUIInputModule>();
#else
                go.AddComponent<StandaloneInputModule>();
#endif
            }

            ownEventSystem.gameObject.SetActive(true);
        }
    }
}

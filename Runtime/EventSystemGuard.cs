using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace ScreenSystem
{
    /// <summary>
    /// Buttons need an EventSystem. When no loaded scene has one, Screen System adds its own under the ScreenSystem
    /// object, and turns it off while a scene brings its own, so there is never more than one.
    /// </summary>
    internal sealed class EventSystemGuard
    {
        private readonly Transform owner;
        private GameObject ownEventSystem;

        public EventSystemGuard(Transform owner)
        {
            this.owner = owner;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            Refresh();
        }

        public void Dispose()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Refresh();

        private void OnSceneUnloaded(Scene scene) => Refresh();

        private void Refresh()
        {
            if (owner == null)
                return;

            bool sceneHasOne = false;
            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (eventSystem.gameObject != ownEventSystem && eventSystem.isActiveAndEnabled)
                {
                    sceneHasOne = true;
                    break;
                }
            }

            if (sceneHasOne)
            {
                if (ownEventSystem != null)
                    ownEventSystem.SetActive(false);
                return;
            }

            if (ownEventSystem == null)
            {
                ownEventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                ownEventSystem.AddComponent<InputSystemUIInputModule>();
#else
                ownEventSystem.AddComponent<StandaloneInputModule>();
#endif
                ownEventSystem.transform.SetParent(owner, false);
            }

            ownEventSystem.SetActive(true);
        }
    }
}

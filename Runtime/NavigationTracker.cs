using System.Collections.Generic;
using UnityEngine;

namespace Tools.ScreenSystem.Scripts
{
    public class NavigationTracker : MonoBehaviour
    {
        private Dictionary<string, float> screenStartTimes = new();

        void OnEnable()
        {
            NavigationEvents.OnNavigationStarted += TrackNavigationStarted;
            NavigationEvents.OnNavigationCompleted += TrackNavigationCompleted;
            NavigationEvents.OnNavigationFailed += TrackNavigationFailed;
            NavigationEvents.OnTransitionInCompleted += TrackScreenOpened;
            NavigationEvents.OnTransitionOutStarted += TrackScreenClosed;
            NavigationEvents.OnBackAtRoot += TrackBackAtRoot;
            NavigationEvents.OnQuitApplicationRequested += TrackQuitRequested;
        }

        void OnDisable()
        {
            NavigationEvents.OnNavigationStarted -= TrackNavigationStarted;
            NavigationEvents.OnNavigationCompleted -= TrackNavigationCompleted;
            NavigationEvents.OnNavigationFailed -= TrackNavigationFailed;
            NavigationEvents.OnTransitionInCompleted -= TrackScreenOpened;
            NavigationEvents.OnTransitionOutStarted -= TrackScreenClosed;
            NavigationEvents.OnBackAtRoot -= TrackBackAtRoot;
            NavigationEvents.OnQuitApplicationRequested -= TrackQuitRequested;
        }

        private void TrackNavigationStarted(string screenId)
        {
            Debug.Log($"[Analytics] Navigation started: {screenId}");
        }

        private void TrackNavigationCompleted(string screenId)
        {
            Debug.Log($"[Analytics] Navigation completed: {screenId}");
        }

        private void TrackNavigationFailed(string screenId, string error)
        {
            Debug.LogError($"[Analytics] Navigation failed: {screenId} - {error}");
        }

        private void TrackScreenOpened(string screenId)
        {
            screenStartTimes[screenId] = Time.realtimeSinceStartup;
            Debug.Log($"[Analytics] Screen view: {screenId}");
        }

        private void TrackScreenClosed(string screenId)
        {
            if (screenStartTimes.TryGetValue(screenId, out float startTime))
            {
                float duration = Time.realtimeSinceStartup - startTime;
                Debug.Log($"[Analytics] Screen duration: {screenId} - {duration:F2}s");
                screenStartTimes.Remove(screenId);
            }
        }

        private void TrackBackAtRoot()
        {
            Debug.Log("[Analytics] Back at root - quit attempted");
        }

        private void TrackQuitRequested()
        {
            Debug.Log("[Analytics] Quit confirmed");
            PlayerPrefs.Save();
        }
    }
}


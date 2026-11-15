using System;

namespace ScreenSystem
{
    public static class NavigationEvents
    {
        public static string CurrentScreenId { get; private set; }
        public static string PreviousScreenId { get; private set; }

        public static event Action<string> OnNavigationStarted;
        public static event Action<string> OnNavigationCompleted;
        public static event Action<string, string> OnNavigationFailed;

        public static event Action<string> OnTransitionInStarted;
        public static event Action<string> OnTransitionInCompleted;
        public static event Action<string> OnTransitionOutStarted;
        public static event Action<string> OnTransitionOutCompleted;

        public static event Action<string> OnScreenPushed;
        public static event Action<string> OnScreenPopped;
        public static event Action OnStackCleared;

        public static event Action OnBackAtRoot;
        public static event Action OnQuitApplicationRequested;

        // With domain reload off, subscribers from the previous Play session would still be called. Drop them.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscribers()
        {
            OnNavigationStarted = null;
            OnNavigationCompleted = null;
            OnNavigationFailed = null;
            OnTransitionInStarted = null;
            OnTransitionInCompleted = null;
            OnTransitionOutStarted = null;
            OnTransitionOutCompleted = null;
            OnScreenPushed = null;
            OnScreenPopped = null;
            OnStackCleared = null;
            OnBackAtRoot = null;
            OnQuitApplicationRequested = null;
        }


        public static void NotifyNavigationStarted(string screenId)
        {
            PreviousScreenId = CurrentScreenId;
            OnNavigationStarted?.Invoke(screenId);
        }

        public static void NotifyNavigationCompleted(string screenId)
        {
            CurrentScreenId = screenId;
            OnNavigationCompleted?.Invoke(screenId);
        }

        public static void NotifyNavigationFailed(string screenId, string error)
        {
            OnNavigationFailed?.Invoke(screenId, error);
        }

        public static void NotifyTransitionInStarted(string screenId)
        {
            OnTransitionInStarted?.Invoke(screenId);
        }

        public static void NotifyTransitionInCompleted(string screenId)
        {
            OnTransitionInCompleted?.Invoke(screenId);
        }

        public static void NotifyTransitionOutStarted(string screenId)
        {
            OnTransitionOutStarted?.Invoke(screenId);
        }

        public static void NotifyTransitionOutCompleted(string screenId)
        {
            OnTransitionOutCompleted?.Invoke(screenId);
        }

        public static void NotifyScreenPushed(string screenId)
        {
            OnScreenPushed?.Invoke(screenId);
        }

        public static void NotifyScreenPopped(string screenId)
        {
            OnScreenPopped?.Invoke(screenId);
        }

        public static void NotifyStackCleared()
        {
            OnStackCleared?.Invoke();
        }

        public static void NotifyBackAtRoot()
        {
            OnBackAtRoot?.Invoke();
        }

        public static void NotifyQuitApplicationRequested()
        {
            OnQuitApplicationRequested?.Invoke();
        }
    }
}


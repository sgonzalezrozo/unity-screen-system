using System.Collections.Generic;
using ScreenSystem.ScriptableObjects;

namespace ScreenSystem
{
    public class ScreenNavigationStack
    {
        private readonly Stack<string> mainHistory = new();
        private readonly HashSet<string> mainHistorySet = new();  // O(1) membership testing
        private string currentModal;
        private string currentAlert;
        private string rootScreenId;

        public int MainHistoryCount
            => mainHistory.Count;

        public bool HasModal
            => !string.IsNullOrEmpty(currentModal);

        public bool HasAlert
            => !string.IsNullOrEmpty(currentAlert);

        public bool CanGoBack
            => mainHistory.Count > 1 || HasModal || HasAlert;

        public string GetCurrentScreen()
            => mainHistory.Count > 0 ? mainHistory.Peek() : null;

        public string GetRootScreen()
            => rootScreenId;

        public bool IsScreenTheRoot(string screenId)
            => !string.IsNullOrEmpty(screenId) && screenId == rootScreenId;

        public void PushMainScreen(string screenId)
        {
            mainHistory.Push(screenId);
            mainHistorySet.Add(screenId);  // O(1) add to set
        }

        public string PopMainScreen()
        {
            if (mainHistory.Count <= 1)
                return null;

            string popped = mainHistory.Pop();
            mainHistorySet.Remove(popped);  // O(1) remove from set
            return popped;
        }

        public string PeekMainScreen()
        {
            return mainHistory.Count > 0 ? mainHistory.Peek() : null;
        }

        public void SetRootScreen(string screenId)
        {
            mainHistory.Clear();
            mainHistorySet.Clear();
            mainHistory.Push(screenId);
            mainHistorySet.Add(screenId);
            rootScreenId = screenId;
        }

        public void ShowModalScreen(string modalScreenId)
        {
            currentModal = modalScreenId;
        }

        public string DismissModalScreen()
        {
            string dismissedModal = currentModal;
            currentModal = null;
            return dismissedModal;
        }

        public string GetCurrentModal()
        {
            return currentModal;
        }

        public bool MainHistoryContains(string screenId)
        {
            return mainHistorySet.Contains(screenId);  // O(1) instead of O(n)
        }

        public List<string> GetMainHistorySnapshot()
        {
            return new List<string>(mainHistory);
        }

        public void ClearModal()
        {
            currentModal = null;
        }

        public void ClearAllModals()
        {
            currentModal = null;
        }

        public void ShowAlertScreen(string alertScreenId)
        {
            currentAlert = alertScreenId;
        }

        public string DismissAlertScreen()
        {
            string dismissedAlert = currentAlert;
            currentAlert = null;
            return dismissedAlert;
        }

        public string GetCurrentAlert()
        {
            return currentAlert;
        }

        public void ClearAlert()
        {
            currentAlert = null;
        }

        public void ClearNonPersistentMainScreens(ScreenRegistry screenRegistry)
        {
            if (screenRegistry == null)
                return;

            List<string> screensToKeep = new();

            foreach (string screenId in mainHistory)
            {
                ScreenDefinition screen = screenRegistry.GetScreen(screenId);
                if (screen == null)
                    continue;

                if (screen.PersistOverScenes)
                    screensToKeep.Add(screenId);
            }

            mainHistory.Clear();
            mainHistorySet.Clear();

            foreach (string screenId in screensToKeep)
            {
                mainHistory.Push(screenId);
                mainHistorySet.Add(screenId);
            }
        }
    }
}

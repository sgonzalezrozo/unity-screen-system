using System.Collections.Generic;
using ScreenSystem.ScriptableObjects;

namespace ScreenSystem
{
    internal class ScreenNavigationStack
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

        /// <summary>Empties the stack: no screens, modal, alert or root.</summary>
        public void Clear()
        {
            mainHistory.Clear();
            mainHistorySet.Clear();
            currentModal = null;
            currentAlert = null;
            rootScreenId = null;
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

        /// <summary>Removes every screen that isn't kept on scene changes, keeping the order of the rest.</summary>
        public void ClearNonPersistentMainScreens(ScreenRegistry screenRegistry, ICollection<string> alwaysKeptScreenIds = null)
        {
            if (screenRegistry == null)
                return;

            // A Stack enumerates from the top, so collect top to bottom and push back in reverse.
            List<string> screensToKeep = new();

            foreach (string screenId in mainHistory)
            {
                if (ScenePersistence.IsKept(screenRegistry.GetScreen(screenId), alwaysKeptScreenIds))
                    screensToKeep.Add(screenId);
            }

            mainHistory.Clear();
            mainHistorySet.Clear();

            for (int i = screensToKeep.Count - 1; i >= 0; i--)
            {
                mainHistory.Push(screensToKeep[i]);
                mainHistorySet.Add(screensToKeep[i]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenNavigationService : IScreenNavigation
    {
        private bool isNavigating;

        private readonly ScreenNavigationStack navigationStack;
        private readonly ScreenNavigator navigator;
        private readonly ScreenModalNavigator modalNavigator;
        private readonly ScreenVisibilityController visibilityController;
        private readonly ScreenRegistry screenRegistry;

        public ScreenNavigationService(ScreenRegistry screenRegistry, IScreenDisplay displayService)
        {
            this.screenRegistry = screenRegistry;
            navigationStack = new ScreenNavigationStack();
            navigator = new ScreenNavigator(screenRegistry, displayService, navigationStack);
            modalNavigator = new ScreenModalNavigator(displayService, navigationStack, screenRegistry);
            visibilityController = new ScreenVisibilityController(displayService);
        }

        public bool CanGoBack
            => navigationStack.CanGoBack;

        public string GetCurrentScreen()
            => navigationStack.GetCurrentScreen();

        public async Task<bool> GoToAsync(string screenId, CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync(async () =>
            {
                return await navigator.GoToAsync(screenId, ct);
            });
        }

        public async Task<bool> GoBackAsync(CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync(async () =>
            {
                return await navigator.GoBackAsync(ct);
            });
        }

        public async Task<bool> PopToAsync(string screenId, CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync(async () =>
            {
                return await navigator.PopToAsync(screenId, ct);
            });
        }

        public async Task<bool> SetRootAsync(string screenId, CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync(async () =>
            {
                return await navigator.SetRootAsync(screenId, ct);
            });
        }

        public string GetRootScreen()
            => navigationStack.GetRootScreen();

        public async Task HideAllScreensAsync(CancellationToken ct = default)
        {
            await visibilityController.HideAllScreensAsync(ct);
        }

        public async Task RestoreAllScreensAsync(CancellationToken ct = default)
        {
            await visibilityController.RestoreAllScreensAsync(ct);
        }

        public async Task NavigateFromModalToScreenAsync(string fullscreenId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(fullscreenId))
            {
                Debug.LogError("[ScreenNav] NavigateFromModalToScreenAsync failed: fullscreenId is null or empty");
                return;
            }

            if (navigationStack.HasModal)
            {
                string activeModal = navigationStack.DismissModalScreen();
                try
                {
                    await modalNavigator.HideModalAsync(activeModal, ct);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ScreenNav] Failed to dismiss modal during redirect: {ex.Message}");
                }
            }

            await GoToAsync(fullscreenId, ct);
        }

        public void ClearNavigationHistoryOnSceneChange()
        {
            navigationStack.ClearAllModals();
            navigationStack.ClearNonPersistentMainScreens(screenRegistry);
        }

        private async Task WithNavigationGuardAsync(Func<Task> operation)
        {
            if (isNavigating)
                return;

            isNavigating = true;
            try
            {
                await operation();
            }
            finally
            {
                isNavigating = false;
            }
        }

        private async Task<T> WithNavigationGuardAsync<T>(Func<Task<T>> operation)
        {
            if (isNavigating)
                return default;

            isNavigating = true;
            try
            {
                return await operation();
            }
            finally
            {
                isNavigating = false;
            }
        }

#if UNITY_EDITOR
        public List<string> GetHistoryForEditor()
            => navigationStack.GetMainHistorySnapshot();

        public List<string> GetModalHistoryForEditor()
        {
            List<string> modalHistory = new();
            string currentModal = navigationStack.GetCurrentModal();
            if (!string.IsNullOrEmpty(currentModal))
                modalHistory.Add(currentModal);
            return modalHistory;
        }

        public List<string> GetAlertHistoryForEditor()
        {
            List<string> alertHistory = new();
            string currentAlert = navigationStack.GetCurrentAlert();
            if (!string.IsNullOrEmpty(currentAlert))
                alertHistory.Add(currentAlert);
            return alertHistory;
        }
#endif
    }
}

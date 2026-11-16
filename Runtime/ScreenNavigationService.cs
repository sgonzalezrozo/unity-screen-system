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
        private string currentOperation;
        private int operationGeneration;

        private readonly ScreenNavigationStack navigationStack;
        private readonly ScreenNavigator navigator;
        private readonly ScreenModalNavigator modalNavigator;
        private readonly ScreenVisibilityController visibilityController;
        private readonly ScreenRegistry screenRegistry;
        private readonly HashSet<string> alwaysKeptScreenIds = new();

        public ScreenNavigationService(ScreenRegistry screenRegistry, IScreenDisplay displayService)
        {
            this.screenRegistry = screenRegistry;
            navigationStack = new ScreenNavigationStack();
            navigator = new ScreenNavigator(screenRegistry, displayService, navigationStack, new AlertManager());
            modalNavigator = new ScreenModalNavigator(displayService, navigationStack, screenRegistry);
            visibilityController = new ScreenVisibilityController(displayService);
        }

        public bool CanGoBack
            => navigationStack.CanGoBack;

        public string GetCurrentScreen()
            => navigationStack.GetCurrentScreen();

        public Task<bool> GoToAsync(string screenId, CancellationToken ct = default)
            => GoToAsync(screenId, NavigationHistory.Push, ct);

        public Task<bool> GoToAsync(string screenId, NavigationHistory history, CancellationToken ct = default)
            => GoToAsync(screenId, history, null, ct);

        public async Task<bool> GoToAsync(string screenId, NavigationHistory history, ScreenParameter parameter, CancellationToken ct = default)
        {
            string operation = history == NavigationHistory.Push ? $"GoTo({screenId})" : $"GoTo({screenId}, {history})";
            return await WithNavigationGuardAsync(operation, async () =>
            {
                return await navigator.GoToAsync(screenId, history, parameter, ct);
            });
        }

        public async Task<bool> GoBackAsync(CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync("GoBack", async () =>
            {
                return await navigator.GoBackAsync(ct);
            });
        }

        public async Task<bool> PopToAsync(string screenId, CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync($"PopTo({screenId})", async () =>
            {
                return await navigator.PopToAsync(screenId, ct);
            });
        }

        public async Task<bool> SetRootAsync(string screenId, CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync($"SetRoot({screenId})", async () =>
            {
                return await navigator.SetRootAsync(screenId, ct);
            });
        }

        public string GetRootScreen()
            => navigationStack.GetRootScreen();

        public bool IsAlertActive
            => navigator.IsAlertActive;

        public async Task<bool> CloseAllAsync(CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync("CloseAll", async () =>
            {
                return await navigator.CloseAllAsync(ct);
            });
        }

        public async Task<bool> DismissAlertAsync(CancellationToken ct = default)
        {
            return await WithNavigationGuardAsync("DismissAlert", async () =>
            {
                return await navigator.DismissAlertAsync(ct);
            });
        }

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

        /// <summary>Screens kept on scene changes even without Persist Over Scenes, by ID.</summary>
        public void SetAlwaysKeptScreens(IEnumerable<string> screenIds)
        {
            alwaysKeptScreenIds.Clear();
            if (screenIds != null)
                alwaysKeptScreenIds.UnionWith(screenIds);
        }

        public void ClearNavigationHistoryOnSceneChange()
        {
            navigationStack.ClearAllModals();
            navigationStack.ClearNonPersistentMainScreens(screenRegistry, alwaysKeptScreenIds);
            navigator.ClearAlert();
            NavigationEvents.NotifyStackCleared();
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

        /// <summary>Why the last call returned false: Transitioning when it was ignored, otherwise the navigator's reason.</summary>
        public NavigationStatus LastStatus { get; private set; } = NavigationStatus.Succeeded;

        private async Task<T> WithNavigationGuardAsync<T>(string operationName, Func<Task<T>> operation)
        {
            if (isNavigating)
            {
                Debug.LogWarning($"[ScreenNav] {operationName} ignored: {currentOperation} is still running.");
                LastStatus = NavigationStatus.Transitioning;
                return default;
            }

            isNavigating = true;
            currentOperation = operationName;
            int generation = ++operationGeneration;
            try
            {
                T result = await operation();
                LastStatus = navigator.LastStatus;
                return result;
            }
            catch (OperationCanceledException) when (generation != operationGeneration)
            {
                // A scene change cancelled this operation. The new scene's calls must not see an exception.
                return default;
            }
            finally
            {
                // A scene change may have released the guard and started a newer operation. Leave that one alone.
                if (generation == operationGeneration)
                {
                    isNavigating = false;
                    currentOperation = null;
                }
            }
        }

        /// <summary>True while a navigation call is running. Calls made meanwhile are ignored and return false.</summary>
        public bool IsNavigating
            => isNavigating;

        /// <summary>
        /// Called on a single scene load. Releases the guard so the new scene can navigate right away, and marks the
        /// running operation as stale. The display service cancels its loads, which release their handles.
        /// </summary>
        public void AbandonRunningOperationForSceneChange()
        {
            operationGeneration++;
            isNavigating = false;
            currentOperation = null;
        }

#if UNITY_EDITOR
        /// <summary>The navigation call running now, such as "GoTo(Shop)", or null.</summary>
        public string CurrentOperationForEditor
            => isNavigating ? currentOperation : null;

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

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenNavigator
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly IScreenDisplay displayService;
        private readonly ScreenNavigationStack navigationStack;
        private readonly IAlertService? alertService;

        public ScreenNavigator(ScreenRegistry screenRegistry, IScreenDisplay displayService, ScreenNavigationStack navigationStack, IAlertService? alertService = null)
        {
            this.screenRegistry = screenRegistry;
            this.displayService = displayService;
            this.navigationStack = navigationStack;
            this.alertService = alertService;
        }

        public async Task<bool> GoToAsync(string screenId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError("[ScreenNav] GoToAsync failed: screenId is null or empty");
                return false;
            }

            if (alertService != null && alertService.IsAlertActive)
            {
                Debug.LogWarning($"[ScreenNav] GoToAsync blocked: alert '{alertService.CurrentAlertId}' is active. Dismiss alert first.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Alert active - navigation blocked");
                return false;
            }

            ScreenDefinition screen = screenRegistry.GetScreen(screenId);

            if (screen == null)
            {
                Debug.LogWarning($"[ScreenNav] GoTo skipped: screen not found: {screenId}");
                NavigationEvents.NotifyNavigationFailed(screenId, "Screen not found");
                return false;
            }

            bool isAlreadyShown = navigationStack.MainHistoryContains(screenId);

            if (navigationStack.HasModal && !isAlreadyShown)
            {
                Debug.LogWarning($"[ScreenNav] GoToAsync blocked: modal '{navigationStack.GetCurrentModal()}' is active and target screen is new. Dismiss modal first.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Modal active - cannot navigate to new screen");
                return false;
            }

            if (navigationStack.HasModal && isAlreadyShown)
            {
                return await GoToWithModalDismissal(screenId, ct);
            }

            bool shouldShowScreen = await TryPushToNavigationStackAsync(screenId, screen, ct);

            if (!shouldShowScreen)
                return false;

            await ShowScreenAndNotifyAsync(screenId, ct);
            return true;
        }

        private async Task<bool> GoToWithModalDismissal(string screenId, CancellationToken ct)
        {
            string modalId = navigationStack.DismissModalScreen();
            navigationStack.ClearModal();

            if (!await PopScreensUntilTargetAsync(screenId, ct))
                return false;

            await ShowTargetScreenAsync(screenId, ct);

            if (displayService.IsScreenInstantiated(modalId))
                await displayService.HideScreen(modalId, screenId, ct);

            return true;
        }

        private async Task<bool> TryPushToNavigationStackAsync(string screenId, ScreenDefinition screen, CancellationToken ct)
        {
            try
            {
                if (screen.screenType == ScreenType.modal)
                {
                    navigationStack.ShowModalScreen(screenId);
                    return true;
                }

                if (!CanShowFullscreenOverModals(screen))
                {
                    Debug.LogError($"[ScreenNav] Cannot show fullscreen ({screenId}) while modal is active. Dismiss modal first.");
                    NavigationEvents.NotifyNavigationFailed(screenId, "Modal active - fullscreen blocked");
                    return false;
                }

                if (navigationStack.MainHistoryContains(screenId))
                {
                    string currentScreen = navigationStack.PeekMainScreen();
                    if (currentScreen == screenId)
                    {
                        bool isScreenVisible = IsScreenCurrentlyVisible(screenId);

                        if (isScreenVisible)
                        {
                            Debug.LogWarning($"[ScreenNav] Screen {screenId} is already at top of stack and visible, skipping navigation");
                            return false;
                        }

                        return true;
                    }

                    await PopToAsync(screenId, ct);
                    return false;
                }

                navigationStack.PushMainScreen(screenId);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] GoTo failed: {ex.Message}\n{ex.StackTrace}");
                NavigationEvents.NotifyNavigationFailed(screenId, ex.Message);
                return false;
            }
        }

        private bool IsScreenCurrentlyVisible(string screenId)
        {
            List<LayerComponent> layers = displayService.GetScreenLayers(screenId);

            if (layers == null || layers.Count == 0)
            {
                Debug.Log($"[ScreenNav] IsScreenCurrentlyVisible: {screenId} has no layers, NOT visible");
                return false;
            }

            int visibleCanvasCount = 0;

            for (int i = 0; i < layers.Count; i++)
            {
                LayerComponent layer = layers[i];
                if (layer == null)
                    continue;

                Canvas canvas = layer.GetComponent<Canvas>();
                if (canvas != null && canvas.enabled)
                    visibleCanvasCount++;
            }

            bool isVisible = visibleCanvasCount > 0;
            return isVisible;
        }

        private bool CanShowFullscreenOverModals(ScreenDefinition screen)
        {
            if (!navigationStack.HasModal)
                return true;

            Debug.LogWarning($"[ScreenNav] CanShowFullscreenOverModals: modal is active, allowing fullscreen screen {screen?.ScreenName}");
            return true;
        }

        private async Task ShowScreenAndNotifyAsync(string screenId, CancellationToken ct)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                NavigationEvents.NotifyNavigationStarted(screenId);

                await displayService.ShowScreen(screenId, true, ct);
                await HideSplashIfActiveAsync(screenId, ct);

                NavigationEvents.NotifyScreenPushed(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[ScreenNav] GoTo cancelled: {screenId}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] GoTo show failed: {ex.Message}\n{ex.StackTrace}");
                NavigationEvents.NotifyNavigationFailed(screenId, ex.Message);
            }
        }

        public async Task<bool> GoBackAsync(CancellationToken ct = default)
        {
            try
            {
                if (alertService != null && alertService.IsAlertActive)
                {
                    Debug.LogWarning($"[ScreenNav] GoBackAsync blocked: alert '{alertService.CurrentAlertId}' is active");
                    return false;
                }

                if (navigationStack.HasModal)
                {
                    await TryGoBackFromModalAsync(ct);
                    return true;
                }

                if (!navigationStack.CanGoBack)
                {
                    NavigationEvents.NotifyBackAtRoot();
                    return false;
                }

                string currentScreen = navigationStack.GetCurrentScreen();
                if (navigationStack.IsScreenTheRoot(currentScreen))
                {
                    Debug.Log("[ScreenNav] At root screen, showing quit alert");
                    NavigationEvents.NotifyBackAtRoot();
                    return false;
                }

                await GoBackFromMainScreenAsync(ct);
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[ScreenNav] GoBackAsync cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] GoBack failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        public async Task<bool> SetRootAsync(string screenId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError("[ScreenNav] SetRootAsync failed: screenId is null or empty");
                return false;
            }

            if (alertService != null && alertService.IsAlertActive)
            {
                Debug.LogWarning($"[ScreenNav] SetRootAsync blocked: alert '{alertService.CurrentAlertId}' is active");
                return false;
            }

            ScreenDefinition screen = screenRegistry.GetScreen(screenId);

            if (screen == null)
            {
                Debug.LogWarning($"[ScreenNav] SetRootAsync failed: screen not found: {screenId}");
                NavigationEvents.NotifyNavigationFailed(screenId, "Screen not found");
                return false;
            }

            try
            {
                string previousRoot = navigationStack.GetRootScreen();

                navigationStack.SetRootScreen(screenId);

                if (!string.IsNullOrEmpty(previousRoot) && displayService.IsScreenInstantiated(previousRoot))
                {
                    await displayService.HideScreen(previousRoot, null, ct);
                }

                NavigationEvents.NotifyNavigationStarted(screenId);
                await displayService.ShowScreen(screenId, true, ct);
                NavigationEvents.NotifyScreenPushed(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);

                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[ScreenNav] SetRootAsync cancelled: {screenId}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] SetRootAsync failed: {ex.Message}\n{ex.StackTrace}");
                NavigationEvents.NotifyNavigationFailed(screenId, ex.Message);
                return false;
            }
        }

        private async Task TryGoBackFromModalAsync(CancellationToken ct)
        {
            string topModal = navigationStack.DismissModalScreen();
            await displayService.HideScreen(topModal, null, ct);
        }

        private async Task GoBackFromMainScreenAsync(CancellationToken ct)
        {
            string leavingId = navigationStack.PopMainScreen();

            await ClearAllModalsAsync(ct);

            string returnToScreen = navigationStack.PeekMainScreen();
            await displayService.HideScreen(leavingId, returnToScreen, ct);

            if (returnToScreen != null)
                await ShowReturnScreenAsync(returnToScreen, ct);
            else
                NavigationEvents.NotifyBackAtRoot();
        }

        private async Task ShowReturnScreenAsync(string screenId, CancellationToken ct)
        {
            try
            {
                await displayService.ShowScreen(screenId, false, ct);

                NavigationEvents.NotifyScreenPopped(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[ScreenNav] GoBack show cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] GoBack show failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        private async Task ClearAllModalsAsync(CancellationToken ct)
        {
            if (!navigationStack.HasModal)
                return;

            string modalId = navigationStack.DismissModalScreen();
            if (displayService.IsScreenInstantiated(modalId))
                await displayService.HideScreen(modalId, null, ct);
        }

        public async Task<bool> PopToAsync(string screenId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError("[ScreenNav] PopToAsync failed: screenId is null or empty");
                return false;
            }

            if (alertService != null && alertService.IsAlertActive)
            {
                Debug.LogWarning($"[ScreenNav] PopToAsync blocked: alert '{alertService.CurrentAlertId}' is active");
                return false;
            }

            if (!navigationStack.MainHistoryContains(screenId))
            {
                Debug.LogWarning($"[ScreenNav] PopToAsync screen not in history: {screenId}");
                return false;
            }

            NavigationEvents.NotifyNavigationStarted(screenId);

            if (!await PopScreensUntilTargetAsync(screenId, ct))
                return false;

            await ShowTargetScreenAsync(screenId, ct);
            return true;
        }

        private async Task<bool> PopScreensUntilTargetAsync(string targetScreenId, CancellationToken ct)
        {
            try
            {
                await ClearAllModalsAsync(ct);

                while (navigationStack.MainHistoryCount > 0 && navigationStack.PeekMainScreen() != targetScreenId)
                {
                    string removedId = navigationStack.PopMainScreen();

                    if (displayService.IsScreenInstantiated(removedId))
                        await displayService.HideScreen(removedId, targetScreenId, ct);
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[ScreenNav] PopTo cancelled: {targetScreenId}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] PopTo failed: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        private async Task ShowTargetScreenAsync(string screenId, CancellationToken ct)
        {
            try
            {
                await displayService.ShowScreen(screenId, false, ct);

                NavigationEvents.NotifyScreenPopped(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[ScreenNav] PopTo show cancelled: {screenId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] PopTo show failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task HideSplashIfActiveAsync(string excludeScreenId, CancellationToken ct)
        {
            Dictionary<string, List<LayerComponent>> cachedScreens = displayService.GetCachedScreens();

            foreach (KeyValuePair<string, List<LayerComponent>> kvp in cachedScreens)
            {
                string screenId = kvp.Key;
                List<LayerComponent> layers = kvp.Value;

                if (layers == null || layers.Count == 0)
                    continue;

                if (screenId == excludeScreenId)
                    continue;

                ScreenDefinition screen = screenRegistry.GetScreen(screenId);

                if (!IsSplashScreen(screen))
                    continue;

                if (!IsScreenVisible(layers))
                    continue;

                await displayService.HideScreen(screenId, null, ct);
            }
        }

        private bool IsSplashScreen(ScreenDefinition screen)
        {
            return screen == null;
        }

        private bool IsScreenVisible(List<LayerComponent> layers)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                LayerComponent layer = layers[i];

                if (layer == null)
                    continue;

                Canvas canvas = layer.GetComponent<Canvas>();

                if (canvas != null && canvas.enabled)
                    return true;
            }

            return false;
        }

        public async Task ShowAlertAsync(string alertId, CancellationToken ct = default)
        {
            if (alertService == null)
            {
                Debug.LogError("[ScreenNav] AlertService not available");
                return;
            }

            if (string.IsNullOrEmpty(alertId))
            {
                Debug.LogError("[ScreenNav] ShowAlert failed: alertId is null or empty");
                return;
            }

            await alertService.ShowAlertAsync(alertId, ct);
        }

        public async Task DismissAlertAsync(CancellationToken ct = default)
        {
            if (alertService == null)
            {
                Debug.LogError("[ScreenNav] AlertService not available");
                return;
            }

            await alertService.DismissAlertAsync(ct);
        }

        public bool IsAlertActive
            => alertService != null && alertService.IsAlertActive;

        public bool IsModalActive
            => navigationStack.HasModal;
    }
}

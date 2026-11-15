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

        // A screen with Ignore Navigation Rules that opened over a modal or alert.
        private string? screenShownOverOverlay;

        public ScreenNavigator(ScreenRegistry screenRegistry, IScreenDisplay displayService, ScreenNavigationStack navigationStack, IAlertService? alertService = null)
        {
            this.screenRegistry = screenRegistry;
            this.displayService = displayService;
            this.navigationStack = navigationStack;
            this.alertService = alertService;
        }

        public Task<bool> GoToAsync(string screenId, CancellationToken ct = default)
            => GoToAsync(screenId, NavigationHistory.Push, ct);

        public async Task<bool> GoToAsync(string screenId, NavigationHistory history, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError("[ScreenNav] GoToAsync failed: screenId is null or empty");
                return false;
            }

            ScreenDefinition screen = screenRegistry.GetScreen(screenId);

            if (screen == null)
            {
                Debug.LogWarning($"[ScreenNav] GoTo skipped: screen not found: {screenId}");
                NavigationEvents.NotifyNavigationFailed(screenId, "Screen not found");
                return false;
            }

            bool ignoreRules = screen.IgnoreNavigationRules;

            if (alertService != null && alertService.IsAlertActive && !ignoreRules)
            {
                Debug.LogWarning($"[ScreenNav] GoToAsync blocked: alert '{alertService.CurrentAlertId}' is active. Dismiss alert first.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Alert active - navigation blocked");
                return false;
            }

            if (history == NavigationHistory.ClearToRoot)
                return await GoToClearingToRootAsync(screenId, screen, ct);

            if (screen.screenType == ScreenType.alert)
                return await ShowAlertScreenAsync(screenId, ct);

            bool isAlreadyShown = navigationStack.MainHistoryContains(screenId);

            if (navigationStack.HasModal && !isAlreadyShown && !ignoreRules)
            {
                Debug.LogWarning($"[ScreenNav] GoToAsync blocked: modal '{navigationStack.GetCurrentModal()}' is active and target screen is new. Dismiss modal first.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Modal active - cannot navigate to new screen");
                return false;
            }

            if (navigationStack.HasModal && isAlreadyShown)
            {
                return await GoToWithModalDismissal(screenId, ct);
            }

            // A screen lower in the stack: going to it means popping back to it.
            if (screen.screenType == ScreenType.fullscreen && isAlreadyShown && navigationStack.PeekMainScreen() != screenId)
                return await PopToAsync(screenId, ct);

            bool opensOverOverlay = ignoreRules && (navigationStack.HasModal || (alertService?.IsAlertActive ?? false));

            if (opensOverOverlay && screen.screenType == ScreenType.modal)
                await ClearAllModalsAsync(ct);

            int historyBefore = navigationStack.MainHistoryCount;
            string modalBefore = navigationStack.GetCurrentModal();
            bool shouldShowScreen = await TryPushToNavigationStackAsync(screenId, screen, ct);

            if (!shouldShowScreen)
                return false;

            if (opensOverOverlay && screen.screenType == ScreenType.fullscreen)
                screenShownOverOverlay = screenId;

            try
            {
                await ShowScreenAndNotifyAsync(screenId, ct, autoHideParents: !opensOverOverlay);
            }
            catch (OperationCanceledException)
            {
                // The caller cancelled before the screen showed. Undo the push so the stack matches what's visible.
                if (navigationStack.MainHistoryCount > historyBefore && navigationStack.PeekMainScreen() == screenId)
                    navigationStack.PopMainScreen();
                if (modalBefore == null && navigationStack.GetCurrentModal() == screenId)
                    navigationStack.ClearModal();
                if (screenShownOverOverlay == screenId)
                    screenShownOverOverlay = null;
                throw;
            }

            return true;
        }

        // Pops every screen above the root and opens the target over it, without showing the root on the way.
        private async Task<bool> GoToClearingToRootAsync(string screenId, ScreenDefinition screen, CancellationToken ct)
        {
            if (screen.screenType != ScreenType.fullscreen)
            {
                Debug.LogError($"[ScreenNav] GoTo({screenId}, ClearToRoot) failed: only fullscreen screens can clear the stack. Use GoTo without it for modals and alerts.");
                NavigationEvents.NotifyNavigationFailed(screenId, "ClearToRoot needs a fullscreen screen");
                return false;
            }

            if (alertService != null && alertService.IsAlertActive)
            {
                Debug.LogWarning($"[ScreenNav] GoTo({screenId}, ClearToRoot) blocked: alert '{alertService.CurrentAlertId}' is active.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Alert active - navigation blocked");
                return false;
            }

            string root = navigationStack.GetRootScreen();
            if (navigationStack.MainHistoryCount == 0 || string.IsNullOrEmpty(root))
                return await GoToAsync(screenId, NavigationHistory.Push, ct);

            if (root == screenId)
                return await PopToAsync(screenId, ct);

            bool alreadyVisibleOnTop = navigationStack.PeekMainScreen() == screenId && !navigationStack.HasModal && IsScreenCurrentlyVisible(screenId);

            NavigationEvents.NotifyNavigationStarted(screenId);
            await ClearAllModalsAsync(ct);

            // The target's shared layers stay visible while the screens above the root hide.
            while (navigationStack.MainHistoryCount > 1)
            {
                string removedId = navigationStack.PopMainScreen();
                if (removedId == screenId)
                    continue;

                if (displayService.IsScreenInstantiated(removedId))
                    await displayService.HideScreen(removedId, screenId, ct);
            }

            navigationStack.PushMainScreen(screenId);
            NavigationEvents.NotifyStackCleared();

            if (alreadyVisibleOnTop)
            {
                NavigationEvents.NotifyNavigationCompleted(screenId);
                return true;
            }

            try
            {
                await ShowScreenAndNotifyAsync(screenId, ct, autoHideParents: true);
            }
            catch (OperationCanceledException)
            {
                // The screens above the root are already gone; drop the target so the stack ends at the root.
                if (navigationStack.PeekMainScreen() == screenId)
                    navigationStack.PopMainScreen();
                throw;
            }

            return true;
        }

        private async Task<bool> ShowAlertScreenAsync(string screenId, CancellationToken ct)
        {
            if (alertService == null)
            {
                Debug.LogError("[ScreenNav] AlertService not available");
                return false;
            }

            navigationStack.ShowAlertScreen(screenId);
            _ = alertService.ShowAlertAsync(screenId);

            await ShowScreenAndNotifyAsync(screenId, ct, autoHideParents: false);
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

                if (layer.IsVisible)
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

        private async Task ShowScreenAndNotifyAsync(string screenId, CancellationToken ct, bool autoHideParents = true)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                NavigationEvents.NotifyNavigationStarted(screenId);

                await displayService.ShowScreen(screenId, autoHideParents, ct);

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
                if (IsCurrentScreenOverOverlay())
                {
                    await GoBackFromScreenOverOverlayAsync(ct);
                    return true;
                }

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
                NavigationEvents.NotifyStackCleared();

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

        private bool IsCurrentScreenOverOverlay()
        {
            bool hasOverlay = navigationStack.HasModal || (alertService?.IsAlertActive ?? false);
            return hasOverlay
                   && !string.IsNullOrEmpty(screenShownOverOverlay)
                   && navigationStack.GetCurrentScreen() == screenShownOverOverlay;
        }

        // Closes only the screen on top, so the modal or alert underneath stays open.
        private async Task GoBackFromScreenOverOverlayAsync(CancellationToken ct)
        {
            string leavingId = navigationStack.PopMainScreen();
            screenShownOverOverlay = null;

            await displayService.HideScreen(leavingId, null, ct);
            NavigationEvents.NotifyScreenPopped(leavingId);
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

        /// <summary>Hides every screen, modal and alert and empties the stack, to hand the UI back to code outside Screen System.</summary>
        public async Task<bool> CloseAllAsync(CancellationToken ct = default)
        {
            string alertId = navigationStack.GetCurrentAlert();
            string modalId = navigationStack.GetCurrentModal();
            List<string> history = navigationStack.GetMainHistorySnapshot();

            navigationStack.Clear();
            alertService?.Clear();
            screenShownOverOverlay = null;

            if (!string.IsNullOrEmpty(alertId) && displayService.IsScreenInstantiated(alertId))
                await displayService.HideScreen(alertId, null, ct);

            if (!string.IsNullOrEmpty(modalId) && displayService.IsScreenInstantiated(modalId))
                await displayService.HideScreen(modalId, null, ct);

            foreach (string screenId in history)
            {
                if (displayService.IsScreenInstantiated(screenId))
                    await displayService.HideScreen(screenId, null, ct);
            }

            NavigationEvents.NotifyStackCleared();
            return true;
        }

        public async Task<bool> DismissAlertAsync(CancellationToken ct = default)
        {
            string alertId = navigationStack.DismissAlertScreen();

            if (string.IsNullOrEmpty(alertId))
                return false;

            alertService?.Clear();
            await displayService.HideScreen(alertId, null, ct);
            return true;
        }

        public void ClearAlert()
        {
            navigationStack.ClearAlert();
            alertService?.Clear();
        }

        public bool IsAlertActive
            => alertService != null && alertService.IsAlertActive;

        public bool IsModalActive
            => navigationStack.HasModal;
    }
}

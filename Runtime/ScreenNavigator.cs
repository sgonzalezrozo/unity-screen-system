#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    internal class ScreenNavigator
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly IScreenDisplay displayService;
        private readonly ScreenNavigationStack navigationStack;
        private readonly IAlertService? alertService;

        // A screen with Ignore Navigation Rules that opened over a modal or alert.
        private string? screenShownOverOverlay;

        /// <summary>Why the last call returned false, or Succeeded.</summary>
        public NavigationStatus LastStatus { get; private set; } = NavigationStatus.Succeeded;

        private bool Fail(NavigationStatus status)
        {
            LastStatus = status;
            return false;
        }

        private bool Succeed()
        {
            LastStatus = NavigationStatus.Succeeded;
            return true;
        }

        public ScreenNavigator(ScreenRegistry screenRegistry, IScreenDisplay displayService, ScreenNavigationStack navigationStack, IAlertService? alertService = null)
        {
            this.screenRegistry = screenRegistry;
            this.displayService = displayService;
            this.navigationStack = navigationStack;
            this.alertService = alertService;
        }

        public Task<bool> GoToAsync(string screenId, CancellationToken ct = default)
            => GoToAsync(screenId, NavigationHistory.Push, null, ct);

        public Task<bool> GoToAsync(string screenId, NavigationHistory history, CancellationToken ct = default)
            => GoToAsync(screenId, history, null, ct);

        /// <summary>
        /// Opens a screen. A non-null <paramref name="parameter"/> is delivered once to the screen's layers when it
        /// shows, or right away when it's already showing on top. It's never kept after this call.
        /// </summary>
        public async Task<bool> GoToAsync(string screenId, NavigationHistory history, ScreenParameter? parameter, CancellationToken ct = default)
        {
            if (parameter == null)
                return await GoToCoreAsync(screenId, history, null, ct);

            displayService.SetPendingParameter(screenId, parameter);
            try
            {
                return await GoToCoreAsync(screenId, history, parameter, ct);
            }
            finally
            {
                // Delivered or not (blocked, failed, cancelled), the value isn't kept for a later show.
                displayService.SetPendingParameter(screenId, null);
            }
        }

        private async Task<bool> GoToCoreAsync(string screenId, NavigationHistory history, ScreenParameter? parameter, CancellationToken ct)
        {
            LastStatus = NavigationStatus.Succeeded;
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError("[ScreenNav] GoToAsync failed: screenId is null or empty");
                return Fail(NavigationStatus.InvalidRequest);
            }

            ScreenDefinition screen = screenRegistry.GetScreen(screenId);

            if (screen == null)
            {
                Debug.LogWarning($"[ScreenNav] GoTo skipped: screen not found: {screenId}");
                NavigationEvents.NotifyNavigationFailed(screenId, "Screen not found");
                return Fail(NavigationStatus.NotFound);
            }

            bool ignoreRules = screen.IgnoreNavigationRules;

            if (alertService != null && alertService.IsAlertActive && !ignoreRules)
            {
                Debug.LogWarning($"[ScreenNav] GoToAsync blocked: alert '{alertService.CurrentAlertId}' is active. Dismiss alert first.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Alert active - navigation blocked");
                return Fail(NavigationStatus.BlockedByAlert);
            }

            if (history == NavigationHistory.ClearToRoot)
                return await GoToClearingToRootAsync(screenId, screen, parameter, ct);

            if (screen.ScreenType == ScreenType.Alert)
                return await ShowAlertScreenAsync(screenId, ct);

            // Already showing on top: there's nothing to show again, but a value passed with GoTo still arrives.
            if (parameter != null && IsShowingOnTop(screenId, screen))
            {
                displayService.DeliverParameter(screenId, parameter);
                return Succeed();
            }

            bool isAlreadyShown = navigationStack.MainHistoryContains(screenId);

            if (navigationStack.HasModal && !isAlreadyShown && !ignoreRules)
            {
                Debug.LogWarning($"[ScreenNav] GoToAsync blocked: modal '{navigationStack.GetCurrentModal()}' is active and target screen is new. Dismiss modal first.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Modal active - cannot navigate to new screen");
                return Fail(NavigationStatus.BlockedByModal);
            }

            if (navigationStack.HasModal && isAlreadyShown)
            {
                return await GoToWithModalDismissal(screenId, ct);
            }

            // A screen lower in the stack: going to it means popping back to it.
            if (screen.ScreenType == ScreenType.Fullscreen && isAlreadyShown && navigationStack.PeekMainScreen() != screenId)
                return await PopToAsync(screenId, ct);

            bool opensOverOverlay = ignoreRules && (navigationStack.HasModal || (alertService?.IsAlertActive ?? false));

            if (opensOverOverlay && screen.ScreenType == ScreenType.Modal)
                await ClearAllModalsAsync(ct);

            int historyBefore = navigationStack.MainHistoryCount;
            string modalBefore = navigationStack.GetCurrentModal();
            bool shouldShowScreen = await TryPushToNavigationStackAsync(screenId, screen, ct);

            // TryPush returns false when it didn't push: blocked, already showing, or it popped back instead.
            if (!shouldShowScreen)
                return LastStatus == NavigationStatus.Succeeded;

            if (opensOverOverlay && screen.ScreenType == ScreenType.Fullscreen)
                screenShownOverOverlay = screenId;

            // Undo the push so the stack matches what's visible.
            void UndoPush()
            {
                if (navigationStack.MainHistoryCount > historyBefore && navigationStack.PeekMainScreen() == screenId)
                    navigationStack.PopMainScreen();
                if (modalBefore == null && navigationStack.GetCurrentModal() == screenId)
                    navigationStack.ClearModal();
                if (screenShownOverOverlay == screenId)
                    screenShownOverOverlay = null;
            }

            bool shown;
            try
            {
                shown = await ShowScreenAndNotifyAsync(screenId, ct, autoHideParents: !opensOverOverlay);
            }
            catch (OperationCanceledException)
            {
                UndoPush();
                throw;
            }

            if (!shown)
            {
                UndoPush();
                return Fail(NavigationStatus.Failed);
            }

            return Succeed();
        }

        // Pops every screen above the root and opens the target over it, without showing the root on the way.
        private bool IsShowingOnTop(string screenId, ScreenDefinition screen)
        {
            bool onTop = screen.ScreenType == ScreenType.Modal
                ? navigationStack.GetCurrentModal() == screenId
                : navigationStack.PeekMainScreen() == screenId && !navigationStack.HasModal;
            return onTop && IsScreenCurrentlyVisible(screenId);
        }

        private async Task<bool> GoToClearingToRootAsync(string screenId, ScreenDefinition screen, ScreenParameter? parameter, CancellationToken ct)
        {
            if (screen.ScreenType != ScreenType.Fullscreen)
            {
                Debug.LogError($"[ScreenNav] GoTo({screenId}, ClearToRoot) failed: only fullscreen screens can clear the stack. Use GoTo without it for modals and alerts.");
                NavigationEvents.NotifyNavigationFailed(screenId, "ClearToRoot needs a fullscreen screen");
                return Fail(NavigationStatus.InvalidRequest);
            }

            if (alertService != null && alertService.IsAlertActive)
            {
                Debug.LogWarning($"[ScreenNav] GoTo({screenId}, ClearToRoot) blocked: alert '{alertService.CurrentAlertId}' is active.");
                NavigationEvents.NotifyNavigationFailed(screenId, "Alert active - navigation blocked");
                return Fail(NavigationStatus.BlockedByAlert);
            }

            string root = navigationStack.GetRootScreen();
            if (navigationStack.MainHistoryCount == 0 || string.IsNullOrEmpty(root))
                return await GoToCoreAsync(screenId, NavigationHistory.Push, parameter, ct);

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
                if (parameter != null)
                    displayService.DeliverParameter(screenId, parameter);
                NavigationEvents.NotifyNavigationCompleted(screenId);
                return Succeed();
            }

            bool shown;
            try
            {
                shown = await ShowScreenAndNotifyAsync(screenId, ct, autoHideParents: true);
            }
            catch (OperationCanceledException)
            {
                // The screens above the root are already gone; drop the target so the stack ends at the root.
                if (navigationStack.PeekMainScreen() == screenId)
                    navigationStack.PopMainScreen();
                throw;
            }

            if (!shown)
            {
                if (navigationStack.PeekMainScreen() == screenId)
                    navigationStack.PopMainScreen();
                return Fail(NavigationStatus.Failed);
            }

            return Succeed();
        }

        private async Task<bool> ShowAlertScreenAsync(string screenId, CancellationToken ct)
        {
            if (alertService == null)
            {
                Debug.LogError("[ScreenNav] AlertService not available");
                return Fail(NavigationStatus.Failed);
            }

            navigationStack.ShowAlertScreen(screenId);
            _ = alertService.ShowAlertAsync(screenId);

            if (!await ShowScreenAndNotifyAsync(screenId, ct, autoHideParents: false))
            {
                // An alert that didn't show must not keep blocking navigation.
                navigationStack.DismissAlertScreen();
                alertService.Clear();
                return Fail(NavigationStatus.Failed);
            }

            return Succeed();
        }

        private async Task<bool> GoToWithModalDismissal(string screenId, CancellationToken ct)
        {
            string modalId = navigationStack.DismissModalScreen();
            navigationStack.ClearModal();

            if (!await PopScreensUntilTargetAsync(screenId, ct))
                return Fail(NavigationStatus.Failed);

            bool shown = await ShowTargetScreenAsync(screenId, ct);

            if (displayService.IsScreenInstantiated(modalId))
                await displayService.HideScreen(modalId, screenId, ct);

            return shown ? Succeed() : Fail(NavigationStatus.Failed);
        }

        private async Task<bool> TryPushToNavigationStackAsync(string screenId, ScreenDefinition screen, CancellationToken ct)
        {
            try
            {
                if (screen.ScreenType == ScreenType.Modal)
                {
                    navigationStack.ShowModalScreen(screenId);
                    return true;
                }

                if (!CanShowFullscreenOverModals(screen))
                {
                    Debug.LogError($"[ScreenNav] Cannot show fullscreen ({screenId}) while modal is active. Dismiss modal first.");
                    NavigationEvents.NotifyNavigationFailed(screenId, "Modal active - fullscreen blocked");
                    return Fail(NavigationStatus.BlockedByModal);
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
                            return Fail(NavigationStatus.AlreadyShowing);
                        }

                        return true;
                    }

                    // Popped back to it instead of pushing: the call succeeded if the pop did.
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
                return Fail(NavigationStatus.Failed);
            }
        }

        private bool IsScreenCurrentlyVisible(string screenId)
        {
            List<LayerComponent> layers = displayService.GetScreenLayers(screenId);

            if (layers == null || layers.Count == 0)
            {
                ScreenSystemLog.Info($"[ScreenNav] IsScreenCurrentlyVisible: {screenId} has no layers, NOT visible");
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

        // Returns false when the screen couldn't be shown.
        private async Task<bool> ShowScreenAndNotifyAsync(string screenId, CancellationToken ct, bool autoHideParents = true)
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                NavigationEvents.NotifyNavigationStarted(screenId);

                if (!await displayService.ShowScreen(screenId, autoHideParents, ct))
                {
                    NavigationEvents.NotifyNavigationFailed(screenId, "The screen couldn't be shown");
                    return false;
                }

                NavigationEvents.NotifyScreenPushed(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);
                return true;
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
                return false;
            }
        }

        public async Task<bool> GoBackAsync(CancellationToken ct = default)
        {
            LastStatus = NavigationStatus.Succeeded;
            try
            {
                if (IsCurrentScreenOverOverlay())
                {
                    await GoBackFromScreenOverOverlayAsync(ct);
                    return Succeed();
                }

                if (alertService != null && alertService.IsAlertActive)
                {
                    Debug.LogWarning($"[ScreenNav] GoBackAsync blocked: alert '{alertService.CurrentAlertId}' is active");
                    return Fail(NavigationStatus.BlockedByAlert);
                }

                if (navigationStack.HasModal)
                {
                    await TryGoBackFromModalAsync(ct);
                    return Succeed();
                }

                if (!navigationStack.CanGoBack)
                {
                    NavigationEvents.NotifyBackAtRoot();
                    return Fail(NavigationStatus.NothingToClose);
                }

                string currentScreen = navigationStack.GetCurrentScreen();
                if (navigationStack.IsScreenTheRoot(currentScreen))
                {
                    NavigationEvents.NotifyBackAtRoot();
                    return Fail(NavigationStatus.NothingToClose);
                }

                return await GoBackFromMainScreenAsync(ct) ? Succeed() : Fail(NavigationStatus.Failed);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[ScreenNav] GoBackAsync cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] GoBack failed: {ex.Message}\n{ex.StackTrace}");
                return Fail(NavigationStatus.Failed);
            }
        }

        public async Task<bool> SetRootAsync(string screenId, CancellationToken ct = default)
        {
            LastStatus = NavigationStatus.Succeeded;
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError("[ScreenNav] SetRootAsync failed: screenId is null or empty");
                return Fail(NavigationStatus.InvalidRequest);
            }

            if (alertService != null && alertService.IsAlertActive)
            {
                Debug.LogWarning($"[ScreenNav] SetRootAsync blocked: alert '{alertService.CurrentAlertId}' is active");
                return Fail(NavigationStatus.BlockedByAlert);
            }

            ScreenDefinition screen = screenRegistry.GetScreen(screenId);

            if (screen == null)
            {
                Debug.LogWarning($"[ScreenNav] SetRootAsync failed: screen not found: {screenId}");
                NavigationEvents.NotifyNavigationFailed(screenId, "Screen not found");
                return Fail(NavigationStatus.NotFound);
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
                if (!await displayService.ShowScreen(screenId, true, ct))
                {
                    NavigationEvents.NotifyNavigationFailed(screenId, "The screen couldn't be shown");
                    return Fail(NavigationStatus.Failed);
                }

                NavigationEvents.NotifyScreenPushed(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);
                return Succeed();
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
                return Fail(NavigationStatus.Failed);
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

        private async Task<bool> GoBackFromMainScreenAsync(CancellationToken ct)
        {
            string leavingId = navigationStack.PopMainScreen();

            await ClearAllModalsAsync(ct);

            string returnToScreen = navigationStack.PeekMainScreen();
            await displayService.HideScreen(leavingId, returnToScreen, ct);

            if (returnToScreen != null)
                return await ShowReturnScreenAsync(returnToScreen, ct);

            NavigationEvents.NotifyBackAtRoot();
            return true;
        }

        private async Task<bool> ShowReturnScreenAsync(string screenId, CancellationToken ct)
        {
            try
            {
                if (!await displayService.ShowScreen(screenId, false, ct))
                {
                    NavigationEvents.NotifyNavigationFailed(screenId, "The screen couldn't be shown");
                    return false;
                }

                NavigationEvents.NotifyScreenPopped(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);
                return true;
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
            LastStatus = NavigationStatus.Succeeded;
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError("[ScreenNav] PopToAsync failed: screenId is null or empty");
                return Fail(NavigationStatus.InvalidRequest);
            }

            if (alertService != null && alertService.IsAlertActive)
            {
                Debug.LogWarning($"[ScreenNav] PopToAsync blocked: alert '{alertService.CurrentAlertId}' is active");
                return Fail(NavigationStatus.BlockedByAlert);
            }

            if (!navigationStack.MainHistoryContains(screenId))
            {
                Debug.LogWarning($"[ScreenNav] PopToAsync screen not in history: {screenId}");
                return Fail(NavigationStatus.NotFound);
            }

            NavigationEvents.NotifyNavigationStarted(screenId);

            if (!await PopScreensUntilTargetAsync(screenId, ct))
                return Fail(NavigationStatus.Failed);

            return await ShowTargetScreenAsync(screenId, ct) ? Succeed() : Fail(NavigationStatus.Failed);
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

        // Returns false when the screen couldn't be shown. A cancelled show isn't a failure.
        private async Task<bool> ShowTargetScreenAsync(string screenId, CancellationToken ct)
        {
            try
            {
                if (!await displayService.ShowScreen(screenId, false, ct))
                {
                    NavigationEvents.NotifyNavigationFailed(screenId, "The screen couldn't be shown");
                    return false;
                }

                NavigationEvents.NotifyScreenPopped(screenId);
                NavigationEvents.NotifyNavigationCompleted(screenId);
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[ScreenNav] PopTo show cancelled: {screenId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenNav] PopTo show failed: {ex.Message}\n{ex.StackTrace}");
                return false;
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
            return Succeed();
        }

        public async Task<bool> DismissAlertAsync(CancellationToken ct = default)
        {
            string alertId = navigationStack.DismissAlertScreen();

            if (string.IsNullOrEmpty(alertId))
                return Fail(NavigationStatus.NothingToClose);

            alertService?.Clear();
            await displayService.HideScreen(alertId, null, ct);
            return Succeed();
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

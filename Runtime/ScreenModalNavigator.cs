using System;
using System.Threading;
using System.Threading.Tasks;
using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEngine;

namespace Tools.ScreenSystem.Scripts
{
    public class ScreenModalNavigator
    {
        private const string LOG_PREFIX = "[ScreenNav]";

        private readonly IScreenDisplay displayService;
        private readonly ScreenNavigationStack navigationStack;
        private readonly ScreenRegistry screenRegistry;

        public ScreenModalNavigator(IScreenDisplay displayService, ScreenNavigationStack navigationStack, ScreenRegistry screenRegistry = null)
        {
            this.displayService = displayService;
            this.navigationStack = navigationStack;
            this.screenRegistry = screenRegistry;
        }

        public async Task ShowModalAsync(string modalScreenId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(modalScreenId))
            {
                Debug.LogError($"{LOG_PREFIX} ShowModalAsync failed: modalScreenId is null or empty");
                return;
            }

            if (screenRegistry != null)
            {
                ScreenDefinition screen = screenRegistry.GetScreen(modalScreenId);
                if (screen == null)
                {
                    if (modalScreenId.Equals("QuitConfirmationModal", StringComparison.OrdinalIgnoreCase))
                        Debug.LogWarning($"{LOG_PREFIX} ShowModalAsync: QuitConfirmationModal not found in registry.");
                    else
                        Debug.LogError($"{LOG_PREFIX} ShowModalAsync failed: modal not found: {modalScreenId}");
                    return;
                }
            }

            if (navigationStack.HasModal)
            {
                string currentModal = navigationStack.GetCurrentModal();
                if (currentModal == modalScreenId)
                {
                    Debug.LogWarning($"{LOG_PREFIX} Modal already shown: {modalScreenId}. Ignoring duplicate request.");
                    return;
                }

                Debug.LogWarning($"{LOG_PREFIX} Modal already open ({currentModal}). Completely dismissing to show new modal ({modalScreenId})");
                try
                {
                    if (displayService.IsScreenInstantiated(currentModal))
                        await displayService.HideScreen(currentModal, null, ct);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"{LOG_PREFIX} Failed to dismiss existing modal: {ex.Message}");
                }
            }

            try
            {
                navigationStack.ShowModalScreen(modalScreenId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} ShowModal stack failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }

            try
            {
                await displayService.ShowScreen(modalScreenId, false, ct);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LOG_PREFIX} ShowModal cancelled: {modalScreenId}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} ShowModal failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public async Task HideModalAsync(string modalScreenId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(modalScreenId))
            {
                Debug.LogError($"{LOG_PREFIX} HideModalAsync failed: modalScreenId is null or empty");
                return;
            }

            try
            {
                if (navigationStack.HasModal)
                {
                    string currentModal = navigationStack.GetCurrentModal();
                    if (currentModal == modalScreenId)
                    {
                        navigationStack.DismissModalScreen();
                    }
                    else
                        Debug.LogWarning($"{LOG_PREFIX} Modal mismatch - expected {modalScreenId}, found {currentModal} at top");
                }

                await displayService.HideScreen(modalScreenId, null, ct);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LOG_PREFIX} HideModal cancelled: {modalScreenId}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} HideModal failed: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }
    }
}


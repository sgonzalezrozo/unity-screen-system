using System;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem.Services
{
    public class BackButtonNavigationHandler : IBackButtonHandler
    {
        private readonly IScreenNavigation navigationService;
        private readonly ScreenDisplayService displayService;
        private readonly ScreenRegistry screenRegistry;
        private bool isHandling;

        public bool IsHandling => isHandling;

        public BackButtonNavigationHandler(
            IScreenNavigation navigationService,
            ScreenDisplayService displayService,
            ScreenRegistry screenRegistry = null)
        {
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.displayService = displayService ?? throw new ArgumentNullException(nameof(displayService));
            this.screenRegistry = screenRegistry;
        }

        public async Task HandleBackPressAsync(CancellationToken ct = default)
        {
            if (isHandling)
                return;

            isHandling = true;

            try
            {
                string topScreen = GetTopScreen();
                if (topScreen != null && IsClosableScreen(topScreen))
                {
                    await navigationService.GoBackAsync();
                    return;
                }

                await navigationService.GoBackAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[BackButtonHandler] GoBack canceled");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                isHandling = false;
            }
        }

        private bool IsClosableScreen(string screenId)
        {
            if (screenRegistry == null)
            {
                Debug.LogWarning("[BackButtonHandler] ScreenRegistry not set, allowing all closable screens");
                return true;
            }

            ScreenDefinition screen = screenRegistry.GetScreen(screenId);
            if (screen != null)
                return screen.screenType != ScreenType.fullscreen;

            Debug.LogWarning($"[BackButtonHandler] Screen not found in registry: {screenId}");
            return true;
        }

        private string GetTopScreen()
            => displayService?.GetTopScreen();
    }
}


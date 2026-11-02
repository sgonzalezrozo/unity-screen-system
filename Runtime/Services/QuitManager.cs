using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts.Services
{
    public class QuitManager : IQuitManager
    {
        private readonly IScreenNavigation navigationService;
        private readonly string quitConfirmationModalId;

        public QuitManager(
            IScreenNavigation navigationService,
            string quitConfirmationModalId)
        {
            this.navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            this.quitConfirmationModalId = quitConfirmationModalId;
        }

        public async Task HandleQuitRequestAsync(CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(quitConfirmationModalId))
            {
                bool success = await navigationService.GoToAsync(quitConfirmationModalId, ct);

                if (!success)
                    QuitApplication();
            }
            else
            {
                QuitApplication();
            }
        }

        private static void QuitApplication()
        {
            NavigationEvents.NotifyQuitApplicationRequested();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}


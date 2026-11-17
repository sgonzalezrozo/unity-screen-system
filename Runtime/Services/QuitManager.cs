using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem.Services
{
    internal class QuitManager : IQuitManager
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

        /// <summary>Replaces the actual quit, for tests. Quit requested is still raised.</summary>
        internal static Action QuitOverride;

        internal static void QuitApplication()
        {
            NavigationEvents.NotifyQuitApplicationRequested();

            if (QuitOverride != null)
            {
                QuitOverride();
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }
    }
}


#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts
{
    public class AlertManager : IAlertService
    {
        private string? currentAlertId;
        private TaskCompletionSource<bool>? dismissalSource;

        public bool IsAlertActive
            => !string.IsNullOrEmpty(currentAlertId);

        public string? CurrentAlertId
            => currentAlertId;

        public async Task ShowAlertAsync(string alertId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(alertId))
                return;

            currentAlertId = alertId;
            dismissalSource = new TaskCompletionSource<bool>();

            try
            {
                using (ct.Register(() => dismissalSource?.TrySetCanceled()))
                {
                    if (dismissalSource != null)
                        await dismissalSource.Task;
                }
            }
            finally
            {
                currentAlertId = null!;
                dismissalSource = null!;
            }
        }

        public async Task DismissAlertAsync(CancellationToken ct = default)
        {
            if (!IsAlertActive || dismissalSource == null)
                return;

            dismissalSource.TrySetResult(true);
            await Task.CompletedTask;
        }
    }
}


#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
    internal class AlertManager : IAlertService
    {
        private string? currentAlertId;
        private TaskCompletionSource<bool>? dismissalSource;

        public bool IsAlertActive
            => !string.IsNullOrEmpty(currentAlertId);

        public string? CurrentAlertId
            => currentAlertId;

        public Task ShowAlertAsync(string alertId, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(alertId))
                return Task.CompletedTask;

            currentAlertId = alertId;
            dismissalSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (ct.CanBeCanceled)
                ct.Register(Clear);

            return dismissalSource.Task;
        }

        public Task DismissAlertAsync(CancellationToken ct = default)
        {
            Clear();
            return Task.CompletedTask;
        }

        // State is cleared here and not after the awaited task, which Unity resumes on a later
        // frame. Navigation must work straight after an alert is dismissed.
        public void Clear()
        {
            TaskCompletionSource<bool>? source = dismissalSource;

            currentAlertId = null;
            dismissalSource = null;

            source?.TrySetResult(true);
        }
    }
}

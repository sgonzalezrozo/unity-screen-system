#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
    public interface IAlertService
    {
        bool IsAlertActive { get; }
        string? CurrentAlertId { get; }

        Task ShowAlertAsync(string alertId, CancellationToken ct = default);
        Task DismissAlertAsync(CancellationToken ct = default);
    }
}


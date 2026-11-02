using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem.Services
{
    public interface IBackButtonHandler
    {
        Task HandleBackPressAsync(CancellationToken ct = default);

        bool IsHandling { get; }
    }
}


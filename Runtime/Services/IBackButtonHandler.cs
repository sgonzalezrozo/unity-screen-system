using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts.Services
{
    public interface IBackButtonHandler
    {
        Task HandleBackPressAsync(CancellationToken ct = default);

        bool IsHandling { get; }
    }
}


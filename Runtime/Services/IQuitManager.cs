using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem.Services
{
    public interface IQuitManager
    {
        Task HandleQuitRequestAsync(CancellationToken ct = default);
    }
}


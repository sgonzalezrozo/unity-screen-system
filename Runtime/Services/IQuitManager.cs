using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts.Services
{
    public interface IQuitManager
    {
        Task HandleQuitRequestAsync(CancellationToken ct = default);
    }
}


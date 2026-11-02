using System.Threading;
using System.Threading.Tasks;
using Tools.ScreenSystem.Scripts.ScriptableObjects;

namespace Tools.ScreenSystem.Scripts.Services
{
    public interface IScreenInitializer
    {
        Task<bool> InitializeAsync(CancellationToken ct = default);
        bool IsInitialized { get; }
        string InitializationError { get; }
        ScreenRegistry GetRegistry();
        ScreenDisplayService GetDisplayService();
    }
}


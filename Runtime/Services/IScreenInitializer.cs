using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;

namespace ScreenSystem.Services
{
    internal interface IScreenInitializer
    {
        Task<bool> InitializeAsync(CancellationToken ct = default);
        bool IsInitialized { get; }
        string InitializationError { get; }
        ScreenRegistry GetRegistry();
        ScreenDisplayService GetDisplayService();
    }
}


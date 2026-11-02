using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts
{
    public interface IScreenContent
    {
        Task InitializeAsync();

        void Cleanup();
    }
}


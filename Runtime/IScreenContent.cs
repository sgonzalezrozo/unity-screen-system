using System.Threading.Tasks;

namespace ScreenSystem
{
    public interface IScreenContent
    {
        Task InitializeAsync();

        void Cleanup();
    }
}


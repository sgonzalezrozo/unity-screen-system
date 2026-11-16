using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
	public interface IScreenNavigationService
	{
		Task<bool> GoTo(string screenId, CancellationToken ct = default);
		Task<bool> GoTo(string screenId, NavigationHistory history, CancellationToken ct = default);
		Task<bool> GoTo<T>(string screenId, T parameter, CancellationToken ct = default);
		Task<bool> GoTo<T>(string screenId, T parameter, NavigationHistory history, CancellationToken ct = default);
		Task<bool> GoBack(CancellationToken ct = default);
		Task<bool> SetRoot(string screenId, CancellationToken ct = default);
		bool CanGoBack { get; }
		string GetCurrentScreen();
		string GetRootScreen();
		void PrepareForSceneChange();
	}
}




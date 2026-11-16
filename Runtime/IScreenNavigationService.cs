using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
	public interface IScreenNavigationService
	{
		Task<NavigationResult> GoTo(string screenId, CancellationToken ct = default);
		Task<NavigationResult> GoTo(string screenId, NavigationHistory history, CancellationToken ct = default);
		Task<NavigationResult> GoTo<T>(string screenId, T parameter, CancellationToken ct = default);
		Task<NavigationResult> GoTo<T>(string screenId, T parameter, NavigationHistory history, CancellationToken ct = default);
		Task<NavigationResult> GoBack(CancellationToken ct = default);
		Task<NavigationResult> SetRoot(string screenId, CancellationToken ct = default);
		bool CanGoBack { get; }
		string GetCurrentScreen();
		string GetRootScreen();
		void PrepareForSceneChange();
	}
}




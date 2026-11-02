using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts
{
	public interface IScreenNavigation
	{
		Task<bool> GoToAsync(string screenId, CancellationToken ct = default);
		Task<bool> GoBackAsync(CancellationToken ct = default);
		Task<bool> PopToAsync(string screenId, CancellationToken ct = default);
		Task<bool> SetRootAsync(string screenId, CancellationToken ct = default);
		Task NavigateFromModalToScreenAsync(string fullscreenId, CancellationToken ct = default);
		Task HideAllScreensAsync(CancellationToken ct = default);
		Task RestoreAllScreensAsync(CancellationToken ct = default);
		bool CanGoBack { get; }
		string GetCurrentScreen();
		string GetRootScreen();
	}
}


using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
	public interface IScreenNavigation
	{
		Task<bool> GoToAsync(string screenId, CancellationToken ct = default);
		Task<bool> GoBackAsync(CancellationToken ct = default);
		Task<bool> PopToAsync(string screenId, CancellationToken ct = default);
		Task<bool> SetRootAsync(string screenId, CancellationToken ct = default);
		Task<bool> DismissAlertAsync(CancellationToken ct = default);
		Task NavigateFromModalToScreenAsync(string fullscreenId, CancellationToken ct = default);
		Task HideAllScreensAsync(CancellationToken ct = default);
		Task RestoreAllScreensAsync(CancellationToken ct = default);
		bool CanGoBack { get; }
		bool IsAlertActive { get; }
		string GetCurrentScreen();
		string GetRootScreen();
	}
}


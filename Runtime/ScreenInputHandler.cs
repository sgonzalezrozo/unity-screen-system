using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScreenSystem
{
	public class ScreenInputHandler
	{
		private readonly IScreenNavigationService screenNavigation;
		private bool isHandling;

		public bool IsHandling
			=> isHandling;

		public ScreenInputHandler(IScreenNavigationService screenNavigation)
		{
			this.screenNavigation = screenNavigation;
		}

		public void CheckForInput(CancellationToken ct)
		{
			if (Keyboard.current?.escapeKey.wasPressedThisFrame ?? false)
				HandleBackButton(ct);
		}

		private void HandleBackButton(CancellationToken ct)
		{
			if (isHandling || screenNavigation == null)
				return;

			isHandling = true;
			_ = GoBackAsync(ct);
		}

		private async Task GoBackAsync(CancellationToken ct)
		{
			try
			{
				await screenNavigation.GoBack(ct);
			}
			catch (OperationCanceledException)
			{
				Debug.LogWarning("[ScreenInputHandler] GoBack cancelled");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[ScreenInputHandler] GoBack failed: {ex.Message}");
			}
			finally
			{
				isHandling = false;
			}
		}
	}
}


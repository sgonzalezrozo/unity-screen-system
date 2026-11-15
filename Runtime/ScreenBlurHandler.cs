using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
	/// <summary>Connects showing a modal or alert to its backdrop. Fullscreen screens get none.</summary>
	public class ScreenBlurHandler
	{
		private readonly BlurBackgroundService blurService;

		public ScreenBlurHandler(BlurBackgroundService blurService)
		{
			this.blurService = blurService;
		}

		private static bool IsOverlay(ScreenDefinition screen)
			=> screen != null && (screen.screenType == ScreenType.modal || screen.screenType == ScreenType.alert);

		/// <summary>Before the overlay's layers become visible: captures the screen underneath when needed.</summary>
		public Task PrepareAsync(string screenId, ScreenDefinition screen, CancellationToken ct)
			=> blurService != null && IsOverlay(screen) ? blurService.PrepareAsync(screenId, screen.screenType, ct) : Task.CompletedTask;

		/// <summary>Once the overlay's layers are visible: places the snapshot or the dim below them.</summary>
		public void Apply(string screenId, ScreenDefinition screen, List<LayerComponent> layers)
		{
			if (blurService != null && IsOverlay(screen))
				blurService.Apply(screenId, screen.screenType, layers);
		}
	}
}

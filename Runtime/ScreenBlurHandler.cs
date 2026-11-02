using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
	public class ScreenBlurHandler
	{
		private readonly BlurBackgroundService blurService;

		public ScreenBlurHandler(BlurBackgroundService blurService)
		{
			this.blurService = blurService;
		}

		public async Task ApplyBlurAsync(string screenId, ScreenDefinition screen, CancellationToken ct)
        {
            if (blurService == null)
				return;

            switch (screen.screenType)
            {
                case ScreenType.modal:
                    await blurService.ShowBlurForModalAsync(screenId, ct);
                    break;
                case ScreenType.fullscreen:
                    await blurService.HideBlurForModalAsync(screenId, ct);
                    break;
            }
        }
	}
}

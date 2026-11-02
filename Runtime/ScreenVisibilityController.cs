using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts
{
    public class ScreenVisibilityController
    {
        private readonly IScreenDisplay displayService;
        private string hiddenScreenId;

        public ScreenVisibilityController(IScreenDisplay displayService)
        {
            this.displayService = displayService;
        }

        public async Task HideAllScreensAsync(CancellationToken ct = default)
        {
            Dictionary<string, List<LayerComponent>> cachedScreens = displayService.GetCachedScreens();

            hiddenScreenId = null;
            if (cachedScreens.Count > 0)
            {
                foreach (string screenId in cachedScreens.Keys)
                {
                    if (!displayService.IsScreenInstantiated(screenId))
                        continue;

                    hiddenScreenId = screenId;
                    break;
                }
            }

            foreach (string screenId in cachedScreens.Keys)
            {
                ct.ThrowIfCancellationRequested();

                if (displayService.IsScreenInstantiated(screenId))
                    await displayService.HideScreen(screenId, null, ct);
            }
        }

        public async Task RestoreAllScreensAsync(CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(hiddenScreenId))
                return;

            if (displayService.IsScreenInstantiated(hiddenScreenId))
            {
                await displayService.ShowScreen(hiddenScreenId, true, ct);
            }

            hiddenScreenId = null;
        }
    }
}


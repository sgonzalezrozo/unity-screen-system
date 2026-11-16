using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScreenSystem
{
    internal interface IScreenHideService
    {
        Task HideScreenAsync(string screenId, string preserveLayersForScreenId = null, CancellationToken ct = default);
        void HideOtherScreensForType(string currentScreenId, ScreenType screenType);
        Task HideOtherScreensAsync(string currentScreenId, ScreenType screenType, CancellationToken ct, List<LayerComponent> preserveLayers = null);
    }
}


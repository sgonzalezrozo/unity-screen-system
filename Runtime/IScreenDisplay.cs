using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts
{
    public interface IScreenDisplay
    {
        Task ShowScreen(string screenId, bool autoHideParents = true, CancellationToken ct = default);
        Task HideScreen(string screenId, string preserveLayersForScreenId = null, CancellationToken ct = default);
        string GetTopScreen();
        List<string> GetActiveScreenIds();
        List<LayerComponent> GetScreenLayers(string screenId);
        void DestroyScreen(string screenId);
        void UnregisterScreenWithoutDestroy(string screenId);
        bool IsScreenInstantiated(string screenId);
        Dictionary<string, List<LayerComponent>> GetCachedScreens();
        void ClearCachedScreens();
    }
}


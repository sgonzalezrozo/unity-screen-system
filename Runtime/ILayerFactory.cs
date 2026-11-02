using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tools.ScreenSystem.Scripts
{    public interface ILayerFactory
    {
        Task<List<LayerComponent>> GetOrInstantiateLayers(string screenId, ScreenDefinition screen, string readableId = null, CancellationToken ct = default);
        void ReassignLayersToScreen(List<LayerComponent> layers, string screenId, ScreenDefinition screen, string readableId = null);
        Task PreloadLayer(string layerName, CancellationToken ct = default);
        Task PreWarmFeatureShells(string featureCategory, List<ScreenDefinition> screens, CancellationToken ct = default);
        Task PreWarmInitialShells(List<ScreenDefinition> allScreens, CancellationToken ct = default);
    }
}

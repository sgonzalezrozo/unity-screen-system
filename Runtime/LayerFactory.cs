using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using Unity.Profiling;
using UnityEngine;

namespace ScreenSystem
{
    public class LayerFactory : ILayerFactory
    {
        private const int INITIAL_LAYER_CAPACITY = 8;
        private const int INITIAL_PREWARM_CAPACITY = 16;

        private readonly LayerInitializer layerInitializer;
        private readonly LayerZOrderManager zOrderManager;

        public LayerFactory(Transform screenContainer, ScreenCacheManager cacheManager = null)
        {
            layerInitializer = new LayerInitializer(screenContainer);
            zOrderManager = new LayerZOrderManager(cacheManager);
        }

        public async Task<List<LayerComponent>> GetOrInstantiateLayers(string screenId, ScreenDefinition screen, string readableId = null, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            string logId = readableId ?? screenId;

            GameObject screenPrefab = await LayerPrefabLoader.LoadScreenPrefab(screen, ct);
            if (screenPrefab == null)
            {
                Debug.LogError($"[LayerFactory] Failed to load screen prefab '{screen.ScreenName}'");
                return new List<LayerComponent>(0);
            }

            ct.ThrowIfCancellationRequested();

            LayerDefinition layerDef = screenPrefab.GetComponent<LayerDefinition>();
            int estimatedCapacity = layerDef != null && layerDef.LayerCount > 0 ? layerDef.LayerCount + 1 : INITIAL_LAYER_CAPACITY;
            List<LayerComponent> screenLayers = new(estimatedCapacity);

            int baseZ = zOrderManager.CalculateBaseZOrder(screen.screenType, screenId);

            // Start the loads of this screen's layers that aren't instantiated yet, all at once. Shared layers
            // already in the scene start nothing. The loop below then finds each prefab loaded.
            if (layerDef != null && layerDef.LayerCount > 0)
            {
                List<string> missing = new(layerDef.LayerCount);
                for (int i = 0; i < layerDef.LayerCount; i++)
                {
                    LayerEntry entry = layerDef.GetLayer(i);
                    string name = layerDef.GetLayerName(i);
                    if (entry != null && !entry.isScreenSeparator && !layerInitializer.HasCachedLayer(name))
                        missing.Add(name);
                }

                if (missing.Count > 0)
                    await LayerPrefabLoader.LoadPrefabsAsync(missing, ct);
            }

            GameObject mainLayerObj = await layerInitializer.GetOrInstantiateLayer(screen.AssetPath, ct);
            // A cancelled load returns null. Report it as a cancellation, not as a missing prefab.
            ct.ThrowIfCancellationRequested();
            if (mainLayerObj == null)
            {
                Debug.LogError($"[LayerFactory] Failed to get main layer for screen '{screen.ScreenName}' (AssetPath: '{screen.AssetPath}')");
                return screenLayers;
            }

            ct.ThrowIfCancellationRequested();

            LayerComponent mainLayer = mainLayerObj.GetComponent<LayerComponent>();
            if (mainLayer == null)
                mainLayer = mainLayerObj.AddComponent<LayerComponent>();

            InjectLayerComponents(mainLayerObj);

            mainLayer.AllowInitialization();

            bool shouldShowMainLayer = !IsLayerAlreadyVisibleAndAssigned(mainLayer);
            mainLayer.AssignToScreen(screenId, baseZ, 0, logId);
            if (shouldShowMainLayer)
                mainLayer.Show();

            screenLayers.Add(mainLayer);

            if (layerDef == null || layerDef.LayerCount <= 0)
                return screenLayers;

            for (int i = 0; i < layerDef.LayerCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                LayerEntry entry = layerDef.GetLayer(i);
                if (entry != null && entry.isScreenSeparator)
                    continue;

                string layerName = layerDef.GetLayerName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                GameObject layerObj = await layerInitializer.GetOrInstantiateLayer(layerName, ct);
                ct.ThrowIfCancellationRequested();
                if (layerObj == null)
                    continue;

                ct.ThrowIfCancellationRequested();

                LayerComponent layer = layerObj.GetComponent<LayerComponent>();
                if (layer == null)
                    layer = layerObj.AddComponent<LayerComponent>();

                InjectLayerComponents(layerObj);

                layer.AllowInitialization();

                int zOffset = layerDef.GetLayerZOffset(i);
                int finalZOrder = baseZ + zOffset;

                bool shouldShowLayer = !IsLayerAlreadyVisibleAndAssigned(layer);
                layer.AssignToScreen(screenId, finalZOrder, screenLayers.Count, logId);
                if (shouldShowLayer)
                    layer.Show();

                screenLayers.Add(layer);
            }

            return screenLayers;
        }

        public void ReassignLayersToScreen(List<LayerComponent> layers, string screenId, ScreenDefinition screen, string readableId = null)
        {
            using ProfilerMarker.AutoScope _ = ScreenSystemMarkers.ReassignLayers.Auto();
            if (layers == null || layers.Count == 0)
                return;

            int baseZ = zOrderManager.CalculateBaseZOrder(screen.screenType, screenId);
            GameObject loadedPrefab = LayerPrefabLoader.GetLoadedScreenPrefab(screen);
            int[] zOffsets = zOrderManager.GetLayerZOffsetsFromScreen(screen, layers.Count, loadedPrefab);
            string logId = readableId ?? screenId;

            for (int i = 0; i < layers.Count; i++)
            {
                LayerComponent layer = layers[i];
                if (layer == null)
                    continue;

                int finalZOrder = baseZ + zOffsets[i];

                // Always reassign when caching to ensure Z order is properly updated.
                // The optimization check is unsafe with shared layers - a layer might be assigned to
                // multiple screens (e.g., TopBar on both ShopDetail and ShopMain), and the current
                // Z order might reflect a DIFFERENT screen's assignment, not this screen's assignment.
                // Removing the check ensures that when each screen is activated, its layers get the
                // correct Z order applied.
                layer.AssignToScreen(screenId, finalZOrder, i, logId);
            }
        }

        public async Task PreloadLayer(string layerName, CancellationToken ct = default)
        {
            await layerInitializer.GetOrInstantiateLayer(layerName, ct);
        }

        public async Task PreWarmFeatureShells(string featureCategory, List<ScreenDefinition> screens, CancellationToken ct = default)
        {
            if (screens == null || screens.Count == 0)
                return;

            HashSet<string> uniqueLayerNames = new(INITIAL_PREWARM_CAPACITY);

            for (int i = 0; i < screens.Count; i++)
            {
                ScreenDefinition screen = screens[i];

                GameObject screenPrefab = await LayerPrefabLoader.LoadScreenPrefab(screen, ct);
                if (screenPrefab == null)
                    continue;

                LayerDefinition layerDef = screenPrefab.GetComponent<LayerDefinition>();
                if (layerDef == null)
                    continue;

                for (int j = 0; j < layerDef.LayerCount; j++)
                {
                    LayerEntry entry = layerDef.GetLayer(j);
                    if (entry != null && entry.isScreenSeparator)
                        continue;

                    string layerName = layerDef.GetLayerName(j);
                    if (!string.IsNullOrEmpty(layerName))
                        uniqueLayerNames.Add(layerName);
                }
            }

            if (uniqueLayerNames.Count == 0)
                return;

            List<Task> prewarmTasks = new(uniqueLayerNames.Count);
            foreach (string layerName in uniqueLayerNames)
            {
                ct.ThrowIfCancellationRequested();
                prewarmTasks.Add(layerInitializer.GetOrInstantiateLayer(layerName, ct));
            }

            await Task.WhenAll(prewarmTasks);
        }

        public async Task PreWarmInitialShells(List<ScreenDefinition> allScreens, CancellationToken ct = default)
        {
            if (allScreens == null || allScreens.Count == 0)
                return;

            for (int i = 0; i < allScreens.Count; i++)
            {
                ScreenDefinition screen = allScreens[i];
                ct.ThrowIfCancellationRequested();

                GameObject screenPrefab = await LayerPrefabLoader.LoadScreenPrefab(screen, ct);

                if (screenPrefab == null)
                    Debug.LogWarning($"[LayerFactory] PreWarm: Could not load screen prefab '{screen.ScreenName}'");
            }
        }

        public LayerInitializer GetLayerInitializer()
            => layerInitializer;

        public HashSet<string> ClearAllCachedLayers(ISet<GameObject> keep = null)
            => layerInitializer.ClearAllCachedLayers(keep);

        private void InjectLayerComponents(GameObject layerObj)
        {
            using ProfilerMarker.AutoScope _ = ScreenSystemMarkers.InjectLayer.Auto();
            MonoBehaviour[] components = layerObj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour component in components)
            {
                if (component != null)
                    ToolInjectionServices.MonoBehaviourInjector.InjectIfNeeded(component);
            }
        }

        private bool IsLayerAlreadyVisibleAndAssigned(LayerComponent layer)
        {
            if (layer == null)
                return false;

            // IsVisible covers UI Toolkit layers too, so a shared UI Toolkit layer isn't shown again with its callbacks.
            bool isGameObjectActive = layer.gameObject.activeSelf;
            bool isVisible = layer.IsVisible;
            bool isAlreadyAssigned = !string.IsNullOrEmpty(layer.CurrentScreenId);

            return isGameObjectActive && isVisible && isAlreadyAssigned;
        }
    }
}

using System.Collections.Generic;
using ScreenSystem.ScriptableObjects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScreenSystem
{
    internal class ScreenCacheManager
    {
        private readonly LoggingDictionary<string, List<LayerComponent>> instantiatedScreenLayers = new("ScreenCache");
        private readonly List<string> activeScreenIds = new();
        private readonly HashSet<string> alwaysKeptScreenIds = new();

        public bool TryGetCachedLayers(string screenId, out List<LayerComponent> layers)
            => instantiatedScreenLayers.TryGetValue(screenId, out layers);

        public void CacheLayers(string screenId, List<LayerComponent> layers)
            => instantiatedScreenLayers[screenId] = layers;

        public bool IsScreenCached(string screenId)
            => instantiatedScreenLayers.ContainsKey(screenId);

        public List<LayerComponent> GetScreenLayers(string screenId)
        {
            bool found = instantiatedScreenLayers.TryGetValue(screenId, out List<LayerComponent> layers);
            if (!found || layers == null)
                return new List<LayerComponent>();

            return layers;
        }

        public void RegisterActiveScreen(string screenId)
        {
            if (!activeScreenIds.Contains(screenId))
                activeScreenIds.Add(screenId);
        }

        public void UnregisterActiveScreen(string screenId)
        {
            activeScreenIds.Remove(screenId);
        }

        public void UnregisterScreenFromAllLevels(string screenId)
        {
            activeScreenIds.Remove(screenId);
        }

        public string GetTopScreen()
        {
            return activeScreenIds.Count > 0 ? activeScreenIds[^1] : null;
        }

        public IReadOnlyList<string> GetActiveScreens()
        {
            return activeScreenIds.AsReadOnly();
        }

        public void RemoveScreen(string screenId)
        {
            instantiatedScreenLayers.Remove(screenId);
        }

        public void ClearActiveScreens()
        {
            activeScreenIds.Clear();
        }

        public void ClearCachedScreens()
            => instantiatedScreenLayers.Clear();

        public void HidePersistentScreenLayers(ScreenRegistry screenRegistry)
        {
            foreach (KeyValuePair<string, List<LayerComponent>> kvp in instantiatedScreenLayers.Pairs)
            {
                string screenId = kvp.Key;
                List<LayerComponent> layers = kvp.Value;

                if (layers == null || layers.Count == 0)
                    continue;

                ScreenDefinition screen = screenRegistry.GetScreen(screenId);
                bool isPersistent = IsScreenPersistent(screen);

                if (!isPersistent)
                    continue;

                foreach (LayerComponent layer in layers)
                {
                    if (layer == null)
                        continue;

                    // DON'T unassign shared layers - they need to maintain their association
                    // Only hide the canvas temporarily during scene transition
                    // layer.UnassignFromScreen(screenId); // REMOVED

                    Canvas canvas = layer.GetComponent<Canvas>();
                    if (canvas != null)
                        canvas.enabled = false;

                    if (layer.GraphicRaycaster != null)
                        layer.GraphicRaycaster.enabled = false;
                }
            }
        }

        /// <summary>Every cached screen's layers, enumerable without copying or allocating.</summary>
        public Dictionary<string, List<LayerComponent>>.ValueCollection CachedLayerLists
            => instantiatedScreenLayers.ValuesView;

        public Dictionary<string, List<LayerComponent>> GetAllCachedScreens()
        {
            Dictionary<string, List<LayerComponent>> result = new(instantiatedScreenLayers.Count);
            foreach (KeyValuePair<string, List<LayerComponent>> kvp in instantiatedScreenLayers.Pairs)
                result[kvp.Key] = kvp.Value;
            return result;
        }

        public void RemoveNonPersistentScreens(ScreenRegistry screenRegistry, LayerInitializer layerInitializer = null)
        {
            HashSet<string> screensToRemove = GetNonPersistentScreenIds(screenRegistry);
            HashSet<string> layersUsedByStayingScreens = new();
            foreach (KeyValuePair<string, List<LayerComponent>> kvp in instantiatedScreenLayers.Pairs)
            {
                string screenId = kvp.Key;

                if (screensToRemove.Contains(screenId))
                    continue;

                List<LayerComponent> layers = kvp.Value;
                if (layers != null)
                {
                    foreach (LayerComponent layer in layers)
                    {
                        if (layer != null && layer.gameObject != null)
                            layersUsedByStayingScreens.Add(layer.LayerName);
                    }
                }
            }

            foreach (string screenId in screensToRemove)
            {
                List<LayerComponent> layers = instantiatedScreenLayers.TryGetValue(screenId, out List<LayerComponent> layerList) ? layerList : null;
                if (layers != null && layers.Count > 0)
                {
                    foreach (LayerComponent layer in layers)
                    {
                        if (layer == null || layer.gameObject == null)
                            continue;

                        string layerName = layer.LayerName;

                        if (layersUsedByStayingScreens.Contains(layerName))
                        {
                            ScreenSystemLog.Info($"[ScreenCacheManager] Preserving layer used by staying screen: {layerName} (removing from: {screenId})");
                            continue;
                        }

                        Object.DestroyImmediate(layer.gameObject);
                    }
                }

                instantiatedScreenLayers.Remove(screenId);
            }
        }

        public HashSet<string> GetNonPersistentScreenIds(ScreenRegistry screenRegistry)
        {
            HashSet<string> screensToRemove = new();

            foreach (KeyValuePair<string, List<LayerComponent>> kvp in instantiatedScreenLayers.Pairs)
            {
                string screenId = kvp.Key;
                ScreenDefinition screen = screenRegistry.GetScreen(screenId);

                bool isPersistent = IsScreenPersistent(screen);

                if (!isPersistent)
                    screensToRemove.Add(screenId);
            }

            return screensToRemove;
        }

        public int UnloadPersistentScreenContent(ScreenRegistry screenRegistry)
        {
            int contentUnloadedCount = 0;

            foreach (KeyValuePair<string, List<LayerComponent>> kvp in instantiatedScreenLayers.Pairs)
            {
                string screenId = kvp.Key;
                List<LayerComponent> layers = kvp.Value;

                if (layers == null || layers.Count == 0)
                    continue;

                ScreenDefinition screen = screenRegistry.GetScreen(screenId);
                bool isPersistent = IsScreenPersistent(screen);

                if (!isPersistent)
                    continue;

                foreach (LayerComponent layer in layers)
                {
                    if (layer == null || !layer.HasContent)
                        continue;

                    layer.DestroyContent();
                    contentUnloadedCount++;
                }
            }

            return contentUnloadedCount;
        }

        /// <summary>True for screens with Persist Over Scenes and for the special screens (splash and quit confirmation).</summary>
        public bool IsScreenPersistent(ScreenDefinition screen)
            => ScenePersistence.IsKept(screen, alwaysKeptScreenIds);

        /// <summary>Screens kept on scene changes even without Persist Over Scenes, by ID.</summary>
        public void SetAlwaysKeptScreens(IEnumerable<string> screenIds)
        {
            alwaysKeptScreenIds.Clear();
            if (screenIds != null)
                alwaysKeptScreenIds.UnionWith(screenIds);
        }
    }
}

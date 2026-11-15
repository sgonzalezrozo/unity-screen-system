using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScreenSystem
{
    public class LayerInitializer
    {
        private readonly Transform screenContainer;
        private readonly IPrefabInstantiator instantiator;
        private readonly LoggingDictionary<string, GameObject> layerCache = new("LayerCache");

         public LayerInitializer(Transform screenContainer, IPrefabInstantiator instantiator = null)
         {
             this.screenContainer = screenContainer;
             this.instantiator = instantiator ?? ToolInjectionServices.PrefabInstantiator;
         }

         public async Task<GameObject> GetOrInstantiateLayer(string layerName, CancellationToken ct = default)
         {
             if (layerCache.TryGetValue(layerName, out GameObject cachedLayer) && cachedLayer != null)
                 return cachedLayer;

             ct.ThrowIfCancellationRequested();

             GameObject layerPrefab = await LoadLayerPrefabAsync(layerName, ct);
             if (layerPrefab == null)
             {
                 Debug.LogError($"[LayerInitializer] Failed to load layer prefab '{layerName}' from Resources or Addressables.");
                 return null;
             }

             ct.ThrowIfCancellationRequested();

             // Another call may have created the layer while this one was loading.
             if (layerCache.TryGetValue(layerName, out cachedLayer) && cachedLayer != null)
                 return cachedLayer;

             GameObject layerInstance = instantiator.Instantiate(layerPrefab);
             layerInstance.name = layerName;

             if (screenContainer != null)
                 layerInstance.transform.SetParent(screenContainer, worldPositionStays: false);

             // Cached before any cancellation check, so the instance is never left untracked.
             layerCache[layerName] = layerInstance;
             return layerInstance;
         }

         // The prefab's handle is held by LayerPrefabLoader and released on scene change, unless the layer is kept.
         private async Task<GameObject> LoadLayerPrefabAsync(string layerName, CancellationToken ct)
         {
             try
             {
                 return await LayerPrefabLoader.LoadPrefabAsync(layerName, ct);
             }
             catch (System.OperationCanceledException)
             {
                 Debug.Log($"[LayerInitializer] Load cancelled for layer '{layerName}'");
                 return null;
             }
             catch (System.Exception ex)
             {
                 Debug.LogError($"[LayerInitializer] Error loading layer '{layerName}': {ex.Message}");
                 return null;
             }
         }

         public void RemoveLayersForScreens(HashSet<string> screenIdsToRemove)
         {
             if (screenIdsToRemove == null || screenIdsToRemove.Count == 0)
                 return;

             List<string> layersToRemove = new();

             foreach (KeyValuePair<string, GameObject> kvp in layerCache.Pairs)
             {
                 string layerName = kvp.Key;
                 GameObject layer = kvp.Value;

                 if (layer == null)
                     layersToRemove.Add(layerName);
             }

             for (int i = 0; i < layersToRemove.Count; i++)
                 layerCache.Remove(layersToRemove[i]);
         }

         public Dictionary<string, GameObject> GetAllCachedLayers()
         {
             return layerCache.ToDictionary();
         }

         /// <summary>
         /// Destroys every cached layer except the ones in <paramref name="keep"/>, which stay cached.
         /// Returns the names (Resources paths or Addressables addresses) of the layers kept.
         /// </summary>
         public HashSet<string> ClearAllCachedLayers(ISet<GameObject> keep = null)
         {
             HashSet<string> kept = new();
             List<string> removed = new();
             foreach (KeyValuePair<string, GameObject> kvp in layerCache.Pairs)
             {
                 if (kvp.Value != null && keep != null && keep.Contains(kvp.Value))
                 {
                     kept.Add(kvp.Key);
                     continue;
                 }

                 if (kvp.Value != null)
                     Object.Destroy(kvp.Value);

                 removed.Add(kvp.Key);
             }

             for (int i = 0; i < removed.Count; i++)
                 layerCache.Remove(removed[i]);

             return kept;
         }
     }
}

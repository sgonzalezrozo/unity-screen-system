using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

             GameObject layerInstance = instantiator.Instantiate(layerPrefab);
             layerInstance.name = layerName;

             if (screenContainer != null)
                 layerInstance.transform.SetParent(screenContainer, worldPositionStays: false);

             ct.ThrowIfCancellationRequested();
             layerCache[layerName] = layerInstance;

             return layerInstance;
         }

         private async Task<GameObject> LoadLayerPrefabAsync(string layerName, CancellationToken ct)
         {
             try
             {
                 GameObject resourcePrefab = Resources.Load<GameObject>(layerName);
                 if (resourcePrefab != null)
                     return resourcePrefab;

                 AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(layerName);

                 if (!handle.IsDone)
                     await handle.Task;

                 ct.ThrowIfCancellationRequested();

                 if (handle.Status == AsyncOperationStatus.Succeeded)
                     return handle.Result;

                 if (handle.IsValid())
                     Addressables.Release(handle);

                 return null;
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

         public void ClearAllCachedLayers()
         {
             foreach (KeyValuePair<string, GameObject> kvp in layerCache.Pairs)
             {
                 if (kvp.Value != null)
                     Object.Destroy(kvp.Value);
             }

             layerCache.Clear();
         }
     }
}

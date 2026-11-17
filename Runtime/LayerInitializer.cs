using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScreenSystem
{
    internal class LayerInitializer
    {
        private readonly Transform screenContainer;
        private readonly IPrefabInstantiator instantiator;
        private readonly LoggingDictionary<string, GameObject> layerCache = new("LayerCache");
        private Transform inactiveHolder;

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

             StartupTimeline.MarkOnce("show.first-layer-loaded");
             ct.ThrowIfCancellationRequested();

             // Another call may have created the layer while this one was loading.
             if (layerCache.TryGetValue(layerName, out cachedLayer) && cachedLayer != null)
                 return cachedLayer;

             GameObject layerInstance = InstantiateInjected(layerPrefab, screenContainer, GetInactiveHolder(), instantiator, ToolInjectionServices.MonoBehaviourInjector);
             layerInstance.name = layerName;
             StartupTimeline.MarkOnce("show.first-layer-instantiated");

             // Cached before any cancellation check, so the instance is never left untracked.
             layerCache[layerName] = layerInstance;
             return layerInstance;
         }

         /// <summary>
         /// Creates a layer with its dependencies injected before its components' Awake and OnEnable run, and only
         /// once. With the default instantiator the copy is created under an inactive holder, every MonoBehaviour is
         /// injected, then the copy moves to <paramref name="container"/> and wakes up. A registered
         /// IInjectingPrefabInstantiator (a container's InstantiatePrefab) owns injection, so it isn't injected again;
         /// any other instantiator creates the copy, and Screen System injects it.
         /// </summary>
         internal static GameObject InstantiateInjected(GameObject prefab, Transform container, Transform inactiveHolder, IPrefabInstantiator instantiator, IMonoBehaviourInjector injector)
         {
             if (instantiator is IInjectingPrefabInstantiator)
             {
                 GameObject injected;
                 using (ScreenSystemMarkers.InstantiateLayer.Auto())
                     injected = instantiator.Instantiate(prefab);
                 if (container != null)
                     injected.transform.SetParent(container, worldPositionStays: false);
                 return injected;
             }

             GameObject instance;
             using (ScreenSystemMarkers.InstantiateLayer.Auto())
                 instance = instantiator != null ? instantiator.Instantiate(prefab, inactiveHolder) : Object.Instantiate(prefab, inactiveHolder, worldPositionStays: false);

             using (ScreenSystemMarkers.InjectLayer.Auto())
             {
                 MonoBehaviour[] components = instance.GetComponentsInChildren<MonoBehaviour>(true);
                 for (int i = 0; i < components.Length; i++)
                 {
                     if (components[i] != null)
                         injector?.InjectIfNeeded(components[i]);
                 }
             }

             // Waking the copy runs its Awake and OnEnable. Measured with instantiation, as before injection moved ahead of it.
             using (ScreenSystemMarkers.InstantiateLayer.Auto())
             {
                 if (container != null)
                     instance.transform.SetParent(container, worldPositionStays: false);
                 else
                     instance.transform.SetParent(null, worldPositionStays: false);
             }

             return instance;
         }

         // Children of an inactive object don't run Awake or OnEnable, so a new layer waits here until it's injected.
         private Transform GetInactiveHolder()
         {
             if (inactiveHolder != null || screenContainer == null)
                 return inactiveHolder;

             GameObject holder = new("LayerInjectionHolder");
             holder.SetActive(false);
             holder.transform.SetParent(screenContainer, worldPositionStays: false);
             inactiveHolder = holder.transform;
             return inactiveHolder;
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
                 ScreenSystemLog.Info($"[LayerInitializer] Load cancelled for layer '{layerName}'");
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

         public bool HasCachedLayer(string layerName)
             => !string.IsNullOrEmpty(layerName) && layerCache.TryGetValue(layerName, out GameObject layer) && layer != null;

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

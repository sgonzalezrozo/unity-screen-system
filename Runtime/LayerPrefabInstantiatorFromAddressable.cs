using System;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ScreenSystem
{
    [DefaultExecutionOrder(-1)]
    public class LayerPrefabInstantiatorFromAddressable : MonoBehaviour
    {
        public event Action<GameObject> OnPrefabInstantiated;

        [SerializeField] private string layerPrefabName;
        [SerializeField] private bool instantiateOnStart;

        internal IPrefabInstantiator injectedInstantiator;

        private AsyncOperationHandle<GameObject> handle;
        private GameObject instance;
        private bool hasInitialized;
        private bool isLoading;
        private CancellationTokenSource lifecycleCts;

        private IPrefabInstantiator Instantiator => injectedInstantiator ?? ToolInjectionServices.PrefabInstantiator;

        private void Awake()
        {
            lifecycleCts = new CancellationTokenSource();
        }

        private void OnEnable()
        {
            if (instantiateOnStart)
                return;

            if (!hasInitialized)
            {
                hasInitialized = true;
                return;
            }

            if (!instance)
                InstantiateAsync(lifecycleCts.Token).Forget("LayerPrefabInstantiator.OnEnable");
        }

        private void Start()
        {
            if (instantiateOnStart && !instance)
                InstantiateAsync(lifecycleCts.Token).Forget("LayerPrefabInstantiator.Start");
        }

        private void OnDestroy()
        {
            lifecycleCts?.Cancel();
            lifecycleCts?.Dispose();

            if (instance)
                Destroy(instance);
            ReleaseHandle();
        }

        // The handle is held while the instance lives and released in OnDestroy, on failure and on cancellation.
        private async Task InstantiateAsync(CancellationToken ct)
        {
            if (isLoading)
                return;

            isLoading = true;
            try
            {
                ReleaseHandle();
                AsyncOperationHandle<GameObject> load = Addressables.LoadAssetAsync<GameObject>(layerPrefabName);
                handle = load;
                await load.Task;

                // OnDestroy may have run during the load and already released the handle.
                if (ct.IsCancellationRequested || !load.IsValid())
                {
                    if (load.IsValid())
                        Addressables.Release(load);
                    handle = default;
                    return;
                }

                if (load.Status != AsyncOperationStatus.Succeeded || !load.Result)
                {
                    Debug.LogError($"[LayerPrefabInstantiator] Failed to load prefab from Addressables: '{layerPrefabName}'");
                    ReleaseHandle();
                    return;
                }

                LayerPrefabLoader.RetainBundlesFor(layerPrefabName);
                instance = Instantiator.Instantiate(load.Result, transform);
                OnPrefabInstantiated?.Invoke(instance);
            }
            finally
            {
                isLoading = false;
            }
        }

        private void ReleaseHandle()
        {
            if (handle.IsValid())
                Addressables.Release(handle);

            handle = default;
        }
    }
}

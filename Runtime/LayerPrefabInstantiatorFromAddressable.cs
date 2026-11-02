using System;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tools.ScreenSystem.Scripts
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
        private IPrefabInstantiator instantiator;
        private CancellationTokenSource lifecycleCts;

        private void Awake()
        {
            instantiator = injectedInstantiator ?? ToolInjectionServices.PrefabInstantiator;
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
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        private async Task InstantiateAsync(CancellationToken ct)
        {
            try
            {
                handle = Addressables.LoadAssetAsync<GameObject>(layerPrefabName);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Failed || !handle.Result)
                {
                    Debug.LogError($"[LayerPrefabInstantiator] Failed to load prefab from Addressables: '{layerPrefabName}'");
                    return;
                }

                ct.ThrowIfCancellationRequested();

                try
                {
                    instance = instantiator.Instantiate(handle.Result, transform);
                    OnPrefabInstantiated?.Invoke(instance);
                }
                finally { }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[LayerPrefabInstantiator] Instantiation of '{layerPrefabName}' was cancelled");
            }
        }
    }
}

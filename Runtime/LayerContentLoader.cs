using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ScreenSystem
{
    internal class LayerContentLoader : ILayerContentLoader
    {
        private const float COROUTINE_TIMEOUT_SECONDS = 10f;

        public event Action<GameObject> OnContentLoaded;
        public event Action OnContentUnloaded;

        private readonly Transform parentTransform;
        private readonly string contentPrefabName;
        private readonly IPrefabInstantiator instantiator;

        private AsyncOperationHandle<GameObject> contentHandle;
        private GameObject contentInstance;
        private bool isLoadingContent;

        public bool HasContent
            => contentInstance != null;

        public bool IsLoadingContent
            => isLoadingContent;

        public async Task WaitForLoadingCompleteAsync(CancellationToken ct = default)
        {
            while (isLoadingContent)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        public LayerContentLoader(Transform parentTransform, string contentPrefabName, IPrefabInstantiator instantiator = null)
        {
            this.parentTransform = parentTransform;
            this.contentPrefabName = contentPrefabName;

            if (instantiator == null)
            {
#if DEBUG_LAYER_COMPONENT
                Debug.LogWarning("[LayerContentLoader] No instantiator provided - Using NullPrefabInstantiator. " +
                              "This is expected if FastIoC DI is not configured for this project.");
#endif
                instantiator = ToolInjectionServices.PrefabInstantiator;
            }

            this.instantiator = instantiator;
        }

        public async Task LoadContentAsync(CancellationToken ct = default)
        {
            if (contentInstance != null)
                return;

            if (isLoadingContent)
                return;

            if (string.IsNullOrEmpty(contentPrefabName))
            {
                Debug.LogError("[LayerContentLoader] Content prefab name is empty");
                return;
            }

            ct.ThrowIfCancellationRequested();

            isLoadingContent = true;

            try
            {
                GameObject prefab = Resources.Load<GameObject>(contentPrefabName);

                if (prefab != null)
                {
                    InstantiatePrefab(prefab);
                    return;
                }

                if (contentHandle.IsValid())
                    Addressables.Release(contentHandle);

                contentHandle = Addressables.LoadAssetAsync<GameObject>(contentPrefabName);

                try
                {
                    await contentHandle.Task;
                    ct.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException)
                {
                    // Clean up handle if cancelled
                    if (contentHandle.IsValid())
                    {
                        Addressables.Release(contentHandle);
                        contentHandle = default;
                    }
                    throw;
                }

                if (!contentHandle.IsValid())
                {
                    Debug.LogWarning($"[LayerContentLoader] Content handle became invalid during load: '{contentPrefabName}'");
                    return;
                }

                if (contentHandle.Status == AsyncOperationStatus.Failed || contentHandle.Result == null)
                {
                    Debug.LogError($"[LayerContentLoader] Failed to load content '{contentPrefabName}' from Resources or Addressables");
                    ReleaseHandle();
                    return;
                }

                // The handle stays held while the instance lives. Releasing it now could unload the bundle under it.
                LayerPrefabLoader.RetainBundlesFor(contentPrefabName);
                InstantiatePrefab(contentHandle.Result);
            }
            finally
            {
                isLoadingContent = false;
            }
        }

        public IEnumerator LoadContentCoroutine()
        {
            if (contentInstance != null)
            {
                Debug.LogWarning($"[LayerContentLoader] Content '{contentPrefabName}' already exists, skipping load");
                yield break;
            }

            if (isLoadingContent)
            {
                Debug.LogWarning($"[LayerContentLoader] Content '{contentPrefabName}' is already being loaded, skipping duplicate load");
                yield break;
            }

            if (string.IsNullOrEmpty(contentPrefabName))
            {
                Debug.LogError("[LayerContentLoader] Content prefab name is empty");
                yield break;
            }

            isLoadingContent = true;

            GameObject prefab = Resources.Load<GameObject>(contentPrefabName);

            if (prefab != null)
            {
                InstantiatePrefab(prefab);
                isLoadingContent = false;
                yield break;
            }

            if (contentHandle.IsValid())
                Addressables.Release(contentHandle);

            contentHandle = Addressables.LoadAssetAsync<GameObject>(contentPrefabName);

            float startTime = Time.realtimeSinceStartup;

            while (!contentHandle.IsDone && (Time.realtimeSinceStartup - startTime) < COROUTINE_TIMEOUT_SECONDS)
                yield return null;

            if (!contentHandle.IsDone)
            {
                Debug.LogError($"[LayerContentLoader] Timeout loading '{contentPrefabName}' from Addressables after {COROUTINE_TIMEOUT_SECONDS}s");

                // Clean up handle on timeout to prevent memory leak
                if (contentHandle.IsValid())
                {
                    Addressables.Release(contentHandle);
                    contentHandle = default;
                }

                isLoadingContent = false;
                yield break;
            }

            if (contentHandle.Status == AsyncOperationStatus.Failed || contentHandle.Result == null)
            {
                Debug.LogError($"[LayerContentLoader] Failed to load content '{contentPrefabName}' from Resources or Addressables");
                ReleaseHandle();
                isLoadingContent = false;
                yield break;
            }

            // The handle stays held while the instance lives. Releasing it now could unload the bundle under it.
            LayerPrefabLoader.RetainBundlesFor(contentPrefabName);
            InstantiatePrefab(contentHandle.Result);
            isLoadingContent = false;
        }

        public bool DestroyContent()
        {
            if (contentInstance == null)
                return false;

            UnityEngine.Object.Destroy(contentInstance);
            contentInstance = null;
            ReleaseHandle();

            OnContentUnloaded?.Invoke();

            return true;
        }

        public void Cleanup()
        {
            if (contentInstance != null)
                UnityEngine.Object.Destroy(contentInstance);

            contentInstance = null;
            ReleaseHandle();
        }

        private void ReleaseHandle()
        {
            if (contentHandle.IsValid())
                Addressables.Release(contentHandle);

            contentHandle = default;
        }

        private void InstantiatePrefab(GameObject prefab)
        {
            contentInstance = instantiator.Instantiate(prefab, parentTransform);

            if (contentInstance == null)
            {
                Debug.LogError($"[LayerContentLoader] Failed to instantiate '{contentPrefabName}'");
                return;
            }

            if (!contentInstance.activeSelf)
                contentInstance.SetActive(true);

            OnContentLoaded?.Invoke(contentInstance);
        }
    }
}

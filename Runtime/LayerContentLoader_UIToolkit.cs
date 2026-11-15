using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using ToolsCore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ScreenSystem
{
    public class LayerContentLoader_UIToolkit : ILayerContentLoader
    {
        public event Action<GameObject> OnContentLoaded;
        public event Action OnContentUnloaded;

        private readonly UIDocument uiDocument;
        private readonly Transform layerTransform;
        private readonly string contentName;
        private GameObject loadedContentGameObject;
        private AsyncOperationHandle<GameObject> contentHandle;

        public bool HasContent => loadedContentGameObject != null;
        public bool IsLoadingContent { get; private set; }

        public LayerContentLoader_UIToolkit(UIDocument uiDocument, string contentName, Transform layerTransform)
        {
            this.uiDocument = uiDocument;
            this.contentName = contentName;
            this.layerTransform = layerTransform ?? throw new System.ArgumentNullException(nameof(layerTransform), "UIToolkit loader requires a parent transform for content instantiation");
        }

         public async Task LoadContentAsync(CancellationToken ct = default)
         {
             if (string.IsNullOrEmpty(contentName) || loadedContentGameObject != null || IsLoadingContent)
                 return;

             IsLoadingContent = true;

             try
             {
                 // Try loading from Resources first
                 GameObject prefab = Resources.Load<GameObject>(contentName);

                 // Fallback to Addressables if not in Resources
                 if (prefab == null)
                 {
                     // Held while the content lives, released by ReleaseHandle on failure, cancellation and unload.
                     ReleaseHandle();
                     contentHandle = Addressables.LoadAssetAsync<GameObject>(contentName);
                     await contentHandle.Task;

                     if (ct.IsCancellationRequested)
                     {
                         ReleaseHandle();
                         ct.ThrowIfCancellationRequested();
                     }

                     if (contentHandle.Status != AsyncOperationStatus.Succeeded || contentHandle.Result == null)
                     {
                         Debug.LogError($"[LayerContentLoader_UIToolkit] Failed to load content prefab '{contentName}' from Resources or Addressables");
                         ReleaseHandle();
                         return;
                     }

                     prefab = contentHandle.Result;
                     LayerPrefabLoader.RetainBundlesFor(contentName);
                 }

                 ct.ThrowIfCancellationRequested();
                // Determine parent transform
                // For bare shells: use layerTransform (no UIDocument yet)
                // For non-bare shells: use UIDocument.transform

                Debug.Log($"[LayerContentLoader_UIToolkit] Loading content: uiDocument={uiDocument}, layerTransform={layerTransform}");

                Transform parentTransform = uiDocument != null ? uiDocument.transform : layerTransform;

                if (parentTransform == null)
                {
                    Debug.LogError($"[LayerContentLoader_UIToolkit] Cannot instantiate content: no parent transform available (uiDocument={uiDocument}, layerTransform={layerTransform})");
                    return;
                }

                Debug.Log($"[LayerContentLoader_UIToolkit] Parent transform found: {parentTransform.gameObject.name}, instantiating {contentName}...");

                // Instantiate as inactive to prevent Awake() from running before injection
                loadedContentGameObject = Object.Instantiate(prefab, parentTransform, false);
                loadedContentGameObject.SetActive(false);
                loadedContentGameObject.name = contentName;

                MonoBehaviour[] components = loadedContentGameObject.GetComponentsInChildren<MonoBehaviour>(true);
                Debug.Log($"[LayerContentLoader_UIToolkit] Found {components.Length} MonoBehaviour components to inject");
                foreach (MonoBehaviour component in components)
                {
                    Debug.Log($"[LayerContentLoader_UIToolkit] Injecting {component.GetType().Name}");
                    ToolInjectionServices.MonoBehaviourInjector.InjectIfNeeded(component);
                }

                loadedContentGameObject.SetActive(true);
                OnContentLoaded?.Invoke(loadedContentGameObject);
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[LayerContentLoader_UIToolkit] Loading cancelled: {contentName}");
            }
            finally
            {
                IsLoadingContent = false;

                // Loaded but never instantiated, for example when an exception stopped it halfway.
                if (loadedContentGameObject == null)
                    ReleaseHandle();
            }
        }

        public void UnloadContent()
        {
            if (loadedContentGameObject != null)
            {
                Object.Destroy(loadedContentGameObject);
                loadedContentGameObject = null;
                OnContentUnloaded?.Invoke();
            }

            ReleaseHandle();
        }

        public bool DestroyContent()
        {
            if (loadedContentGameObject == null)
                return false;

            UnloadContent();
            return true;
        }

        private void ReleaseHandle()
        {
            if (contentHandle.IsValid())
                Addressables.Release(contentHandle);

            contentHandle = default;
        }

        public async Task WaitForLoadingCompleteAsync(CancellationToken ct = default)
        {
            while (IsLoadingContent)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }
        }

        public IEnumerator LoadContentCoroutine()
        {
            yield return null;
            yield return LoadContentAsync().ConfigureAwait(false);
        }

        public void Cleanup()
        {
            UnloadContent();
        }
    }
}

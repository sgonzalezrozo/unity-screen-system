using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tools.ScreenSystem.Scripts
{
    public class ScreenDisplayService : IScreenDisplay
    {
        private const string LOG_PREFIX = "[ScreenDisplay]";

        private readonly ScreenRegistry screenRegistry;
        private readonly ScreenCacheManager cacheManager;
        private readonly ScreenShowService showService;
        private readonly ScreenHideService hideService;
        private readonly LayerFactory layerFactory;
        private readonly BlurBackgroundService blurService;

        private readonly object navigationLock = new();
        private Task activeNavigationTask = Task.CompletedTask;
        private CancellationTokenSource navigationCts;
        private int navigationOperationId;

         public ScreenDisplayService(ScreenRegistry screenRegistry, Transform screenContainer, BlurBackgroundLayer sharedBlurBackground)
         {
             this.screenRegistry = screenRegistry;
             cacheManager = new ScreenCacheManager();
             layerFactory = new LayerFactory(screenContainer, cacheManager);
             blurService = new BlurBackgroundService(sharedBlurBackground, cacheManager);
             hideService = new ScreenHideService(screenRegistry, cacheManager, blurService);
             showService = new ScreenShowService(screenRegistry, layerFactory, cacheManager, hideService, blurService);
         }

        public async Task ShowScreen(string screenId, bool autoHideParents = true, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError($"{LOG_PREFIX} ShowScreen failed: screenId is null or empty");
                return;
            }

            Func<CancellationToken, int, CancellationTokenSource, Task> operation = async (operationCt, opId, linkedCts) =>
            {
                await ContinueWithShow(screenId, autoHideParents, operationCt, opId, linkedCts);
            };

            await ExecuteNavigationOperation(operation, screenId, "show");
        }

        public async Task HideScreen(string screenId, string preserveLayersForScreenId = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError($"{LOG_PREFIX} HideScreen failed: screenId is null or empty");
                return;
            }

            Func<CancellationToken, int, CancellationTokenSource, Task> operation = async (operationCt, opId, linkedCts) =>
            {
                await ContinueWithHide(screenId, preserveLayersForScreenId, operationCt, opId, linkedCts);
            };

            await ExecuteNavigationOperation(operation, screenId, "hide");
        }

        private async Task ExecuteNavigationOperation(Func<CancellationToken, int, CancellationTokenSource, Task> operation, string screenId, string operationName)
        {
            Task currentTask;
            CancellationToken operationCt;
            CancellationTokenSource linkedCts;
            int opId;
            bool cancelledPrevious;
            int previousOpId;

            lock (navigationLock)
            {
                cancelledPrevious = activeNavigationTask != null && !activeNavigationTask.IsCompleted;
                previousOpId = navigationOperationId;

                if (cancelledPrevious)
                    Debug.LogWarning($"{LOG_PREFIX} Cancelling previous operation op:{previousOpId}");

                navigationCts?.Cancel();
                navigationCts = new CancellationTokenSource();
                opId = ++navigationOperationId;

                linkedCts = CancellationTokenSource.CreateLinkedTokenSource(navigationCts.Token);
                operationCt = linkedCts.Token;

                activeNavigationTask = operation(operationCt, opId, linkedCts);
                currentTask = activeNavigationTask;
            }

            if (cancelledPrevious)
                Debug.LogWarning($"{LOG_PREFIX} Previous operation cancelled. previous-op:{previousOpId}, current-op:{opId}");

            try
            {
                await currentTask;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LOG_PREFIX} ExecuteNavigationOperation operation cancelled. op:{opId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} ExecuteNavigationOperation operation failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private async Task ContinueWithShow(string screenId, bool autoHideParents, CancellationToken ct, int opId, CancellationTokenSource linkedCts)
        {
            bool isStaleAtStart = opId != navigationOperationId;

            if (ct.IsCancellationRequested || isStaleAtStart)
            {
                linkedCts.Dispose();
                return;
            }

            try
            {
                await showService.ShowScreenAsync(screenId, autoHideParents, ct);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LOG_PREFIX} ContinueWithShow cancelled. op:{opId}, screen:{screenId}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} ContinueWithShow exception: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        private async Task ContinueWithHide(string screenId, string preserveLayersForScreenId, CancellationToken ct, int opId, CancellationTokenSource linkedCts)
        {
            bool isStaleAtStart = opId != navigationOperationId;

            if (ct.IsCancellationRequested || isStaleAtStart)
            {
                linkedCts.Dispose();
                return;
            }

            try
            {
                await hideService.HideScreenAsync(screenId, preserveLayersForScreenId, ct);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{LOG_PREFIX} ContinueWithHide cancelled. op:{opId}, screen:{screenId}");
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LOG_PREFIX} ContinueWithHide exception: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        public string GetTopScreen()
            => cacheManager.GetTopScreen();

        public List<string> GetActiveScreenIds()
        {
            IReadOnlyList<string> activeScreens = cacheManager.GetActiveScreens();
            List<string> result = new(activeScreens.Count);
            for (int i = 0; i < activeScreens.Count; i++)
                result.Add(activeScreens[i]);
            return result;
        }

        public List<LayerComponent> GetScreenLayers(string screenId)
            => cacheManager.GetScreenLayers(screenId);

        public void DestroyScreen(string screenId)
        {
            ScreenDefinition screen = screenRegistry.GetScreen(screenId);

            if (screen != null)
                cacheManager.UnregisterActiveScreen(screenId);

            List<LayerComponent> layers = cacheManager.GetScreenLayers(screenId);
            if (layers is { Count: > 0 })
            {
                foreach (LayerComponent layer in layers)
                {
                    if (layer != null && layer.gameObject != null)
                        Object.Destroy(layer.gameObject);
                }
            }

            cacheManager.RemoveScreen(screenId);
        }

        public void UnregisterScreenWithoutDestroy(string screenId)
        {
            cacheManager.UnregisterScreenFromAllLevels(screenId);
        }

        public bool IsScreenInstantiated(string screenId)
            => cacheManager.IsScreenCached(screenId);

        public Dictionary<string, List<LayerComponent>> GetCachedScreens()
            => cacheManager.GetAllCachedScreens();

        public void ClearActiveScreens()
        {
            cacheManager.ClearActiveScreens();
        }

        public void HidePersistentScreenLayers()
        {
            cacheManager.HidePersistentScreenLayers(screenRegistry);
        }

        public void ClearCachedScreens()
            => cacheManager.ClearCachedScreens();

        public void RemoveNonPersistentScreens()
        {
            HashSet<string> screenIdsToRemove = cacheManager.GetNonPersistentScreenIds(screenRegistry);

            if (screenIdsToRemove.Count <= 0)
                return;

            LayerInitializer layerInitializer = layerFactory.GetLayerInitializer();
            layerInitializer.RemoveLayersForScreens(screenIdsToRemove);

            cacheManager.RemoveNonPersistentScreens(screenRegistry, layerInitializer);
        }

        public int UnloadPersistentScreenContent()
        {
            return cacheManager.UnloadPersistentScreenContent(screenRegistry);
        }

        public LayerFactory GetLayerFactory()
            => layerFactory;

        public ScreenCacheManager GetCacheManager()
            => cacheManager;

        public void ClearAllCachedResources()
        {
            LayerPrefabLoader.ReleaseAllLoadedPrefabs();
            layerFactory.ClearAllCachedLayers();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenShowService
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly IScreenHideService hideService;
        private readonly ScreenLayerPreparer layerPreparer;
        private readonly ScreenContentLoader contentLoader;
        private readonly ScreenBlurHandler blurHandler;
        private readonly ScreenLayerRetriever layerRetriever;
        private readonly ScreenCacheManager cacheManager;

        public ScreenShowService(
            ScreenRegistry screenRegistry,
            LayerFactory layerFactory,
            ScreenCacheManager cacheManager,
            IScreenHideService hideService,
            BlurBackgroundService blurService)
        {
            this.screenRegistry = screenRegistry ?? throw new ArgumentNullException(nameof(screenRegistry));
            this.hideService = hideService ?? throw new ArgumentNullException(nameof(hideService));
            this.cacheManager = cacheManager;

            layerPreparer = new ScreenLayerPreparer(cacheManager);
            contentLoader = new ScreenContentLoader();
            blurHandler = new ScreenBlurHandler(blurService);
            layerRetriever = new ScreenLayerRetriever(layerFactory, cacheManager);
        }

        public async Task ShowScreenAsync(string screenId, bool autoHideParents = true, CancellationToken ct = default)
        {
            await ShowScreenAsyncInternal(screenId, autoHideParents, ct);
        }

        private async Task ShowScreenAsyncInternal(string screenId, bool autoHideParents, CancellationToken ct)
        {
            ScreenMetrics.Instance.BeginOperation($"ShowScreen({screenId})");
            ScreenMetrics.Instance.UpdateScreenStatus(screenId, ScreenMetrics.ScreenStatus.Loading);

            try
            {
                ct.ThrowIfCancellationRequested();

                ScreenDefinition screen = ValidateAndGetScreen(screenId);
                if (screen == null)
                {
                    Debug.LogWarning($"[ScreenShowService] Screen '{screenId}' was not found.");
                    return;
                }

                ct.ThrowIfCancellationRequested();

                NavigationEvents.NotifyTransitionInStarted(screenId);

                string logId = GetReadableScreenId(screen);

                layerRetriever.RegisterActiveScreen(screenId);

                List<LayerComponent> layers = await layerRetriever.GetOrCreateLayersAsync(screenId, screen, logId, ct);

                if (layers == null || layers.Count == 0)
                {
                    Debug.LogWarning($"[ScreenShowService] No layers retrieved for screen: {screenId}");
                    return;
                }

                // for (int i = 0; i < layers.Count; i++)

                ct.ThrowIfCancellationRequested();

                await layerPreparer.PrepareLayersForDisplayAsync(layers, ct);
                contentLoader.StartBackgroundContentLoads(layers, ct);

                await contentLoader.WaitForAllLayersContentLoadedAsync(layers, ct);

                ct.ThrowIfCancellationRequested();

                layerPreparer.ActivateLayers(layers, screenId);

                if (autoHideParents && screen.screenType == ScreenType.fullscreen)
                {
                    await hideService.HideOtherScreensAsync(screenId, screen.screenType, ct, layers);
                }
                else if (autoHideParents && screen.screenType == ScreenType.modal)
                {
                    await hideService.HideOtherScreensAsync(screenId, screen.screenType, ct, layers);
                }

                RecordMetrics(screenId, screen, layers);
                await blurHandler.ApplyBlurAsync(screenId, screen, ct);

                NavigationEvents.NotifyTransitionInCompleted(screenId);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"[ScreenShowService] ShowScreenAsyncInternal cancelled for: {screenId}");
                ScreenMetrics.Instance.UpdateScreenStatus(screenId, ScreenMetrics.ScreenStatus.Unloaded);
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScreenShowService] Error showing screen '{screenId}': {ex.Message}\n{ex.StackTrace}");
                ScreenMetrics.Instance.UpdateScreenStatus(screenId, ScreenMetrics.ScreenStatus.Unloaded);
            }
            finally
            {
                ScreenMetrics.Instance.EndOperationSilent(screenId);
            }
        }

        private ScreenDefinition ValidateAndGetScreen(string screenId)
        {
            ScreenDefinition screen = screenRegistry.GetScreen(screenId);
            if (screen == null)
                ScreenMetrics.Instance.UpdateScreenStatus(screenId, ScreenMetrics.ScreenStatus.Unloaded);

            return screen;
        }

        private void RecordMetrics(string screenId, ScreenDefinition screen, List<LayerComponent> layers)
        {
            float loadTimeMs = ScreenMetrics.Instance.EndOperation(screenId);
            ScreenMetrics.Instance.RecordScreenLoad(screenId, screen.ScreenName, loadTimeMs, layers.Count, fromCache: false);
            ScreenMetrics.Instance.UpdateScreenStatus(screenId, ScreenMetrics.ScreenStatus.Active);
        }

        private string GetReadableScreenId(ScreenDefinition screen)
            => screen?.ScreenName ?? "Unknown";
    }
}

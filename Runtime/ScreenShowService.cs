using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    internal class ScreenShowService
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly IScreenHideService hideService;
        private readonly ScreenLayerPreparer layerPreparer;
        private readonly ScreenContentLoader contentLoader;
        private readonly ScreenBlurHandler blurHandler;
        private readonly ScreenLayerRetriever layerRetriever;
        private readonly ScreenCacheManager cacheManager;

        // Set by the navigator for the GoTo that carries a value; taken by the first show of that screen.
        private string pendingParameterScreenId;
        private ScreenParameter pendingParameter;

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

        public void SetPendingParameter(string screenId, ScreenParameter parameter)
        {
            pendingParameterScreenId = parameter != null ? screenId : null;
            pendingParameter = parameter;
        }

        private ScreenParameter TakePendingParameter(string screenId)
        {
            if (pendingParameter == null || pendingParameterScreenId != screenId)
                return null;

            ScreenParameter parameter = pendingParameter;
            pendingParameter = null;
            pendingParameterScreenId = null;
            return parameter;
        }

        /// <summary>Shows a screen. Returns false if it isn't registered, has no layers, or an exception was thrown.</summary>
        public Task<bool> ShowScreenAsync(string screenId, bool autoHideParents = true, CancellationToken ct = default)
            => ShowScreenAsyncInternal(screenId, autoHideParents, ct);

        private async Task<bool> ShowScreenAsyncInternal(string screenId, bool autoHideParents, CancellationToken ct)
        {
            using (ScreenSystemMarkers.Metrics.Auto())
            {
                ScreenMetrics.Instance.BeginOperation($"ShowScreen({screenId})");
                ScreenMetrics.Instance.UpdateScreenStatus(screenId, ScreenMetrics.ScreenStatus.Loading);
            }

            try
            {
                ct.ThrowIfCancellationRequested();

                ScreenDefinition screen = ValidateAndGetScreen(screenId);
                if (screen == null)
                {
                    Debug.LogWarning($"[ScreenShowService] Screen '{screenId}' was not found.");
                    return false;
                }

                ct.ThrowIfCancellationRequested();

                NavigationEvents.NotifyTransitionInStarted(screenId);

                string logId = GetReadableScreenId(screen);

                layerRetriever.RegisterActiveScreen(screenId);

                // A modal or alert captures the screen underneath before any of its own layers become visible.
                await blurHandler.PrepareAsync(screenId, screen, ct);

                List<LayerComponent> layers = await layerRetriever.GetOrCreateLayersAsync(screenId, screen, logId, ct);

                if (layers == null || layers.Count == 0)
                {
                    Debug.LogError($"[ScreenShowService] Screen '{screenId}' has no layers: its prefab didn't load. Check its ScreenDefinition's asset path.");
                    return false;
                }

                // for (int i = 0; i < layers.Count; i++)

                ct.ThrowIfCancellationRequested();

                await layerPreparer.PrepareLayersForDisplayAsync(layers, ct);
                contentLoader.StartBackgroundContentLoads(layers, ct);

                await contentLoader.WaitForAllLayersContentLoadedAsync(layers, ct);

                ct.ThrowIfCancellationRequested();

                // Before activation, so OnScreenShown and the first drawn frame already have the value.
                TakePendingParameter(screenId)?.DeliverAndReport(screenId, layers);

                using (ScreenSystemMarkers.ActivateLayers.Auto())
                    layerPreparer.ActivateLayers(layers, screenId);

                if (autoHideParents && screen.ScreenType == ScreenType.Fullscreen)
                {
                    await hideService.HideOtherScreensAsync(screenId, screen.ScreenType, ct, layers);
                }
                else if (autoHideParents && screen.ScreenType == ScreenType.Modal)
                {
                    await hideService.HideOtherScreensAsync(screenId, screen.ScreenType, ct, layers);
                }

                using (ScreenSystemMarkers.Metrics.Auto())
                    RecordMetrics(screenId, screen, layers);
                blurHandler.Apply(screenId, screen, layers);

                NavigationEvents.NotifyTransitionInCompleted(screenId);
                return true;
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
                return false;
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

        // Compiled out of release builds, with its arguments, unless SCREEN_SYSTEM_METRICS is defined.
        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD"), System.Diagnostics.Conditional("SCREEN_SYSTEM_METRICS")]
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

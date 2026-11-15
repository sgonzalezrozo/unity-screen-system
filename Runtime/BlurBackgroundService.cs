using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
	public class BlurBackgroundService
	{
		private const string LOG_PREFIX = "[Blur]";

		private readonly BlurBackgroundLayer sharedBlurBackground;
		private readonly ScreenCacheManager cacheManager;
		private bool isBlurActive;
		private int activeModalCount;
		private readonly Dictionary<string, LayerComponent> modalLayerCache = new();

		public BlurBackgroundService(BlurBackgroundLayer sharedBlurBackground, ScreenCacheManager cacheManager = null)
		{
			this.sharedBlurBackground = sharedBlurBackground;
			this.cacheManager = cacheManager;
			isBlurActive = false;
			activeModalCount = 0;
		}

		public bool IsBlurActive
			=> isBlurActive;

		public int ActiveModalCount
			=> activeModalCount;

		public async Task ShowBlurForModalAsync(string screenId, CancellationToken ct = default)
		{
			if (sharedBlurBackground == null)
				return;

			activeModalCount++;

			if (isBlurActive)
				return;

			isBlurActive = true;

			Canvas blurCanvas = sharedBlurBackground.GetComponent<Canvas>();
			if (blurCanvas != null)
			{
				LayerComponent modalLayer = GetOrFindModalLayer(screenId);
				if (modalLayer != null)
				{
					Canvas modalCanvas = modalLayer.GetComponent<Canvas>();
					if (modalCanvas != null)
					{
						int modalSortOrder = modalCanvas.sortingOrder;
						int blurSortOrder = modalSortOrder - 1;
						blurCanvas.sortingOrder = blurSortOrder;
					}
				}

				blurCanvas.overrideSorting = true;
				blurCanvas.sortingLayerName = "Default";
			}

			try
			{
				await sharedBlurBackground.ShowAsync(ct);
			}
			catch (System.OperationCanceledException)
			{
				isBlurActive = false;
				activeModalCount--;
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"{LOG_PREFIX} ShowAsync failed: {ex.Message}");
				isBlurActive = false;
				activeModalCount--;
			}
		}

		private LayerComponent GetOrFindModalLayer(string screenId)
		{
			// A cached layer is destroyed when its screen unloads. Drop it instead of returning it.
			if (modalLayerCache.TryGetValue(screenId, out LayerComponent cachedLayer))
			{
				if (cachedLayer != null)
					return cachedLayer;

				modalLayerCache.Remove(screenId);
			}

			// Use cache manager if available (preferred, O(1) lookup)
			if (cacheManager != null)
			{
				List<LayerComponent> layers = cacheManager.GetScreenLayers(screenId);
				if (layers != null && layers.Count > 0)
				{
					LayerComponent layer = layers[0];
					modalLayerCache[screenId] = layer;
					return layer;
				}
				return null;
			}

			// Fallback to FindObjectsByType if no cache manager (O(n) operation)
			LayerComponent[] allLayers = Object.FindObjectsByType<LayerComponent>(FindObjectsSortMode.None);
			foreach (LayerComponent layer in allLayers)
			{
				if (layer.CurrentScreenId == screenId)
				{
					modalLayerCache[screenId] = layer;
					return layer;
				}
			}
			return null;
		}

		public async Task HideBlurForModalAsync(string screenId, CancellationToken ct = default)
		{
			if (sharedBlurBackground == null)
				return;

			activeModalCount--;
			if (activeModalCount < 0)
				activeModalCount = 0;

			if (activeModalCount > 0)
				return;

			if (!isBlurActive)
				return;

			try
			{
				await sharedBlurBackground.HideAsync(ct);
				isBlurActive = false;
				modalLayerCache.Remove(screenId);
			}
			catch (System.OperationCanceledException)
			{
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"{LOG_PREFIX} HideAsync failed: {ex.Message}");
			}
		}

		public void HideAllBlurs()
		{
			if (sharedBlurBackground == null || !isBlurActive)
				return;

			activeModalCount = 0;
			isBlurActive = false;
			modalLayerCache.Clear();
			sharedBlurBackground.HideAsync().Forget("HideAllBlurs");
		}

		public void Cleanup()
		{
			activeModalCount = 0;
			isBlurActive = false;
			modalLayerCache.Clear();
		}
	}
}

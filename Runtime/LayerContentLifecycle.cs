using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
	internal class LayerContentLifecycle
	{
		private readonly ILayerContentLoader contentLoader;
		private readonly string contentPrefabName;
		private readonly LoadTiming loadTiming;
		private CancellationToken currentNavigationToken;

		internal enum LoadTiming
		{
			OnFirstShow,
			OnEveryShow,
			Manual
		}

		public bool HasContent
			=> contentLoader?.HasContent ?? false;

		public bool IsLoadingContent
			=> contentLoader?.IsLoadingContent ?? false;

		public LayerContentLifecycle(ILayerContentLoader contentLoader, string contentPrefabName, LoadTiming loadTiming)
		{
			this.contentLoader = contentLoader;
			this.contentPrefabName = contentPrefabName;
			this.loadTiming = loadTiming;
		}

		public void SetNavigationToken(CancellationToken ct)
		{
			currentNavigationToken = ct;
		}

		public void HandleOnEnable(MonoBehaviour monoBehaviour)
		{
			if (string.IsNullOrEmpty(contentPrefabName))
				return;

			if (loadTiming == LoadTiming.Manual)
				return;

			CancellationToken ct = currentNavigationToken;

			if (loadTiming == LoadTiming.OnFirstShow && !contentLoader.HasContent)
			{
				LoadContentAsync(ct).Forget("LayerContentLifecycle.OnEnable (OnFirstShow)");
			}
			else if (loadTiming == LoadTiming.OnEveryShow)
			{
				if (contentLoader.HasContent)
					contentLoader.DestroyContent();

				LoadContentAsync(ct).Forget("LayerContentLifecycle.OnEnable (OnEveryShow)");
			}
		}

		public void LoadContent(MonoBehaviour monoBehaviour)
		{
			if (string.IsNullOrEmpty(contentPrefabName))
				return;

			if (contentLoader.HasContent)
			{
				Debug.LogWarning($"[LayerContentLifecycle] Content '{contentPrefabName}' already loaded");
				return;
			}

			monoBehaviour.StartCoroutine(contentLoader.LoadContentCoroutine());
		}

		public async Task LoadContentAsync(CancellationToken ct = default)
		{
			if (string.IsNullOrEmpty(contentPrefabName))
				return;

			if (contentLoader.HasContent)
				return;

			try
			{
				ct.ThrowIfCancellationRequested();
				await contentLoader.LoadContentAsync(ct);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Debug.LogError(
					$"[LayerContentLifecycle] LoadContentAsync failed. prefab:{contentPrefabName}, " +
					$"error:{ex.Message}\n{ex.StackTrace}");
				throw;
			}
		}

		public async Task WaitForLoadingCompleteAsync(CancellationToken ct = default)
		{
			await contentLoader.WaitForLoadingCompleteAsync(ct);
		}

		public bool DestroyContent()
		{
			return contentLoader.DestroyContent();
		}

		public void UnloadContent()
		{
			if (!contentLoader.HasContent)
				return;

			contentLoader.DestroyContent();
		}

		public void ReloadContent(MonoBehaviour monoBehaviour)
		{
			if (contentLoader.HasContent)
				contentLoader.DestroyContent();

			monoBehaviour.StartCoroutine(contentLoader.LoadContentCoroutine());
		}

		public void Cleanup()
		{
			if (contentLoader != null && contentLoader.HasContent)
				contentLoader.DestroyContent();

			contentLoader?.Cleanup();
		}
	}
}


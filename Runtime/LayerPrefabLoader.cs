using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Tools.ScreenSystem.Scripts
{
    public static class LayerPrefabLoader
    {
        private static readonly List<AsyncOperationHandle<GameObject>> loadedHandles = new();

        public static async Task<GameObject> LoadScreenPrefab(ScreenDefinition screen, CancellationToken ct = default)
        {
            if (screen == null)
                return null;

            string assetPath = screen.AssetPath;
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"[LayerPrefabLoader] AssetPath is EMPTY for screen '{screen.ScreenName}' (ScreenDefinition: {screen.name})\n" +
                               $"MIGRATION REQUIRED: Open ScreenDefinition asset '{screen.name}' in Inspector and drag the prefab onto it to populate AssetPath.\n" +
                               $"If prefab is in Resources folder (e.g., Assets/Resources/Splash/SplashMain.prefab), AssetPath will be: 'Splash/SplashMain'");
                return null;
            }

            GameObject resourcePrefab = Resources.Load<GameObject>(assetPath);
            if (resourcePrefab != null)
                return resourcePrefab;

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(assetPath);

            ct.ThrowIfCancellationRequested();

            if (!handle.IsDone)
                await handle.Task;

            ct.ThrowIfCancellationRequested();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[LayerPrefabLoader] Failed to load screen prefab '{assetPath}' from Addressables");
                if (handle.IsValid())
                    Addressables.Release(handle);
                return null;
            }

            // Track handle for later cleanup - prefab will be kept alive
            if (handle.IsValid())
                loadedHandles.Add(handle);

            return handle.Result;
        }

        public static GameObject GetLoadedScreenPrefab(ScreenDefinition screen)
        {
            if (screen == null || string.IsNullOrEmpty(screen.AssetPath))
                return null;

            GameObject resourcePrefab = Resources.Load<GameObject>(screen.AssetPath);
            if (resourcePrefab != null)
                return resourcePrefab;

            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(screen.AssetPath);

            if (handle.IsDone && handle.Status == AsyncOperationStatus.Succeeded)
                return handle.Result;

            return null;
        }

        public static void ReleaseAllLoadedPrefabs()
        {
            foreach (AsyncOperationHandle<GameObject> handle in loadedHandles)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            loadedHandles.Clear();
        }
    }
}


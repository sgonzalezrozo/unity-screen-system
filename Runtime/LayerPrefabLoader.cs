using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    /// <summary>
    /// Loads screen and layer prefabs from Resources or Addressables. Addressables handles are held, one per address,
    /// until a scene change releases those of non-persistent screens, or until ScreenManager is destroyed.
    /// </summary>
    public static class LayerPrefabLoader
    {
        private static PrefabHandleCache cache = CreateCache();

        private static PrefabHandleCache CreateCache()
            => new(AddressablesPrefabLoad.Start, address => Resources.Load<GameObject>(address), new AddressablesBundleKeeper());

        // With domain reload off, statics survive Play Mode, but the old handles don't. Start empty.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetForPlayMode()
            => cache = CreateCache();

        /// <summary>Number of Addressables handles held. Used by tests and the benchmark to check for leaks.</summary>
        [UnityEngine.Scripting.Preserve]
        public static int HeldHandleCount
            => cache.HeldCount;

        [UnityEngine.Scripting.Preserve]
        public static IEnumerable<string> HeldAddresses
            => cache.HeldAddresses;

        /// <summary>Number of AssetBundles kept open. Bundles stay open for the session once a screen needs them.</summary>
        [UnityEngine.Scripting.Preserve]
        public static int HeldBundleCount
            => cache.BundleKeeper?.HeldCount ?? 0;

        /// <summary>Keeps the bundles of an address open. Call it after loading an asset directly through Addressables.</summary>
        public static void RetainBundlesFor(string address)
            => cache.BundleKeeper?.Retain(address);

        /// <summary>Closes every kept bundle. Only for teardown, after every asset handle is released.</summary>
        public static void ReleaseAllBundles()
            => cache.BundleKeeper?.ReleaseAll();

        public static async Task<GameObject> LoadScreenPrefab(ScreenDefinition screen, CancellationToken ct = default)
        {
            if (screen == null)
                return null;

            if (string.IsNullOrEmpty(screen.AssetPath))
            {
                Debug.LogError($"[LayerPrefabLoader] AssetPath is empty for screen '{screen.ScreenName}' ({screen.name}). Drag its prefab onto the ScreenDefinition to fill it.");
                return null;
            }

            GameObject prefab = await cache.LoadAsync(screen.AssetPath, ct);
            if (prefab == null)
                Debug.LogError($"[LayerPrefabLoader] Failed to load screen prefab '{screen.AssetPath}' from Resources or Addressables");

            return prefab;
        }

        /// <summary>Loads a layer prefab by its Resources path or Addressables address.</summary>
        public static Task<GameObject> LoadPrefabAsync(string address, CancellationToken ct = default)
            => cache.LoadAsync(address, ct);

        /// <summary>Returns a prefab that is already loaded, without starting a load.</summary>
        public static GameObject GetLoadedScreenPrefab(ScreenDefinition screen)
            => screen == null ? null : cache.GetLoaded(screen.AssetPath);

        /// <summary>Releases every held handle except those whose address is in <paramref name="keepAddresses"/>.</summary>
        public static void ReleaseAllLoadedPrefabs(ISet<string> keepAddresses = null)
            => cache.ReleaseAllExcept(keepAddresses);
    }
}

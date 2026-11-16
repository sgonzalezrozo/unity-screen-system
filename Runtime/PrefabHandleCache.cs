using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace ScreenSystem
{
    /// <summary>One prefab load that must be released exactly once.</summary>
    internal interface IPrefabLoad
    {
        Task Task { get; }
        bool Succeeded { get; }
        GameObject Result { get; }
        void Release();

        /// <summary>Finishes the load now when that can't stall on the network. Returns true if it's done.</summary>
        bool TryCompleteNow();
    }

    internal sealed class AddressablesPrefabLoad : IPrefabLoad
    {
        // Whether every bundle an address needs is on the device, cached per address.
        private static readonly Dictionary<string, bool> localAddresses = new();

        private readonly string address;
        private AsyncOperationHandle<GameObject> handle;

        private AddressablesPrefabLoad(string address)
        {
            this.address = address;
            handle = Addressables.LoadAssetAsync<GameObject>(address);
        }

        // An async load waits for later frames to finish, about 3 frames on device (100 ms at 30 fps) even when the
        // data is a few milliseconds away in a local bundle. Local loads finish now instead. Remote bundles stay
        // async, so a download never blocks the frame.
        public bool TryCompleteNow()
        {
            if (!handle.IsValid())
                return false;

            if (handle.IsDone)
                return true;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL fetches every bundle with a web request, which WaitForCompletion can't wait on. Stay async.
            return false;
#else
            if (!IsLocal(address))
                return false;

            using (ScreenSystemMarkers.LoadPrefab.Auto())
                handle.WaitForCompletion();
            return handle.IsDone;
#endif
        }

        private static bool IsLocal(string address)
        {
            if (localAddresses.TryGetValue(address, out bool local))
                return local;

            local = true;
            AsyncOperationHandle<IList<IResourceLocation>> locations = Addressables.LoadResourceLocationsAsync(address, typeof(GameObject));
            try
            {
                locations.WaitForCompletion();
                if (locations.Status != AsyncOperationStatus.Succeeded || locations.Result == null || locations.Result.Count == 0)
                {
                    local = false;
                }
                else
                {
                    foreach (IResourceLocation location in locations.Result)
                    {
                        if (location?.Dependencies == null)
                            continue;

                        foreach (IResourceLocation bundle in location.Dependencies)
                        {
                            string id = bundle?.InternalId ?? "";
                            if (id.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || id.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                                local = false;
                        }
                    }
                }
            }
            finally
            {
                if (locations.IsValid())
                    Addressables.Release(locations);
            }

            localAddresses[address] = local;
            return local;
        }

        public static IPrefabLoad Start(string address)
            => new AddressablesPrefabLoad(address);

        // handle.Task completes through a deferred callback even when the load already finished,
        // which costs frames on every load. A finished handle returns a completed task instead.
        public Task Task
            => handle.IsValid() && !handle.IsDone ? handle.Task : Task.CompletedTask;

        public bool Succeeded
            => handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded;

        public GameObject Result
            => Succeeded ? handle.Result : null;

        public void Release()
        {
            if (handle.IsValid())
                Addressables.Release(handle);

            handle = default;
        }
    }

    /// <summary>Keeps the AssetBundles an address depends on open, so releasing the asset doesn't close them.</summary>
    internal interface IBundleKeeper
    {
        int HeldCount { get; }
        void Retain(string address);
        void ReleaseAll();
    }

    /// <summary>
    /// Holds one handle per AssetBundle for the whole session. Closing and reopening a bundle on every scene change
    /// leaked about 29 MB of native memory per change on device, while unloading only the assets stayed flat.
    /// A bundle is opened the first time a screen needs it, never ahead of time.
    /// </summary>
    internal sealed class AddressablesBundleKeeper : IBundleKeeper
    {
        private readonly Dictionary<string, AsyncOperationHandle> held = new();

        public int HeldCount
            => held.Count;

        public void Retain(string address)
        {
            if (string.IsNullOrEmpty(address))
                return;

            AsyncOperationHandle<IList<IResourceLocation>> locations = Addressables.LoadResourceLocationsAsync(address, typeof(GameObject));
            try
            {
                // Locations come from the catalog that's already loaded, so this completes at once.
                locations.WaitForCompletion();
                if (locations.Status != AsyncOperationStatus.Succeeded || locations.Result == null)
                    return;

                foreach (IResourceLocation location in locations.Result)
                {
                    if (location == null || !location.HasDependencies)
                        continue;

                    foreach (IResourceLocation bundle in location.Dependencies)
                    {
                        string key = bundle.InternalId;
                        if (string.IsNullOrEmpty(key) || held.ContainsKey(key))
                            continue;

                        // The asset load already opened the bundle, so this only adds a reference to it.
                        held[key] = Addressables.ResourceManager.ProvideResource<IAssetBundleResource>(bundle);
                    }
                }
            }
            finally
            {
                if (locations.IsValid())
                    Addressables.Release(locations);
            }
        }

        public void ReleaseAll()
        {
            foreach (AsyncOperationHandle handle in held.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }

            held.Clear();
        }
    }

    /// <summary>
    /// Holds at most one Addressables handle per address. Every handle it starts is either kept here or released
    /// before the call returns: on failure, on cancellation and when another call for the same address won.
    /// Kept handles are released by <see cref="ReleaseAllExcept"/> and <see cref="ReleaseAll"/>.
    /// Prefabs found in Resources aren't tracked, because Resources needs no release.
    /// </summary>
    internal sealed class PrefabHandleCache
    {
        private readonly Func<string, IPrefabLoad> startLoad;
        private readonly Func<string, GameObject> loadFromResources;
        private readonly IBundleKeeper bundleKeeper;
        private readonly Dictionary<string, IPrefabLoad> held = new();

        public PrefabHandleCache(Func<string, IPrefabLoad> startLoad, Func<string, GameObject> loadFromResources, IBundleKeeper bundleKeeper = null)
        {
            this.startLoad = startLoad ?? throw new ArgumentNullException(nameof(startLoad));
            this.loadFromResources = loadFromResources ?? (_ => null);
            this.bundleKeeper = bundleKeeper;
        }

        public IBundleKeeper BundleKeeper
            => bundleKeeper;

        public int HeldCount
            => held.Count;

        public IEnumerable<string> HeldAddresses
            => held.Keys;

        public async Task<GameObject> LoadAsync(string address, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            ct.ThrowIfCancellationRequested();

            if (held.TryGetValue(address, out IPrefabLoad existing))
                return existing.Result;

            GameObject fromResources = loadFromResources(address);
            if (fromResources != null)
                return fromResources;

            return await FinishLoadAsync(address, startLoad(address), ct);
        }

        /// <summary>
        /// Loads several addresses at once: every load not held yet is started before any is waited on, so
        /// Addressables reads them together and the total is close to the slowest one instead of the sum.
        /// Held and Resources addresses start nothing. Every started handle is kept or released, even on cancellation.
        /// </summary>
        public async Task LoadManyAsync(IReadOnlyList<string> addresses, CancellationToken ct = default)
        {
            if (addresses == null || addresses.Count == 0)
                return;

            ct.ThrowIfCancellationRequested();

            List<(string address, IPrefabLoad load)> started = new(addresses.Count);
            HashSet<string> seen = new();
            foreach (string address in addresses)
            {
                if (string.IsNullOrEmpty(address) || !seen.Add(address) || held.ContainsKey(address))
                    continue;

                if (loadFromResources(address) != null)
                    continue;

                started.Add((address, startLoad(address)));
            }

            bool cancelled = false;
            Exception failure = null;
            foreach ((string address, IPrefabLoad load) in started)
            {
                // Finish every load, even after a cancellation, so each handle is released or kept.
                try
                {
                    await FinishLoadAsync(address, load, ct);
                }
                catch (OperationCanceledException)
                {
                    cancelled = true;
                }
                catch (Exception ex)
                {
                    failure ??= ex;
                }
            }

            if (cancelled)
                throw new OperationCanceledException(ct);
            if (failure != null)
                throw failure;
        }

        private async Task<GameObject> FinishLoadAsync(string address, IPrefabLoad load, CancellationToken ct)
        {
            try
            {
                if (!load.TryCompleteNow())
                    await load.Task;
            }
            catch
            {
                load.Release();
                throw;
            }

            if (ct.IsCancellationRequested)
            {
                load.Release();
                ct.ThrowIfCancellationRequested();
            }

            if (!load.Succeeded || load.Result == null)
            {
                load.Release();
                return null;
            }

            // Another call for the same address finished first. Keep its handle and drop this one.
            if (held.TryGetValue(address, out IPrefabLoad winner))
            {
                load.Release();
                return winner.Result;
            }

            held[address] = load;
            bundleKeeper?.Retain(address);
            return load.Result;
        }

        public GameObject GetLoaded(string address)
        {
            if (string.IsNullOrEmpty(address))
                return null;

            return held.TryGetValue(address, out IPrefabLoad load) ? load.Result : loadFromResources(address);
        }

        public void ReleaseAllExcept(ISet<string> keep)
        {
            List<string> released = new();
            foreach (KeyValuePair<string, IPrefabLoad> pair in held)
            {
                if (keep != null && keep.Contains(pair.Key))
                    continue;

                pair.Value.Release();
                released.Add(pair.Key);
            }

            for (int i = 0; i < released.Count; i++)
                held.Remove(released[i]);
        }

        public void ReleaseAll()
            => ReleaseAllExcept(null);
    }
}

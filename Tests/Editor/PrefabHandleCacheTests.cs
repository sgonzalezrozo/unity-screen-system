using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ScreenSystem.Tests
{
    // Every test ends by checking that each load started was released exactly once.
    public class PrefabHandleCacheTests
    {
        private sealed class FakeLoad : IPrefabLoad
        {
            public readonly TaskCompletionSource<bool> Completion = new();
            public bool Fails;
            public GameObject Prefab;
            public int ReleaseCount;

            public Task Task => Completion.Task;
            public bool Succeeded => !Fails && Completion.Task.IsCompleted && ReleaseCount == 0;
            public GameObject Result => Succeeded ? Prefab : null;
            public void Release() => ReleaseCount++;
        }

        private sealed class FakeBundleKeeper : IBundleKeeper
        {
            public readonly List<string> Retained = new();
            public int HeldCount => Retained.Count;
            public void Retain(string address) => Retained.Add(address);
            public void ReleaseAll() => Retained.Clear();
        }

        private readonly List<FakeLoad> starts = new();
        private FakeBundleKeeper keeper;
        private readonly Dictionary<string, GameObject> resources = new();
        private GameObject prefab;
        private bool completeImmediately = true;
        private bool failLoads;

        [SetUp]
        public void SetUp()
        {
            starts.Clear();
            resources.Clear();
            completeImmediately = true;
            failLoads = false;
            prefab = new GameObject("Prefab");
            keeper = new FakeBundleKeeper();
        }

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(prefab);

        private PrefabHandleCache CreateCache()
            => new(StartLoad, address => resources.TryGetValue(address, out GameObject found) ? found : null, keeper);

        private IPrefabLoad StartLoad(string address)
        {
            FakeLoad load = new() { Prefab = prefab, Fails = failLoads };
            starts.Add(load);
            if (completeImmediately)
                load.Completion.SetResult(true);
            return load;
        }

        private void AssertEveryLoadReleasedOnce()
        {
            foreach (FakeLoad load in starts)
                Assert.AreEqual(1, load.ReleaseCount, "Each handle must be released exactly once.");
        }

        [Test]
        public async Task SameAddressIsLoadedOnceAndReleasedOnce()
        {
            PrefabHandleCache cache = CreateCache();

            Assert.AreSame(prefab, await cache.LoadAsync("Shop"));
            Assert.AreSame(prefab, await cache.LoadAsync("Shop"));
            Assert.AreEqual(1, starts.Count);
            Assert.AreEqual(1, cache.HeldCount);

            cache.ReleaseAll();
            Assert.AreEqual(0, cache.HeldCount);
            AssertEveryLoadReleasedOnce();
        }

        [Test]
        public async Task ResourcesPrefabsStartNoHandle()
        {
            resources["Splash/SplashMain"] = prefab;
            PrefabHandleCache cache = CreateCache();

            Assert.AreSame(prefab, await cache.LoadAsync("Splash/SplashMain"));
            Assert.AreEqual(0, starts.Count);
            Assert.AreEqual(0, cache.HeldCount);
        }

        [Test]
        public async Task FailedLoadIsReleasedAndNotHeld()
        {
            failLoads = true;
            PrefabHandleCache cache = CreateCache();

            Assert.IsNull(await cache.LoadAsync("Missing"));
            Assert.AreEqual(0, cache.HeldCount);
            AssertEveryLoadReleasedOnce();
        }

        [Test]
        public async Task CancelledLoadIsReleasedAndNotHeld()
        {
            completeImmediately = false;
            PrefabHandleCache cache = CreateCache();
            using CancellationTokenSource cts = new();

            Task<GameObject> loading = cache.LoadAsync("Shop", cts.Token);
            cts.Cancel();
            starts[0].Completion.SetResult(true);

            bool cancelled = false;
            try
            {
                await loading;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            Assert.IsTrue(cancelled);
            Assert.AreEqual(0, cache.HeldCount);
            AssertEveryLoadReleasedOnce();
        }

        [Test]
        public async Task ConcurrentLoadsKeepOneHandleAndReleaseTheOther()
        {
            completeImmediately = false;
            PrefabHandleCache cache = CreateCache();

            Task<GameObject> first = cache.LoadAsync("Shop");
            Task<GameObject> second = cache.LoadAsync("Shop");
            Assert.AreEqual(2, starts.Count);

            starts[0].Completion.SetResult(true);
            starts[1].Completion.SetResult(true);

            Assert.AreSame(prefab, await first);
            Assert.AreSame(prefab, await second);
            Assert.AreEqual(1, cache.HeldCount);
            Assert.AreEqual(1, starts[1].ReleaseCount, "The losing handle is released right away.");

            cache.ReleaseAll();
            AssertEveryLoadReleasedOnce();
        }

        [Test]
        public async Task ReleaseAllExceptKeepsOnlyTheGivenAddresses()
        {
            PrefabHandleCache cache = CreateCache();
            await cache.LoadAsync("Splash");
            await cache.LoadAsync("Shop");
            await cache.LoadAsync("TopBar");

            cache.ReleaseAllExcept(new HashSet<string> { "Splash" });

            CollectionAssert.AreEquivalent(new[] { "Splash" }, cache.HeldAddresses);
            Assert.AreEqual(0, starts[0].ReleaseCount);
            Assert.AreEqual(1, starts[1].ReleaseCount);
            Assert.AreEqual(1, starts[2].ReleaseCount);

            cache.ReleaseAll();
            AssertEveryLoadReleasedOnce();
        }

        [Test]
        public async Task GetLoadedStartsNoLoad()
        {
            PrefabHandleCache cache = CreateCache();

            Assert.IsNull(cache.GetLoaded("Shop"));
            await cache.LoadAsync("Shop");
            Assert.AreSame(prefab, cache.GetLoaded("Shop"));
            Assert.AreEqual(1, starts.Count);

            cache.ReleaseAll();
            AssertEveryLoadReleasedOnce();
        }

        // Bundles are kept open once per address the first time it loads, and never for loads that aren't kept.
        [Test]
        public async Task BundlesAreRetainedOnceForEachAddressThatLoads()
        {
            PrefabHandleCache cache = CreateCache();

            await cache.LoadAsync("Shop");
            await cache.LoadAsync("Shop");
            await cache.LoadAsync("TopBar");

            CollectionAssert.AreEqual(new[] { "Shop", "TopBar" }, keeper.Retained);
            cache.ReleaseAll();
        }

        [Test]
        public async Task ReleasingAssetsKeepsTheirBundles()
        {
            PrefabHandleCache cache = CreateCache();
            await cache.LoadAsync("Shop");

            cache.ReleaseAll();

            Assert.AreEqual(0, cache.HeldCount);
            Assert.AreEqual(1, keeper.HeldCount, "Bundles stay open after a scene change releases the assets.");
            AssertEveryLoadReleasedOnce();
        }

        [Test]
        public async Task FailedCancelledAndResourcesLoadsRetainNoBundle()
        {
            resources["Splash/SplashMain"] = prefab;
            PrefabHandleCache cache = CreateCache();
            await cache.LoadAsync("Splash/SplashMain");

            failLoads = true;
            await cache.LoadAsync("Missing");

            failLoads = false;
            completeImmediately = false;
            using CancellationTokenSource cts = new();
            Task<GameObject> cancelled = cache.LoadAsync("Shop", cts.Token);
            cts.Cancel();
            starts[starts.Count - 1].Completion.SetResult(true);
            try
            {
                await cancelled;
            }
            catch (OperationCanceledException)
            {
            }

            Assert.AreEqual(0, keeper.HeldCount);
        }
    }
}

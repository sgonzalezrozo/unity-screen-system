using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using Unity.Profiling;
using Unity.Profiling.LowLevel;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ScreenSystem.Benchmark
{
    /// <summary>
    /// Drives the project's own screens through the public ScreenManager API and records timings,
    /// allocations, frame spikes, idle cost, memory across scene changes and log noise.
    /// The screens, modals and scenes come from the ScreenRegistry and the build settings.
    /// </summary>
    public sealed class ScreenSystemBenchmarkRunner : MonoBehaviour
    {
        public const string AutoRunKey = "ScreenSystem.Benchmark.AutoRun";
        public const string CommitKey = "ScreenSystem.Benchmark.Commit";

        [SerializeField] private string commitHash = "";
        [SerializeField] private int coldRuns = 3;
        [SerializeField] private int warmRepetitions = 20;
        [SerializeField] private int idleFrames = 300;
        [SerializeField] private int sceneCycles = 12;
        [SerializeField] private int settleFrames = 20;
        [SerializeField] private int settleTimeoutFrames = 900;

        private const int FRAME_RING = 8192;
        private readonly float[] frameDurations = new float[FRAME_RING];
        private readonly long[] frameAllocations = new long[FRAME_RING];
        private readonly List<FrameWaiter> frameWaiters = new();
        private readonly Dictionary<string, List<NavSample>> samples = new();
        private readonly BenchmarkResult result = new();
        private readonly Dictionary<string, ScreenType> screenTypes = new();
        private readonly HashSet<LayerComponent> visibleLayers = new();
        private readonly List<NavSample> renderedLayerSamples = new();
        private int windowRuleChecks;
        private int windowRuleViolations;
        private int maxFullscreens;
        private int maxModals;
        private int maxAlerts;
        private int maxCoveredLayersRendering;
        private string firstViolation;

        private ScreenSystemBenchmarkReport report;
        private ProfilerRecorder batchesRecorder;
        private ProfilerRecorder setPassRecorder;
        private ProfilerRecorder gcAllocRecorder;
        private string status = "Starting";
        private double idleAllocPerFrame = -1;
        private int orphanHandleNotes;
        private readonly Dictionary<string, int> errorMessages = new();
        private int logCount;
        private int errorCount;
        private int quitRequests;
        private int navigationCount;
        private int navigationLogCount;
        private List<string> sceneNames;
        private int sceneCursor;

        public static bool IsRunning { get; private set; }

        private struct NavSample
        {
            public double Ms;
            public int Frames;
            public long AllocBytes;
            public double MaxFrameMs;
            public bool Ok;
        }

        private sealed class FrameWaiter
        {
            public int TargetFrame;
            public TaskCompletionSource<bool> Source;
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartFromEditor()
        {
            if (!SessionState.GetBool(AutoRunKey, false))
                return;

            SessionState.EraseBool(AutoRunKey);
            GameObject go = new("ScreenSystemBenchmarkRunner");
            ScreenSystemBenchmarkRunner runner = go.AddComponent<ScreenSystemBenchmarkRunner>();
            runner.commitHash = SessionState.GetString(CommitKey, "");
        }
#endif

        /// <summary>Starts a run unless one is already running. Use it from a button to run on a device.</summary>
        public static void StartBenchmark()
        {
            if (IsRunning || FindAnyObjectByType<ScreenSystemBenchmarkRunner>() != null)
                return;

            new GameObject("ScreenSystemBenchmarkRunner").AddComponent<ScreenSystemBenchmarkRunner>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            // GC.GetAllocatedBytesForCurrentThread returns 0 on Unity's Mono, so allocations come from the profiler counter.
            // It's available in the Editor and Development builds only.
            gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            StartMemoryCounters();

            if (string.IsNullOrWhiteSpace(commitHash))
            {
                TextAsset commitAsset = Resources.Load<TextAsset>("ScreenSystemBenchmarkCommit");
                if (commitAsset != null)
                    commitHash = commitAsset.text.Trim();
            }

            Application.logMessageReceived += OnLog;
            NavigationEvents.OnQuitApplicationRequested += OnQuitRequested;
        }

        private void Start()
        {
            RunAsync().Forget("ScreenSystemBenchmarkRunner.RunAsync");
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            NavigationEvents.OnQuitApplicationRequested -= OnQuitRequested;
            batchesRecorder.Dispose();
            setPassRecorder.Dispose();
            gcAllocRecorder.Dispose();
            foreach ((string _, ProfilerRecorder recorder) in memoryCounters)
                recorder.Dispose();
            IsRunning = false;
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, Screen.width - 20, 60), $"Screen System benchmark: {status}");
        }

        // Values read in frame N describe frame N-1.
        private void Update()
        {
            frameDurations[Time.frameCount % FRAME_RING] = Time.unscaledDeltaTime * 1000f;
            frameAllocations[Time.frameCount % FRAME_RING] = gcAllocRecorder.Valid ? gcAllocRecorder.LastValue : -1;

            for (int i = frameWaiters.Count - 1; i >= 0; i--)
            {
                FrameWaiter waiter = frameWaiters[i];
                if (Time.frameCount < waiter.TargetFrame)
                    continue;

                frameWaiters.RemoveAt(i);
                waiter.Source.TrySetResult(true);
            }
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            logCount++;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            errorCount++;
            string key = condition.Length > 200 ? condition.Substring(0, 200) : condition;
            errorMessages.TryGetValue(key, out int count);
            errorMessages[key] = count + 1;
        }

        // The most frequent errors, so a device run explains its own error count.
        private void AddErrorNotes()
        {
            List<KeyValuePair<string, int>> sorted = new(errorMessages);
            sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
            for (int i = 0; i < sorted.Count && i < 10; i++)
                result.notes.Add($"Error x{sorted[i].Value}: {sorted[i].Key}");
        }

        // Splits memory by kind, so growth across scene changes points at its source.
        private void AddMemoryBreakdown(string prefix)
        {
            AddMetric($"{prefix}.managed-heap", UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong(), "bytes", "last", 1);
            AddMetric($"{prefix}.native-total", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(), "bytes", "last", 1);
            AddMetric($"{prefix}.graphics-driver", UnityEngine.Profiling.Profiler.GetAllocatedMemoryForGraphicsDriver(), "bytes", "last", 1);
            AddMetric($"{prefix}.textures", Resources.FindObjectsOfTypeAll<Texture>().Length, "count", "last", 1);
            AddMetric($"{prefix}.meshes", Resources.FindObjectsOfTypeAll<Mesh>().Length, "count", "last", 1);
            AddMetric($"{prefix}.materials", Resources.FindObjectsOfTypeAll<Material>().Length, "count", "last", 1);
            AddMetric($"{prefix}.gameobjects", Resources.FindObjectsOfTypeAll<GameObject>().Length, "count", "last", 1);
            AddMetric($"{prefix}.scriptable-objects", Resources.FindObjectsOfTypeAll<ScriptableObject>().Length, "count", "last", 1);

            int bundles = 0;
            foreach (AssetBundle _ in AssetBundle.GetAllLoadedAssetBundles())
                bundles++;
            AddMetric($"{prefix}.assetbundles", bundles, "count", "last", 1);

            int cachedOperations = AddressablesOperationCacheCount();
            if (cachedOperations >= 0)
                AddMetric($"{prefix}.addressables-cached-operations", cachedOperations, "count", "last", 1);

            TrackNewObjects(prefix);
            RecordMemoryCounters();
        }

        // Every Memory profiler counter the player exposes (texture, mesh, shader, asset bundle, profiler, GC...),
        // read once per scene cycle, so growth that isn't in any object shows which subsystem holds it.
        private readonly List<(string name, ProfilerRecorder recorder)> memoryCounters = new();
        private readonly Dictionary<string, List<long>> memoryCounterSamples = new();

        private void StartMemoryCounters()
        {
            List<ProfilerRecorderHandle> handles = new();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.Category != ProfilerCategory.Memory)
                    continue;
                if (description.UnitType != ProfilerMarkerDataUnit.Bytes && description.UnitType != ProfilerMarkerDataUnit.Count)
                    continue;

                ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, description.Name);
                if (recorder.Valid)
                    memoryCounters.Add(($"{description.Name} ({(description.UnitType == ProfilerMarkerDataUnit.Bytes ? "bytes" : "count")})", recorder));
            }
        }

        private void RecordMemoryCounters()
        {
            foreach ((string name, ProfilerRecorder recorder) in memoryCounters)
            {
                if (!memoryCounterSamples.TryGetValue(name, out List<long> values))
                {
                    values = new List<long>();
                    memoryCounterSamples[name] = values;
                }

                values.Add(recorder.LastValue);
            }
        }

        // The counters that grew most from the second cycle to the last. Cycle 1 is skipped as a warm-up.
        private void AddMemoryCounterNotes()
        {
            List<(string name, long first, long last, long growth)> growth = new();
            foreach (KeyValuePair<string, List<long>> pair in memoryCounterSamples)
            {
                List<long> values = pair.Value;
                if (values.Count < 3)
                    continue;

                long first = values[1];
                long last = values[values.Count - 1];
                growth.Add((pair.Key, first, last, last - first));
                AddMetric($"memory.counter.{pair.Key}.growth", last - first, pair.Key.EndsWith("(bytes)") ? "bytes" : "count", "last-cycle2", values.Count);
            }

            growth.Sort((a, b) => Math.Abs(b.growth).CompareTo(Math.Abs(a.growth)));
            for (int i = 0; i < growth.Count && i < 15; i++)
                result.notes.Add($"Counter {growth[i].name}: cycle 2 = {growth[i].first:N0}, last = {growth[i].last:N0}, growth = {growth[i].growth:N0}");

            result.notes.Add($"Memory counters read: {memoryCounters.Count}.");
        }

        private Dictionary<int, (string type, string name, long bytes)> previousObjects;
        private int bundlesReopened;

        // Lists the objects that appeared since the previous cycle and are still alive. In a steady state this
        // should be empty, so anything here is what accumulates across scene changes.
        private void TrackNewObjects(string prefix)
        {
            Dictionary<int, (string type, string name, long bytes)> current = new();
            foreach (UnityEngine.Object obj in Resources.FindObjectsOfTypeAll<UnityEngine.Object>())
            {
                if (obj == null || obj is GameObject || obj is Component)
                    continue;

                current[obj.GetInstanceID()] = (obj.GetType().Name, obj.name, UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(obj));
            }

            if (previousObjects != null)
            {
                Dictionary<string, (int count, long bytes)> byType = new();
                List<(string type, string name, long bytes)> added = new();
                foreach (KeyValuePair<int, (string type, string name, long bytes)> pair in current)
                {
                    if (previousObjects.ContainsKey(pair.Key))
                        continue;

                    added.Add(pair.Value);
                    byType.TryGetValue(pair.Value.type, out (int count, long bytes) total);
                    byType[pair.Value.type] = (total.count + 1, total.bytes + pair.Value.bytes);
                }

                int removed = 0;
                foreach (int id in previousObjects.Keys)
                    removed += current.ContainsKey(id) ? 0 : 1;

                long addedBytes = 0;
                foreach ((string type, string name, long bytes) item in added)
                    addedBytes += item.bytes;

                // A bundle that shows up as new was closed and opened again. That leaked native memory on device.
                int reopened = byType.TryGetValue("AssetBundle", out (int count, long bytes) bundleTotal) ? bundleTotal.count : 0;
                bundlesReopened += reopened;
                AddMetric($"{prefix}.bundles-reopened", reopened, "count", "since-previous-cycle", 1);
                AddMetric("addressables.bundles-reopened-total", bundlesReopened, "count", "total", 1);

                AddMetric($"{prefix}.objects-added", added.Count, "count", "since-previous-cycle", 1);
                AddMetric($"{prefix}.objects-removed", removed, "count", "since-previous-cycle", 1);
                AddMetric($"{prefix}.objects-added-bytes", addedBytes, "bytes", "since-previous-cycle", 1);

                List<KeyValuePair<string, (int count, long bytes)>> types = new(byType);
                types.Sort((a, b) => b.Value.bytes.CompareTo(a.Value.bytes));
                for (int i = 0; i < types.Count && i < 8; i++)
                    result.notes.Add($"{prefix} added {types[i].Value.count} {types[i].Key} ({types[i].Value.bytes / 1024} KB)");

                added.Sort((a, b) => b.bytes.CompareTo(a.bytes));
                for (int i = 0; i < added.Count && i < 12; i++)
                    result.notes.Add($"{prefix} new: {added[i].type} '{added[i].name}' {added[i].bytes / 1024} KB");
            }

            previousObjects = current;
        }

        // Size of Addressables' internal operation cache, read by reflection. -1 when it can't be read.
        private static int AddressablesOperationCacheCount()
        {
            try
            {
                Type addressables = Type.GetType("UnityEngine.AddressableAssets.Addressables, Unity.Addressables");
                object resourceManager = addressables?.GetProperty("ResourceManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.GetValue(null);
                object cache = resourceManager?.GetType().GetField("m_AssetOperationCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(resourceManager);
                return cache is System.Collections.ICollection collection ? collection.Count : -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        private void OnQuitRequested()
            => quitRequests++;

        private Task WaitFrames(int count)
        {
            TaskCompletionSource<bool> source = new();
            frameWaiters.Add(new FrameWaiter { TargetFrame = Time.frameCount + Mathf.Max(1, count), Source = source });
            return source.Task;
        }

        private async Task RunAsync()
        {
            IsRunning = true;
            report = new ScreenSystemBenchmarkReport();
            result.session = ScreenSystemBenchmarkReport.CreateSession(commitHash);
            Debug.Log($"[ScreenSystemBenchmark] Started. Results: {report.Folder}");
            if (!RuntimeApi.HasHandleTracking)
                result.notes.Add("This Screen System version doesn't track Addressables handles, so handle metrics are missing.");

            try
            {
                float bootStart = Time.realtimeSinceStartup;
                while (ScreenManager.Instance == null)
                    await WaitFrames(1);

                bool initialized = await ScreenManager.Instance.WaitForInitializationAsync();
                AddMetric("boot.screen-manager-ready", Time.realtimeSinceStartup, "s", "since-startup", 1);
                if (!initialized)
                {
                    result.notes.Add("ScreenManager failed to initialize. Run stopped.");
                    return;
                }

                CollectTargets();
                await SettleAsync();
                AddMetric("boot.first-settle", Time.realtimeSinceStartup - bootStart, "s", "single", 1);
                Save();

                await MeasureIdleAsync("idle.root-only");
                Save();

                for (int run = 0; run < coldRuns; run++)
                {
                    status = $"Cold pass {run + 1}/{coldRuns}";
                    await LoadNextSceneAsync(record: true);
                    await ColdPassAsync();
                    Save();
                }

                status = "Warm pass";
                await WarmPassAsync();
                Save();

                await MeasureIdleAsync("idle.all-cached");
                Save();

                status = "Scene cycles";
                await SceneCyclesAsync();

                AddMetric("logs.per-navigation", navigationCount > 0 ? (double)navigationLogCount / navigationCount : 0, "lines", "mean", navigationCount);
                AddMetric("logs.errors-total", errorCount, "lines", "total", 1);
                AddMetric("quit-requests", quitRequests, "count", "total", 1);
                AddWindowRuleMetrics();
                AddErrorNotes();
                AddMemoryCounterNotes();
                Save();
                status = $"Finished. {report.JsonPath}";
                Debug.Log($"[ScreenSystemBenchmark] Finished. {report.JsonPath}");
            }
            catch (Exception ex)
            {
                status = $"Aborted: {ex.Message}";
                result.notes.Add($"Run aborted: {ex.GetType().Name}: {ex.Message}");
                Save();
                Debug.LogException(ex);
            }
            finally
            {
                IsRunning = false;
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#endif
            }
        }

        private void CollectTargets()
        {
            ScreenRegistry registry = ScreenSystemSettings.Instance.ScreenRegistry;
            foreach (ScreenDefinition screen in registry.GetAllScreens())
            {
                if (screen == null || string.IsNullOrEmpty(screen.ScreenName))
                    continue;

                screenTypes[screen.ScreenName] = screen.screenType;

                if (screen.PersistOverScenes)
                    result.persistentScreens.Add(screen.ScreenName);
                else if (screen.screenType == ScreenType.modal)
                    result.modalScreens.Add(screen.ScreenName);
                else if (screen.screenType == ScreenType.fullscreen && !screen.IgnoreNavigationRules)
                    result.fullscreenScreens.Add(screen.ScreenName);
            }

            sceneNames = new List<string>();
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                sceneNames.Add(Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i)));

            result.scenes.AddRange(sceneNames);
            sceneCursor = Mathf.Max(0, sceneNames.IndexOf(SceneManager.GetActiveScene().name));
        }

        // Waits until the scene's own start flow stops changing the current screen.
        private async Task SettleAsync()
        {
            string last = null;
            int stable = 0;
            for (int i = 0; i < settleTimeoutFrames; i++)
            {
                await WaitFrames(1);
                string current = ScreenManager.Instance.GetCurrentScreen();
                // The stack changes when a call starts, so also wait for the call to finish loading.
                bool idle = !RuntimeApi.IsTransitioning(ScreenManager.Instance);
                stable = idle && current != null && current == last ? stable + 1 : 0;
                last = current;
                if (stable >= settleFrames)
                    return;
            }

            result.notes.Add($"Settle timed out on scene '{SceneManager.GetActiveScene().name}' (current screen: {last ?? "none"}).");
        }

        private async Task LoadNextSceneAsync(bool record)
        {
            if (sceneNames.Count == 0)
                throw new InvalidOperationException("No scenes in the build settings.");

            sceneCursor = (sceneCursor + 1) % sceneNames.Count;
            string sceneName = sceneNames[sceneCursor];

            Stopwatch watch = Stopwatch.StartNew();
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            while (!operation.isDone)
                await WaitFrames(1);

            double loadMs = watch.Elapsed.TotalMilliseconds;
            await SettleAsync();
            if (record)
                AddSample("scene-change.load", new NavSample { Ms = loadMs, Ok = true });
        }

        private bool IsInstantiated(string screen)
        {
            List<LayerComponent> layers = ScreenManager.Instance.GetScreenLayers(screen);
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i] != null)
                    return true;
            }

            return false;
        }

        private static string ResolveId(string screenName)
        {
            ScreenDefinition definition = ScreenSystemSettings.Instance.ScreenRegistry.GetScreen(screenName);
            return definition != null ? definition.ScreenId : screenName;
        }

        private async Task ColdPassAsync()
        {
            ScreenManager manager = ScreenManager.Instance;
            string root = manager.GetRootScreen();

            foreach (string screen in result.fullscreenScreens)
            {
                if (ResolveId(screen) == root)
                    continue;

                if (IsInstantiated(screen))
                {
                    result.notes.Add($"'{screen}' was already loaded when the cold pass reached it. Not counted as cold.");
                    continue;
                }

                NavSample sample = await MeasureAsync(() => manager.GoTo(screen));
                AddSample("cold-show", sample);
                AddSample($"cold-show.{screen}", sample);
            }

            if (!string.IsNullOrEmpty(root) && manager.GetCurrentScreen() != root)
                AddSample("pop-to-root", await MeasureAsync(() => manager.PopTo(root)));

            foreach (string modal in result.modalScreens)
            {
                bool cold = !IsInstantiated(modal);
                NavSample open = await MeasureAsync(() => manager.GoTo(modal));
                AddSample(cold ? "modal-open-cold" : "modal-open-warm", open);
                if (open.Ok)
                    AddSample("modal-close", await MeasureAsync(() => manager.GoBack()));
            }
        }

        private async Task WarmPassAsync()
        {
            ScreenManager manager = ScreenManager.Instance;
            string root = manager.GetRootScreen();

            // Load everything once, so the timed loop only sees cached screens.
            foreach (string screen in result.fullscreenScreens)
            {
                if (ResolveId(screen) == root || IsInstantiated(screen))
                    continue;

                await manager.GoTo(screen);
                await manager.PopTo(root);
            }

            for (int rep = 0; rep < warmRepetitions; rep++)
            {
                foreach (string screen in result.fullscreenScreens)
                {
                    if (ResolveId(screen) == root)
                        continue;

                    if (manager.GetCurrentScreen() != root)
                        await manager.PopTo(root);

                    NavSample show = await MeasureAsync(() => manager.GoTo(screen));
                    AddSample("warm-show", show);
                    AddSample($"warm-show.{screen}", show);

                    if (show.Ok && manager.CanGoBack)
                        AddSample("warm-back", await MeasureAsync(() => manager.GoBack()));
                }

                foreach (string modal in result.modalScreens)
                {
                    NavSample open = await MeasureAsync(() => manager.GoTo(modal));
                    AddSample("modal-open-warm", open);
                    if (open.Ok)
                        AddSample("modal-close", await MeasureAsync(() => manager.GoBack()));
                }
            }
        }

        private async Task SceneCyclesAsync()
        {
            ScreenManager manager = ScreenManager.Instance;
            string persistent = result.persistentScreens.Count > 0 ? result.persistentScreens[0] : null;
            long firstManaged = 0, firstNative = 0;
            int firstLayers = 0;
            int firstHandles = 0;
            int maxOrphanHandles = 0;

            for (int cycle = 0; cycle < sceneCycles; cycle++)
            {
                bool persistentOpened = false;
                if (persistent != null)
                    persistentOpened = await manager.GoTo(persistent);

                await LoadNextSceneAsync(record: true);

                if (persistentOpened)
                {
                    List<LayerComponent> layers = manager.GetScreenLayers(persistent);
                    int alive = 0;
                    for (int i = 0; i < layers.Count; i++)
                        alive += layers[i] != null ? 1 : 0;

                    AddSample("persistent.layers-alive-ratio", new NavSample { Ms = layers.Count > 0 ? (double)alive / layers.Count : 0, Ok = true });

                    // The new scene's first fullscreen screen must hide the persistent one (a splash, for example).
                    bool stillVisible = layers.Count > 0 && IsRendering(layers[0]);
                    AddSample("persistent.visible-after-scene-start", new NavSample { Ms = stillVisible ? 1 : 0, Ok = true });
                }

                await CollectAsync();
                long managed = GC.GetTotalMemory(false);
                long native = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                int layerCount = CountLayers();
                if (cycle == 0)
                {
                    firstManaged = managed;
                    firstNative = native;
                    firstLayers = layerCount;
                }

                AddSample("scene-change.layers-after-cleanup", new NavSample { Ms = layerCount, Ok = true });
                AddMemoryBreakdown($"memory.cycle{cycle + 1}");
                int handles = RuntimeApi.HeldHandleCount();
                if (cycle == 0)
                    firstHandles = handles;

                if (RuntimeApi.HasHandleTracking)
                {
                    AddSample("scene-change.handles-after-cleanup", new NavSample { Ms = handles, Ok = true });
                    maxOrphanHandles = Math.Max(maxOrphanHandles, CountOrphanHandles());
                }

                if (cycle == sceneCycles - 1)
                {
                    AddMetric("scene-change.managed-growth", managed - firstManaged, "bytes", $"cycle{sceneCycles}-cycle1", sceneCycles);
                    AddMetric("scene-change.native-growth", native - firstNative, "bytes", $"cycle{sceneCycles}-cycle1", sceneCycles);
                    AddMetric("scene-change.layer-growth", layerCount - firstLayers, "count", $"cycle{sceneCycles}-cycle1", sceneCycles);
                    if (RuntimeApi.HasHandleTracking)
                        AddMetric("scene-change.handle-growth", handles - firstHandles, "count", $"cycle{sceneCycles}-cycle1", sceneCycles);
                    AddMetric("memory.managed-after-cleanup", managed, "bytes", "last", 1);
                    AddMetric("memory.native-after-cleanup", native, "bytes", "last", 1);
                }

                string root = manager.GetRootScreen();
                foreach (string screen in result.fullscreenScreens)
                {
                    if (ResolveId(screen) != root && await manager.GoTo(screen))
                        await manager.PopTo(root);
                }

                await CollectAsync();
                if (cycle == sceneCycles - 1)
                {
                    AddMetric("memory.managed-all-loaded", GC.GetTotalMemory(false), "bytes", "last", 1);
                    AddMetric("memory.native-all-loaded", UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(), "bytes", "last", 1);
                    AddMetric("layers.instances-all-loaded", CountLayers(), "count", "last", 1);
                    AddMetric("layers.references-all-loaded", CountLayerReferences(), "count", "last", 1);
                    if (RuntimeApi.HasHandleTracking)
                        AddMetric("addressables.handles-all-loaded", RuntimeApi.HeldHandleCount(), "count", "last", 1);
                    if (RuntimeApi.HasBundleTracking)
                        AddMetric("addressables.bundles-held", RuntimeApi.HeldBundleCount(), "count", "last", 1);
                }

                if (RuntimeApi.HasHandleTracking)
                {
                    maxOrphanHandles = Math.Max(maxOrphanHandles, CountOrphanHandles());
                    AddMetric("addressables.orphan-handles", maxOrphanHandles, "count", "max", cycle + 1);
                }

                Save();
            }
        }

        private static int CountLayers()
        {
#if UNITY_2023_1_OR_NEWER
            return FindObjectsByType<LayerComponent>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
#else
            return FindObjectsOfType<LayerComponent>(true).Length;
#endif
        }

        private static bool IsRendering(LayerComponent layer)
        {
            if (layer == null || !layer.gameObject.activeInHierarchy)
                return false;

            if (layer.IsCanvasEnabled)
                return true;

            UnityEngine.UIElements.UIDocument document = layer.GetComponent<UnityEngine.UIElements.UIDocument>();
            return document != null && document.enabled;
        }

        // The rule: at most one fullscreen, one modal and one alert rendering, and no layer of a covered screen rendering.
        // A screen counts as rendering when its main layer (the screen prefab) is rendering.
        private void CheckWindowRule()
        {
            int fullscreens = 0, modals = 0, alerts = 0;
            visibleLayers.Clear();

            foreach (KeyValuePair<string, ScreenType> pair in screenTypes)
            {
                List<LayerComponent> layers = ScreenManager.Instance.GetScreenLayers(pair.Key);
                if (layers.Count == 0 || !IsRendering(layers[0]))
                    continue;

                if (pair.Value == ScreenType.modal)
                    modals++;
                else if (pair.Value == ScreenType.alert)
                    alerts++;
                else
                    fullscreens++;

                foreach (LayerComponent layer in layers)
                    visibleLayers.Add(layer);
            }

            int rendering = 0, covered = 0;
#if UNITY_2023_1_OR_NEWER
            LayerComponent[] all = FindObjectsByType<LayerComponent>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            LayerComponent[] all = FindObjectsOfType<LayerComponent>();
#endif
            foreach (LayerComponent layer in all)
            {
                if (!IsRendering(layer))
                    continue;

                rendering++;
                if (!visibleLayers.Contains(layer))
                    covered++;
            }

            windowRuleChecks++;
            maxFullscreens = Mathf.Max(maxFullscreens, fullscreens);
            maxModals = Mathf.Max(maxModals, modals);
            maxAlerts = Mathf.Max(maxAlerts, alerts);
            maxCoveredLayersRendering = Mathf.Max(maxCoveredLayersRendering, covered);
            renderedLayerSamples.Add(new NavSample { Ms = rendering, Ok = true });

            bool violated = fullscreens > 1 || modals > 1 || alerts > 1 || covered > 0;
            if (!violated)
                return;

            windowRuleViolations++;
            firstViolation ??= $"After navigating to '{ScreenManager.Instance.GetCurrentScreen()}': {fullscreens} fullscreen, {modals} modal, {alerts} alert, {covered} covered layers rendering.";
        }

        private void AddWindowRuleMetrics()
        {
            AddMetric("render.window-rule-violations", windowRuleViolations, "count", "total", windowRuleChecks);
            AddMetric("render.max-fullscreens", maxFullscreens, "count", "max", windowRuleChecks);
            AddMetric("render.max-modals", maxModals, "count", "max", windowRuleChecks);
            AddMetric("render.max-alerts", maxAlerts, "count", "max", windowRuleChecks);
            AddMetric("render.max-covered-layers", maxCoveredLayersRendering, "count", "max", windowRuleChecks);
            AddMetric("render.layers-rendering", Percentile(renderedLayerSamples, s => s.Ms, 0.5), "count", "median", renderedLayerSamples.Count);
            AddMetric("render.layers-rendering.max", Percentile(renderedLayerSamples, s => s.Ms, 1.0), "count", "max", renderedLayerSamples.Count);
            if (firstViolation != null)
                result.notes.Add($"First window rule violation: {firstViolation}");
        }

        // Addressables handles held for an address that no loaded screen uses. Any value above 0 is a leak.
        private int CountOrphanHandles()
        {
            HashSet<string> inUse = new();
            foreach (ScreenDefinition screen in ScreenSystemSettings.Instance.ScreenRegistry.GetAllScreens())
            {
                if (screen == null)
                    continue;

                bool instantiated = false;
                foreach (LayerComponent layer in ScreenManager.Instance.GetScreenLayers(screen.ScreenId))
                {
                    if (layer == null)
                        continue;

                    instantiated = true;
                    inUse.Add(layer.LayerName);
                }

                if (instantiated && !string.IsNullOrEmpty(screen.AssetPath))
                    inUse.Add(screen.AssetPath);
            }

            int orphans = 0;
            foreach (string address in RuntimeApi.HeldAddresses())
            {
                if (!inUse.Contains(address))
                {
                    orphans++;
                    if (orphanHandleNotes++ < 5)
                        result.notes.Add($"Handle held for '{address}', which no loaded screen uses.");
                }
            }

            return orphans;
        }

        private long SumAllocations(int fromFrame, int toFrame)
        {
            long total = 0;
            for (int f = fromFrame; f <= toFrame; f++)
                total += Math.Max(0, frameAllocations[f % FRAME_RING]);
            return total;
        }

        // Sum of each screen's layer list. Above the instance count when screens share layers.
        private int CountLayerReferences()
        {
            int total = 0;
            List<string> all = new(result.fullscreenScreens);
            all.AddRange(result.modalScreens);
            all.AddRange(result.persistentScreens);
            foreach (string screen in all)
            {
                foreach (LayerComponent layer in ScreenManager.Instance.GetScreenLayers(screen))
                    total += layer != null ? 1 : 0;
            }

            return total;
        }

        private async Task CollectAsync()
        {
            AsyncOperation unload = Resources.UnloadUnusedAssets();
            while (!unload.isDone)
                await WaitFrames(1);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            await WaitFrames(2);
        }

        private async Task MeasureIdleAsync(string name)
        {
            await WaitFrames(30);
            status = $"{name}...";
            int startFrame = Time.frameCount;
            await WaitFrames(idleFrames);
            int frames = Time.frameCount - startFrame;
            long bytes = SumAllocations(startFrame + 1, startFrame + frames);
            if (idleAllocPerFrame < 0)
                idleAllocPerFrame = frames > 0 ? (double)bytes / frames : 0;

            double total = 0, max = 0;
            for (int f = startFrame + 1; f <= startFrame + frames; f++)
            {
                double ms = frameDurations[f % FRAME_RING];
                total += ms;
                max = Math.Max(max, ms);
            }

            AddMetric($"{name}.alloc-per-frame", frames > 0 ? (double)bytes / frames : 0, "bytes", "mean", frames);
            AddMetric($"{name}.frame-ms", frames > 0 ? total / frames : 0, "ms", "mean", frames);
            AddMetric($"{name}.frame-ms-max", max, "ms", "max", frames);
            if (batchesRecorder.Valid)
                AddMetric($"{name}.batches", batchesRecorder.LastValue, "count", "last", 1);
            if (setPassRecorder.Valid)
                AddMetric($"{name}.setpass", setPassRecorder.LastValue, "count", "last", 1);
            AddMetric($"{name}.layer-instances", CountLayers(), "count", "last", 1);
        }

        private async Task<NavSample> MeasureAsync(Func<Task<bool>> navigation)
        {
            int startFrame = Time.frameCount;
            int startLogs = logCount;
            Stopwatch watch = Stopwatch.StartNew();
            bool ok;

            try
            {
                ok = await navigation();
            }
            catch (Exception ex)
            {
                ok = false;
                result.notes.Add($"Navigation threw {ex.GetType().Name}: {ex.Message}");
            }

            watch.Stop();
            int endFrame = Time.frameCount;
            navigationCount++;
            navigationLogCount += logCount - startLogs;
            CheckWindowRule();

            // A frame's duration and allocations are known on the next Update, so wait one frame before reading them.
            await WaitFrames(1);
            double maxFrame = 0;
            for (int f = startFrame + 1; f <= endFrame + 1; f++)
                maxFrame = Math.Max(maxFrame, frameDurations[f % FRAME_RING]);

            // Whole-frame allocations, minus what an idle frame allocates.
            int frameSpan = endFrame - startFrame + 1;
            long alloc = Math.Max(0, SumAllocations(startFrame + 1, endFrame + 1) - (long)(Math.Max(0, idleAllocPerFrame) * frameSpan));

            return new NavSample
            {
                Ms = watch.Elapsed.TotalMilliseconds,
                Frames = endFrame - startFrame,
                AllocBytes = alloc,
                MaxFrameMs = maxFrame,
                Ok = ok
            };
        }

        private void AddSample(string phase, NavSample sample)
        {
            if (!samples.TryGetValue(phase, out List<NavSample> list))
            {
                list = new List<NavSample>();
                samples[phase] = list;
            }

            list.Add(sample);
        }

        private void AddMetric(string name, double value, string unit, string aggregation, int count)
        {
            result.metrics.RemoveAll(m => m.name == name);
            result.metrics.Add(new BenchmarkMetric { name = name, value = value, unit = unit, aggregation = aggregation, samples = count });
        }

        private void Save()
        {
            foreach (KeyValuePair<string, List<NavSample>> pair in samples)
            {
                string phase = pair.Key;
                List<NavSample> list = pair.Value;
                bool isValueOnly = phase.StartsWith("persistent.") || phase.StartsWith("scene-change.layers") || phase.StartsWith("scene-change.handles") || phase == "scene-change.load";

                if (isValueOnly)
                {
                    string unit = phase == "scene-change.load" ? "ms" : phase.StartsWith("persistent.") ? "ratio" : "count";
                    AddMetric(phase, Percentile(list, s => s.Ms, 0.5), unit, "median", list.Count);
                    if (phase == "scene-change.load")
                        AddMetric(phase + ".p95", Percentile(list, s => s.Ms, 0.95), unit, "p95", list.Count);
                    continue;
                }

                int failures = 0;
                foreach (NavSample s in list)
                    failures += s.Ok ? 0 : 1;

                bool perScreen = phase.Split('.').Length > 1;
                AddMetric($"{phase}.ms", Percentile(list, s => s.Ms, 0.5), "ms", "median", list.Count);
                if (perScreen)
                {
                    AddMetric($"{phase}.ms.max", Percentile(list, s => s.Ms, 1.0), "ms", "max", list.Count);
                    continue;
                }

                AddMetric($"{phase}.ms.p95", Percentile(list, s => s.Ms, 0.95), "ms", "p95", list.Count);
                AddMetric($"{phase}.frames", Percentile(list, s => s.Frames, 0.5), "frames", "median", list.Count);
                AddMetric($"{phase}.alloc", Percentile(list, s => s.AllocBytes, 0.5), "bytes", "median", list.Count);
                AddMetric($"{phase}.max-frame-ms", Percentile(list, s => s.MaxFrameMs, 1.0), "ms", "max", list.Count);
                AddMetric($"{phase}.failures", failures, "count", "total", list.Count);
            }

            result.metrics.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            report.Save(result);
        }

        private static double Percentile(List<NavSample> list, Func<NavSample, double> selector, double p)
        {
            if (list.Count == 0)
                return 0;

            double[] values = new double[list.Count];
            for (int i = 0; i < list.Count; i++)
                values[i] = selector(list[i]);

            Array.Sort(values);
            int index = (int)Math.Ceiling(p * values.Length) - 1;
            return values[Mathf.Clamp(index, 0, values.Length - 1)];
        }
    }
}

using Unity.Profiling;

namespace ScreenSystem
{
    /// <summary>
    /// Profiler markers for the synchronous steps of showing, hiding and unloading screens. They show the package's
    /// cost in the Unity Profiler and can be read with ProfilerRecorder. They compile out of Release builds.
    /// Markers wrap only code that runs within one frame, never across an await.
    /// </summary>
    public static class ScreenSystemMarkers
    {
        public const string LoadPrefabName = "ScreenSystem.LoadPrefab";
        public const string InstantiateLayerName = "ScreenSystem.InstantiateLayer";
        public const string InjectLayerName = "ScreenSystem.InjectLayer";
        public const string ReassignLayersName = "ScreenSystem.ReassignLayers";
        public const string PrepareLayersName = "ScreenSystem.PrepareLayers";
        public const string ActivateLayersName = "ScreenSystem.ActivateLayers";
        public const string HideLayersName = "ScreenSystem.HideLayers";
        public const string LifecycleCallbacksName = "ScreenSystem.LifecycleCallbacks";
        public const string MetricsName = "ScreenSystem.Metrics";
        public const string SceneCleanupName = "ScreenSystem.SceneCleanup";

        /// <summary>Every marker name, for tools that record them all.</summary>
        public static readonly string[] All =
        {
            LoadPrefabName, InstantiateLayerName, InjectLayerName, ReassignLayersName, PrepareLayersName,
            ActivateLayersName, HideLayersName, LifecycleCallbacksName, MetricsName, SceneCleanupName
        };

        internal static readonly ProfilerMarker LoadPrefab = new(ProfilerCategory.Loading, LoadPrefabName);
        internal static readonly ProfilerMarker InstantiateLayer = new(ProfilerCategory.Scripts, InstantiateLayerName);
        internal static readonly ProfilerMarker InjectLayer = new(ProfilerCategory.Scripts, InjectLayerName);
        internal static readonly ProfilerMarker ReassignLayers = new(ProfilerCategory.Scripts, ReassignLayersName);
        internal static readonly ProfilerMarker PrepareLayers = new(ProfilerCategory.Scripts, PrepareLayersName);
        internal static readonly ProfilerMarker ActivateLayers = new(ProfilerCategory.Scripts, ActivateLayersName);
        internal static readonly ProfilerMarker HideLayers = new(ProfilerCategory.Scripts, HideLayersName);
        internal static readonly ProfilerMarker LifecycleCallbacks = new(ProfilerCategory.Scripts, LifecycleCallbacksName);
        internal static readonly ProfilerMarker Metrics = new(ProfilerCategory.Scripts, MetricsName);
        internal static readonly ProfilerMarker SceneCleanup = new(ProfilerCategory.Scripts, SceneCleanupName);
    }
}

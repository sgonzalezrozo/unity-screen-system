using System.Collections.Generic;
using System.Reflection;

namespace ScreenSystem.Benchmark
{
    /// <summary>
    /// Reads APIs that only newer Screen System versions have, so the same benchmark can run against an older
    /// version for an A/B comparison. Missing APIs return neutral values.
    /// </summary>
    internal static class RuntimeApi
    {
        private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;

        private static readonly PropertyInfo isTransitioning =
            typeof(ScreenManager).GetProperty("IsTransitioning", BindingFlags.Public | BindingFlags.Instance);

        private static readonly PropertyInfo heldHandleCount =
            typeof(LayerPrefabLoader).GetProperty("HeldHandleCount", PublicStatic);

        private static readonly PropertyInfo heldAddresses =
            typeof(LayerPrefabLoader).GetProperty("HeldAddresses", PublicStatic);

        private static readonly PropertyInfo heldBundleCount =
            typeof(LayerPrefabLoader).GetProperty("HeldBundleCount", PublicStatic);

        public static bool HasBundleTracking
            => heldBundleCount != null;

        public static int HeldBundleCount()
            => heldBundleCount != null ? (int)heldBundleCount.GetValue(null) : 0;

        /// <summary>ScreenSystem.* profiler marker names, or none on versions without them.</summary>
        public static string[] MarkerNames()
        {
            System.Type markers = System.Type.GetType("ScreenSystem.ScreenSystemMarkers, ScreenSystem");
            return markers?.GetField("All", PublicStatic)?.GetValue(null) as string[] ?? System.Array.Empty<string>();
        }

        public static bool HasHandleTracking
            => heldHandleCount != null && heldAddresses != null;

        public static bool IsTransitioning(ScreenManager manager)
            => manager != null && isTransitioning != null && (bool)isTransitioning.GetValue(manager);

        public static int HeldHandleCount()
            => heldHandleCount != null ? (int)heldHandleCount.GetValue(null) : 0;

        public static IEnumerable<string> HeldAddresses()
            => heldAddresses?.GetValue(null) as IEnumerable<string> ?? new List<string>();
    }
}

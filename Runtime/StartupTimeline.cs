using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ScreenSystem
{
    /// <summary>
    /// Timestamps of Screen System's startup steps, for measuring what it adds to an app's launch. Compiled only
    /// into the Editor, Development builds and builds with SCREEN_SYSTEM_METRICS, so release builds pay nothing.
    /// </summary>
    internal static class StartupTimeline
    {
        internal readonly struct Mark
        {
            public readonly string Name;
            public readonly double Seconds;
            public readonly int Frame;

            public Mark(string name, double seconds, int frame)
            {
                Name = name;
                Seconds = seconds;
                Frame = frame;
            }
        }

        private static readonly List<Mark> marks = new();
        private static readonly HashSet<string> once = new();

        internal static IReadOnlyList<Mark> Marks
            => marks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            marks.Clear();
            once.Clear();
        }

        /// <summary>Records <paramref name="name"/> the first time it's reached in this session.</summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("SCREEN_SYSTEM_METRICS")]
        internal static void MarkOnce(string name)
        {
            if (once.Add(name))
                marks.Add(new Mark(name, Time.realtimeSinceStartupAsDouble, Time.frameCount));
        }
    }
}

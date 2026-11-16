using System.Diagnostics;
using Object = UnityEngine.Object;

namespace ScreenSystem
{
    /// <summary>
    /// Informational logs. Compiled out, with their message strings, unless SCREEN_SYSTEM_VERBOSE is defined, so
    /// navigation doesn't format or print messages. Warnings and errors use Debug.LogWarning and Debug.LogError.
    /// </summary>
    internal static class ScreenSystemLog
    {
        [Conditional("SCREEN_SYSTEM_VERBOSE")]
        public static void Info(string message, Object context = null)
            => UnityEngine.Debug.Log(message, context);
    }
}

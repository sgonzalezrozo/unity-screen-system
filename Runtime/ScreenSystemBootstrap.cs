using UnityEngine;

namespace ScreenSystem
{
    public class ScreenSystemBootstrap : MonoBehaviour
    {
        private static ScreenSystemBootstrap instance;

        // With Enter Play Mode Options and domain reload off, statics survive between Play sessions. Start each one clean.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
            => instance = null;

        public static ScreenSystemBootstrap Instance
            => instance;

        public static bool IsInitialized
            => instance != null;

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            StackTraceLogType previousLogType = Application.GetStackTraceLogType(LogType.Warning);
            Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Application.SetStackTraceLogType(LogType.Warning, previousLogType);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }
    }
}


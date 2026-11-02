using UnityEngine;

namespace ScreenSystem
{
    public class ScreenSystemBootstrap : MonoBehaviour
    {
        private static ScreenSystemBootstrap instance;

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


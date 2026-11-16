using UnityEngine;

namespace ScreenSystem
{
    internal static class ScreenManagerBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            #if UNITY_2023_1_OR_NEWER
            ScreenManager existingScreenManager = Object.FindFirstObjectByType<ScreenManager>();
            #else
            ScreenManager existingScreenManager = Object.FindObjectOfType<ScreenManager>();
            #endif

            if (existingScreenManager != null)
                return;

            GameObject screenSystemPrefab = Resources.Load<GameObject>("Tools/Screen System/Prefabs/ScreenSystem");
            if (screenSystemPrefab != null)
            {
                GameObject screenSystemInstance = Object.Instantiate(screenSystemPrefab);
                screenSystemInstance.name = "ScreenSystem";
            }
            else
            {
                Debug.LogWarning("[ScreenSystem] ScreenSystem prefab not found at Resources/Tools/Screen System/Prefabs/ScreenSystem.prefab. Run Tools > Screen System > Setup.");
                ScreenManager.ReportNotAvailable();
            }
        }
    }
}


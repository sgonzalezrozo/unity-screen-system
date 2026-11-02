using ScreenSystem;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    public class ScreenSystemDiagnostic : MonoBehaviour
    {
        private void Start()
        {
            ScreenSystemSettings settings = ScreenSystemSettings.Instance;
        
            Debug.Log("=== SCREEN SYSTEM DIAGNOSTIC ===");
            Debug.Log($"Settings: {(settings != null ? "FOUND" : "NULL")}");
        
            if (settings == null)
            {
                Debug.LogError("ScreenSystemSettings is NULL!");
                return;
            }
        
            ScreenRegistry registry = settings.ScreenRegistry;
            Debug.Log($"ScreenRegistry reference: {(registry != null ? "FOUND" : "NULL")}");
            Debug.Log($"ScreenRegistry asset path: {(registry != null ? AssetDatabase.GetAssetPath(registry) : "N/A")}");
        
            if (registry == null)
            {
                Debug.LogError("ScreenRegistry is NULL - this should cause failures!");
                return;
            }
        
            Debug.Log($"ScreenRegistry.Screens count: {registry.Screens.Count}");
        
            if (registry.Screens.Count == 0)
            {
                Debug.LogWarning("ScreenRegistry.Screens is EMPTY (0 items)");
            }
            else
            {
                Debug.Log("First 3 screens:");
                for (int i = 0; i < Mathf.Min(3, registry.Screens.Count); i++)
                {
                    ScreenDefinition screen = registry.Screens[i];
                    Debug.Log($"  [{i}] {screen.ScreenName} (ID: {screen.ScreenId})");
                }
            }
        
            Debug.Log("=== END DIAGNOSTIC ===");
        }
    }
}

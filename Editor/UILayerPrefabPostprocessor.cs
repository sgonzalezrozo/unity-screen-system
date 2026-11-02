#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{    public class UILayerPrefabPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer < 0)
            {
                // UI layer doesn't exist, skip
                return;
            }

            foreach (string assetPath in importedAssets)
            {
                // Only process prefabs in specific folders
                if (!assetPath.EndsWith(".prefab"))
                    continue;

                // Check if it's in a Layer or Content folder (or matches naming pattern)
                bool shouldProcess = assetPath.Contains("/Layer") ||
                                    assetPath.Contains("/Content") ||
                                    assetPath.Contains("Layer.prefab") ||
                                    assetPath.Contains("Content.prefab");

                if (!shouldProcess)
                    continue;

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                    continue;

                // Check if it needs layer update
                bool needsUpdate = false;
                if (prefab.layer != uiLayer)
                {
                    needsUpdate = true;
                }
                else
                {
                    // Check children
                    Transform[] children = prefab.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in children)
                    {
                        if (child.gameObject.layer != uiLayer)
                        {
                            needsUpdate = true;
                            break;
                        }
                    }
                }

                if (needsUpdate)
                {
                    // Load the prefab for editing
                    string prefabPath = AssetDatabase.GetAssetPath(prefab);
                    GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

                    // Set layer recursively
                    SetLayerRecursively(prefabContents, uiLayer);

                    // Save the modified prefab
                    PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                    PrefabUtility.UnloadPrefabContents(prefabContents);

                    Debug.Log($"[UILayerPostprocessor] Set UI layer on prefab and children: {assetPath}");
                }
            }
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null)
                return;

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }

    // UI Layer Prefab Utility menu items removed - no longer needed
}
#endif

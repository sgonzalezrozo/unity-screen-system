using System.Collections.Generic;
using System.IO;
using Tools.ScreenSystem.Scripts;
using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Tools.ScreenSystem.Editor
{
    public class ScreenLayersManager
    {
        public class LayerData
        {
            public string LayerName;
            public int ZOrder;
            public bool HasShell;
            public bool HasContent;
        }

        private string screenId;
        private ScreenDefinition screenDef;
        private List<LayerData> layers;

        private const string CONTENT_BASE_PATH = "Assets/Addressables/Content";

        public ScreenLayersManager(string screenId)
        {
            this.screenId = screenId;
            this.layers = new List<LayerData>();
        }

        public List<LayerData> GetLayers()
            => layers;

        public ScreenDefinition GetScreenDefinition()
            => screenDef;

        public bool LoadScreenData()
        {
            Debug.Log($"[ScreenLayersManager] Loading data for screen: '{screenId}'");

            if (!ScreenSystemSettings.IsValid())
            {
                Debug.LogWarning("[ScreenLayersManager] ScreenSystem not set up");
                return false;
            }

            ScreenRegistry registry = ScreenSystemSettings.Instance.ScreenRegistry;
            if (registry == null)
            {
                Debug.LogError("[ScreenLayersManager] ScreenRegistry is null");
                return false;
            }

            List<ScreenDefinition> screens = registry.GetAllScreens();
            if (screens == null || screens.Count == 0)
            {
                Debug.LogWarning("[ScreenLayersManager] ScreenRegistry has no screens");
                return false;
            }

            screenDef = registry.GetScreen(screenId);
            if (screenDef == null)
            {
                Debug.LogError($"[ScreenLayersManager] Screen '{screenId}' not found in registry");
                return false;
            }

            layers.Clear();

            if (layers.Count == 0)
            {
                layers.Add(new LayerData
                {
                    LayerName = $"{screenId}Layer",
                    ZOrder = 10,
                    HasShell = false,
                    HasContent = false
                });
            }

            Debug.Log($"[ScreenLayersManager] Loaded {layers.Count} layers for screen '{screenId}'");
            return true;
        }

        public void SaveLayers()
        {
            if (screenDef == null)
            {
                Debug.LogWarning("[ScreenLayersManager] Cannot save - screen definition is null");
                return;
            }

            ScreenRegistry registry = ScreenSystemSettings.Instance.ScreenRegistry;
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ScreenLayersManager] Saved {layers.Count} layers for screen '{screenId}'");
        }

        public int CreateContentPrefabs()
        {
            int contentCreated = 0;

            CreateDirectoryIfNeeded(CONTENT_BASE_PATH);

            foreach (LayerData layer in layers)
            {
                if (layer.HasContent)
                    continue;

                CreateContentPrefab(layer);
                contentCreated++;
            }

            AssetDatabase.Refresh();

            Debug.Log($"[ScreenLayersManager] Created {contentCreated} content prefabs");
            return contentCreated;
        }

        public bool LayerShellExists(string layerName)
        {
            return false;
        }

        public bool LayerContentExists(string layerName)
        {
            string contentName = layerName.Replace("Layer", "Content");
            string path = $"{CONTENT_BASE_PATH}/{contentName}.prefab";

            return File.Exists(path);
        }

        private void CreateContentPrefab(LayerData layer)
        {
            string contentName = layer.LayerName.Replace("Layer", "Content");
            string path = $"{CONTENT_BASE_PATH}/{contentName}.prefab";

            GameObject content = new GameObject(contentName);
            RectTransform rectTransform = content.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;

            PrefabUtility.SaveAsPrefabAsset(content, path);
            Object.DestroyImmediate(content);

            Debug.Log($"[ScreenLayersManager] Created content: {path}");
        }

        private void CreateDirectoryIfNeeded(string path)
        {
            if (Directory.Exists(path))
                return;

            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }
}


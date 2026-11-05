using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LayerDefinitionClass = ScreenSystem.LayerDefinition;

namespace ScreenSystem.Editor.LayerDefinitionEditor
{
    /// <summary>
    /// Manages layer preview instantiation and rendering in the editor.
    /// Single responsibility: Create, update, and destroy layer preview instances.
    /// </summary>
    public class LayerDefinitionPreviewManager
    {
        private const double UPDATE_THROTTLE = 0.5;
        private const int PREVIEW_BASE_SORTING_ORDER = -1000;

        private readonly Dictionary<string, GameObject> previewInstances = new();
        private readonly Dictionary<string, GameObject> previewPrefabCache = new();

        private GameObject previewContainer;
        private bool previewsInitialized;
        private double lastUpdateTime;

        private UnityEditor.Editor currentPreviewEditor;

        /// <summary>
        /// Update previews based on layer definition state.
        /// Throttles updates to prevent performance issues.
        /// </summary>
        public void Update(LayerDefinitionClass layerDef)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (layerDef == null)
                return;

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage == null)
                return;

            if (!layerDef.ShowLayerPreviews)
            {
                if (!previewsInitialized)
                    return;

                DestroyAllPreviews();
                DestroyPreviewContainer();
                previewsInitialized = false;
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            if (previewsInitialized && (currentTime - lastUpdateTime) < UPDATE_THROTTLE)
                return;

            lastUpdateTime = currentTime;

            if (!previewsInitialized)
            {
                InitializePreviews(layerDef);
                previewsInitialized = true;
                return;
            }

            UpdateExistingPreviews(layerDef);
        }

        public void CreatePreviewForLayer(string layerName, int index, LayerDefinitionClass layerDef)
        {
            GameObject prefab = FindLayerPrefab(layerName);
            if (prefab == null)
                return;

            PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject instance = Object.Instantiate(prefab);

            if (instance == null)
                return;

            instance.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.NotEditable;
            instance.name = $"[PREVIEW] {layerName}";
            SetHideFlagsRecursively(instance);

            RectTransform prefabRect = prefab.GetComponent<RectTransform>();
            RectTransform instanceRect = instance.GetComponent<RectTransform>();

            if (prefabStage != null)
                SceneManager.MoveGameObjectToScene(instance, prefabStage.scene);

            GameObject container = GetOrCreatePreviewContainer(prefabStage);
            instance.transform.SetParent(container.transform, false);

            if (instanceRect != null && prefabRect != null)
            {
                instanceRect.anchorMin = prefabRect.anchorMin;
                instanceRect.anchorMax = prefabRect.anchorMax;
                instanceRect.anchoredPosition = prefabRect.anchoredPosition;
                instanceRect.sizeDelta = prefabRect.sizeDelta;
                instanceRect.pivot = prefabRect.pivot;
                instanceRect.localRotation = prefabRect.localRotation;
                instanceRect.localScale = prefabRect.localScale;
            }
            else
            {
                instance.transform.position = layerDef.transform.position;
                instance.transform.rotation = layerDef.transform.rotation;
                instance.transform.localScale = Vector3.one;
            }

            bool isVisible = layerDef.IsLayerVisible(index);
            instance.SetActive(isVisible);

            Canvas canvas = instance.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = layerDef.GetLayerZOffset(index);
                Debug.Log($"[LayerDefinitionPreviewManager] Created preview for layer '{layerName}' with sorting order {canvas.sortingOrder}");
            }

            previewInstances[layerName] = instance;
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        public void UpdatePreviewVisibility(string layerName, bool visible)
        {
            if (!previewInstances.TryGetValue(layerName, out GameObject instance))
                return;

            if (instance == null)
                return;

            instance.SetActive(visible);
        }

        public void DrawSceneIndicators(LayerDefinitionClass layerDef)
        {
            if (!layerDef.ShowLayerPreviews)
                return;

            Handles.BeginGUI();
            float yOffset = Screen.height - 30;

            for (int i = 0; i < layerDef.LayerCount; i++)
            {
                string layerName = layerDef.GetLayerName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                bool hasPreview = previewInstances.ContainsKey(layerName) && previewInstances[layerName] != null;
                bool hasPrefab = FindLayerPrefab(layerName) != null;
                string statusIcon = hasPreview ? "●" : (hasPrefab ? "○" : "×");
                Color textColor = hasPreview ? Color.green : (hasPrefab ? Color.yellow : Color.red);

                GUIStyle miniStyle = new GUIStyle
                {
                    normal = { textColor = textColor },
                    fontSize = 10,
                    fontStyle = FontStyle.Normal,
                    alignment = TextAnchor.MiddleLeft
                };

                GUI.Label(new Rect(10, yOffset - (i * 15), 200, 15), $"{statusIcon} {layerName}", miniStyle);
            }

            Handles.EndGUI();
        }

        public void Cleanup()
        {
            DestroyAllPreviews();
            DestroyPreviewContainer();
            previewsInitialized = false;

            if (currentPreviewEditor != null)
            {
                Object.DestroyImmediate(currentPreviewEditor);
                currentPreviewEditor = null;
            }
        }

        public GameObject FindLayerPrefab(string layerName)
        {
            if (previewPrefabCache.TryGetValue(layerName, out GameObject cachedPrefab))
                return cachedPrefab;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
                return null;

            AddressableAssetEntry entry = FindAddressableEntry(settings, layerName);
            if (entry == null)
                return null;

            string assetPath = AssetDatabase.GUIDToAssetPath(entry.guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null)
                previewPrefabCache[layerName] = prefab;

            return prefab;
        }

        private void InitializePreviews(LayerDefinitionClass layerDef)
        {
            for (int i = 0; i < layerDef.LayerCount; i++)
            {
                string layerName = layerDef.GetLayerName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                if (previewInstances.ContainsKey(layerName) && previewInstances[layerName] != null)
                    continue;

                CreatePreviewForLayer(layerName, i, layerDef);
            }
        }

        private void UpdateExistingPreviews(LayerDefinitionClass layerDef)
        {
            HashSet<string> currentLayerNames = new();
            for (int i = 0; i < layerDef.LayerCount; i++)
            {
                string layerName = layerDef.GetLayerName(i);
                if (!string.IsNullOrEmpty(layerName))
                    currentLayerNames.Add(layerName);
            }

            List<string> layersToRemove = new();
            foreach (string previewLayerName in previewInstances.Keys)
            {
                if (!currentLayerNames.Contains(previewLayerName))
                    layersToRemove.Add(previewLayerName);
            }

            foreach (string layerName in layersToRemove)
                DestroyPreviewForLayer(layerName);

            List<string> layersToCreate = new();
            for (int i = 0; i < layerDef.LayerCount; i++)
            {
                string layerName = layerDef.GetLayerName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                if (!previewInstances.ContainsKey(layerName) || previewInstances[layerName] == null)
                    layersToCreate.Add(layerName);
            }

            foreach (string layerName in layersToCreate)
            {
                int index = 0;
                for (int i = 0; i < layerDef.LayerCount; i++)
                {
                    if (layerDef.GetLayerName(i) == layerName)
                    {
                        index = i;
                        break;
                    }
                }
                CreatePreviewForLayer(layerName, index, layerDef);
            }

            for (int i = 0; i < layerDef.LayerCount; i++)
            {
                string layerName = layerDef.GetLayerName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                if (!previewInstances.TryGetValue(layerName, out GameObject instance) || instance == null)
                    continue;

                Canvas canvas = instance.GetComponent<Canvas>();
                if (canvas == null)
                    continue;

                int newZOrder = layerDef.GetLayerZOffset(i);
                if (canvas.sortingOrder != newZOrder)
                {
                    canvas.sortingOrder = newZOrder;
                    Debug.Log($"[LayerDefinitionPreviewManager] Updated sorting order for layer '{layerName}' to {newZOrder}");
                }
            }
        }

        private GameObject GetOrCreatePreviewContainer(PrefabStage prefabStage)
        {
            if (previewContainer != null)
                return previewContainer;

            previewContainer = new GameObject("[LAYER PREVIEWS - DO NOT SAVE]");
            previewContainer.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.NotEditable;

            Canvas canvas = previewContainer.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = PREVIEW_BASE_SORTING_ORDER;
            Debug.Log($"[LayerDefinitionPreviewManager] Created preview container with sorting order {canvas.sortingOrder}");

            RectTransform rectTransform = previewContainer.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }

            if (prefabStage != null)
                SceneManager.MoveGameObjectToScene(previewContainer, prefabStage.scene);

            return previewContainer;
        }

        private void DestroyPreviewContainer()
        {
            if (previewContainer != null)
            {
                Object.DestroyImmediate(previewContainer);
                previewContainer = null;
            }
        }

        private void SetHideFlagsRecursively(GameObject obj)
        {
            if (obj == null)
                return;

            obj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy | HideFlags.NotEditable;

            foreach (Transform child in obj.transform)
            {
                if (child != null && child.gameObject != null)
                    SetHideFlagsRecursively(child.gameObject);
            }
        }

        private void DestroyPreviewForLayer(string layerName)
        {
            if (!previewInstances.TryGetValue(layerName, out GameObject instance))
                return;

            if (instance != null)
                Object.DestroyImmediate(instance);

            previewInstances.Remove(layerName);
        }

        private void DestroyAllPreviews()
        {
            foreach (GameObject instance in previewInstances.Values)
            {
                if (instance != null)
                    Object.DestroyImmediate(instance);
            }

            previewInstances.Clear();
        }

        private AddressableAssetEntry FindAddressableEntry(AddressableAssetSettings settings, string layerName)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(layerName);
            if (entry != null)
                return entry;

            foreach (AddressableAssetGroup group in settings.groups)
            {
                if (group == null)
                    continue;

                foreach (AddressableAssetEntry e in group.entries)
                {
                    if (e.address == layerName)
                        return e;
                }
            }

            return null;
        }
    }
}


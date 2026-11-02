using System.Collections.Generic;
using UnityEngine;

namespace ScreenSystem
{
    public class LayerDefinition : MonoBehaviour
    {
        [Tooltip("List of layers for this screen. Drag to reorder. Top of list = behind screen, bottom = on top.")]
        [SerializeField] private List<LayerEntry> layers = new();

        [SerializeField] private List<bool> layerVisibility = new();

        [HideInInspector]
        [SerializeField] public int screenSeparatorIndex;

        [Header("Editor Visualization")]
        [Tooltip("Show full layer prefab previews in Scene View")]
        [SerializeField] private bool showLayerPreviews = true;

        [Tooltip("Color for layer gizmos")]
        [SerializeField] private Color gizmoColor = new Color(1f, 0.7f, 0f, 0.8f);

        [Tooltip("Width of each layer preview in Scene View")]
        [SerializeField] private float layerWidth = 200f;

        [Tooltip("Height of each layer preview in Scene View")]
        [SerializeField] private float layerHeight = 100f;

        [Header("Framework Override")]
        [Tooltip("Enable to override the framework from ScreenDefinition")]
        [SerializeField] private bool useFrameworkOverride = false;

        [Tooltip("Framework for this layer (if override is enabled)")]
        [SerializeField] private UIFramework frameworkOverride = UIFramework.uGUI;

        public IReadOnlyList<LayerEntry> Layers
            => layers;

        public bool ShowLayerPreviews
            => showLayerPreviews;

        public Color GizmoColor
            => gizmoColor;

        public float LayerWidth
            => layerWidth;

        public float LayerHeight
            => layerHeight;

        public int LayerCount
            => layers.Count;

        public UIFramework GetFramework(ScreenDefinition screenDefinition)
        {
            if (useFrameworkOverride)
                return frameworkOverride;
            return screenDefinition.Framework;
        }

        public LayerEntry GetLayer(int index)
        {
            if (index < 0 || index >= layers.Count)
                return null;

            return layers[index];
        }

        public string GetLayerName(int index)
        {
            LayerEntry entry = GetLayer(index);
            return entry?.layerName;
        }

        public int GetLayerZOffset(int index)
        {
            LayerEntry entry = GetLayer(index);
            if (entry == null)
                return 0;

            if (entry.isScreenSeparator)
                return 0;

            int screenIndex = GetScreenSeparatorIndex();

            if (index < screenIndex)
            {
                int distanceFromScreen = screenIndex - index;
                return -distanceFromScreen * 2;
            }
            else
            {
                int distanceFromScreen = index - screenIndex;
                return distanceFromScreen * 2;
            }
        }

        private int GetScreenSeparatorIndex()
        {
            for (int i = 0; i < layers.Count; i++)
            {
                if (layers[i].isScreenSeparator)
                    return i;
            }

            return screenSeparatorIndex;
        }

        public void SetScreenSeparatorIndex(int index)
        {
            screenSeparatorIndex = Mathf.Clamp(index, 0, layers.Count);
        }

        public void AddLayer(string layerName)
        {
            if (string.IsNullOrEmpty(layerName))
                return;

            layers.Add(new LayerEntry(layerName, 2, false));

            if (layers.Count == 1)
                screenSeparatorIndex = 0;
        }

        public bool EnsureScreenSeparatorExists()
        {
            bool hasScreenSeparator = false;
            for (int i = 0; i < layers.Count; i++)
            {
                if (!layers[i].isScreenSeparator)
                    continue;

                hasScreenSeparator = true;
                break;
            }

            if (!hasScreenSeparator)
            {
                int insertIndex = Mathf.Clamp(screenSeparatorIndex, 0, layers.Count);
                layers.Insert(insertIndex, new LayerEntry("SCREEN", 0, true));
                return true;
            }

            return false;
        }

        public void RemoveLayer(int index)
        {
            if (index < 0 || index >= layers.Count)
                return;

            layers.RemoveAt(index);
        }

        public void MoveLayer(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= layers.Count)
                return;

            if (toIndex < 0 || toIndex >= layers.Count)
                return;

            LayerEntry layer = layers[fromIndex];
            layers.RemoveAt(fromIndex);
            layers.Insert(toIndex, layer);
        }

        public void RecalculateOffsets()
        {
        }

        public bool IsLayerVisible(int index)
        {
            while (layerVisibility.Count < layers.Count)
                layerVisibility.Add(true);

            if (index < 0 || index >= layerVisibility.Count)
                return true;

            return layerVisibility[index];
        }

        public void SetLayerVisibility(int index, bool visible)
        {
            while (layerVisibility.Count < layers.Count)
                layerVisibility.Add(true);

            if (index < 0 || index >= layerVisibility.Count)
                return;

            layerVisibility[index] = visible;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showLayerPreviews || layers.Count == 0)
                return;
        }
#endif
    }
}


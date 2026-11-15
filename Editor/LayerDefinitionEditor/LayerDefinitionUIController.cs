using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor.LayerDefinitionEditor
{
    /// <summary>
    /// Handles user input and interactions for layer definition editor.
    /// Single responsibility: Handle drag&drop and layer addition logic.
    /// </summary>
    public class LayerDefinitionUIController
    {
        private readonly LayerDefinitionAssetManager assetManager;

        public LayerDefinitionUIController()
        {
            assetManager = new LayerDefinitionAssetManager();
        }

        /// <summary>
        /// Handle drag and drop of prefabs onto the editor.
        /// </summary>
        public void HandleDragAndDrop(Rect dropArea, SerializedProperty layersProperty, SerializedObject serializedObject)
        {
            Event evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!CanAcceptDrag())
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        ProcessDragAndDrop(layersProperty, serializedObject);
                        evt.Use();
                    }
                    break;
            }
        }

        private bool CanAcceptDrag()
        {
            // Check ObjectReferences
            if (DragAndDrop.objectReferences.Length > 0)
            {
                foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is GameObject)
                        return true;
                }
            }

            // Check paths
            if (DragAndDrop.paths.Length > 0)
            {
                foreach (string path in DragAndDrop.paths)
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                        return true;
                }
            }

            return false;
        }

        private void ProcessDragAndDrop(SerializedProperty layersProperty, SerializedObject serializedObject)
        {
            // Process ObjectReferences
            foreach (UnityEngine.Object draggedObject in DragAndDrop.objectReferences)
            {
                if (draggedObject is GameObject prefab)
                    AddLayerFromPrefab(prefab, layersProperty, serializedObject);
            }

            // Process paths
            foreach (string path in DragAndDrop.paths)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                    AddLayerFromPrefab(prefab, layersProperty, serializedObject);
            }
        }

        private void AddLayerFromPrefab(GameObject prefab, SerializedProperty layersProperty, SerializedObject serializedObject)
        {
            if (!assetManager.ValidatePrefabCanBeAdded(prefab))
                return;

            // A prefab that isn't Addressable or in Resources is marked Addressable, so the layer loads at runtime.
            string layerName = ScreenSystemAssets.EnsureLoadable(prefab);
            if (string.IsNullOrEmpty(layerName))
                return;

            // Check for duplicates
            HashSet<string> existingLayers = new();
            for (int i = 0; i < layersProperty.arraySize; i++)
            {
                SerializedProperty element = layersProperty.GetArrayElementAtIndex(i);
                SerializedProperty layerNameProp = element.FindPropertyRelative("layerName");
                existingLayers.Add(layerNameProp.stringValue);
            }

            if (existingLayers.Contains(layerName))
                return;

            // Add new layer entry
            int index = layersProperty.arraySize;
            layersProperty.arraySize++;

            SerializedProperty newElement = layersProperty.GetArrayElementAtIndex(index);
            SerializedProperty nameProperty = newElement.FindPropertyRelative("layerName");
            SerializedProperty amountProperty = newElement.FindPropertyRelative("amount");
            SerializedProperty isSeparatorProperty = newElement.FindPropertyRelative("isScreenSeparator");

            nameProperty.stringValue = layerName;
            amountProperty.intValue = 2;
            isSeparatorProperty.boolValue = false;

            serializedObject.ApplyModifiedProperties();
        }
    }
}


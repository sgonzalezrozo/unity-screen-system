using ScreenSystem;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using LayerDefinitionClass = ScreenSystem.LayerDefinition;

namespace ScreenSystem.Editor.LayerDefinitionEditor
{
    /// <summary>
    /// Handles all UI rendering and reorderable list setup for layer definition editor.
    /// Single responsibility: Only draw UI elements and manage list rendering.
    /// </summary>
    public class LayerDefinitionView
    {
        private readonly ReorderableList reorderableList;
        private readonly LayerDefinitionUIController controller;
        private readonly SerializedObject serializedObject;
        private readonly LayerDefinitionClass targetLayerDef;
        private readonly LayerDefinitionPreviewManager previewManager;

        public LayerDefinitionView(SerializedObject serializedObject, LayerDefinitionPreviewManager previewManager)
        {
            this.serializedObject = serializedObject;
            this.targetLayerDef = (LayerDefinitionClass)serializedObject.targetObject;
            this.previewManager = previewManager;
            this.controller = new LayerDefinitionUIController();

            SerializedProperty layersProperty = serializedObject.FindProperty("layers");
            reorderableList = new ReorderableList(serializedObject, layersProperty, true, true, true, true);
            SetupReorderableList();
        }

        public void DrawInspector()
        {
            serializedObject.Update();
            EditorGUILayout.Space(5);
            reorderableList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void SetupReorderableList()
        {
            SerializedProperty layersProperty = reorderableList.serializedProperty;

            reorderableList.drawHeaderCallback = DrawHeader;
            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => DrawElement(rect, index, layersProperty);
            reorderableList.elementHeightCallback = (int index) => EditorGUIUtility.singleLineHeight + 4;
            reorderableList.drawFooterCallback = (Rect rect) => ReorderableList.defaultBehaviours.DrawFooter(rect, reorderableList);
            reorderableList.onAddCallback = HandleAddLayer;
            reorderableList.onRemoveCallback = HandleRemoveLayer;
            reorderableList.onReorderCallbackWithDetails = HandleReorderLayers;
        }

        private void DrawHeader(Rect rect)
        {
            float labelWidth = rect.width - 100;
            float zOrderWidth = 100;

            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect zOrderRect = new Rect(rect.x + labelWidth, rect.y, zOrderWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(labelRect, "Layers (Drag prefabs or reorder)");
            EditorGUI.LabelField(zOrderRect, "Z-Order", EditorStyles.boldLabel);

            controller.HandleDragAndDrop(rect, reorderableList.serializedProperty, serializedObject);
        }

        private void DrawElement(Rect rect, int index, SerializedProperty layersProperty)
        {
            SerializedProperty element = layersProperty.GetArrayElementAtIndex(index);
            SerializedProperty layerNameProp = element.FindPropertyRelative("layerName");
            SerializedProperty isScreenSeparatorProp = element.FindPropertyRelative("isScreenSeparator");

            bool isScreenSeparator = isScreenSeparatorProp.boolValue;
            rect.y += 2;

            if (isScreenSeparator)
            {
                DrawScreenSeparatorElement(rect, layerNameProp);
                return;
            }

            DrawNormalLayerElement(rect, index, element, layerNameProp);
        }

        private void DrawScreenSeparatorElement(Rect rect, SerializedProperty layerNameProp)
        {
            Rect backgroundRect = new Rect(rect.x - 5, rect.y - 2, rect.width + 10, EditorGUIUtility.singleLineHeight + 4);
            EditorGUI.DrawRect(backgroundRect, new Color(1f, 0.85f, 0f, 0.2f));

            float labelWidth = 80;
            float nameWidth = rect.width - labelWidth - 80 - 15;
            float zOrderWidth = 60;

            Rect labelRect = new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight);
            Rect nameRect = new Rect(rect.x + labelWidth + 5, rect.y, nameWidth, EditorGUIUtility.singleLineHeight);
            Rect zOrderRect = new Rect(rect.x + labelWidth + nameWidth + 10, rect.y, zOrderWidth, EditorGUIUtility.singleLineHeight);

            GUI.color = new Color(1f, 0.85f, 0f);
            EditorGUI.LabelField(labelRect, "z = 0", EditorStyles.boldLabel);
            GUI.color = Color.white;

            GUI.enabled = false;
            EditorGUI.LabelField(nameRect, "SCREEN", EditorStyles.boldLabel);
            GUI.enabled = true;

            EditorGUI.LabelField(zOrderRect, "0", EditorStyles.miniLabel);
        }

        private void DrawNormalLayerElement(Rect rect, int index, SerializedProperty element, SerializedProperty layerNameProp)
        {
            int screenIndex = FindScreenSeparatorIndex();
            bool isBehind = index < screenIndex;

            float labelWidth2 = 80;
            float nameWidth2 = rect.width - labelWidth2 - 80 - 15 - 25 - 25 - 20;
            float zOrderWidth2 = 60;
            float statusWidth2 = 60;
            float playButtonWidth2 = 20;
            float toggleWidth2 = 20;
            float spacerWidth2 = 5;

            Rect labelRect = new Rect(rect.x, rect.y, labelWidth2, EditorGUIUtility.singleLineHeight);
            Rect nameRect = new Rect(rect.x + labelWidth2 + 5, rect.y, nameWidth2, EditorGUIUtility.singleLineHeight);
            Rect zOrderRect = new Rect(rect.x + labelWidth2 + nameWidth2 + 10, rect.y, zOrderWidth2, EditorGUIUtility.singleLineHeight);
            Rect statusRect = new Rect(rect.xMax - statusWidth2 - playButtonWidth2 - toggleWidth2 - spacerWidth2 - spacerWidth2 - 5, rect.y, statusWidth2, EditorGUIUtility.singleLineHeight);
            Rect playButtonRect = new Rect(rect.xMax - playButtonWidth2 - toggleWidth2 - spacerWidth2 - 5, rect.y + 2, playButtonWidth2, EditorGUIUtility.singleLineHeight);
            Rect toggleRect = new Rect(rect.xMax - toggleWidth2, rect.y + 2, toggleWidth2, EditorGUIUtility.singleLineHeight);

            // Position indicator
            string positionLabel = isBehind ? "BEHIND" : "ON TOP";
            Color positionColor = isBehind ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.8f, 0.5f);

            GUI.color = positionColor;
            EditorGUI.LabelField(labelRect, positionLabel, EditorStyles.boldLabel);
            GUI.color = Color.white;

            // Layer name field
            EditorGUI.PropertyField(nameRect, layerNameProp, GUIContent.none);

            // Z-Order (read-only)
            int zOrder = targetLayerDef.GetLayerZOffset(index);
            EditorGUI.LabelField(zOrderRect, zOrder.ToString("+0;-0;0"), EditorStyles.miniLabel);

            // Status indicator
            string layerName = layerNameProp.stringValue;
            GameObject prefab = previewManager.FindLayerPrefab(layerName);
            if (!string.IsNullOrEmpty(layerName))
            {
                if (prefab != null)
                {
                    GUI.color = Color.green;
                    EditorGUI.LabelField(statusRect, "Found");
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = new Color(1f, 0.5f, 0f);
                    EditorGUI.LabelField(statusRect, "Missing");
                    GUI.color = Color.white;
                }
            }

            // // Play button for LayerTransition
            // if (prefab != null)
            // {
            //     LayerTransition layerTransition = prefab.GetComponent<LayerTransition>();
            //     if (layerTransition != null)
            //     {
            //         GUI.color = new Color(0.7f, 1f, 0.7f);
            //         if (GUI.Button(playButtonRect, "▶", EditorStyles.miniButton))
            //             previewManager.TriggerLayerTransitionPreview(prefab, layerTransition);
            //         GUI.color = Color.white;
            //     }
            // }

            // Visibility toggle
            bool isVisible = targetLayerDef.IsLayerVisible(index);
            EditorGUI.BeginChangeCheck();
            bool newVisibility = EditorGUI.Toggle(toggleRect, isVisible);
            if (EditorGUI.EndChangeCheck())
            {
                targetLayerDef.SetLayerVisibility(index, newVisibility);
                previewManager.UpdatePreviewVisibility(layerName, newVisibility);
                EditorUtility.SetDirty(targetLayerDef);
                SceneView.RepaintAll();
            }
        }

        private void HandleAddLayer(ReorderableList list)
        {
            int index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            list.index = index;

            SerializedProperty element = list.serializedProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("layerName").stringValue = "NewLayer";
            element.FindPropertyRelative("amount").intValue = 2;
            element.FindPropertyRelative("isScreenSeparator").boolValue = false;
        }

        private void HandleRemoveLayer(ReorderableList list)
        {
            if (list.index >= 0 && list.index < reorderableList.serializedProperty.arraySize)
            {
                SerializedProperty element = reorderableList.serializedProperty.GetArrayElementAtIndex(list.index);
                SerializedProperty isScreenSeparatorProp = element.FindPropertyRelative("isScreenSeparator");

                if (isScreenSeparatorProp.boolValue)
                {
                    EditorUtility.DisplayDialog("Cannot Remove", "The SCREEN separator cannot be removed.", "OK");
                    return;
                }

                SerializedProperty layerNameProp = element.FindPropertyRelative("layerName");
                previewManager.UpdatePreviewVisibility(layerNameProp.stringValue, false);
            }

            ReorderableList.defaultBehaviours.DoRemoveButton(list);
        }

        private void HandleReorderLayers(ReorderableList list, int oldIndex, int newIndex)
        {
            EditorUtility.SetDirty(targetLayerDef);
            previewManager.Update(targetLayerDef);
            SceneView.RepaintAll();
        }

        private int FindScreenSeparatorIndex()
        {
            for (int i = 0; i < targetLayerDef.LayerCount; i++)
            {
                LayerEntry entry = targetLayerDef.GetLayer(i);
                if (entry != null && entry.isScreenSeparator)
                    return i;
            }
            return -1;
        }
    }
}


using System.Collections.Generic;
using System.Linq;
using ScreenSystem;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor.ScreenManagerEditor
{
    /// <summary>
    /// Handles rendering of the screen hierarchy and stack display.
    /// Single responsibility: Display active screens, modals, and screen details only.
    /// </summary>
    public class ScreenHierarchyPanel
    {
        private bool screenStackFoldout = true;
        private Dictionary<string, bool> screenDetailFoldouts = new();
        private GUIStyle headerStyle;

        public void Initialize(GUIStyle header)
        {
            headerStyle = header;
        }

        public void DrawScreenStackSection(ScreenManager screenManager, ScreenRegistry registry)
        {
            screenStackFoldout = EditorGUILayout.Foldout(screenStackFoldout, "SCREEN STACK", true, headerStyle);

            if (!screenStackFoldout)
                return;

            EditorGUILayout.BeginVertical("box");

            List<string> history = screenManager.GetNavigationHistoryForEditor();
            List<string> modalHistory = screenManager.GetModalHistoryForEditor();

            string activeScreenId = DetermineActiveScreenId(modalHistory, history);
            bool hasModals = modalHistory.Count > 0;

            HashSet<string> stackedScreenIds = new();
            foreach (string id in history)
                stackedScreenIds.Add(id);
            foreach (string id in modalHistory)
                stackedScreenIds.Add(id);

            bool hasContent = false;

            if (modalHistory.Count > 0)
            {
                hasContent = true;
                DrawSubHeader("Modals");
                for (int i = 0; i < modalHistory.Count; i++)
                    DrawScreenEntry(modalHistory[i], activeScreenId, hasModals, true, screenManager, registry);

                EditorGUILayout.Space(5);
            }

            if (history.Count > 0)
            {
                hasContent = true;
                DrawSubHeader("History");
                for (int i = 0; i < history.Count; i++)
                    DrawScreenEntry(history[i], activeScreenId, hasModals, false, screenManager, registry);

                EditorGUILayout.Space(5);
            }

            List<string> otherActive = CollectOtherActiveScreens(screenManager, stackedScreenIds);
            if (otherActive.Count > 0)
            {
                hasContent = true;
                DrawSubHeader("Other Active");
                foreach (string screenId in otherActive)
                    DrawScreenEntry(screenId, activeScreenId, hasModals, false, screenManager, registry);
            }

            if (!hasContent)
                EditorGUILayout.HelpBox("No screens in stack", MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        public void Reset()
        {
            screenStackFoldout = true;
            screenDetailFoldouts.Clear();
        }

        private string DetermineActiveScreenId(List<string> modalHistory, List<string> history)
        {
            if (modalHistory.Count > 0)
                return modalHistory[0];

            if (history.Count > 0)
                return history[0];

            return null;
        }

        private List<string> CollectOtherActiveScreens(ScreenManager screenManager, HashSet<string> stackedScreenIds)
        {
            List<string> others = new();

            List<string> activeIds = screenManager.GetActiveScreenIds();
            if (activeIds != null)
            {

                foreach (string screenId in activeIds)
                {
                    if (!stackedScreenIds.Contains(screenId))
                        others.Add(screenId);
                }
            }

            return others;
        }

        private void DrawScreenEntry(string screenId, string activeScreenId, bool hasModals, bool isModal, ScreenManager screenManager, ScreenRegistry registry)
        {
            ScreenDefinition screenDef = registry?.GetScreen(screenId);
            string screenName = screenDef?.ScreenName ?? screenId;
            string levelLabel = "";

            List<LayerComponent> layers = screenManager.GetScreenLayers(screenId);
            bool isActive = screenId == activeScreenId;
            bool hasVisibleCanvas = layers.Count > 0 && layers.Any(l => l?.Canvas != null && l.Canvas.enabled);
            bool isVisible = !isActive && hasModals && hasVisibleCanvas;

            Color iconColor;
            string visibilityIcon;

            if (isActive)
            {
                iconColor = new Color(1f, 0.8f, 0f);
                visibilityIcon = "\u25CF";
            }
            else if (isVisible)
            {
                iconColor = new Color(0.4f, 0.9f, 0.4f);
                visibilityIcon = "\u25CF";
            }
            else
            {
                iconColor = new Color(0.5f, 0.5f, 0.5f);
                visibilityIcon = "\u25CB";
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            GUIStyle iconStyle = new(EditorStyles.label) { normal = { textColor = iconColor } };
            EditorGUILayout.LabelField(visibilityIcon, iconStyle, GUILayout.Width(14));

            GUIStyle nameStyle = new(EditorStyles.boldLabel);
            if (isActive)
                nameStyle.normal.textColor = new Color(1f, 0.8f, 0f);

            EditorGUILayout.LabelField(screenName, nameStyle);
            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField(levelLabel, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(70));

            if (isActive)
            {
                GUIStyle badgeStyle = new(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0f) },
                    fontStyle = FontStyle.Bold
                };
                string badgeLabel = isModal ? "\u25C0 MODAL" : "\u25C0 TOP";
                EditorGUILayout.LabelField(badgeLabel, badgeStyle, GUILayout.Width(55));
            }

            if (layers.Count > 0 && layers[0] != null)
            {
                if (GUILayout.Button("\u25BA", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    Selection.activeGameObject = layers[0].gameObject;
                    EditorGUIUtility.PingObject(layers[0].gameObject);
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!screenDetailFoldouts.ContainsKey(screenId))
                screenDetailFoldouts[screenId] = false;

            screenDetailFoldouts[screenId] = EditorGUILayout.Foldout(screenDetailFoldouts[screenId], "Details", true);

            if (screenDetailFoldouts[screenId])
                DrawScreenDetails(screenId, layers, registry);

            EditorGUILayout.EndVertical();
        }

        private void DrawScreenDetails(string screenId, List<LayerComponent> layers, ScreenRegistry registry)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField($"Layers ({layers.Count}):", EditorStyles.miniLabel);

            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                EditorGUILayout.BeginHorizontal();

                bool layerVisible = layer.Canvas != null && layer.Canvas.enabled;
                Color layerColor = layerVisible ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.5f, 0.5f, 0.5f);
                string layerIcon = layerVisible ? "\u25CF" : "\u25CB";
                GUIStyle layerIconStyle = new(EditorStyles.miniLabel) { normal = { textColor = layerColor } };

                EditorGUILayout.LabelField(layerIcon, layerIconStyle, GUILayout.Width(14));
                EditorGUILayout.LabelField(layer.LayerName, EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Z:{layer.CurrentZOrder}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(50));

                if (GUILayout.Button("\u25BA", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    Selection.activeGameObject = layer.gameObject;
                    EditorGUIUtility.PingObject(layer.gameObject);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        private void DrawSubHeader(string label)
        {
            GUIStyle subHeaderStyle = new(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField(label, subHeaderStyle);
        }
    }
}


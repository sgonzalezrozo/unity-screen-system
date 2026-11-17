using System.Collections.Generic;
using ScreenSystem.Editor.ScreenManagerEditor;
using ScreenSystem;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    [CustomEditor(typeof(ScreenManager))]
    public class ScreenManagerInspector : UnityEditor.Editor
    {
        private const double REPAINT_INTERVAL = 0.5;
        private const int MAX_SCREEN_NAME_LENGTH = 28;

        private ScreenManager screenManager;
        private double lastRepaintTime;

        private Dictionary<string, bool> screenFoldouts = new();
        private bool activeScreensFoldout = true;

        private GUIStyle headerStyle;
        private GUIStyle topBadgeStyle;
        private GUIStyle visibleDotStyle;
        private GUIStyle hiddenDotStyle;
        private bool stylesInitialized;

        // Looked up once per selection: it searches the project's ScreenDefinitions.
        private List<string> missingSpecialScreens;

        private void OnEnable()
        {
            screenManager = (ScreenManager)target;
            missingSpecialScreens = Application.isPlaying ? new List<string>() : SpecialScreensCreator.Missing();
            lastRepaintTime = EditorApplication.timeSinceStartup;
        }

        public override void OnInspectorGUI()
        {
            if (target == null || screenManager == null)
            {
                EditorGUILayout.HelpBox("ScreenManager not available.", MessageType.Warning);
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see stack state.", MessageType.Info);
                DrawMissingSpecialScreens();
                EditorGUILayout.Space(5);
                DrawDefaultInspector();
                return;
            }

            InitializeStyles();

            EditorGUILayout.Space(5);
            DrawStackState();
            EditorGUILayout.Space(10);
            DrawOpenWindowButton();
            EditorGUILayout.Space(10);
            DrawDefaultInspector();

            ScheduleRepaint();
        }

        private void DrawMissingSpecialScreens()
        {
            if (missingSpecialScreens == null || missingSpecialScreens.Count == 0)
                return;

            EditorGUILayout.HelpBox(
                $"{string.Join(" and ", missingSpecialScreens)} not found. Without a splash nothing shows while the game boots; without a quit confirmation, back at the root quits straight away.",
                MessageType.Warning);
            if (GUILayout.Button("Create Special Screens"))
            {
                SpecialScreensCreator.Create();
                missingSpecialScreens = SpecialScreensCreator.Missing();
                GUIUtility.ExitGUI();
            }
        }

        private void InitializeStyles()
        {
            if (stylesInitialized)
                return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.8f, 0.9f, 1f) }
            };

            topBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.8f, 0f) },
                fontStyle = FontStyle.Bold
            };

            visibleDotStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.4f, 0.9f, 0.4f) }
            };

            hiddenDotStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f) }
            };

            stylesInitialized = true;
        }

        private void DrawStackState()
        {
            EditorGUILayout.LabelField("SCREEN STACK STATE", headerStyle);
            EditorGUILayout.BeginVertical("box");

            string currentScreenId = GetCurrentScreenIdSafe();
            ScreenRegistry registry = screenManager.GetRegistryForEditor();

            DrawActiveScreensSection(currentScreenId, registry);

            EditorGUILayout.EndVertical();
        }

        private void DrawActiveScreensSection(string currentScreenId, ScreenRegistry registry)
        {
            string topScreen = screenManager.GetTopScreen();
            List<string> activeScreenIds = screenManager.GetActiveScreenIds();
            int screenCount = activeScreenIds.Count;

            string sectionLabel = $"Active Screens";
            if (screenCount > 0)
                sectionLabel += $"  [{screenCount}]";

            activeScreensFoldout = EditorGUILayout.Foldout(activeScreensFoldout, sectionLabel, true, EditorStyles.foldoutHeader);

            if (!activeScreensFoldout)
                return;

            EditorGUI.indentLevel++;

            if (screenCount == 0)
            {
                EditorGUILayout.LabelField("(empty)", EditorStyles.centeredGreyMiniLabel);
                EditorGUI.indentLevel--;
                return;
            }

            for (int i = activeScreenIds.Count - 1; i >= 0; i--)
            {
                string screenId = activeScreenIds[i];
                bool isTop = screenId == topScreen;
                DrawScreenEntry(screenId, isTop, registry);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawScreenEntry(string screenId, bool isTop, ScreenRegistry registry)
        {
            if (!screenFoldouts.ContainsKey(screenId))
                screenFoldouts[screenId] = false;

            List<LayerComponent> layers = screenManager.GetScreenLayers(screenId);
            ScreenDefinition screenDef = registry?.GetScreen(screenId);
            string displayName = screenDef?.ScreenName ?? screenId;

            if (displayName.Length > MAX_SCREEN_NAME_LENGTH)
                displayName = displayName.Substring(0, MAX_SCREEN_NAME_LENGTH - 2) + "..";

            int screenBaseZOrder = GetScreenBaseZOrder(layers);

            EditorGUILayout.BeginHorizontal();

            string foldoutLabel = $"{displayName}  ({layers.Count} layers)";
            screenFoldouts[screenId] = EditorGUILayout.Foldout(screenFoldouts[screenId], foldoutLabel, true);

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Z: {screenBaseZOrder}", EditorStyles.miniLabel, GUILayout.Width(55));

            if (isTop)
                EditorGUILayout.LabelField("\u25C0 TOP", topBadgeStyle, GUILayout.Width(45));

            if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                SelectFirstLayer(layers);

            EditorGUILayout.EndHorizontal();

            if (!screenFoldouts[screenId])
                return;

            EditorGUI.indentLevel++;

            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                DrawLayerEntry(layer);
            }

            EditorGUI.indentLevel--;
        }

        private void DrawLayerEntry(LayerComponent layer)
        {
            bool isVisible = layer.Canvas != null && layer.Canvas.enabled;
            GUIStyle dotStyle = isVisible ? visibleDotStyle : hiddenDotStyle;
            string icon = isVisible ? "\u25CF" : "\u25CB";

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"  {icon}", dotStyle, GUILayout.Width(20));
            EditorGUILayout.LabelField(layer.LayerName, EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            EditorGUILayout.LabelField($"Z: {layer.CurrentZOrder}", EditorStyles.miniLabel, GUILayout.Width(55));

            if (layer.IsShared)
                EditorGUILayout.LabelField("shared", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(40));

            if (GUILayout.Button("\u25BA", EditorStyles.miniButton, GUILayout.Width(22)))
            {
                Selection.activeGameObject = layer.gameObject;
                EditorGUIUtility.PingObject(layer.gameObject);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOpenWindowButton()
        {
            if (GUILayout.Button("Open Screen Manager Window", GUILayout.Height(25)))
                ScreenManagerEditorWindow.ShowWindow();
        }

        private string GetCurrentScreenIdSafe()
        {
            try
            {
                return screenManager?.GetCurrentScreen();
            }
            catch (System.NullReferenceException)
            {
                return null;
            }
        }

        private int GetScreenBaseZOrder(List<LayerComponent> layers)
        {
            if (layers.Count == 0)
                return 0;

            int minZ = int.MaxValue;
            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                if (layer.CurrentZOrder < minZ)
                    minZ = layer.CurrentZOrder;
            }

            return minZ == int.MaxValue ? 0 : minZ;
        }

        private void SelectFirstLayer(List<LayerComponent> layers)
        {
            if (layers.Count == 0)
                return;

            LayerComponent firstValid = null;
            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                firstValid = layer;
                break;
            }

            if (firstValid == null)
                return;

            Selection.activeGameObject = firstValid.gameObject;
            EditorGUIUtility.PingObject(firstValid.gameObject);
        }

        private void ScheduleRepaint()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastRepaintTime < REPAINT_INTERVAL)
                return;

            lastRepaintTime = currentTime;
            Repaint();
        }
    }
}

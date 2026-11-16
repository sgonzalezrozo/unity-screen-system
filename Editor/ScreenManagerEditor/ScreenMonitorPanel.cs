using System;
using System.Collections.Generic;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor.ScreenManagerEditor
{
    /// <summary>
    /// The live part of the Screen Manager window: what back does next, what's rendering now from front to back,
    /// and the back stack down to the root, with buttons to select objects and navigate.
    /// </summary>
    public class ScreenMonitorPanel
    {
        private static readonly Color Rendering = new(0.4f, 0.9f, 0.4f);
        private static readonly Color Hidden = new(0.5f, 0.5f, 0.5f);
        private static readonly Color Top = new(1f, 0.8f, 0f);
        private static readonly Color Backdrop = new(0.55f, 0.75f, 1f);

        private readonly Dictionary<string, bool> layerFoldouts = new();
        private readonly Dictionary<LayerComponent, int> layerUseCount = new();
        private readonly List<string> stackIds = new();
        private readonly List<string> historyNames = new();

        private GUIStyle headerStyle;
        private GUIStyle subHeaderStyle;
        private GUIStyle nameStyle;
        private GUIStyle topNameStyle;
        private GUIStyle dotRendering;
        private GUIStyle dotHidden;
        private GUIStyle dotBackdrop;
        private GUIStyle actionStyle;
        private GUIStyle statusStyle;

        public void Initialize(GUIStyle header)
        {
            headerStyle = header;
            subHeaderStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            nameStyle = new GUIStyle(EditorStyles.boldLabel);
            topNameStyle = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = Top } };
            dotRendering = new GUIStyle(EditorStyles.label) { normal = { textColor = Rendering } };
            dotHidden = new GUIStyle(EditorStyles.label) { normal = { textColor = Hidden } };
            dotBackdrop = new GUIStyle(EditorStyles.label) { normal = { textColor = Backdrop } };
            actionStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            statusStyle = new GUIStyle(EditorStyles.helpBox) { fontSize = 11, wordWrap = true };
        }

        public void Reset()
            => layerFoldouts.Clear();

        public void Draw(ScreenManager manager)
        {
            ScreenRegistry registry = manager.GetRegistryForEditor();
            List<string> history = manager.GetNavigationHistoryForEditor();
            List<string> modals = manager.GetModalHistoryForEditor();
            string modal = modals.Count > 0 ? modals[0] : null;
            string alert = manager.GetAlertForEditor();

            CountLayerUse(manager, history, modal, alert);

            historyNames.Clear();
            foreach (string id in history)
                historyNames.Add(DisplayName(registry, id));

            List<BackStep> steps = BackStackDescriber.Describe(historyNames, DisplayName(registry, modal), DisplayName(registry, alert),
                manager.QuitsOnBackAtRootForEditor, manager.GetQuitConfirmationForEditor());

            DrawStatus(manager, steps);
            EditorGUILayout.Space(6);
            DrawNowShowing(manager, registry, history, modal, alert);
            EditorGUILayout.Space(6);
            DrawBackStack(manager, registry, history, modal, alert, steps);
        }

        private void DrawStatus(ScreenManager manager, List<BackStep> steps)
        {
            string running = manager.GetRunningOperationForEditor();
            string state = !manager.IsInitialized ? "Initializing…" : running != null ? $"{running} is running" : "Idle";
            EditorGUILayout.LabelField($"{state}\nNext back → {BackStackDescriber.NextBack(steps)}", statusStyle);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = manager.IsInitialized && running == null;
            if (GUILayout.Button("Back", EditorStyles.miniButtonLeft))
                Run(() => manager.GoBack());
            GUI.enabled = manager.IsInitialized && running == null && manager.IsAlertActive;
            if (GUILayout.Button("Dismiss Alert", EditorStyles.miniButtonMid))
                Run(() => manager.DismissAlert());
            GUI.enabled = manager.IsInitialized && running == null;
            if (GUILayout.Button("Close All", EditorStyles.miniButtonRight))
                Run(() => manager.CloseAll());
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawNowShowing(ScreenManager manager, ScreenRegistry registry, List<string> history, string modal, string alert)
        {
            EditorGUILayout.LabelField("NOW SHOWING  (front to back)", headerStyle);
            EditorGUILayout.BeginVertical("box");

            bool any = false;
            if (alert != null)
                any |= DrawRenderingScreen(manager, registry, alert, "alert");

            if (manager.IsDimBackdropVisibleForEditor)
            {
                DrawBackdropRow("Dim between the modal and the alert");
                any = true;
            }

            if (modal != null)
                any |= DrawRenderingScreen(manager, registry, modal, "modal");

            if (manager.IsSnapshotBackdropVisibleForEditor)
            {
                DrawBackdropRow("Blurred snapshot: the screen under it isn't rendering");
                any = true;
            }

            foreach (string id in history)
                any |= DrawRenderingScreen(manager, registry, id, id == history[0] ? "current" : "in stack");

            foreach (string id in manager.GetActiveScreenIds() ?? new List<string>())
            {
                if (!stackIds.Contains(id))
                    any |= DrawRenderingScreen(manager, registry, id, "outside the stack");
            }

            if (!any)
                EditorGUILayout.LabelField("Nothing is rendering.", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        // Draws the screen and its rendering layers if any of them renders. Returns whether it drew anything.
        private bool DrawRenderingScreen(ScreenManager manager, ScreenRegistry registry, string id, string role)
        {
            List<LayerComponent> layers = SortedByZ(manager.GetScreenLayers(id));
            bool renders = false;
            foreach (LayerComponent layer in layers)
                renders |= IsRendering(layer);

            if (!renders)
                return false;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("●", dotRendering, GUILayout.Width(14));
            EditorGUILayout.LabelField(DisplayName(registry, id), role == "current" || role == "modal" || role == "alert" ? topNameStyle : nameStyle);
            EditorGUILayout.LabelField(role, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(110));
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            foreach (LayerComponent layer in layers)
            {
                if (IsRendering(layer))
                    DrawLayerRow(layer);
            }

            EditorGUI.indentLevel--;
            return true;
        }

        private void DrawBackdropRow(string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("■", dotBackdrop, GUILayout.Width(14));
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBackStack(ScreenManager manager, ScreenRegistry registry, List<string> history, string modal, string alert, List<BackStep> steps)
        {
            EditorGUILayout.LabelField("BACK STACK  (top to root)", headerStyle);
            EditorGUILayout.BeginVertical("box");

            if (steps.Count == 0)
                EditorGUILayout.LabelField(BackStackDescriber.NothingOpen, EditorStyles.miniLabel);

            bool canNavigate = manager.IsInitialized && manager.GetRunningOperationForEditor() == null;
            int historyIndex = 0;
            for (int i = 0; i < steps.Count; i++)
            {
                BackStep step = steps[i];
                string id = step.Kind switch
                {
                    BackStepKind.Alert => alert,
                    BackStepKind.Modal => modal,
                    _ => history[historyIndex++]
                };

                DrawStackEntry(manager, id, step, i == 0, canNavigate && step.Kind is BackStepKind.Screen or BackStepKind.Root && historyIndex > 1);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStackEntry(ScreenManager manager, string id, BackStep step, bool isTop, bool canPopTo)
        {
            List<LayerComponent> layers = SortedByZ(manager.GetScreenLayers(id));
            bool renders = false;
            foreach (LayerComponent layer in layers)
                renders |= IsRendering(layer);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(renders ? "●" : "○", renders ? dotRendering : dotHidden, GUILayout.Width(14));
            EditorGUILayout.LabelField(step.Screen, isTop ? topNameStyle : nameStyle);
            EditorGUILayout.LabelField(KindLabel(step.Kind), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(60));

            GUI.enabled = canPopTo;
            if (GUILayout.Button(new GUIContent("Pop To", "Return to this screen, closing the ones above it"), EditorStyles.miniButton, GUILayout.Width(52)))
                Run(() => manager.PopTo(id));
            GUI.enabled = true;

            GUI.enabled = layers.Count > 0 && layers[0] != null;
            if (GUILayout.Button(new GUIContent("►", "Select the screen's object"), EditorStyles.miniButton, GUILayout.Width(22)))
                Select(layers[0]);
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(step.Action, actionStyle);

            if (!layerFoldouts.TryGetValue(id, out bool open))
                open = isTop;

            layerFoldouts[id] = EditorGUILayout.Foldout(open, $"Layers ({layers.Count})", true);
            if (layerFoldouts[id])
            {
                EditorGUI.indentLevel++;
                foreach (LayerComponent layer in layers)
                    DrawLayerRow(layer);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawLayerRow(LayerComponent layer)
        {
            if (layer == null)
                return;

            bool rendering = IsRendering(layer);
            string state = rendering ? "" : layer.IsRenderSuspended ? "  (behind the blur)" : "  (hidden)";
            string shared = layerUseCount.TryGetValue(layer, out int uses) && uses > 1 ? $"  shared by {uses}" : "";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(rendering ? "●" : "○", rendering ? dotRendering : dotHidden, GUILayout.Width(28));
            EditorGUILayout.LabelField($"{layer.LayerName}{shared}{state}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Z {layer.CurrentZOrder}", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(44));
            if (GUILayout.Button(new GUIContent("►", "Select the layer"), EditorStyles.miniButton, GUILayout.Width(22)))
                Select(layer);
            EditorGUILayout.EndHorizontal();
        }

        private void CountLayerUse(ScreenManager manager, List<string> history, string modal, string alert)
        {
            stackIds.Clear();
            stackIds.AddRange(history);
            if (modal != null)
                stackIds.Add(modal);
            if (alert != null)
                stackIds.Add(alert);

            layerUseCount.Clear();
            List<string> ids = new(stackIds);
            foreach (string id in manager.GetActiveScreenIds() ?? new List<string>())
            {
                if (!ids.Contains(id))
                    ids.Add(id);
            }

            foreach (string id in ids)
            {
                foreach (LayerComponent layer in manager.GetScreenLayers(id))
                {
                    if (layer != null)
                        layerUseCount[layer] = layerUseCount.TryGetValue(layer, out int count) ? count + 1 : 1;
                }
            }
        }

        private static bool IsRendering(LayerComponent layer)
            => layer != null && layer.gameObject.activeInHierarchy && layer.IsVisible && !layer.IsRenderSuspended;

        private static List<LayerComponent> SortedByZ(List<LayerComponent> layers)
        {
            List<LayerComponent> sorted = new();
            if (layers != null)
            {
                foreach (LayerComponent layer in layers)
                {
                    if (layer != null)
                        sorted.Add(layer);
                }
            }

            sorted.Sort((a, b) => b.CurrentZOrder.CompareTo(a.CurrentZOrder));
            return sorted;
        }

        private static string KindLabel(BackStepKind kind) => kind switch
        {
            BackStepKind.Alert => "alert",
            BackStepKind.Modal => "modal",
            BackStepKind.Root => "root",
            _ => "screen"
        };

        private static string DisplayName(ScreenRegistry registry, string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            ScreenDefinition definition = registry != null ? registry.GetScreen(id) : null;
            return definition != null ? definition.ScreenName : id;
        }

        private static void Select(LayerComponent layer)
        {
            if (layer == null)
                return;

            Selection.activeGameObject = layer.gameObject;
            EditorGUIUtility.PingObject(layer.gameObject);
        }

        private static async void Run(Func<System.Threading.Tasks.Task<bool>> navigation)
        {
            try
            {
                await navigation();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}

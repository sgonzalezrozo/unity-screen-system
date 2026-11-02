using Tools.ScreenSystem.Scripts.ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Tools.ScreenSystem.Editor
{
	public class ScreenLayersUIRenderer
	{
		private ScreenLayersManager manager;
		private Vector2 scrollPosition;

		public ScreenLayersUIRenderer(ScreenLayersManager manager)
		{
			this.manager = manager;
			scrollPosition = Vector2.zero;
		}

		public void DrawMainUI()
		{
			// Check if system is set up first
			if (!ScreenSystemSettings.IsValid())
			{
				DrawSetupRequiredUI();
				return;
			}

			if (manager.GetScreenDefinition() == null)
			{
				DrawScreenNotFoundUI();
				return;
			}

			DrawMainContent();
		}

		public void DrawMainContent()
		{
			DrawHeader();

			EditorGUILayout.Space(10);

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			DrawLayersList();

			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space(10);

			DrawBottomButtons();
		}

		private void DrawSetupRequiredUI()
		{
			EditorGUILayout.Space(20);

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.HelpBox(
				"⚠️ SCREEN SYSTEM NOT SET UP\n\n" +
				"The Screen System requires initial setup before you can edit layers.\n\n" +
				"Click the button below to run the setup (takes 30 seconds).",
				MessageType.Error);

			EditorGUILayout.Space(10);

			if (GUILayout.Button("🔧 Run Setup Now", GUILayout.Height(40)))
			{
				ScreenSystemSetup.SetupProject();
				manager.LoadScreenData();
			}

			EditorGUILayout.Space(10);

			EditorGUILayout.LabelField("Setup creates:", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("• Assets/Resources/Screen System/");
			EditorGUILayout.LabelField("• ScreenSystemSettings.asset");
			EditorGUILayout.LabelField("• ScreenRegistry.asset");
			EditorGUILayout.LabelField("• TransitionConfig.asset");

			EditorGUILayout.EndVertical();
		}

		private void DrawScreenNotFoundUI()
		{
			EditorGUILayout.Space(20);

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.HelpBox(
				"❌ SCREEN NOT FOUND\n\n" +
				"Screen not found in registry.\n\n" +
				"Make sure you've imported your feature to the registry:\n" +
				"1. Open Feature Editor\n" +
				"2. Click 'Save & Apply'\n" +
				"3. Try opening this window again",
				MessageType.Error);

			EditorGUILayout.Space(10);

			EditorGUILayout.EndVertical();
		}

		private void DrawHeader()
		{
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField($"Layers: {manager.GetLayers().Count}", EditorStyles.boldLabel);
			EditorGUILayout.EndVertical();
		}

		private void DrawLayersList()
		{
			EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);

			for (int i = 0; i < manager.GetLayers().Count; i++)
			{
				DrawLayerItem(i);
				EditorGUILayout.Space(5);
			}

			if (GUILayout.Button("+ Add Layer", GUILayout.Height(30)))
			{
				manager.GetLayers().Add(new ScreenLayersManager.LayerData
				{
					LayerName = $"{manager.GetScreenDefinition().ScreenId}Layer{manager.GetLayers().Count + 1}",
					ZOrder = 1000 + manager.GetLayers().Count * 10,
					HasShell = false,
					HasContent = false
				});
			}
		}

		private void DrawLayerItem(int index)
		{
			ScreenLayersManager.LayerData layer = manager.GetLayers()[index];

			EditorGUILayout.BeginVertical("box");

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"Layer {index + 1}", EditorStyles.boldLabel, GUILayout.Width(80));

			if (GUILayout.Button("Remove", GUILayout.Width(70)))
			{
				if (EditorUtility.DisplayDialog("Remove Layer", $"Remove layer '{layer.LayerName}'?", "Remove", "Cancel"))
					manager.GetLayers().RemoveAt(index);

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				return;
			}

			EditorGUILayout.EndHorizontal();

			layer.LayerName = EditorGUILayout.TextField("Layer Name", layer.LayerName);
			layer.ZOrder = EditorGUILayout.IntField("Z-Order", layer.ZOrder);

			EditorGUILayout.Space(5);

			DrawLayerStatus(layer);

			EditorGUILayout.EndVertical();
		}

		private void DrawLayerStatus(ScreenLayersManager.LayerData layer)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Status:", GUILayout.Width(80));

			GUI.color = layer.HasShell ? Color.green : Color.red;
			EditorGUILayout.LabelField(layer.HasShell ? "✓ Shell" : "✗ Shell", GUILayout.Width(60));

			GUI.color = layer.HasContent ? Color.green : Color.red;
			EditorGUILayout.LabelField(layer.HasContent ? "✓ Content" : "✗ Content", GUILayout.Width(80));

			GUI.color = Color.white;
			EditorGUILayout.EndHorizontal();
		}

		private void DrawBottomButtons()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Save Layers", GUILayout.Height(35)))
			{
				manager.SaveLayers();
				EditorUtility.DisplayDialog("Success", $"Saved {manager.GetLayers().Count} layers", "OK");
				manager.LoadScreenData();
			}

			if (GUILayout.Button("Create Empty Content", GUILayout.Height(35)))
			{
				int contentCreated = manager.CreateContentPrefabs();
				EditorUtility.DisplayDialog("Success",
					$"Created {contentCreated} content prefab(s)\n\nNote: Shells are created automatically at runtime from the ShellTemplate.",
					"OK");
				manager.LoadScreenData();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(5);

			EditorGUILayout.HelpBox(
				"Save Layers: Updates the screen registry with layer configuration.\n\n" +
				"Create Empty Content: Generates empty content prefabs for missing layers.\n" +
				"(Shells are created automatically at runtime from ShellTemplate)",
				MessageType.Info);
		}
	}
}


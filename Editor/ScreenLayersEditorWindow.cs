using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
	public class ScreenLayersEditorWindow : EditorWindow
	{
		private string screenId;
		private ScreenLayersManager manager;
		private ScreenLayersUIRenderer uiRenderer;

		public static void OpenForScreen(string screenId)
		{
			ScreenLayersEditorWindow window = GetWindow<ScreenLayersEditorWindow>("Screen Layers Editor");
			window.minSize = new Vector2(500, 400);
			window.maxSize = new Vector2(800, 800);
			window.screenId = screenId;
			window.InitializeManagers();
			window.manager.LoadScreenData();
			window.Show();
			window.Focus();

			Debug.Log($"[ScreenLayersEditor] Opening editor for screen: {screenId}");
		}

		private void InitializeManagers()
		{
			if (manager == null)
			{
				manager = new ScreenLayersManager(screenId);
				uiRenderer = new ScreenLayersUIRenderer(manager);
			}
		}

		private void OnGUI()
		{
			InitializeManagers();

			if (uiRenderer != null)
				uiRenderer.DrawMainUI();
		}
	}
}

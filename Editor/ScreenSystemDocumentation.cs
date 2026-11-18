using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Links to the Screen System documentation. After the package is imported, shows a welcome dialog once per project.
    /// </summary>
    [InitializeOnLoad]
    internal static class ScreenSystemDocumentation
    {
        internal const string Url = "https://github.com/sgonzalezrozo/unity-screen-system#readme";

        private const string ShownKeyPrefix = "com.sgonzalezrozo.screen-system.WelcomeShown:";

        static ScreenSystemDocumentation()
        {
            if (Application.isBatchMode || EditorPrefs.GetBool(ShownKey, false))
                return;

            EditorApplication.update += ShowWelcomeWhenIdle;
        }

        // One key per project, so the dialog shows again in another project.
        private static string ShownKey => ShownKeyPrefix + Application.dataPath;

        private static void ShowWelcomeWhenIdle()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.update -= ShowWelcomeWhenIdle;
            if (EditorPrefs.GetBool(ShownKey, false))
                return;

            EditorPrefs.SetBool(ShownKey, true);
            bool open = EditorUtility.DisplayDialog(
                "Screen System is installed",
                "Next, run Tools > Screen System > Setup. It creates the settings, the screen registry, the ScreenSystem prefab and the splash and quit confirmation screens.\n\nThe documentation shows how to create screens and navigate between them.",
                "Open Documentation",
                "Close");

            if (open)
                OpenDocumentation();
        }

        [MenuItem("Tools/Screen System/Documentation", priority = 100)]
        private static void OpenDocumentationFromMenu() => OpenDocumentation();

        internal static void OpenDocumentation() => Application.OpenURL(Url);
    }
}

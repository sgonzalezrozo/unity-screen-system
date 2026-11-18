using UnityEditor;
using UnityEngine;

namespace ScreenSystem.Editor
{
    /// <summary>
    /// Links to the Screen System documentation.
    /// </summary>
    internal static class ScreenSystemDocumentation
    {
        internal const string Url = "https://github.com/sgonzalezrozo/unity-screen-system#readme";

        [MenuItem("Tools/Screen System/Documentation", priority = 100)]
        private static void OpenDocumentationFromMenu() => OpenDocumentation();

        internal static void OpenDocumentation() => Application.OpenURL(Url);
    }
}

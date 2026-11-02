using UnityEngine;

namespace ScreenSystem
{
    public interface IShellTemplateProvider
    {
        GameObject GetShellTemplate(UIFramework framework);
    }

    public class ShellTemplateProvider : IShellTemplateProvider
    {
        private readonly GameObject shellTemplate_uGUI;
        private readonly GameObject shellTemplate_UIToolkit;

        public ShellTemplateProvider(GameObject uguiShell, GameObject uitkShell)
        {
            shellTemplate_uGUI = uguiShell;
            shellTemplate_UIToolkit = uitkShell;
        }

        public GameObject GetShellTemplate(UIFramework framework)
            => framework == UIFramework.uGUI ? shellTemplate_uGUI : shellTemplate_UIToolkit;
    }
}


using UnityEngine;
using UnityEngine.UIElements;

namespace ScreenSystem
{
    internal class UIDocumentController
    {
        private readonly UIDocument uiDocument;
        private readonly string documentName;

        public UIDocument UIDocument => uiDocument;

        public UIDocumentController(GameObject gameObject)
        {
            documentName = gameObject.name;
            uiDocument = gameObject.GetComponent<UIDocument>();
            if (uiDocument == null)
                uiDocument = gameObject.AddComponent<UIDocument>();
        }

        public void ApplyZOrder(int zOrder)
        {
            if (uiDocument == null)
                return;

            uiDocument.sortingOrder = zOrder;
        }

        /// <summary>True when the document is enabled and its root isn't hidden with display: none.</summary>
        public bool IsVisible
        {
            get
            {
                if (uiDocument == null || !uiDocument.enabled)
                    return false;

                VisualElement root = uiDocument.rootVisualElement;
                return root == null || root.style.display.value != DisplayStyle.None;
            }
        }

        public void Show()
        {
            if (uiDocument == null)
            {
                Debug.LogWarning($"[UIDocumentController] Show failed - UIDocument is null for {documentName}");
                return;
            }

            if (!uiDocument.enabled)
                uiDocument.enabled = true;

            VisualElement root = uiDocument.rootVisualElement;
            if (root != null)
                root.style.display = DisplayStyle.Flex;
        }

        // Hides with display: none instead of disabling the UIDocument. Disabling destroys the visual tree and enabling
        // clones it again, about 330 KB of allocations per show on a Pixel 5. The hidden root isn't drawn, laid out or
        // picked, like a uGUI layer whose Canvas is disabled.
        public void Hide()
        {
            if (uiDocument == null)
            {
                Debug.LogWarning($"[UIDocumentController] Hide failed - UIDocument is null for {documentName}");
                return;
            }

            VisualElement root = uiDocument.enabled ? uiDocument.rootVisualElement : null;
            if (root != null)
                root.style.display = DisplayStyle.None;
            else
                uiDocument.enabled = false;
        }
    }
}


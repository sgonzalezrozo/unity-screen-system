using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.ScreenSystem.Scripts
{
    public class UIDocumentController
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

        public void Show()
        {
            if (uiDocument == null)
            {
                Debug.LogWarning($"[UIDocumentController] Show failed - UIDocument is null for {documentName}");
                return;
            }

            uiDocument.enabled = true;
        }

        public void Hide()
        {
            if (uiDocument == null)
            {
                Debug.LogWarning($"[UIDocumentController] Hide failed - UIDocument is null for {documentName}");
                return;
            }

            uiDocument.enabled = false;
        }
    }
}


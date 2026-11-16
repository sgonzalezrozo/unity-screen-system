using UnityEngine.UIElements;

namespace ScreenSystem
{
    internal class LayerUIView_UIToolkit : ILayerUIView
    {
        private readonly UIDocument uiDocument;

        public LayerUIView_UIToolkit(UIDocument uiDocument)
        {
            this.uiDocument = uiDocument;
        }

        public void SetAlpha(float alpha)
        {
            if (uiDocument?.rootVisualElement != null)
                uiDocument.rootVisualElement.style.opacity = alpha;
        }

        public float GetAlpha()
        {
            if (uiDocument?.rootVisualElement != null)
                return uiDocument.rootVisualElement.style.opacity.value;
            return 1f;
        }

        public void Show()
        {
            if (uiDocument?.rootVisualElement != null)
                uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (uiDocument?.rootVisualElement != null)
                uiDocument.rootVisualElement.style.display = DisplayStyle.None;
        }
    }
}


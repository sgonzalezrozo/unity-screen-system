using UnityEngine;

namespace ScreenSystem
{
    public static class ScreenVisibility
    {
        public static bool IsVisible(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            LayerComponent layer = gameObject.GetComponentInParent<LayerComponent>();

            if (layer == null)
                return false;

            return layer.IsCanvasEnabled;
        }

        public static bool IsVisible(Component component)
        {
            if (component == null)
                return false;

            return IsVisible(component.gameObject);
        }

        public static LayerComponent GetParentLayer(GameObject gameObject)
        {
            if (gameObject == null)
                return null;

            return gameObject.GetComponentInParent<LayerComponent>();
        }
    }
}


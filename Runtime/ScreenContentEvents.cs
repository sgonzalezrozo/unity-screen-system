using System;
using UnityEngine;

namespace Tools.ScreenSystem.Scripts
{
    public class ScreenContentEvents : MonoBehaviour, IScreenContentEvents
    {
        public event Action OnContentStartLoading;
        public event Action OnContentLoaded;

        private LayerComponent layerComponent;

        private void Awake()
        {
            layerComponent = GetComponent<LayerComponent>();
            if (layerComponent == null)
            {
                Debug.LogError("[ScreenContentEvents] LayerComponent not found on this GameObject");
                enabled = false;
                return;
            }

            SubscribeToLayerEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromLayerEvents();
        }

        private void SubscribeToLayerEvents()
        {
            if (layerComponent == null)
                return;

            layerComponent.OnContentLoaded += NotifyContentLoaded;
        }

        private void UnsubscribeFromLayerEvents()
        {
            if (layerComponent == null)
                return;

            layerComponent.OnContentLoaded -= NotifyContentLoaded;
        }

        private void NotifyContentLoaded(GameObject contentGO)
        {
            OnContentStartLoading?.Invoke();
            OnContentLoaded?.Invoke();
        }
    }
}


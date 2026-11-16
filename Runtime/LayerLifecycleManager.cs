using UnityEngine;

namespace ScreenSystem
{
    internal class LayerLifecycleManager : ILayerLifecycle
    {
        private readonly ILayerUIView uiView;
        private readonly SharedLayerManager sharedManager;

        public string CurrentScreenId => sharedManager.CurrentScreenId;
        public int CurrentZOrder => sharedManager.CurrentZOrder;

        public LayerLifecycleManager(ILayerUIView uiView, SharedLayerManager sharedManager)
        {
            this.uiView = uiView;
            this.sharedManager = sharedManager;
        }

        public void Assign(string screenId, int zOrder, int layerOrder, string readableId)
        {
            if (sharedManager == null)
            {
                Debug.LogError("[LayerLifecycleManager] Dependencies not initialized");
                return;
            }

            sharedManager.AssignToScreen(screenId, zOrder, layerOrder);
        }

        public void Unassign(string screenId)
        {
            if (sharedManager == null)
                return;

            sharedManager.UnassignFromScreen(screenId);
        }

        public void Show()
        {
            uiView?.Show();
        }

        public void Hide()
        {
            if (uiView == null)
                return;

            uiView.Hide();
        }
    }
}

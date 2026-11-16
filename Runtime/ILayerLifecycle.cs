namespace ScreenSystem
{
    internal interface ILayerLifecycle
    {
        string CurrentScreenId { get; }
        int CurrentZOrder { get; }
        void Assign(string screenId, int zOrder, int layerOrder, string readableId);
        void Unassign(string screenId);
        void Show();
        void Hide();
    }
}

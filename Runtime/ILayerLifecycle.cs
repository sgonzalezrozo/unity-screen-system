namespace Tools.ScreenSystem.Scripts
{
    public interface ILayerLifecycle
    {
        string CurrentScreenId { get; }
        int CurrentZOrder { get; }
        void Assign(string screenId, int zOrder, int layerOrder, string readableId);
        void Unassign(string screenId);
        void Show();
        void Hide();
    }
}

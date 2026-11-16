namespace ScreenSystem
{
    /// <summary>
    /// Receives the value passed with <c>GoTo(screen, value)</c>. Any component on any layer of the screen can
    /// implement it, several of them too. Each one gets the value once per GoTo, before the layers are shown, so the
    /// first frame already draws with it. Shared layers that are already visible get it as well.
    /// GoBack and PopTo don't deliver it again, and Screen System doesn't keep it: store what you need.
    /// </summary>
    public interface IScreenParameter<T>
    {
        void OnScreenParameter(T value);
    }
}

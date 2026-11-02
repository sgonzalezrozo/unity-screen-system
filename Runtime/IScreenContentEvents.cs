using System;

namespace ScreenSystem
{
    public interface IScreenContentEvents
    {
        event Action OnContentStartLoading;
        event Action OnContentLoaded;
    }
}

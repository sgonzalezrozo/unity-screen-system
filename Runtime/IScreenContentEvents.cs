using System;

namespace Tools.ScreenSystem.Scripts
{
    public interface IScreenContentEvents
    {
        event Action OnContentStartLoading;
        event Action OnContentLoaded;
    }
}

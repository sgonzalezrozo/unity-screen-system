namespace ScreenSystem
{
    /// <summary>Why a navigation call did or didn't happen.</summary>
    public enum NavigationStatus
    {
        /// <summary>The call did what it asked.</summary>
        Succeeded,

        /// <summary>Screen System isn't initialized yet. Await <see cref="ScreenManager.WhenReadyAsync"/> first.</summary>
        NotReady,

        /// <summary>No screen with that name or ID, or, for PopTo, it isn't in the stack.</summary>
        NotFound,

        /// <summary>Another navigation call is running. Calls are ignored, never queued.</summary>
        Transitioning,

        /// <summary>A modal is open, and only one is open at a time.</summary>
        BlockedByModal,

        /// <summary>An alert is open and blocks navigation until DismissAlert().</summary>
        BlockedByAlert,

        /// <summary>The screen is already showing on top.</summary>
        AlreadyShowing,

        /// <summary>There's nothing to close: back at the root, or DismissAlert() with no alert open.</summary>
        NothingToClose,

        /// <summary>The request isn't valid, such as an empty name or ClearToRoot with a modal.</summary>
        InvalidRequest,

        /// <summary>The screen couldn't be shown: its prefab didn't load or an exception was thrown. The console has the details.</summary>
        Failed
    }

    /// <summary>
    /// The outcome of a navigation call. Converts to bool, so <c>if (await screens.GoTo(...))</c> keeps working; read
    /// <see cref="Status"/> to know why a call didn't happen.
    /// </summary>
    public readonly struct NavigationResult
    {
        public readonly NavigationStatus Status;

        /// <summary>The screen the call was about, when there is one.</summary>
        public readonly string Screen;

        public NavigationResult(NavigationStatus status, string screen = null)
        {
            Status = status;
            Screen = screen;
        }

        public bool Succeeded
            => Status == NavigationStatus.Succeeded;

        public static implicit operator bool(NavigationResult result)
            => result.Succeeded;

        public static bool operator true(NavigationResult result)
            => result.Succeeded;

        public static bool operator false(NavigationResult result)
            => !result.Succeeded;

        public override string ToString()
            => string.IsNullOrEmpty(Screen) ? Status.ToString() : $"{Status} ({Screen})";
    }
}

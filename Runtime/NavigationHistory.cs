namespace ScreenSystem
{
    /// <summary>What GoTo does with the back stack.</summary>
    public enum NavigationHistory
    {
        /// <summary>Adds the screen on top of the stack. Back returns to the previous screen.</summary>
        Push,

        /// <summary>
        /// Removes every screen above the root, then opens the screen over it. Back returns to the root. For tab
        /// bars: from Inventory with A, B, C open, GoTo("Store", ClearToRoot) leaves Root, Store. The root isn't
        /// shown on the way. Fullscreen screens only.
        /// </summary>
        ClearToRoot
    }
}

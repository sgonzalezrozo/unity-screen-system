using UnityEngine;
using UnityEngine.EventSystems;

namespace ScreenSystem
{
    /// <summary>
    /// The EventSystem Screen System adds when no scene has one. A subclass so Unity's "There are 2 event systems"
    /// check, which only counts the exact EventSystem type, never counts it while it hands over to a scene's own.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class ScreenSystemEventSystem : EventSystem
    {
    }
}

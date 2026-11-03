using ScreenSystem;
using UnityEngine;

public class SampleScreen : MonoBehaviour, IScreenLifecycle
{
    public void OnScreenShown()
    {
        Debug.Log($"Shown: {gameObject.name}");
    }

    public void OnScreenHidden()
    {
        Debug.Log($"Hidden: {gameObject.name}");
    }
}

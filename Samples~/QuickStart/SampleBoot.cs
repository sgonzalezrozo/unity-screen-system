using System;
using ScreenSystem;
using UnityEngine;

public class SampleBoot : MonoBehaviour
{
    [SerializeField] private string firstScreen = "ShopMain";

    private async void Start()
    {
        try
        {
            ScreenManager screens = await ScreenManager.WhenReadyAsync();
            if (screens != null)
                await screens.SetRoot(firstScreen);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}

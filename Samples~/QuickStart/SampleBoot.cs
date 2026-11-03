using System;
using System.Threading.Tasks;
using ScreenSystem;
using UnityEngine;

public class SampleBoot : MonoBehaviour
{
    [SerializeField] private string firstScreen = "ShopMain";

    private async void Start()
    {
        try
        {
            while (ScreenManager.Instance == null)
                await Task.Yield();

            await ScreenManager.Instance.WaitForInitializationAsync();
            await ScreenManager.Instance.SetRoot(firstScreen);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}

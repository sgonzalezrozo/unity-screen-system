using System;
using ScreenSystem.Services;
using UnityEngine;

namespace ScreenSystem
{
    /// <summary>
    /// The two buttons of a quit confirmation modal. Point the confirm button's On Click at <see cref="Quit"/> and the
    /// other at <see cref="Cancel"/>. Tools > Screen System > Create Special Screens builds a modal wired this way.
    /// </summary>
    [AddComponentMenu("Screen System/Quit Confirmation Layer")]
    public sealed class QuitConfirmationLayer : MonoBehaviour
    {
        /// <summary>Raises <see cref="NavigationEvents.OnQuitApplicationRequested"/>, then quits, or stops Play Mode in the Editor.</summary>
        public void Quit()
            => QuitManager.QuitApplication();

        /// <summary>Closes the modal, like back. The app keeps running on the screen it was on.</summary>
        public async void Cancel()
        {
            ScreenManager manager = ScreenManager.Instance;
            if (manager == null)
                return;

            try
            {
                await manager.GoBack();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}

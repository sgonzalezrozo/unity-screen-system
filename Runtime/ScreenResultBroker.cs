using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    /// <summary>
    /// Holds the pending results of GoToForResult. One modal and one alert can be open at a time, so there's at most
    /// one pending result for each. A result always completes, once its screen is no longer open: Returned with the
    /// value staged by CloseWithResult, or Dismissed. Completing only after the screen closed means the caller's next
    /// navigation doesn't run into the close that's still in progress.
    /// </summary>
    internal sealed class ScreenResultBroker
    {
        internal interface IPending
        {
            string ScreenId { get; }

            /// <summary>Keeps the value to return when the screen closes. Returns false if it isn't the expected type.</summary>
            bool TryStage<TValue>(TValue value);

            /// <summary>Forgets a staged value, when the close it was for didn't happen.</summary>
            void Unstage();

            /// <summary>Completes: Returned with the staged value if there is one, otherwise Dismissed.</summary>
            void Complete();
        }

        internal sealed class Pending<T> : IPending
        {
            private readonly TaskCompletionSource<ModalResult<T>> completion = new();
            private bool staged;
            private T stagedValue;

            public Pending(string screenId, NavigationResult navigation)
            {
                ScreenId = screenId;
                Navigation = navigation;
            }

            public string ScreenId { get; }

            public NavigationResult Navigation { get; set; }

            public Task<ModalResult<T>> Task
                => completion.Task;

            public bool TryStage<TValue>(TValue value)
            {
                if (value is T typed)
                    stagedValue = typed;
                else if (value == null && default(T) == null)
                    stagedValue = default;
                else
                    return false;

                staged = true;
                return true;
            }

            public void Unstage()
            {
                staged = false;
                stagedValue = default;
            }

            public void Complete()
                => completion.TrySetResult(staged
                    ? new ModalResult<T>(ModalResultStatus.Returned, stagedValue, Navigation)
                    : new ModalResult<T>(ModalResultStatus.Dismissed, default, Navigation));
        }

        private IPending modal;
        private IPending alert;

        public bool HasPendingModal
            => modal != null;

        public bool HasPendingAlert
            => alert != null;

        /// <summary>Starts waiting for <paramref name="screenId"/>'s result. Returns the replaced pending result, if any.</summary>
        public Pending<T> Begin<T>(string screenId, bool isAlert, out IPending replaced)
        {
            Pending<T> pending = new(screenId, new NavigationResult(NavigationStatus.Succeeded, screenId));
            replaced = isAlert ? alert : modal;
            if (isAlert)
                alert = pending;
            else
                modal = pending;
            return pending;
        }

        /// <summary>Puts back what Begin replaced, after the screen didn't open.</summary>
        public void Restore(IPending pending, IPending previous, bool isAlert)
        {
            if (isAlert && (alert == pending || alert == null))
                alert = previous;
            else if (!isAlert && (modal == pending || modal == null))
                modal = previous;
        }

        /// <summary>
        /// Stages <paramref name="value"/> for the topmost overlay: the alert's result if one is open, otherwise the
        /// modal's. It's returned when that screen closes. Returns the pending result, or null when there's none or
        /// the value is of another type (logged; the screen then closes as Dismissed).
        /// </summary>
        public IPending Stage<TValue>(TValue value, bool alertOnTop)
        {
            IPending pending = alertOnTop ? alert : modal;
            if (pending == null)
                return null;

            if (pending.TryStage(value))
                return pending;

            Debug.LogError($"[ScreenSystem] CloseWithResult({typeof(TValue).Name}) on '{pending.ScreenId}', but GoToForResult waits for another type. The result is Dismissed.");
            return null;
        }

        /// <summary>Completes results whose screens are no longer open. Called after every navigation and scene change.</summary>
        public void Settle(string currentModalId, string currentAlertId)
        {
            if (modal != null && modal.ScreenId != currentModalId)
            {
                modal.Complete();
                modal = null;
            }

            if (alert != null && alert.ScreenId != currentAlertId)
            {
                alert.Complete();
                alert = null;
            }
        }

        /// <summary>Completes every pending result, such as when Screen System is destroyed.</summary>
        public void CompleteAll()
        {
            modal?.Complete();
            alert?.Complete();
            modal = null;
            alert = null;
        }
    }
}

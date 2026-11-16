namespace ScreenSystem
{
    /// <summary>How a modal or alert opened with GoToForResult ended.</summary>
    public enum ModalResultStatus
    {
        /// <summary>It called CloseWithResult; the value is in <see cref="ModalResult{T}.Value"/>.</summary>
        Returned,

        /// <summary>It closed without a value: back, CloseAll, a GoTo that closed it, a scene change, or a value of another type.</summary>
        Dismissed,

        /// <summary>It didn't open; <see cref="ModalResult{T}.Navigation"/> says why.</summary>
        NotOpened
    }

    /// <summary>
    /// The answer of a modal or alert opened with <c>GoToForResult</c>:
    /// <code>
    /// ModalResult&lt;bool&gt; answer = await screens.GoToForResult&lt;bool&gt;(ScreenIds.ConfirmPurchase);
    /// if (answer.Returned &amp;&amp; answer.Value)
    ///     Buy();
    /// </code>
    /// </summary>
    public readonly struct ModalResult<T>
    {
        public readonly ModalResultStatus Status;

        /// <summary>The value passed to CloseWithResult. Default unless <see cref="Returned"/>.</summary>
        public readonly T Value;

        /// <summary>The result of opening it. Its Status says why when this is NotOpened.</summary>
        public readonly NavigationResult Navigation;

        public ModalResult(ModalResultStatus status, T value, NavigationResult navigation)
        {
            Status = status;
            Value = value;
            Navigation = navigation;
        }

        public bool Returned
            => Status == ModalResultStatus.Returned;

        public override string ToString()
            => Returned ? $"Returned {Value}" : Status == ModalResultStatus.NotOpened ? $"NotOpened ({Navigation})" : Status.ToString();
    }
}

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    internal interface ILayerContentLoader
    {
        event Action<GameObject> OnContentLoaded;
        event Action OnContentUnloaded;

        bool HasContent { get; }
        bool IsLoadingContent { get; }

        Task LoadContentAsync(CancellationToken ct = default);
        IEnumerator LoadContentCoroutine();
        Task WaitForLoadingCompleteAsync(CancellationToken ct = default);
        bool DestroyContent();
        void Cleanup();
    }
}


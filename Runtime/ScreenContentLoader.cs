using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ScreenSystem
{
    internal class ScreenContentLoader
    {
        private const string LOG_PREFIX = "[ScreenShow]";

        public void StartBackgroundContentLoads(List<LayerComponent> layers, CancellationToken ct)
        {
            foreach (LayerComponent layer in layers)
            {
                if (layer == null)
                    continue;

                if (layer.HasContent || layer.IsLoadingContent)
                    continue;

                layer.LoadContentAsync(ct).Forget("ScreenShowService.StartBackgroundContentLoads");
            }
        }

        public async Task WaitForAllLayersContentLoadedAsync(List<LayerComponent> layers, CancellationToken ct)
        {
            List<Task> tasks = CollectionPool.RentTaskList();
            try
            {
                foreach (LayerComponent layer in layers)
                {
                    if (layer == null)
                        continue;

                    if (layer.HasContent)
                        continue;

                    if (layer.IsLoadingContent)
                        tasks.Add(layer.WaitForLoadingCompleteAsync(ct));
                }

                if (tasks.Count > 0)
                {
                    try
                    {
                        await Task.WhenAll(tasks);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ScreenContentLoader] WaitForAllLayersContentLoadedAsync error: {ex.Message}");
                    }
                }
            }
            finally
            {
                CollectionPool.ReturnTaskList(tasks);
            }
        }
    }
}

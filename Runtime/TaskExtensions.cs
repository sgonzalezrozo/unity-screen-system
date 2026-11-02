using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Tools.ScreenSystem.Scripts
{
    public static class TaskExtensions
    {
        public static async void Forget(this Task task, string context = null)
        {
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"[{context ?? "Task"}] Operation cancelled (expected)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{context ?? "Task"}] Background task failed: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}


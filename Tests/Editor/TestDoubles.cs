using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem.Tests
{
    /// <summary>Records show and hide calls instead of loading prefabs.</summary>
    internal sealed class FakeScreenDisplay : IScreenDisplay
    {
        public readonly List<string> Calls = new();
        public readonly HashSet<string> Instantiated = new();
        public TaskCompletionSource<bool> PendingShow;
        public bool ThrowIfCancelled;
        public readonly HashSet<string> FailShow = new();
        public string PendingParameterScreenId;
        public ScreenParameter PendingParameter;

        public async Task<bool> ShowScreen(string screenId, bool autoHideParents = true, CancellationToken ct = default)
        {
            if (FailShow.Contains(screenId))
            {
                Calls.Add($"fail:{screenId}");
                return false;
            }

            if (ThrowIfCancelled)
                ct.ThrowIfCancellationRequested();

            if (PendingParameter != null && PendingParameterScreenId == screenId)
            {
                Calls.Add($"param:{screenId}");
                PendingParameter = null;
                PendingParameterScreenId = null;
            }

            Calls.Add($"show:{screenId}");
            Instantiated.Add(screenId);
            if (PendingShow != null)
                await PendingShow.Task;

            return true;
        }

        public Task HideScreen(string screenId, string preserveLayersForScreenId = null, CancellationToken ct = default)
        {
            Calls.Add($"hide:{screenId}");
            return Task.CompletedTask;
        }

        public string GetTopScreen() => null;
        public List<string> GetActiveScreenIds() => new();
        public List<LayerComponent> GetScreenLayers(string screenId) => new();
        public void DestroyScreen(string screenId) => Instantiated.Remove(screenId);
        public void UnregisterScreenWithoutDestroy(string screenId) { }
        public bool IsScreenInstantiated(string screenId) => Instantiated.Contains(screenId);
        public Dictionary<string, List<LayerComponent>> GetCachedScreens() => new();
        public void ClearCachedScreens() => Instantiated.Clear();

        public void SetPendingParameter(string screenId, ScreenParameter parameter)
        {
            PendingParameterScreenId = parameter != null ? screenId : null;
            PendingParameter = parameter;
        }

        public void DeliverParameter(string screenId, ScreenParameter parameter)
            => Calls.Add($"param-now:{screenId}");
    }

    /// <summary>Builds an in-memory registry. Dispose destroys the created assets.</summary>
    internal sealed class TestRegistry : System.IDisposable
    {
        public readonly ScreenRegistry Registry = ScriptableObject.CreateInstance<ScreenRegistry>();

        public TestRegistry Add(string name, ScreenType type = ScreenType.fullscreen, bool persist = false, bool ignoreRules = false)
        {
            ScreenDefinition definition = ScriptableObject.CreateInstance<ScreenDefinition>();
            definition.ScreenName = name;
            definition.ScreenId = name;
            definition.AssetPath = name;
            definition.screenType = type;
            definition.PersistOverScenes = persist;
            definition.IgnoreNavigationRules = ignoreRules;
            Registry.Screens.Add(definition);
            Registry.ForceReinitialize();
            return this;
        }

        public void Dispose()
        {
            foreach (ScreenDefinition screen in Registry.Screens)
                Object.DestroyImmediate(screen);
            Object.DestroyImmediate(Registry);
        }
    }
}

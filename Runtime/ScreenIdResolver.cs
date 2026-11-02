using ScreenSystem.ScriptableObjects;
using UnityEngine;

namespace ScreenSystem
{
    public class ScreenIdResolver
    {
        private const string LOG_PREFIX = "[ScreenIdResolver]";

        private readonly ScreenRegistry screenRegistry;

        public ScreenIdResolver(ScreenRegistry screenRegistry)
        {
            this.screenRegistry = screenRegistry;
        }

        private bool ValidateScreenId(string screenId, string methodName)
        {
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogError($"{LOG_PREFIX} {methodName} failed: screenId is null or empty");
                return false;
            }

            return true;
        }

        private string ResolveScreenId(string requestedId)
        {
            if (string.IsNullOrEmpty(requestedId) || screenRegistry == null)
                return requestedId;

            ScreenDefinition screen = screenRegistry.GetScreen(requestedId);
            return screen != null ? screen.ScreenId : requestedId;
        }

        public bool TryResolveScreenId(string requestedId, string methodName, out string resolvedId)
        {
            resolvedId = requestedId;

            if (!ValidateScreenId(requestedId, methodName))
                return false;

            resolvedId = ResolveScreenId(requestedId);
            return true;
        }
    }
}


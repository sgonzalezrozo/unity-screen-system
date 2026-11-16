using System.Collections.Generic;

namespace ScreenSystem
{
    internal class SharedLayerManager
    {
        private class ScreenAssignment
        {
            public string ScreenId;
            public int ZOrder;
            public int LayerOrder;
        }

        private readonly List<ScreenAssignment> screenAssignments = new();
        private string currentScreenId;
        private int currentZOrder;
        private int currentLayerOrder;

        public string CurrentScreenId
            => currentScreenId;

        public int CurrentZOrder
            => currentZOrder;

        public int CurrentLayerOrder
            => currentLayerOrder;

        public int AssignmentCount
            => screenAssignments.Count;

        public bool IsBeingUsed
            => screenAssignments.Count > 0;

        public IReadOnlyList<string> AssignedScreens
        {
            get
            {
                List<string> result = new(screenAssignments.Count);
                for (int i = 0; i < screenAssignments.Count; i++)
                    result.Add(screenAssignments[i].ScreenId);
                return result;
            }
        }

        public void AssignToScreen(string screenId, int zOrder, int layerOrder)
        {
            ScreenAssignment existing = FindAssignment(screenId);
            if (existing != null)
            {
                existing.ZOrder = zOrder;
                existing.LayerOrder = layerOrder;
            }
            else
            {
                screenAssignments.Add(new ScreenAssignment
                {
                    ScreenId = screenId,
                    ZOrder = zOrder,
                    LayerOrder = layerOrder
                });
            }

            RecalculateActiveScreen();
        }

        public void UnassignFromScreen(string screenId)
        {
            for (int i = screenAssignments.Count - 1; i >= 0; i--)
            {
                if (screenAssignments[i].ScreenId == screenId)
                    screenAssignments.RemoveAt(i);
            }
            RecalculateActiveScreen();
        }

        private void RecalculateActiveScreen()
        {
            if (screenAssignments.Count == 0)
            {
                currentScreenId = null;
                currentZOrder = 0;
                currentLayerOrder = 0;
                return;
            }

            ScreenAssignment topmost = FindHighestZOrderAssignment();

            currentScreenId = topmost.ScreenId;
            currentZOrder = topmost.ZOrder;
            currentLayerOrder = topmost.LayerOrder;
        }

        private ScreenAssignment FindAssignment(string screenId)
        {
            for (int i = 0; i < screenAssignments.Count; i++)
            {
                if (screenAssignments[i].ScreenId == screenId)
                    return screenAssignments[i];
            }
            return null;
        }

        private ScreenAssignment FindHighestZOrderAssignment()
        {
            ScreenAssignment topmost = screenAssignments[0];
            for (int i = 1; i < screenAssignments.Count; i++)
            {
                if (screenAssignments[i].ZOrder > topmost.ZOrder)
                    topmost = screenAssignments[i];
            }
            return topmost;
        }
    }
}

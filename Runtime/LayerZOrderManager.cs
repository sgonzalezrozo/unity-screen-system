using System.Collections.Generic;
using UnityEngine;

namespace Tools.ScreenSystem.Scripts
{
    public class LayerZOrderManager
    {
        private const int MIN_BASE_Z_ORDER = 10;
        private const int NAVIGATION_DEPTH_OFFSET = 20;

        private readonly ScreenCacheManager cacheManager;

        public LayerZOrderManager(ScreenCacheManager cacheManager = null)
        {
            this.cacheManager = cacheManager;
        }

        public int CalculateBaseZOrder(ScreenType screenType, string screenId)
        {
            int navigationDepth = 0;

            if (cacheManager != null)
            {
                IEnumerable<string> activeScreenIds = cacheManager.GetActiveScreens();

                int currentIndex = 0;
                int existingIndex = -1;

                using (IEnumerator<string> enumerator = activeScreenIds.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == screenId)
                            existingIndex = currentIndex;
                        currentIndex++;
                    }
                }

                if (existingIndex >= 0)
                {
                    navigationDepth = existingIndex;
                }
                else
                {
                    navigationDepth = currentIndex - 1;
                    if (navigationDepth < 0)
                        navigationDepth = 0;
                }
            }

            int baseZ = MIN_BASE_Z_ORDER + (navigationDepth * NAVIGATION_DEPTH_OFFSET);

            return baseZ;
        }

        public int[] GetLayerZOffsetsFromScreen(ScreenDefinition screen, int layerCount, GameObject loadedPrefab = null)
        {
            int[] offsets = new int[layerCount];
            offsets[0] = 0;

            if (loadedPrefab == null)
            {
                for (int i = 1; i < layerCount; i++)
                    offsets[i] = i * 2;
                return offsets;
            }

            LayerDefinition layerDef = loadedPrefab.GetComponent<LayerDefinition>();
            if (layerDef == null || layerDef.LayerCount <= 0)
            {
                for (int i = 1; i < layerCount; i++)
                    offsets[i] = i * 2;
                return offsets;
            }

            int offsetIndex = 1;
            for (int i = 0; i < layerDef.LayerCount && offsetIndex < layerCount; i++)
            {
                LayerEntry entry = layerDef.GetLayer(i);
                if (entry is { isScreenSeparator: true })
                    continue;

                string layerName = layerDef.GetLayerName(i);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                int zOffset = layerDef.GetLayerZOffset(i);
                offsets[offsetIndex] = zOffset;
                offsetIndex++;
            }

            for (int i = offsetIndex; i < layerCount; i++)
                offsets[i] = i * 2;

            return offsets;
        }
    }
}

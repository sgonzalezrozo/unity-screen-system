using System;
using UnityEngine;

namespace ScreenSystem
{
    [Serializable]
    public class LayerEntry
    {
        [Tooltip("Name of the layer to load from Addressables")]
        public string layerName;

        [Tooltip("Amount of offset from screen (direction determined by position in list)")]
        public uint amount;

        [Tooltip("Is this the screen separator (z=0)?")]
        public bool isScreenSeparator;

        public LayerEntry(string name, uint offset, bool isScreen = false)
        {
            layerName = name;
            amount = offset;
            isScreenSeparator = isScreen;
        }
    }
}

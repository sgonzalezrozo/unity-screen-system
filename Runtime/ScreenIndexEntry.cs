using System;

namespace ScreenSystem
{
    [Serializable]
    public class ScreenIndexEntry
    {
        public string FeatureId { get; set; }
        public int LocalIndex { get; set; }

        public ScreenIndexEntry(string featureId, int localIndex)
        {
            FeatureId = featureId;
            LocalIndex = localIndex;
        }
    }
}

using Tools.Core;
using UnityEngine;

namespace Tools.ScreenSystem.Scripts
{
    [DefaultExecutionOrder(-1)]
    public class LayerPrefabInstantiator : MonoBehaviour
    {
        [SerializeField] private GameObject layerPrefab;

        internal IPrefabInstantiator injectedInstantiator;

        private GameObject instance;
        private bool hasInitialized;
        private IPrefabInstantiator instantiator;

        private void Awake()
        {
            instantiator = injectedInstantiator ?? new NullPrefabInstantiator();
        }

        private void OnEnable()
        {
            if (!hasInitialized)
            {
                hasInitialized = true;
                return;
            }

            if (!instance)
                LoadNow();
        }

        private void LoadNow()
        {
            instance = instantiator.Instantiate(layerPrefab, transform);
        }
    }
}

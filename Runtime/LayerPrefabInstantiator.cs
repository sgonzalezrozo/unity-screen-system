using ToolsCore;
using UnityEngine;

namespace ScreenSystem
{
    [DefaultExecutionOrder(-1)]
    public class LayerPrefabInstantiator : MonoBehaviour
    {
        [SerializeField] private GameObject layerPrefab;

        internal IPrefabInstantiator injectedInstantiator;

        private GameObject instance;
        private bool hasInitialized;

        private IPrefabInstantiator Instantiator => injectedInstantiator ?? ToolInjectionServices.PrefabInstantiator;

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
            instance = Instantiator.Instantiate(layerPrefab, transform);
        }
    }
}

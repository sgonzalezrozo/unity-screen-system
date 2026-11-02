using UnityEngine;

namespace Tools.ScreenSystem.Scripts.Core
{
    public class FastIoCScreenPrefabInstantiator : IPrefabInstantiator
    {
        private readonly global::Tools.Core.IPrefabInstantiator coreInstantiator;

        public FastIoCScreenPrefabInstantiator(global::Tools.Core.IPrefabInstantiator coreInstantiator)
        {
            this.coreInstantiator = coreInstantiator ?? throw new System.ArgumentNullException(nameof(coreInstantiator));
        }

        public GameObject Instantiate(GameObject prefab, Transform parent = null)
        {
            return coreInstantiator.Instantiate(prefab, parent);
        }

        public GameObject Instantiate(GameObject prefab, Transform parent, object data)
        {
            return coreInstantiator.Instantiate(prefab, parent, data);
        }

        public T Instantiate<T>(GameObject prefab, Transform parent = null) where T : Component
        {
            return coreInstantiator.Instantiate<T>(prefab, parent);
        }

        public T Instantiate<T>(GameObject prefab, Transform parent, object data) where T : Component
        {
            return coreInstantiator.Instantiate<T>(prefab, parent, data);
        }
    }
}


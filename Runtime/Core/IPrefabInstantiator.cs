using UnityEngine;

namespace Tools.ScreenSystem.Scripts.Core
{
    public interface IPrefabInstantiator
    {
        GameObject Instantiate(GameObject prefab, Transform parent = null);

        GameObject Instantiate(GameObject prefab, Transform parent, object data);

        T Instantiate<T>(GameObject prefab, Transform parent = null) where T : Component;

        T Instantiate<T>(GameObject prefab, Transform parent, object data) where T : Component;
    }
}


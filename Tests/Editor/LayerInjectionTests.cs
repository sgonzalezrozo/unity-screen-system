using System.Collections.Generic;
using NUnit.Framework;
using ToolsCore;
using UnityEngine;

namespace ScreenSystem.Tests
{
    public class LayerInjectionTests
    {
        private sealed class RecordingInjector : IMonoBehaviourInjector
        {
            public readonly List<MonoBehaviour> Injected = new();
            public readonly List<bool> ActiveWhenInjected = new();

            public void InjectIfNeeded(MonoBehaviour component)
            {
                Injected.Add(component);
                ActiveWhenInjected.Add(component.gameObject.activeInHierarchy);
            }
        }

        // Stands in for a container's instantiator, which injects as it creates the copy.
        private sealed class InjectingInstantiator : IInjectingPrefabInstantiator
        {
            public int Calls;

            public GameObject Instantiate(GameObject prefab, Transform parent = null)
            {
                Calls++;
                return Object.Instantiate(prefab, parent);
            }

            public GameObject Instantiate(GameObject prefab, Transform parent, object data) => Instantiate(prefab, parent);
            public T Instantiate<T>(GameObject prefab, Transform parent = null) where T : Component => Instantiate(prefab, parent).GetComponent<T>();
            public T Instantiate<T>(GameObject prefab, Transform parent, object data) where T : Component => Instantiate<T>(prefab, parent);
        }

        private readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in objects)
                if (go != null)
                    Object.DestroyImmediate(go);
            objects.Clear();
        }

        private GameObject Make(string name, bool active = true)
        {
            GameObject go = new(name);
            go.SetActive(active);
            objects.Add(go);
            return go;
        }

        private GameObject Template()
        {
            GameObject template = Make("Layer");
            template.AddComponent<ParameterReceiverStub>();
            GameObject child = new("Child");
            child.transform.SetParent(template.transform);
            child.AddComponent<ParameterReceiverStub>();
            return template;
        }

        [Test]
        public void EveryComponentIsInjectedOnceWhileTheCopyIsStillInactive()
        {
            GameObject container = Make("Container");
            GameObject holder = Make("Holder", active: false);
            holder.transform.SetParent(container.transform);
            RecordingInjector injector = new();

            GameObject layer = LayerInitializer.InstantiateInjected(Template(), container.transform, holder.transform, new NullPrefabInstantiator(), injector);
            objects.Add(layer);

            Assert.AreEqual(2, injector.Injected.Count, "each MonoBehaviour once");
            CollectionAssert.AreEqual(new[] { false, false }, injector.ActiveWhenInjected, "injected before Awake and OnEnable can run");
            Assert.AreSame(container.transform, layer.transform.parent);
            Assert.IsTrue(layer.activeInHierarchy, "awake once it's in place");
        }

        // A custom instantiator that doesn't inject still creates the copy, and Screen System injects it.
        private sealed class PlainInstantiator : IPrefabInstantiator
        {
            public int Calls;

            public GameObject Instantiate(GameObject prefab, Transform parent = null)
            {
                Calls++;
                return Object.Instantiate(prefab, parent);
            }

            public GameObject Instantiate(GameObject prefab, Transform parent, object data) => Instantiate(prefab, parent);
            public T Instantiate<T>(GameObject prefab, Transform parent = null) where T : Component => Instantiate(prefab, parent).GetComponent<T>();
            public T Instantiate<T>(GameObject prefab, Transform parent, object data) where T : Component => Instantiate<T>(prefab, parent);
        }

        [Test]
        public void ACustomInstantiatorThatDoesntInjectIsUsedAndInjectedOnce()
        {
            GameObject container = Make("Container");
            GameObject holder = Make("Holder", active: false);
            RecordingInjector injector = new();
            PlainInstantiator instantiator = new();

            GameObject layer = LayerInitializer.InstantiateInjected(Template(), container.transform, holder.transform, instantiator, injector);
            objects.Add(layer);

            Assert.AreEqual(1, instantiator.Calls);
            Assert.AreEqual(2, injector.Injected.Count);
            CollectionAssert.AreEqual(new[] { false, false }, injector.ActiveWhenInjected);
        }

        [Test]
        public void ARegisteredInstantiatorOwnsInjection()
        {
            GameObject container = Make("Container");
            RecordingInjector injector = new();
            InjectingInstantiator instantiator = new();

            GameObject layer = LayerInitializer.InstantiateInjected(Template(), container.transform, null, instantiator, injector);
            objects.Add(layer);

            Assert.AreEqual(1, instantiator.Calls);
            Assert.AreEqual(0, injector.Injected.Count, "not injected a second time");
            Assert.AreSame(container.transform, layer.transform.parent);
        }
    }
}

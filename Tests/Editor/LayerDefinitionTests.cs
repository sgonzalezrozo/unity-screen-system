using NUnit.Framework;
using UnityEngine;

namespace ScreenSystem.Tests
{
    public class LayerDefinitionTests
    {
        private GameObject gameObject;

        [SetUp]
        public void SetUp()
            => gameObject = new GameObject("Screen");

        [TearDown]
        public void TearDown()
            => Object.DestroyImmediate(gameObject);

        // Layers listed above SCREEN draw behind it and layers below draw in front, two sort orders apart.
        [Test]
        public void GetLayerZOffset_DependsOnDistanceFromScreen()
        {
            LayerDefinition definition = gameObject.AddComponent<LayerDefinition>();
            definition.AddLayer("FarBackground");
            definition.AddLayer("Background");
            definition.SetScreenSeparatorIndex(2);
            definition.EnsureScreenSeparatorExists();
            definition.AddLayer("TopBar");
            definition.AddLayer("Navigation");

            Assert.AreEqual("SCREEN", definition.GetLayerName(2));
            Assert.AreEqual(-4, definition.GetLayerZOffset(0));
            Assert.AreEqual(-2, definition.GetLayerZOffset(1));
            Assert.AreEqual(0, definition.GetLayerZOffset(2));
            Assert.AreEqual(2, definition.GetLayerZOffset(3));
            Assert.AreEqual(4, definition.GetLayerZOffset(4));
        }

        [Test]
        public void EnsureScreenSeparatorExists_AddsItOnlyOnce()
        {
            LayerDefinition definition = gameObject.AddComponent<LayerDefinition>();
            definition.AddLayer("Background");

            Assert.IsTrue(definition.EnsureScreenSeparatorExists());
            Assert.IsFalse(definition.EnsureScreenSeparatorExists());
            Assert.AreEqual(2, definition.LayerCount);
        }
    }
}

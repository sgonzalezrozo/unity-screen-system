using System.Collections.Generic;
using NUnit.Framework;
using ScreenSystem.Editor;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    // Creates real assets in the project, then removes them and their Addressables entries.
    public class ScreenAuthoringTests
    {
        private const string Root = "Assets/__ScreenAuthoringTest";
        private AddressableAssetSettings settings;

        [SetUp]
        public void SetUp()
        {
            settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || ScreenSystemAssets.LoadRegistry() == null)
                Assert.Ignore("Needs a project with Addressables and Screen System set up.");
        }

        [TearDown]
        public void TearDown()
        {
            if (settings != null)
            {
                foreach (string guid in AssetDatabase.FindAssets("", new[] { Root }))
                    settings.RemoveAssetEntry(guid);
            }

            AssetDatabase.DeleteAsset(Root);
            ScreenRegistrySync.Sync(logChanges: false);
        }

        [Test]
        public void BuildsACompleteScreenFromCode()
        {
            ScreenDefinition screen = ScreenAuthoring.CreateScreen($"{Root}/Quest", "QuestMain");
            GameObject background = ScreenAuthoring.CreateLayer($"{Root}/Shared", "QuestBackground");
            GameObject topBar = ScreenAuthoring.CreateLayer($"{Root}/Shared", "QuestTopBar");
            GameObject glow = ScreenAuthoring.CreateLayer($"{Root}/Shared", "QuestGlow");
            GameObject tooltip = ScreenAuthoring.CreateLayer($"{Root}/Shared", "QuestTooltip");

            Assert.IsTrue(ScreenAuthoring.AddLayer(screen, topBar, LayerPlacement.Front));
            Assert.IsTrue(ScreenAuthoring.AddLayer(screen, background, LayerPlacement.Back));
            Assert.IsTrue(ScreenAuthoring.AddLayer("QuestMain", glow, LayerPlacement.BehindScreen));
            Assert.IsTrue(ScreenAuthoring.AddLayer(screen, tooltip, LayerPlacement.InFrontOfScreen));

            CollectionAssert.AreEqual(
                new[] { "QuestBackground", "QuestGlow", ScreenAuthoring.ScreenEntryName, "QuestTooltip", "QuestTopBar" },
                ScreenAuthoring.GetLayers(screen));

            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(topBar)));
            Assert.IsNotNull(entry, "layers are made loadable");
            Assert.AreEqual("QuestTopBar", entry.address);
            Assert.IsNotNull(topBar.GetComponent<Canvas>());
        }

        [Test]
        public void AddingTheSameLayerTwiceKeepsOneEntry()
        {
            ScreenDefinition screen = ScreenAuthoring.CreateScreen($"{Root}/Quest", "QuestMain");
            GameObject topBar = ScreenAuthoring.CreateLayer($"{Root}/Shared", "QuestTopBar");

            ScreenAuthoring.AddLayer(screen, topBar);
            Assert.IsTrue(ScreenAuthoring.AddLayer(screen, topBar, LayerPlacement.Back));

            CollectionAssert.AreEqual(new[] { ScreenAuthoring.ScreenEntryName, "QuestTopBar" }, ScreenAuthoring.GetLayers(screen));
        }

        [Test]
        public void RemoveLayerTakesItOut()
        {
            ScreenDefinition screen = ScreenAuthoring.CreateScreen($"{Root}/Quest", "QuestMain");
            ScreenAuthoring.AddLayer(screen, ScreenAuthoring.CreateLayer($"{Root}/Shared", "QuestTopBar"));

            Assert.IsTrue(ScreenAuthoring.RemoveLayer(screen, "QuestTopBar"));
            Assert.IsFalse(ScreenAuthoring.RemoveLayer(screen, "QuestTopBar"));
            Assert.IsFalse(ScreenAuthoring.RemoveLayer(screen, ScreenAuthoring.ScreenEntryName), "the screen entry stays");
            CollectionAssert.AreEqual(new[] { ScreenAuthoring.ScreenEntryName }, ScreenAuthoring.GetLayers(screen));
        }

        [Test]
        public void AScreenCantBeItsOwnLayer()
        {
            ScreenDefinition screen = ScreenAuthoring.CreateScreen($"{Root}/Quest", "QuestMain");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenAuthoring.FindScreenPrefabPath(screen));
            using LogCapture log = new();

            Assert.IsFalse(ScreenAuthoring.AddLayer(screen, prefab));
            log.AssertLogged(LogType.Error, "can't be a layer of itself");
        }

        [Test]
        public void CreateScreenTwiceReturnsTheSameScreen()
        {
            ScreenDefinition first = ScreenAuthoring.CreateScreen($"{Root}/Quest", "QuestMain", ScreenType.Modal);

            Assert.AreSame(first, ScreenAuthoring.CreateScreen($"{Root}/Quest", "QuestMain"));
            Assert.AreSame(first, ScreenAuthoring.FindScreen("Quest_QuestMain"));
            Assert.AreEqual(ScreenType.Modal, first.ScreenType);
        }
    }
}

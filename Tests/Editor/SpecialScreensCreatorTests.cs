using System.Collections.Generic;
using NUnit.Framework;
using ScreenSystem.Editor;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ScreenSystem.Tests
{
    // Creates real assets in the project, then removes them and their Addressables entries.
    public class SpecialScreensCreatorTests
    {
        private const string Root = "Assets/__SpecialScreensTest";
        private const string Splash = "TestSplashScreen";
        private const string Quit = "TestQuitConfirmation";
        private AddressableAssetSettings settings;
        private LogCapture log;

        [SetUp]
        public void SetUp()
        {
            settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || ScreenSystemAssets.LoadRegistry() == null)
                Assert.Ignore("Needs a project with Addressables and Screen System set up.");

            log = new LogCapture();
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
            log?.Dispose();
            log = null;
        }

        [Test]
        public void CreatesASplashAndAWiredQuitConfirmation()
        {
            CollectionAssert.AreEqual(new[] { Splash, Quit }, SpecialScreensCreator.Create(Root, Splash, Quit));

            ScreenDefinition splash = ScreenAuthoring.FindScreen(Splash);
            Assert.AreEqual(ScreenType.Fullscreen, splash.ScreenType);
            Assert.IsTrue(splash.IgnoreNavigationRules, "the splash shows over anything while the game boots");

            ScreenDefinition quit = ScreenAuthoring.FindScreen(Quit);
            Assert.AreEqual(ScreenType.Modal, quit.ScreenType);
            Assert.IsNotNull(ScreenSystemAssets.LoadRegistry().GetAllScreens().Find(s => s == quit), "registered");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreenAuthoring.FindScreenPrefabPath(quit));
            QuitConfirmationLayer layer = prefab.GetComponent<QuitConfirmationLayer>();
            Assert.IsNotNull(layer);
            Dictionary<string, string> wiring = new();
            foreach (Button button in prefab.GetComponentsInChildren<Button>(true))
            {
                Assert.AreEqual(1, button.onClick.GetPersistentEventCount(), button.name);
                Assert.AreSame(layer, button.onClick.GetPersistentTarget(0), button.name);
                wiring[button.name] = button.onClick.GetPersistentMethodName(0);
            }

            Assert.AreEqual(nameof(QuitConfirmationLayer.Quit), wiring["Quit"]);
            Assert.AreEqual(nameof(QuitConfirmationLayer.Cancel), wiring["Cancel"]);
        }

        [Test]
        public void RunningItAgainCreatesNothing()
        {
            SpecialScreensCreator.Create(Root, Splash, Quit);
            CollectionAssert.IsEmpty(SpecialScreensCreator.Create(Root, Splash, Quit));
        }

        [Test]
        public void AnEmptyIdIsSkipped()
        {
            CollectionAssert.AreEqual(new[] { Quit }, SpecialScreensCreator.Create(Root, "", Quit));
        }
    }
}

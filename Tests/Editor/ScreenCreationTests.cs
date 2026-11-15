using System.Collections.Generic;
using NUnit.Framework;
using ScreenSystem.Editor;
using ScreenSystem.ScriptableObjects;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace ScreenSystem.Tests
{
    public class ScreenCreationTests
    {
        private readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in created)
                Object.DestroyImmediate(obj);
            created.Clear();
        }

        private ScreenDefinition Screen(string name, string id = null)
        {
            ScreenDefinition definition = ScriptableObject.CreateInstance<ScreenDefinition>();
            definition.ScreenName = name;
            definition.ScreenId = id ?? name;
            created.Add(definition);
            return definition;
        }

        [Test]
        public void MergeKeepsOrderDropsMissingAndAppendsNew()
        {
            ScreenDefinition a = Screen("A"), b = Screen("B"), c = Screen("C"), gone = Screen("Gone");
            List<ScreenDefinition> current = new() { b, null, gone, a, b };

            List<ScreenDefinition> merged = ScreenRegistrySync.Merge(current, new[] { a, b, c }, out int added, out int removed);

            CollectionAssert.AreEqual(new[] { b, a, c }, merged);
            Assert.AreEqual(1, added);
            Assert.AreEqual(3, removed, "the null, the deleted screen and the duplicate");
        }

        [Test]
        public void MergeWithNothingToChangeReportsNoChanges()
        {
            ScreenDefinition a = Screen("A"), b = Screen("B");

            ScreenRegistrySync.Merge(new List<ScreenDefinition> { a, b }, new[] { b, a }, out int added, out int removed);

            Assert.AreEqual(0, added);
            Assert.AreEqual(0, removed);
        }

        [Test]
        public void FindDuplicatesReportsSharedNamesAndIds()
        {
            List<string> problems = ScreenRegistrySync.FindDuplicates(new[]
            {
                Screen("Main", "Shop_Main"), Screen("Main", "Ligas_Main"), Screen("Detail", "Shop_Main"), Screen("Other", "Other")
            });

            Assert.AreEqual(2, problems.Count);
            StringAssert.Contains("'Main'", problems[0]);
            StringAssert.Contains("'Shop_Main'", problems[1]);
        }

        [TestCase("Assets/Client/Shop/Services/ShopMain.asset", "Shop_ShopMain")]
        [TestCase("Assets/Client UGui/Shop/Screens/ShopMain.asset", "Shop_ShopMain")]
        [TestCase("Assets/Client/Shop/ShopMain.asset", "Shop_ShopMain")]
        [TestCase("Assets/My Feature/Services/ShopMain.asset", "MyFeature_ShopMain")]
        [TestCase("", null)]
        public void DefaultScreenIdUsesTheFeatureFolder(string path, string expected)
        {
            Assert.AreEqual(expected, ScreenDefinition.DefaultScreenId(path, "ShopMain"));
        }

        [TestCase("Assets/Resources/Splash/SplashMain.prefab", "Splash/SplashMain")]
        [TestCase("Assets/Client/Shop/Resources/ShopMain.prefab", "ShopMain")]
        [TestCase("Assets/Client/Shop/ShopMain/ShopMain.prefab", null)]
        public void ResourcesPathIsRelativeToTheResourcesFolder(string path, string expected)
        {
            Assert.AreEqual(expected, ScreenSystemAssets.ResourcesPath(path));
        }

        // Creates real assets in the project, then removes them and their Addressables entry.
        [Test]
        public void CreateScreenMakesALoadableRegisteredScreenInTheRulesLayout()
        {
            const string root = "Assets/__ScreenSystemCreationTest";
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            ScreenRegistry registry = ScreenSystemAssets.LoadRegistry();
            if (settings == null || registry == null)
                Assert.Ignore("Needs a project with Addressables and Screen System set up.");

            string feature = $"{root}/Quest";
            string prefabGuid = null;
            try
            {
                ScreenDefinition definition = FeatureCreatorWizard.CreateScreen(feature, "Quest", "QuestMain", ScreenType.modal, UIFramework.uGUI);

                Assert.IsNotNull(definition);
                Assert.AreEqual($"{feature}/Services/QuestMain.asset", AssetDatabase.GetAssetPath(definition));
                Assert.AreEqual("Quest_QuestMain", definition.ScreenId);
                Assert.AreEqual(ScreenType.modal, definition.screenType);

                string prefabPath = $"{feature}/QuestMain/QuestMain.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.IsNotNull(prefab, "prefab in its screen folder");
                Assert.IsNotNull(prefab.GetComponent<Canvas>());
                Assert.IsNotNull(prefab.GetComponent<LayerDefinition>());

                prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
                AddressableAssetEntry entry = settings.FindAssetEntry(prefabGuid);
                Assert.IsNotNull(entry, "prefab marked Addressable");
                Assert.AreEqual("QuestMain", entry.address);
                Assert.AreEqual("QuestMain", definition.AssetPath);

                CollectionAssert.Contains(registry.Screens, definition);
                Assert.IsNull(FeatureCreatorWizard.CreateScreen(feature, "Quest", "QuestMain", ScreenType.modal, UIFramework.uGUI), "existing screens are skipped");
                Assert.AreEqual(feature, FeatureCreatorWizard.FindFeatureFolder($"{feature}/QuestMain"));
                Assert.AreEqual(feature, FeatureCreatorWizard.FindFeatureFolder($"{feature}/Services"));
            }
            finally
            {
                if (prefabGuid != null)
                    settings.RemoveAssetEntry(prefabGuid);
                AssetDatabase.DeleteAsset(root);
                ScreenRegistrySync.Sync(logChanges: false);
            }

            Assert.IsFalse(registry.Screens.Exists(s => s == null), "deleted screen removed from the registry");
        }
    }
}

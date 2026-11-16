using System.Collections.Generic;
using NUnit.Framework;
using ScreenSystem.Editor;
using UnityEngine;

namespace ScreenSystem.Tests
{
    public class ScreenIdsGeneratorTests
    {
        private readonly List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in created)
                Object.DestroyImmediate(obj);
            created.Clear();
        }

        private ScreenDefinition Screen(string name, string id, ScreenType type = ScreenType.fullscreen)
        {
            ScreenDefinition screen = ScriptableObject.CreateInstance<ScreenDefinition>();
            screen.ScreenName = name;
            screen.ScreenId = id;
            screen.screenType = type;
            created.Add(screen);
            return screen;
        }

        [Test]
        public void EachScreenBecomesAConstantNamedAfterItWithItsId()
        {
            string code = ScreenIdsGenerator.Generate(new[] { Screen("ShopMain", "Shop_ShopMain"), Screen("ShopModal", "Shop_ShopModal", ScreenType.modal) });

            StringAssert.Contains("public static class ScreenIds", code);
            StringAssert.Contains("public const string ShopMain = \"Shop_ShopMain\";", code);
            StringAssert.Contains("public const string ShopModal = \"Shop_ShopModal\";", code);
            StringAssert.Contains("/// <summary>modal</summary>", code);
        }

        [Test]
        public void OutputIsStableWhateverTheRegistryOrder()
        {
            ScreenDefinition a = Screen("Alpha", "F_Alpha"), b = Screen("Beta", "F_Beta");

            Assert.AreEqual(ScreenIdsGenerator.Generate(new[] { a, b }), ScreenIdsGenerator.Generate(new[] { b, a }));
        }

        [Test]
        public void DuplicateNamesGetASuffixAndScreensWithoutIdAreSkipped()
        {
            string code = ScreenIdsGenerator.Generate(new[] { Screen("Main", "Shop_Main"), Screen("Main", "Ligas_Main"), Screen("NoId", "") });

            StringAssert.Contains("public const string Main = \"Ligas_Main\";", code);
            StringAssert.Contains("public const string Main_2 = \"Shop_Main\";", code);
            StringAssert.DoesNotContain("NoId", code);
        }

        [TestCase("ShopMain", "ShopMain")]
        [TestCase("Shop Main", "Shop_Main")]
        [TestCase("2Players", "_2Players")]
        [TestCase("class", "@class")]
        [TestCase("Shop-Detail", "Shop_Detail")]
        public void NamesBecomeValidIdentifiers(string name, string expected)
        {
            Assert.AreEqual(expected, ScreenIdsGenerator.Identifier(name));
        }
    }
}

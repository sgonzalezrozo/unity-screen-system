using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    public class ScreenRegistryTests
    {
        [Test]
        public void GetScreen_FindsByIdAndByName()
        {
            using TestRegistry screens = new TestRegistry().Add("Shop");
            ScreenDefinition shop = screens.Registry.Screens[0];
            shop.ScreenId = "Shop_Main";
            screens.Registry.ForceReinitialize();

            Assert.AreSame(shop, screens.Registry.GetScreen("Shop_Main"));
            Assert.AreSame(shop, screens.Registry.GetScreen("Shop"));
            Assert.IsNull(screens.Registry.GetScreen("Missing"));
        }

        [Test]
        public void GetScreen_SkipsScreensWithoutId()
        {
            using TestRegistry screens = new TestRegistry().Add("NoId");
            screens.Registry.Screens[0].ScreenId = "";
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("empty ScreenId"));
            screens.Registry.ForceReinitialize();

            Assert.IsNull(screens.Registry.GetScreen("NoId"));
        }
    }
}

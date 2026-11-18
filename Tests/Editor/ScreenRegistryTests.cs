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
            using LogCapture log = new();
            screens.Registry.ForceReinitialize();
            log.AssertLogged(LogType.Warning, "empty ScreenId");

            Assert.IsNull(screens.Registry.GetScreen("NoId"));
        }

        // With domain reload off, the registry asset keeps its index between Play sessions.
        // ScreenManager rebuilds it on start so screens added in between are found.
        [Test]
        public void ForceReinitializeFindsScreensAddedAfterTheIndexWasBuilt()
        {
            using TestRegistry screens = new TestRegistry().Add("Shop");
            ScreenDefinition added = ScriptableObject.CreateInstance<ScreenDefinition>();
            added.ScreenName = "Added";
            added.ScreenId = "Added";

            try
            {
                screens.Registry.Screens.Add(added);
                screens.Registry.Initialize();
                Assert.IsNull(screens.Registry.GetScreen("Added"), "Initialize keeps the index it already built.");

                screens.Registry.ForceReinitialize();
                Assert.AreSame(added, screens.Registry.GetScreen("Added"));
            }
            finally
            {
                screens.Registry.Screens.Remove(added);
                Object.DestroyImmediate(added);
            }
        }
    }
}

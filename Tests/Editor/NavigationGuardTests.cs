using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    public class NavigationGuardTests
    {
        private TestRegistry screens;
        private FakeScreenDisplay display;
        private ScreenNavigationService service;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            screens = new TestRegistry().Add("Root").Add("Shop").Add("Detail").Add("Lobby");
            display = new FakeScreenDisplay();
            service = new ScreenNavigationService(screens.Registry, display);

            // The fake display completes synchronously, so this doesn't block.
            Assert.IsTrue(service.SetRootAsync("Root").GetAwaiter().GetResult());
        }

        [TearDown]
        public void TearDown()
        {
            screens.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public async Task CallsDuringATransitionAreIgnored()
        {
            display.PendingShow = new TaskCompletionSource<bool>();
            Task<bool> first = service.GoToAsync("Shop");

            Assert.IsTrue(service.IsNavigating);
            Assert.IsFalse(await service.GoToAsync("Detail"), "A second call during the transition returns false.");

            display.PendingShow.SetResult(true);
            Assert.IsTrue(await first);
            Assert.IsFalse(service.IsNavigating);
            Assert.AreEqual("Shop", service.GetCurrentScreen());
        }

        // A scene change must free the guard, so the new scene's SetRoot isn't ignored while an old load finishes.
        [Test]
        public async Task SceneChangeLetsTheNewSceneNavigateWhileAnOldCallFinishes()
        {
            TaskCompletionSource<bool> oldLoad = new();
            display.PendingShow = oldLoad;
            Task<bool> oldCall = service.GoToAsync("Shop");

            service.AbandonRunningOperationForSceneChange();
            Assert.IsFalse(service.IsNavigating);

            display.PendingShow = null;
            Assert.IsTrue(await service.SetRootAsync("Lobby"), "The new scene's call runs.");

            TaskCompletionSource<bool> newLoad = new();
            display.PendingShow = newLoad;
            Task<bool> newCall = service.GoToAsync("Detail");

            oldLoad.SetResult(true);
            await oldCall;
            Assert.IsTrue(service.IsNavigating, "The old call finishing must not release the newer call's guard.");

            newLoad.SetResult(true);
            Assert.IsTrue(await newCall);
            Assert.IsFalse(service.IsNavigating);
        }
    }
}

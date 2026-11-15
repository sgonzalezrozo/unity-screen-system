using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    public class ScreenNavigatorTests
    {
        private TestRegistry screens;
        private FakeScreenDisplay display;
        private ScreenNavigationStack stack;
        private AlertManager alerts;
        private ScreenNavigator navigator;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            screens = new TestRegistry()
                .Add("Root")
                .Add("Shop")
                .Add("Detail")
                .Add("Modal", ScreenType.modal)
                .Add("OtherModal", ScreenType.modal)
                .Add("Alert", ScreenType.alert)
                .Add("Splash", ignoreRules: true);

            display = new FakeScreenDisplay();
            stack = new ScreenNavigationStack();
            alerts = new AlertManager();
            navigator = new ScreenNavigator(screens.Registry, display, stack, alerts);
        }

        [TearDown]
        public void TearDown()
        {
            screens.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }

        private async Task StartAtRoot()
        {
            Assert.IsTrue(await navigator.SetRootAsync("Root"));
            display.Calls.Clear();
        }

        [Test]
        public async Task GoTo_PushesAndShowsTheScreen()
        {
            await StartAtRoot();

            Assert.IsTrue(await navigator.GoToAsync("Shop"));
            Assert.AreEqual("Shop", stack.GetCurrentScreen());
            CollectionAssert.Contains(display.Calls, "show:Shop");
        }

        [Test]
        public async Task GoTo_UnknownScreenReturnsFalse()
        {
            await StartAtRoot();

            Assert.IsFalse(await navigator.GoToAsync("Missing"));
            Assert.AreEqual("Root", stack.GetCurrentScreen());
        }

        [Test]
        public async Task GoBack_ReturnsToThePreviousScreen()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");

            Assert.IsTrue(await navigator.GoBackAsync());
            Assert.AreEqual("Root", stack.GetCurrentScreen());
            CollectionAssert.Contains(display.Calls, "hide:Shop");
        }

        [Test]
        public async Task GoBack_AtTheRootReturnsFalse()
        {
            await StartAtRoot();

            Assert.IsFalse(await navigator.GoBackAsync());
        }

        [Test]
        public async Task Modal_BlocksANewFullscreenScreen()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Modal");

            Assert.IsFalse(await navigator.GoToAsync("Shop"));
            Assert.AreEqual("Modal", stack.GetCurrentModal());
        }

        [Test]
        public async Task Modal_BlocksASecondModal()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Modal");

            Assert.IsFalse(await navigator.GoToAsync("OtherModal"));
            Assert.AreEqual("Modal", stack.GetCurrentModal());
        }

        [Test]
        public async Task GoBack_ClosesTheModalFirst()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Modal");

            Assert.IsTrue(await navigator.GoBackAsync());
            Assert.IsFalse(stack.HasModal);
            Assert.AreEqual("Shop", stack.GetCurrentScreen());
        }

        [Test]
        public async Task GoTo_ScreenInTheStackClosesTheModalAndReturnsToIt()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Modal");

            Assert.IsTrue(await navigator.GoToAsync("Root"));
            Assert.IsFalse(stack.HasModal);
            Assert.AreEqual("Root", stack.GetCurrentScreen());
        }

        [Test]
        public async Task Alert_BlocksNavigationUntilDismissed()
        {
            await StartAtRoot();
            Assert.IsTrue(await navigator.GoToAsync("Alert"));
            Assert.IsTrue(navigator.IsAlertActive);

            Assert.IsFalse(await navigator.GoToAsync("Shop"));
            Assert.IsFalse(await navigator.GoBackAsync());
            Assert.IsFalse(await navigator.PopToAsync("Root"));
            Assert.IsFalse(await navigator.SetRootAsync("Shop"));

            Assert.IsTrue(await navigator.DismissAlertAsync());
            Assert.IsFalse(navigator.IsAlertActive);
            Assert.IsTrue(await navigator.GoToAsync("Shop"));
        }

        [Test]
        public async Task Alert_IsNotAddedToTheStack()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Alert");

            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.AreEqual(1, stack.MainHistoryCount);
        }

        [Test]
        public async Task IgnoreNavigationRules_OpensOverAModalAndGoBackClosesItFirst()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Modal");

            Assert.IsTrue(await navigator.GoToAsync("Splash"));
            Assert.AreEqual("Splash", stack.GetCurrentScreen());
            Assert.IsTrue(stack.HasModal);

            Assert.IsTrue(await navigator.GoBackAsync());
            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.IsTrue(stack.HasModal, "The modal underneath stays open.");
        }

        [Test]
        public async Task PopTo_RemovesEveryScreenAboveTheTarget()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Detail");

            Assert.IsTrue(await navigator.PopToAsync("Root"));
            Assert.AreEqual(1, stack.MainHistoryCount);
            CollectionAssert.Contains(display.Calls, "hide:Detail");
            CollectionAssert.Contains(display.Calls, "hide:Shop");
        }

        [Test]
        public async Task SetRoot_ReplacesTheHistory()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");

            Assert.IsTrue(await navigator.SetRootAsync("Detail"));
            Assert.AreEqual("Detail", stack.GetRootScreen());
            Assert.AreEqual(1, stack.MainHistoryCount);
        }

        // Known bug: GoTo for a screen lower in the stack runs PopTo and then returns false.
        [Test, Category("KnownBug")]
        public async Task GoTo_ScreenLowerInTheStackReturnsTrue()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Detail");

            bool result = await navigator.GoToAsync("Root");

            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.IsTrue(result);
        }
    }
}

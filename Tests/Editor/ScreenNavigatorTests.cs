using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    public class ScreenNavigatorTests
    {
        private LogCapture log;

        private TestRegistry screens;
        private FakeScreenDisplay display;
        private ScreenNavigationStack stack;
        private AlertManager alerts;
        private ScreenNavigator navigator;

        [SetUp]
        public void SetUp()
        {
            log = new LogCapture();
            screens = new TestRegistry()
                .Add("Root")
                .Add("Shop")
                .Add("Detail")
                .Add("Store")
                .Add("Modal", ScreenType.Modal)
                .Add("OtherModal", ScreenType.Modal)
                .Add("Alert", ScreenType.Alert)
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
            log.Dispose();
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

        // GoTo for a screen already in the stack works like PopTo, by design.
        [Test]
        public async Task GoTo_ScreenLowerInTheStackPopsBackToItAndReturnsTrue()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Detail");

            bool result = await navigator.GoToAsync("Root");

            Assert.IsTrue(result);
            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.AreEqual(1, stack.MainHistoryCount, "Shop and Detail are removed from the stack.");
            CollectionAssert.Contains(display.Calls, "hide:Detail");
            CollectionAssert.Contains(display.Calls, "hide:Shop");
        }

        [Test]
        public async Task GoTo_CancelledBeforeTheScreenShowsLeavesTheStackAsItWas()
        {
            await StartAtRoot();
            display.ThrowIfCancelled = true;
            using System.Threading.CancellationTokenSource cts = new();
            cts.Cancel();

            bool cancelled = false;
            try
            {
                await navigator.GoToAsync("Shop", cts.Token);
            }
            catch (System.OperationCanceledException)
            {
                cancelled = true;
            }

            Assert.IsTrue(cancelled);
            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.AreEqual(1, stack.MainHistoryCount);
        }

        [Test]
        public async Task GoTo_CancelledModalIsNotLeftOpen()
        {
            await StartAtRoot();
            display.ThrowIfCancelled = true;
            using System.Threading.CancellationTokenSource cts = new();
            cts.Cancel();

            try
            {
                await navigator.GoToAsync("Modal", cts.Token);
            }
            catch (System.OperationCanceledException)
            {
            }

            Assert.IsFalse(stack.HasModal);
        }

        // A tab bar: from Detail, with Root and Shop below it, switching to Store leaves Root, Store.
        [Test]
        public async Task GoToClearToRoot_RemovesEveryScreenAboveTheRootWithoutShowingIt()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Detail");
            display.Calls.Clear();

            Assert.IsTrue(await navigator.GoToAsync("Store", NavigationHistory.ClearToRoot));

            Assert.AreEqual(2, stack.MainHistoryCount);
            Assert.AreEqual("Store", stack.GetCurrentScreen());
            Assert.AreEqual("Root", stack.GetRootScreen());
            CollectionAssert.Contains(display.Calls, "hide:Detail");
            CollectionAssert.Contains(display.Calls, "hide:Shop");
            CollectionAssert.Contains(display.Calls, "show:Store");
            CollectionAssert.DoesNotContain(display.Calls, "show:Root", "The root isn't drawn on the way.");
        }

        [Test]
        public async Task GoToClearToRoot_BackReturnsToTheRoot()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Store", NavigationHistory.ClearToRoot);

            Assert.IsTrue(await navigator.GoBackAsync());
            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.AreEqual(1, stack.MainHistoryCount);
        }

        [Test]
        public async Task GoToClearToRoot_ClosesAnOpenModal()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Modal");
            display.Instantiated.Add("Modal");

            Assert.IsTrue(await navigator.GoToAsync("Store", NavigationHistory.ClearToRoot));

            Assert.IsFalse(stack.HasModal);
            CollectionAssert.Contains(display.Calls, "hide:Modal");
            Assert.AreEqual(2, stack.MainHistoryCount);
        }

        [Test]
        public async Task GoToClearToRoot_TargetAlreadyInTheStackEndsRightAboveTheRoot()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Store");
            await navigator.GoToAsync("Shop");
            display.Calls.Clear();

            Assert.IsTrue(await navigator.GoToAsync("Store", NavigationHistory.ClearToRoot));

            CollectionAssert.AreEqual(new[] { "Store", "Root" }, stack.GetMainHistorySnapshot());
            CollectionAssert.Contains(display.Calls, "hide:Shop");
            CollectionAssert.DoesNotContain(display.Calls, "hide:Store");
            CollectionAssert.Contains(display.Calls, "show:Store");
        }

        [Test]
        public async Task GoToClearToRoot_ToTheRootPopsBackToIt()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");

            Assert.IsTrue(await navigator.GoToAsync("Root", NavigationHistory.ClearToRoot));
            Assert.AreEqual(1, stack.MainHistoryCount);
            Assert.AreEqual("Root", stack.GetCurrentScreen());
        }

        [Test]
        public async Task GoToClearToRoot_RejectsModalsAndKeepsTheStack()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");

            Assert.IsFalse(await navigator.GoToAsync("Modal", NavigationHistory.ClearToRoot));
            log.AssertLogged(UnityEngine.LogType.Error, "only fullscreen screens can clear the stack");
            Assert.AreEqual(2, stack.MainHistoryCount);
            Assert.IsFalse(stack.HasModal);
        }

        [Test]
        public async Task GoToClearToRoot_OnAnEmptyStackOpensTheScreen()
        {
            Assert.IsTrue(await navigator.GoToAsync("Store", NavigationHistory.ClearToRoot));
            Assert.AreEqual("Store", stack.GetCurrentScreen());
        }

        // Migrating from hand-made UI: CloseAll hands the screen back to it.
        [Test]
        public async Task CloseAll_HidesEveryScreenAndEmptiesTheStack()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Modal");
            display.Calls.Clear();

            Assert.IsTrue(await navigator.CloseAllAsync());

            Assert.AreEqual(0, stack.MainHistoryCount);
            Assert.IsFalse(stack.HasModal);
            Assert.IsNull(stack.GetRootScreen());
            CollectionAssert.IsSupersetOf(display.Calls, new[] { "hide:Modal", "hide:Shop", "hide:Root" });
        }

        [Test]
        public async Task CloseAll_ClosesAnOpenAlert()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Alert");

            Assert.IsTrue(await navigator.CloseAllAsync());

            Assert.IsFalse(navigator.IsAlertActive);
            Assert.IsFalse(stack.HasAlert);
            CollectionAssert.Contains(display.Calls, "hide:Alert");
        }

        [Test]
        public async Task CloseAll_ThenGoToOpensTheScreenAgain()
        {
            await StartAtRoot();
            await navigator.CloseAllAsync();

            Assert.IsTrue(await navigator.GoToAsync("Shop"));
            Assert.AreEqual("Shop", stack.GetCurrentScreen());
            Assert.AreEqual(1, stack.MainHistoryCount);
        }
    }
}

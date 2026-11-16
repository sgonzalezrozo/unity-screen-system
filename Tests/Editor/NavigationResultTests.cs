using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    public class NavigationResultTests
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
            screens = new TestRegistry().Add("Root").Add("Shop").Add("Detail").Add("Modal", ScreenType.modal).Add("Alert", ScreenType.alert);
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
        public void ConvertsToBoolAndSaysWhy()
        {
            NavigationResult ok = new(NavigationStatus.Succeeded, "Shop");
            NavigationResult blocked = new(NavigationStatus.BlockedByModal, "Shop");

            Assert.IsTrue(ok);
            Assert.IsFalse(blocked);
            Assert.IsTrue(!blocked);
            Assert.AreEqual("BlockedByModal (Shop)", blocked.ToString());
        }

        // Before, a show that failed still returned true and left the screen in the stack.
        [Test]
        public async Task AShowThatFailsReturnsFailedAndUndoesThePush()
        {
            await StartAtRoot();
            display.FailShow.Add("Shop");

            Assert.IsFalse(await navigator.GoToAsync("Shop"));

            Assert.AreEqual(NavigationStatus.Failed, navigator.LastStatus);
            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.AreEqual(1, stack.MainHistoryCount);
        }

        [Test]
        public async Task AModalThatFailsToShowIsNotLeftOpen()
        {
            await StartAtRoot();
            display.FailShow.Add("Modal");

            Assert.IsFalse(await navigator.GoToAsync("Modal"));
            Assert.IsFalse(stack.HasModal);
        }

        [Test]
        public async Task AnAlertThatFailsToShowDoesNotBlockNavigation()
        {
            await StartAtRoot();
            display.FailShow.Add("Alert");

            Assert.IsFalse(await navigator.GoToAsync("Alert"));

            Assert.IsFalse(navigator.IsAlertActive);
            Assert.IsTrue(await navigator.GoToAsync("Shop"), "navigation still works");
        }

        [Test]
        public async Task EachRefusalHasItsReason()
        {
            await StartAtRoot();

            Assert.IsFalse(await navigator.GoToAsync("Missing"));
            Assert.AreEqual(NavigationStatus.NotFound, navigator.LastStatus);

            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("only fullscreen screens can clear the stack"));
            Assert.IsFalse(await navigator.GoToAsync("Modal", NavigationHistory.ClearToRoot));
            Assert.AreEqual(NavigationStatus.InvalidRequest, navigator.LastStatus);

            Assert.IsFalse(await navigator.DismissAlertAsync());
            Assert.AreEqual(NavigationStatus.NothingToClose, navigator.LastStatus);

            Assert.IsFalse(await navigator.GoBackAsync());
            Assert.AreEqual(NavigationStatus.NothingToClose, navigator.LastStatus, "back at the root");

            await navigator.GoToAsync("Modal");
            Assert.IsFalse(await navigator.GoToAsync("Shop"));
            Assert.AreEqual(NavigationStatus.BlockedByModal, navigator.LastStatus);
            await navigator.GoBackAsync();

            await navigator.GoToAsync("Alert");
            Assert.IsFalse(await navigator.GoToAsync("Shop"));
            Assert.AreEqual(NavigationStatus.BlockedByAlert, navigator.LastStatus);
        }

        [Test]
        public async Task SuccessResetsTheReason()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Missing");

            Assert.IsTrue(await navigator.GoToAsync("Shop"));
            Assert.AreEqual(NavigationStatus.Succeeded, navigator.LastStatus);
        }

        [Test]
        public async Task ACallDuringAnotherIsReportedAsTransitioning()
        {
            ScreenNavigationService service = new(screens.Registry, display);
            await service.SetRootAsync("Root");
            display.PendingShow = new TaskCompletionSource<bool>();

            Task<bool> running = service.GoToAsync("Shop");
            Assert.IsFalse(await service.GoToAsync("Detail"));
            Assert.AreEqual(NavigationStatus.Transitioning, service.LastStatus);

            display.PendingShow.SetResult(true);
            Assert.IsTrue(await running);
            Assert.AreEqual(NavigationStatus.Succeeded, service.LastStatus);
        }
    }
}

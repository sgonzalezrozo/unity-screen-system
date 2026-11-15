using NUnit.Framework;

namespace ScreenSystem.Tests
{
    public class NavigationStackTests
    {
        [Test]
        public void SetRoot_ClearsHistoryAndBecomesCurrent()
        {
            ScreenNavigationStack stack = new();
            stack.PushMainScreen("A");
            stack.SetRootScreen("Root");

            Assert.AreEqual("Root", stack.GetCurrentScreen());
            Assert.AreEqual("Root", stack.GetRootScreen());
            Assert.AreEqual(1, stack.MainHistoryCount);
            Assert.IsFalse(stack.MainHistoryContains("A"));
        }

        [Test]
        public void PopMainScreen_NeverPopsTheRoot()
        {
            ScreenNavigationStack stack = new();
            stack.SetRootScreen("Root");

            Assert.IsNull(stack.PopMainScreen());
            Assert.AreEqual("Root", stack.GetCurrentScreen());
        }

        [Test]
        public void CanGoBack_IsTrueWithAModalOverTheRoot()
        {
            ScreenNavigationStack stack = new();
            stack.SetRootScreen("Root");
            Assert.IsFalse(stack.CanGoBack);

            stack.ShowModalScreen("Modal");
            Assert.IsTrue(stack.CanGoBack);
        }

        [Test]
        public void ClearNonPersistentMainScreens_KeepsOnlyPersistentScreens()
        {
            using TestRegistry screens = new TestRegistry()
                .Add("Root")
                .Add("Loading", persist: true)
                .Add("Shop");

            ScreenNavigationStack stack = new();
            stack.SetRootScreen("Root");
            stack.PushMainScreen("Loading");
            stack.PushMainScreen("Shop");

            stack.ClearNonPersistentMainScreens(screens.Registry);

            Assert.AreEqual(1, stack.MainHistoryCount);
            Assert.AreEqual("Loading", stack.GetCurrentScreen());
            Assert.IsFalse(stack.MainHistoryContains("Shop"));
        }

        [Test]
        public void ClearNonPersistentMainScreens_KeepsTheOrderOfPersistentScreens()
        {
            using TestRegistry screens = new TestRegistry()
                .Add("Bottom", persist: true)
                .Add("Top", persist: true);

            ScreenNavigationStack stack = new();
            stack.SetRootScreen("Bottom");
            stack.PushMainScreen("Top");

            stack.ClearNonPersistentMainScreens(screens.Registry);

            Assert.AreEqual("Top", stack.GetCurrentScreen());
        }

        [Test]
        public void ClearNonPersistentMainScreens_KeepsTheSpecialScreens()
        {
            using TestRegistry screens = new TestRegistry()
                .Add("Root")
                .Add("SplashMain")
                .Add("Shop");

            ScreenNavigationStack stack = new();
            stack.SetRootScreen("Root");
            stack.PushMainScreen("SplashMain");
            stack.PushMainScreen("Shop");

            stack.ClearNonPersistentMainScreens(screens.Registry, new[] { "SplashMain" });

            Assert.AreEqual(1, stack.MainHistoryCount);
            Assert.AreEqual("SplashMain", stack.GetCurrentScreen());
        }
    }
}

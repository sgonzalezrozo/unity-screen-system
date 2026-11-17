using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ScreenSystem.Services;

namespace ScreenSystem.Tests
{
    public class QuitManagerTests
    {
        private sealed class FakeNavigation : IScreenNavigation
        {
            public bool OpenSucceeds = true;
            public readonly List<string> Opened = new();

            public Task<bool> GoToAsync(string screenId, CancellationToken ct = default)
            {
                Opened.Add(screenId);
                return Task.FromResult(OpenSucceeds);
            }

            public Task<bool> GoToAsync(string screenId, NavigationHistory history, CancellationToken ct = default) => GoToAsync(screenId, ct);
            public Task<bool> GoToAsync(string screenId, NavigationHistory history, ScreenParameter parameter, CancellationToken ct = default) => GoToAsync(screenId, ct);
            public Task<bool> GoBackAsync(CancellationToken ct = default) => Task.FromResult(false);
            public Task<bool> PopToAsync(string screenId, CancellationToken ct = default) => Task.FromResult(false);
            public Task<bool> SetRootAsync(string screenId, CancellationToken ct = default) => Task.FromResult(false);
            public Task<bool> DismissAlertAsync(CancellationToken ct = default) => Task.FromResult(false);
            public Task<bool> CloseAllAsync(CancellationToken ct = default) => Task.FromResult(false);
            public bool CanGoBack => false;
            public bool IsAlertActive => false;
            public string GetCurrentScreen() => null;
            public string GetRootScreen() => null;
        }

        private FakeNavigation navigation;
        private int quits;
        private int quitRequests;

        [SetUp]
        public void SetUp()
        {
            navigation = new FakeNavigation();
            quits = 0;
            quitRequests = 0;
            QuitManager.QuitOverride = () => quits++;
            NavigationEvents.OnQuitApplicationRequested += CountRequest;
        }

        [TearDown]
        public void TearDown()
        {
            QuitManager.QuitOverride = null;
            NavigationEvents.OnQuitApplicationRequested -= CountRequest;
        }

        private void CountRequest() => quitRequests++;

        [Test]
        public async Task BackAtTheRootOpensTheConfirmationInsteadOfQuitting()
        {
            await new QuitManager(navigation, "QuitConfirmation").HandleQuitRequestAsync();

            CollectionAssert.AreEqual(new[] { "QuitConfirmation" }, navigation.Opened);
            Assert.AreEqual(0, quits);
            Assert.AreEqual(0, quitRequests);
        }

        [Test]
        public async Task WithoutTheConfirmationScreenBackAtTheRootQuits()
        {
            navigation.OpenSucceeds = false;
            await new QuitManager(navigation, "QuitConfirmation").HandleQuitRequestAsync();

            Assert.AreEqual(1, quits);
            Assert.AreEqual(1, quitRequests);
        }

        [Test]
        public async Task WithNoConfirmationIdBackAtTheRootQuits()
        {
            await new QuitManager(navigation, "").HandleQuitRequestAsync();

            Assert.IsEmpty(navigation.Opened);
            Assert.AreEqual(1, quits);
        }

        [Test]
        public void TheQuitButtonRaisesQuitRequestedAndQuits()
        {
            UnityEngine.GameObject go = new("Quit");
            try
            {
                go.AddComponent<QuitConfirmationLayer>().Quit();
                Assert.AreEqual(1, quitRequests);
                Assert.AreEqual(1, quits);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}

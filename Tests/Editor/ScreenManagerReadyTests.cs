using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ScreenSystem.Tests
{
    public class ScreenManagerReadyTests
    {
        [SetUp]
        public void SetUp() => ScreenManager.ResetStatics();

        [TearDown]
        public void TearDown() => ScreenManager.ResetStatics();

        [Test]
        public void WaitsUntilTheManagerIsReady()
        {
            Task<ScreenManager> ready = ScreenManager.WhenReadyAsync();

            Assert.IsFalse(ready.IsCompleted, "nothing to poll: the task waits");
        }

        [Test]
        public async Task CallsBeforeAndAfterReadyGetTheSameAnswer()
        {
            Task<ScreenManager> before = ScreenManager.WhenReadyAsync();
            ScreenManager.ReportNotAvailable();

            Assert.IsNull(await before);
            Assert.IsNull(await ScreenManager.WhenReadyAsync(), "a late caller doesn't wait forever");
        }

        [Test]
        public async Task OnlyTheFirstReportCounts()
        {
            Task<ScreenManager> ready = ScreenManager.WhenReadyAsync();
            ScreenManager.ReportNotAvailable();
            ScreenManager.ReportReady(null);

            Assert.IsNull(await ready);
        }

        [Test]
        public async Task CancellingStopsWaiting()
        {
            using CancellationTokenSource cts = new();
            Task<ScreenManager> ready = ScreenManager.WhenReadyAsync(cts.Token);

            cts.Cancel();

            // Awaited, not blocked on: the continuation runs on the main thread.
            bool cancelled = false;
            try
            {
                await ready;
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            Assert.IsTrue(cancelled);
        }

        // With domain reload off, a new Play session must not see the previous session's answer.
        [Test]
        public void ANewPlaySessionStartsWaitingAgain()
        {
            ScreenManager.ReportNotAvailable();
            ScreenManager.ResetStatics();

            Assert.IsFalse(ScreenManager.WhenReadyAsync().IsCompleted);
        }
    }
}

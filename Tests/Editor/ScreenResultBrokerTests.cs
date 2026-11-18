using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    public class ScreenResultBrokerTests
    {
        private ScreenResultBroker broker;

        [SetUp]
        public void SetUp() => broker = new ScreenResultBroker();

        [Test]
        public async Task AStagedValueIsReturnedOnceTheModalCloses()
        {
            ScreenResultBroker.Pending<bool> pending = broker.Begin<bool>("Confirm", false, out _);
            broker.Settle("Confirm", null);

            broker.Stage(true, alertOnTop: false);
            Assert.IsFalse(pending.Task.IsCompleted, "not before the modal has closed");

            broker.Settle(null, null);
            ModalResult<bool> result = await pending.Task;

            Assert.AreEqual(ModalResultStatus.Returned, result.Status);
            Assert.IsTrue(result.Value);
        }

        [Test]
        public async Task ClosingWithoutAValueIsDismissed()
        {
            ScreenResultBroker.Pending<int> pending = broker.Begin<int>("Picker", false, out _);

            broker.Settle(null, null); // back, CloseAll, a GoTo that closed it, or a scene change

            ModalResult<int> result = await pending.Task;
            Assert.AreEqual(ModalResultStatus.Dismissed, result.Status);
            Assert.AreEqual(0, result.Value);
        }

        [Test]
        public async Task AValueOfAnotherTypeIsDismissedAndLogged()
        {
            ScreenResultBroker.Pending<bool> pending = broker.Begin<bool>("Confirm", false, out _);
            using LogCapture log = new();

            Assert.IsNull(broker.Stage("yes", alertOnTop: false));
            log.AssertLogged(LogType.Error, "waits for another type");
            broker.Settle(null, null);

            Assert.AreEqual(ModalResultStatus.Dismissed, (await pending.Task).Status);
        }

        [Test]
        public async Task AnUnstagedValueIsntReturnedByALaterClose()
        {
            ScreenResultBroker.Pending<bool> pending = broker.Begin<bool>("Confirm", false, out _);

            broker.Stage(true, alertOnTop: false).Unstage(); // the close was ignored
            broker.Settle(null, null);

            Assert.AreEqual(ModalResultStatus.Dismissed, (await pending.Task).Status);
        }

        [Test]
        public async Task AnAlertOverAModalAnswersFirstAndTheModalKeepsWaiting()
        {
            ScreenResultBroker.Pending<string> modal = broker.Begin<string>("Picker", false, out _);
            ScreenResultBroker.Pending<bool> alert = broker.Begin<bool>("Retry", true, out _);

            broker.Stage(true, alertOnTop: true);
            broker.Settle("Picker", null); // the alert closed, the modal is still open

            Assert.IsTrue((await alert.Task).Value);
            Assert.IsFalse(modal.Task.IsCompleted);

            broker.Stage("sword", alertOnTop: false);
            broker.Settle(null, null);
            Assert.AreEqual("sword", (await modal.Task).Value);
        }

        [Test]
        public void ANullValueIsReturnedForAReferenceType()
        {
            ScreenResultBroker.Pending<string> pending = broker.Begin<string>("Picker", false, out _);

            Assert.IsNotNull(broker.Stage<string>(null, alertOnTop: false));
            broker.Settle(null, null);

            Assert.AreEqual(ModalResultStatus.Returned, pending.Task.Result.Status);
        }

        [Test]
        public void AFailedOpenPutsThePreviousPendingResultBack()
        {
            ScreenResultBroker.Pending<bool> first = broker.Begin<bool>("Confirm", false, out _);
            ScreenResultBroker.Pending<bool> second = broker.Begin<bool>("Other", false, out ScreenResultBroker.IPending previous);

            broker.Settle("Confirm", null); // the second one was blocked: Confirm is still the open modal
            broker.Restore(second, previous, false);

            Assert.IsTrue(broker.HasPendingModal);
            Assert.IsFalse(first.Task.IsCompleted, "the first caller keeps waiting for Confirm");
        }

        [Test]
        public void CompleteAllLeavesNobodyWaiting()
        {
            ScreenResultBroker.Pending<bool> modal = broker.Begin<bool>("Confirm", false, out _);
            ScreenResultBroker.Pending<bool> alert = broker.Begin<bool>("Retry", true, out _);

            broker.CompleteAll();

            Assert.IsTrue(modal.Task.IsCompleted && alert.Task.IsCompleted);
            Assert.IsFalse(broker.HasPendingModal || broker.HasPendingAlert);
        }
    }
}

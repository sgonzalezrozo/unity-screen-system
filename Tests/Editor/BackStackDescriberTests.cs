using System.Collections.Generic;
using NUnit.Framework;
using ScreenSystem.Editor.ScreenManagerEditor;

namespace ScreenSystem.Tests
{
    public class BackStackDescriberTests
    {
        private static readonly string[] History = { "Detail", "Shop", "Home" };

        [Test]
        public void EachScreenReturnsToTheOneBelowAndTheRootOpensTheQuitModal()
        {
            List<BackStep> steps = BackStackDescriber.Describe(History, null, null, true, "QuitConfirmation");

            Assert.AreEqual(3, steps.Count);
            Assert.AreEqual("Back returns to Shop", steps[0].Action);
            Assert.AreEqual("Back returns to Home", steps[1].Action);
            Assert.AreEqual(BackStepKind.Root, steps[2].Kind);
            Assert.AreEqual("Back opens QuitConfirmation", steps[2].Action);
            Assert.AreEqual("Detail: Back returns to Shop", BackStackDescriber.NextBack(steps));
        }

        [Test]
        public void AnAlertBlocksBackAndAModalClosesFirst()
        {
            List<BackStep> steps = BackStackDescriber.Describe(History, "ShopModal", "ConnectionLost", true, null);

            Assert.AreEqual(BackStepKind.Alert, steps[0].Kind);
            StringAssert.Contains("DismissAlert()", steps[0].Action);
            Assert.AreEqual(BackStepKind.Modal, steps[1].Kind);
            Assert.AreEqual("Back closes it", steps[1].Action);
            Assert.AreEqual("Detail", steps[2].Screen);
        }

        [Test]
        public void TheRootQuitsWithoutAQuitModalOrRaisesTheEventWhenQuittingIsOff()
        {
            Assert.AreEqual("Back quits the app (no QuitConfirmation modal)", BackStackDescriber.Describe(new[] { "Home" }, null, null, true, null)[0].Action);
            StringAssert.Contains("OnBackAtRoot", BackStackDescriber.Describe(new[] { "Home" }, null, null, false, "QuitConfirmation")[0].Action);
        }

        [Test]
        public void NothingOpenLeavesBackToYourOwnUI()
        {
            List<BackStep> steps = BackStackDescriber.Describe(new List<string>(), null, null, true, null);

            Assert.AreEqual(0, steps.Count);
            Assert.AreEqual(BackStackDescriber.NothingOpen, BackStackDescriber.NextBack(steps));
        }
    }
}

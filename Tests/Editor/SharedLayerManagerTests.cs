using NUnit.Framework;

namespace ScreenSystem.Tests
{
    // A shared layer (a TopBar used by several screens) keeps one assignment per screen. They must not pile up.
    public class SharedLayerManagerTests
    {
        [Test]
        public void AssigningTheSameScreenAgainDoesNotAddAnAssignment()
        {
            SharedLayerManager manager = new();

            for (int i = 0; i < 100; i++)
                manager.AssignToScreen("Shop", 10 + i, 0);

            Assert.AreEqual(1, manager.AssignmentCount);
            Assert.AreEqual(109, manager.CurrentZOrder, "The latest z order wins.");
        }

        [Test]
        public void TheTopmostScreenSetsTheDrawOrder()
        {
            SharedLayerManager manager = new();
            manager.AssignToScreen("Shop", 10, 0);
            manager.AssignToScreen("Detail", 30, 0);

            Assert.AreEqual("Detail", manager.CurrentScreenId);
            Assert.AreEqual(30, manager.CurrentZOrder);

            manager.UnassignFromScreen("Detail");
            Assert.AreEqual("Shop", manager.CurrentScreenId);
            Assert.AreEqual(10, manager.CurrentZOrder);
        }

        [Test]
        public void UnassigningEveryScreenLeavesNothing()
        {
            SharedLayerManager manager = new();
            manager.AssignToScreen("Shop", 10, 0);
            manager.AssignToScreen("Detail", 30, 0);

            manager.UnassignFromScreen("Shop");
            manager.UnassignFromScreen("Detail");
            manager.UnassignFromScreen("Missing");

            Assert.AreEqual(0, manager.AssignmentCount);
            Assert.IsFalse(manager.IsBeingUsed);
            Assert.IsNull(manager.CurrentScreenId);
        }
    }
}

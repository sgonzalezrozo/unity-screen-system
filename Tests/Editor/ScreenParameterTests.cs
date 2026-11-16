using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ScreenSystem.Tests
{
    public class ScreenParameterTests
    {
        private readonly List<GameObject> objects = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in objects)
                Object.DestroyImmediate(go);
            objects.Clear();
            LogAssert.ignoreFailingMessages = false;
        }

        private LayerComponent Layer(string name, out ParameterReceiverStub receiver, bool receiverOnChild = false)
        {
            GameObject root = new(name);
            objects.Add(root);
            LayerComponent layer = root.AddComponent<LayerComponent>();
            GameObject host = root;
            if (receiverOnChild)
            {
                host = new GameObject("Child");
                host.transform.SetParent(root.transform);
                host.SetActive(false);
            }

            receiver = host.AddComponent<ParameterReceiverStub>();
            return layer;
        }

        [Test]
        public void EveryReceiverOnEveryLayerGetsTheValueOnce()
        {
            LayerComponent background = Layer("Background", out ParameterReceiverStub first);
            LayerComponent list = Layer("List", out ParameterReceiverStub second, receiverOnChild: true);

            int received = ScreenParameter.Create("shop-args").DeliverTo(new[] { background, list });

            Assert.AreEqual(2, received);
            CollectionAssert.AreEqual(new object[] { "shop-args" }, first.Received);
            CollectionAssert.AreEqual(new object[] { "shop-args" }, second.Received, "inactive children receive it too");
        }

        [Test]
        public void OnlyReceiversOfTheValueTypeGetIt()
        {
            LayerComponent layer = Layer("Layer", out ParameterReceiverStub receiver);
            GameObject other = new("NoReceiver");
            objects.Add(other);
            LayerComponent empty = other.AddComponent<LayerComponent>();

            Assert.AreEqual(1, ScreenParameter.Create(40).DeliverTo(new[] { layer, empty }));
            Assert.AreEqual(0, ScreenParameter.Create(1.5f).DeliverTo(new[] { layer, empty }));
            CollectionAssert.AreEqual(new object[] { 40 }, receiver.Received);
        }

        [Test]
        public void NoReceiverForTheTypeLogsAWarning()
        {
            LayerComponent layer = Layer("Layer", out _);
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("no component on its layers implements IScreenParameter<Single>"));

            ScreenParameter.Create(1.5f).DeliverAndReport("Shop", new[] { layer });
        }
    }

    public class ScreenParameterNavigationTests
    {
        private TestRegistry screens;
        private FakeScreenDisplay display;
        private ScreenNavigationStack stack;
        private ScreenNavigator navigator;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            screens = new TestRegistry().Add("Root").Add("Shop").Add("Detail").Add("Modal", ScreenType.Modal);
            display = new FakeScreenDisplay();
            stack = new ScreenNavigationStack();
            navigator = new ScreenNavigator(screens.Registry, display, stack, new AlertManager());
        }

        [TearDown]
        public void TearDown()
        {
            screens.Dispose();
            LogAssert.ignoreFailingMessages = false;
        }

        private static ScreenParameter Value() => ScreenParameter.Create("value");

        private async Task StartAtRoot()
        {
            Assert.IsTrue(await navigator.SetRootAsync("Root"));
            display.Calls.Clear();
        }

        [Test]
        public async Task GoToDeliversTheValueBeforeTheScreenShowsAndDropsIt()
        {
            await StartAtRoot();

            Assert.IsTrue(await navigator.GoToAsync("Shop", NavigationHistory.Push, Value()));

            CollectionAssert.AreEqual(new[] { "param:Shop", "show:Shop" }, display.Calls);
            Assert.IsNull(display.PendingParameter, "not kept after the call");
        }

        [Test]
        public async Task BackDoesNotDeliverItAgain()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop", NavigationHistory.Push, Value());
            await navigator.GoToAsync("Detail");
            display.Calls.Clear();

            await navigator.GoBackAsync();

            CollectionAssert.Contains(display.Calls, "show:Shop");
            CollectionAssert.DoesNotContain(display.Calls, "param:Shop");
        }

        [Test]
        public async Task ABlockedGoToDoesNotKeepTheValueForALaterShow()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Modal");

            Assert.IsFalse(await navigator.GoToAsync("Shop", NavigationHistory.Push, Value()));
            Assert.IsNull(display.PendingParameter);

            await navigator.GoBackAsync();
            await navigator.GoToAsync("Shop");
            CollectionAssert.DoesNotContain(display.Calls, "param:Shop");
        }

        [Test]
        public async Task GoToAScreenLowerInTheStackDeliversItWhenItShowsAgain()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");
            await navigator.GoToAsync("Detail");
            display.Calls.Clear();

            Assert.IsTrue(await navigator.GoToAsync("Shop", NavigationHistory.Push, Value()));

            CollectionAssert.Contains(display.Calls, "param:Shop");
            Assert.AreEqual("Shop", stack.GetCurrentScreen());
        }

        [Test]
        public async Task ClearToRootDeliversIt()
        {
            await StartAtRoot();
            await navigator.GoToAsync("Shop");

            Assert.IsTrue(await navigator.GoToAsync("Detail", NavigationHistory.ClearToRoot, Value()));

            CollectionAssert.Contains(display.Calls, "param:Detail");
            Assert.AreEqual(2, stack.MainHistoryCount);
        }

        [Test]
        public async Task ModalGetsIt()
        {
            await StartAtRoot();

            Assert.IsTrue(await navigator.GoToAsync("Modal", NavigationHistory.Push, Value()));

            CollectionAssert.AreEqual(new[] { "param:Modal", "show:Modal" }, display.Calls);
        }
    }
}

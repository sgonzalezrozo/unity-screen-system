# Code Examples

[← Back to README](../README.md)

Copy-paste recipes for everyday tasks. All snippets assume `using ScreenSystem;`.

**Navigation**
- [Connect buttons to screens](#connect-buttons-to-screens)
- [Check the result](#check-the-result)
- [Change scenes](#change-scenes)
- [Switch tabs from a bottom bar](#switch-tabs-from-a-bottom-bar)
- [Handle back at the root](#handle-back-at-the-root)
- [Keep a screen across scenes](#keep-a-screen-across-scenes)

**Modals and alerts**
- [Open a modal](#open-a-modal)
- [Get an answer from a modal](#get-an-answer-from-a-modal)
- [Show an alert](#show-an-alert)
- [One modal and one alert at a time](#one-modal-and-one-alert-at-a-time)
- [Show a screen over a modal or alert](#show-a-screen-over-a-modal-or-alert)

**Screen content**
- [Pass a value to a screen](#pass-a-value-to-a-screen)
- [Inject services into a screen](#inject-services-into-a-screen)
- [Animate a screen](#animate-a-screen)

---

## Connect buttons to screens

Hook these methods up to UI buttons. `Forget` runs the navigation without waiting for it and logs any exception.

```csharp
using ScreenSystem;
using UnityEngine;

public class ShopMainScreen : MonoBehaviour
{
    public void OpenDetail() => ScreenManager.Instance.GoTo("ShopDetail").Forget("Open detail");
    public void OpenModal() => ScreenManager.Instance.GoTo("ShopModal").Forget("Open modal");
    public void Back() => ScreenManager.Instance.GoBack().Forget("Back");
}
```

`ShopModal` opens as a modal because its `ScreenDefinition` type is `Modal`.

## Check the result

```csharp
bool opened = await ScreenManager.Instance.GoTo("ShopDetail");

if (!opened)
    Debug.LogWarning("ShopDetail didn't open.");
```

For the reason a call didn't happen, read `NavigationResult.Status`. See [API Reference › NavigationResult](api-reference.md#navigationresult).

## Change scenes

Load scenes as usual. Screen System cleans up automatically when a new scene loads in single mode, so you don't need to call anything first.

```csharp
using UnityEngine.SceneManagement;

SceneManager.LoadScene("MainMenu");
```

Additive loads don't change your screens.

## Switch tabs from a bottom bar

Make the bottom bar a layer that each tab screen lists, so it's shared and stays on screen. Its buttons clear the history above the root, so back from any tab returns home instead of walking through the tabs:

```csharp
public async void OnStoreTab()
{
    // From Inventory with A, B, C open, this leaves Home, Store.
    await ScreenManager.Instance.GoTo("Store", NavigationHistory.ClearToRoot);
}
```

Plain `GoTo("Store")` keeps the history instead: back from Store returns to C. Each tab doesn't keep its own back stack.

## Handle back at the root

`GoBack()` and the Escape key close an open modal first, then step back through the stack. At the root, Screen System opens a modal called `QuitConfirmation`. If that screen doesn't exist, the app quits straight away, and Play Mode stops in the Editor. With **Quit On Back At Root** off in `ScreenSystemSettings`, back at the root only raises `NavigationEvents.OnBackAtRoot`. With no screen open, back does nothing.

To confirm before quitting, create a modal screen named `QuitConfirmation` (Setup can [create one for you](getting-started.md#the-two-special-screens)). You can change that name with **Quit Confirmation Modal Id** on the `ScreenManager`.

To react to the app quitting:

```csharp
NavigationEvents.OnQuitApplicationRequested += SaveGame;
```

## Keep a screen across scenes

Turn on **Persist Over Scenes** on the `ScreenDefinition`. This suits screens such as a loading overlay. The splash screen (**Splash Screen Id** on the `ScreenManager`) and the quit confirmation are kept across scenes without it.

When a scene loads in single mode, a persistent screen keeps its layers and its visibility, but the navigation history is cleared. A persistent screen that's showing when the scene loads, such as a splash, stays up until the new scene opens its first screen.

## Open a modal

Set the `ScreenDefinition` type to `Modal`, then use the same navigation calls:

```csharp
await ScreenManager.Instance.GoTo("ShopModal");
await ScreenManager.Instance.GoBack(); // Closes the modal.
```

- One modal is open at a time. Calling `GoTo` for another new screen fails until the modal closes. See [One modal and one alert at a time](#one-modal-and-one-alert-at-a-time).
- Calling `GoTo` for a screen already in the stack closes the modal and returns to that screen.

## Get an answer from a modal

For a confirmation or a picker, open the modal with `GoToForResult` and await its answer:

```csharp
ModalResult<bool> answer = await ScreenManager.Instance.GoToForResult<bool>(ScreenIds.ConfirmPurchase);
if (answer.Returned && answer.Value)
    Buy();

// With a value for the modal's IScreenParameter<T> components:
ModalResult<bool> answer = await ScreenManager.Instance.GoToForResult<ProductRequest, bool>(
    ScreenIds.ConfirmPurchase, new ProductRequest("sword_01"));
```

The modal answers and closes itself with `CloseWithResult`:

```csharp
public void OnYes() => ScreenManager.Instance.CloseWithResult(true);
public void OnNo() => ScreenManager.Instance.CloseWithResult(false);
```

| `answer.Status` | When |
|---|---|
| `Returned` | The modal called `CloseWithResult(value)`; `answer.Value` holds it |
| `Dismissed` | It closed without a value: back, `CloseAll()`, a `GoTo` that closed it, a scene change, or a value of another type (logged as an error) |
| `NotOpened` | It didn't open; `answer.Navigation` says why, such as `BlockedByModal` |

- **Completes after the modal closes:** the caller resumes once the modal is gone, so its next `GoTo` isn't refused as `Transitioning`.
- **Always completes:** whatever closes the modal, nobody is left waiting.
- **Modals and alerts:** an alert such as "Connection lost: Retry / Quit" can answer too. An alert over a modal answers first, and the modal keeps waiting for its own answer.
- **Cancellation:** cancelling the token passed to `GoToForResult` closes the modal and throws `OperationCanceledException`.
- Fullscreen screens can't answer; `GoToForResult` returns `NotOpened` with `InvalidRequest`.

## Show an alert

Set the `ScreenDefinition` type to `Alert`, then open it with `GoTo`:

```csharp
await ScreenManager.Instance.GoTo("ConnectionLost");

// From the alert's button:
await ScreenManager.Instance.DismissAlert();
```

- An alert opens over everything, including modals, and isn't added to the navigation stack.
- While it's open, `GoTo`, `GoBack`, `PopTo` and `SetRoot` fail, and the Escape key does nothing. Call `DismissAlert()` to close it.
- Use `IsAlertActive` to check whether one is open.
- A single scene load closes the alert.

## One modal and one alert at a time

Screen System is designed this way:

- **One modal:** while a modal is open, `GoTo` for another new screen or modal fails. Close it first, or show the next step inside the same modal. Your code decides what happens to the previous modal; Screen System doesn't stack them.
- **One alert:** an alert opens over everything, including the modal, and blocks navigation until `DismissAlert()`. A second alert can't open over it.
- **Why:** at most one fullscreen, one modal and one alert render at once, which keeps overdraw low, and back always has one obvious thing to close.
- **A full-screen overlay outside the stack** is a modal whose prefab covers the screen. It still counts as the one modal.

## Show a screen over a modal or alert

Turn on **Ignore Navigation Rules** on the `ScreenDefinition` for splash screens and critical errors:

- A fullscreen screen opens on top and leaves the modal or alert open underneath. `GoBack()` closes that screen first.
- A modal screen replaces the open modal.

## Pass a value to a screen

`GoTo(screen, value)` passes any type to the screen. Every component on any of its layers that implements `IScreenParameter<T>` for that type receives it.

**1. Define the value.** A small struct or class with what this navigation is about:

```csharp
public readonly struct ProductRequest
{
    public readonly string ProductId;
    public ProductRequest(string productId) => ProductId = productId;
}
```

**2. Receive it on any layer.** Store it, then use it when the screen shows. Here the product list lives on the screen prefab and the title on a shared top bar layer; both get the same value:

```csharp
// On the ShopDetail screen prefab
public class ProductDetails : MonoBehaviour, IScreenParameter<ProductRequest>, IScreenShow
{
    private string productId;

    public void OnScreenParameter(ProductRequest request) => productId = request.ProductId;
    public void OnScreenShown() => Render(productId);
}

// On the shared TopBar layer
public class TopBarTitle : MonoBehaviour, IScreenParameter<ProductRequest>
{
    [SerializeField] private TMP_Text title;

    public void OnScreenParameter(ProductRequest request) => title.text = request.ProductId;
}
```

**3. Open the screen with it:**

```csharp
await ScreenManager.Instance.GoTo("ShopDetail", new ProductRequest("sword_01"));

// Also with modals, and combined with clearing the stack
await ScreenManager.Instance.GoTo("ConfirmPurchase", new ProductRequest("sword_01"));
await ScreenManager.Instance.GoTo("Store", new StoreTab("offers"), NavigationHistory.ClearToRoot);
```

**When it arrives:**

| Situation | What the receivers get |
|---|---|
| `GoTo` opens the screen | The value, once, before the layers are shown. `OnScreenParameter` runs before `OnScreenShown`, so the first frame already uses it. |
| A shared layer is already visible | The value too, although its `OnScreenShown` doesn't run again. |
| The screen is already showing on top | The value right away. The screen isn't shown again; update what depends on it in `OnScreenParameter`. |
| `GoBack` or `PopTo` returns to the screen | Nothing. The screen still has what it stored. |
| `GoTo` without a value | Nothing. Receivers keep their last value. |
| `GoTo` is blocked or cancelled | Nothing, and the value is dropped. |

**Rules of thumb:**

- **Store what you need.** Screen System drops the value after the call, so it never keeps your objects in memory.
- **Typed, no casting.** If no component on the screen takes that type, the console warns, naming the screen and the type.
- **Values or state.** Pass what this navigation is about, such as which product to show. Keep state that several screens share, such as the inventory, in a Model.
- **Keep it small.** Pass an ID or a small struct rather than a large object graph, and load the rest from your Model or backend.

## Inject services into a screen

Components on a screen's layers are injected by whatever container you connect (see [Dependency Injection](dependency-injection.md)). With [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), add `internal` fields to any MonoBehaviour on the prefab or its children:

```csharp
using ScreenSystem;
using UnityEngine;

public class ShopMainScreen : MonoBehaviour, IScreenShow
{
    internal IInventoryService inventory;

    public void OnScreenShown()
    {
        Debug.Log($"Items: {inventory.ItemCount}");
    }
}
```

Bind `IInventoryService` in an installer. Fast IoC regenerates after your scripts compile.

## Animate a screen

Screen System has no built-in transitions: animations belong to the layers.

- **Intro:** start it from `OnScreenShown()`. A layer shared with the previous screen stays visible and doesn't get the callback again, so a top bar doesn't replay its intro on every screen.
- **Outro:** play it first, and navigate when it ends. Screen System hides a screen at once, and `OnScreenHidden()` runs once it's hidden, too late to be seen.

```csharp
public void OpenShop() => StartCoroutine(OutroThenOpenShop());

private IEnumerator OutroThenOpenShop()
{
    yield return PlayOutro(); // your sequence, such as a 0.25 s fade of the content
    _ = ScreenManager.Instance.GoTo(ScreenIds.ShopMain);
}
```

With a tween library it's the same: play the sequence and call `GoTo` from its completion callback. Keep in mind:

- Only one fullscreen screen renders at a time, so don't fade out all of its layers: until the next screen shows, the camera's background color, or black, shows through. Animate the content and keep a background opaque.
- Hidden layers stay loaded until the scene changes, in the state the outro left them (transparent, moved off screen). Restore that state in `OnScreenHidden()`, or before the intro in `OnScreenShown()`.

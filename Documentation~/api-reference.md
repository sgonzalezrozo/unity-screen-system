# API Reference

[← Back to README](../README.md)

- [ScreenManager](#screenmanager)
- [NavigationResult](#navigationresult)
- [Screen IDs](#screen-ids)
- [Navigation behavior](#navigation-behavior)
- [Lifecycle interfaces](#lifecycle-interfaces)
- [Events](#events)

---

## ScreenManager

All navigation methods return a `NavigationResult` and take an optional `CancellationToken`.

```csharp
ScreenManager screens = ScreenManager.Instance;

await screens.GoTo("ShopDetail");
await screens.GoTo("Store", NavigationHistory.ClearToRoot);
await screens.GoBack();
await screens.PopTo("ShopMain");
await screens.SetRoot("Lobby");
```

| Member | Purpose |
|---|---|
| `ScreenManager.Instance` | The single manager. Set before your scene's `Awake` runs, but may still be initializing. |
| `ScreenManager.WhenReadyAsync()` | Await the initialized manager. Returns `null`, and logs why, if initialization failed. |
| `GoTo(screen)` | Open a screen. For a screen already in the stack, return to it like `PopTo`. |
| `GoTo(screen, value)` | Open a screen and pass a value to every `IScreenParameter<T>` on its layers |
| `GoTo(screen, NavigationHistory.ClearToRoot)` | Remove every screen above the root, then open a fullscreen screen over it. For tab bars. |
| `GoBack()` | Return to the previous screen |
| `PopTo(screen)` | Return to a screen already in the stack |
| `SetRoot(screen)` | Replace the root screen |
| `DismissAlert()` | Close the open alert |
| `GoToForResult<T>(screen)` | Open a modal or alert and await its answer as a `ModalResult<T>` |
| `GoToForResult<TParam, T>(screen, value)` | The same, passing a value to the modal's `IScreenParameter<TParam>` components |
| `CloseWithResult(value)` | Close the topmost modal or alert and return a value to its `GoToForResult` |
| `CloseAll()` | Hide every screen, modal and alert and empty the stack, to return to UI outside Screen System |
| `CanGoBack` | Check whether there's a screen to return to |
| `IsAlertActive` | Check whether an alert is open |
| `IsTransitioning` | Check whether a navigation call is running |
| `GetCurrentScreen()` | Get the current screen ID |

`ScreenManager` implements `IScreenNavigationService`. Depend on the interface to test your code with a fake.

## NavigationResult

It converts to `bool`, and its `Status` says why a call didn't happen:

```csharp
NavigationResult result = await screens.GoTo(ScreenIds.ShopMain);
if (!result)
    Debug.Log(result.Status); // BlockedByModal, BlockedByAlert, Transitioning, NotFound, Failed...
```

| `Status` | Meaning |
|---|---|
| `Succeeded` | The call did what it asked |
| `NotReady` | Screen System isn't initialized yet; await `ScreenManager.WhenReadyAsync()` |
| `NotFound` | No screen with that name or ID, or, for `PopTo`, it isn't in the stack |
| `Transitioning` | Another navigation call is running; calls are ignored, never queued |
| `BlockedByModal` | A modal is open |
| `BlockedByAlert` | An alert is open; call `DismissAlert()` |
| `AlreadyShowing` | The screen is already showing on top |
| `NothingToClose` | Back at the root, or `DismissAlert()` with no alert |
| `InvalidRequest` | An empty name, or `ClearToRoot` with a modal or alert |
| `Failed` | The screen couldn't be shown: its prefab didn't load or an exception was thrown. The stack is left as it was. |

For `ModalResult<T>` (`Returned`, `Dismissed`, `NotOpened`), see [Get an answer from a modal](code-examples.md#get-an-answer-from-a-modal).

## Screen IDs

Each `screen` argument can be the screen's name or its ID. The name comes from the prefab, such as `ShopMain`. The ID survives renames, and the wizard creates it from the feature and screen names, such as `Shop_ShopMain`. Names must be unique, because the first match wins.

Prefer the generated constants, which are checked at compile time:

```csharp
await screens.GoTo(ScreenIds.ShopDetail);
```

`ScreenIds` has one constant per screen, named after it and holding its ID. It's regenerated when a `ScreenDefinition` is created, renamed or deleted, so a renamed or deleted screen breaks the code that uses it at compile time instead of failing at runtime. **Generate Screen IDs** and **Screen IDs Path** in [`ScreenSystemSettings`](editor-tools.md#settings) turn it off or move it; put it inside your assembly definition's folder if your code is in one.

## Navigation behavior

- **Going to a screen already in the stack:** `GoTo` works like `PopTo`. It removes every screen above that one from the stack, hides them, and shows it again. This is by design: the stack never holds the same screen twice.
- **Clearing to the root:** `GoTo(screen, NavigationHistory.ClearToRoot)` closes the open modal, hides every screen above the root and opens the screen over the root, without drawing the root on the way. Back then returns to the root. It works only for fullscreen screens, and fails while an alert is open.
- **While a navigation call is running:** another one is ignored, logs which call is running, and returns `Transitioning`. Calls are never queued, by design.
- **Unknown screen ID:** a warning is logged, and the call returns `NotFound`.
- **A screen that can't be shown:** its prefab didn't load or an exception was thrown. The call returns `Failed` and the stack is left as it was, so a failed modal isn't left open and a failed alert doesn't block navigation.
- **Active modal:** `GoTo` for a new screen fails until the modal closes. An alert or a screen with **Ignore Navigation Rules** can still open.
- **Active alert:** navigation fails until `DismissAlert()`, except for screens with **Ignore Navigation Rules**.
- **Back at the root:** `GoBack()` starts the [quit flow](code-examples.md#handle-back-at-the-root).
- **Escape key:** runs back navigation through the Input System.
- **Scene changes:** a single scene load cancels a navigation still loading, so the new scene can open its first screen right away.
- **Cancellation:** cancelling the token passed to a navigation call stops it before the screen shows and leaves the stack as it was. The call throws `OperationCanceledException`.
- **Before initialization:** a navigation call logs an error and returns `NotReady`. Await `ScreenManager.WhenReadyAsync()` first.
- **Asset loading:** a screen's asset path is tried in Resources first, then Addressables.

## Lifecycle interfaces

Implement these on any component of a screen or layer prefab. They run per layer: a layer shared with the previous screen stays visible and doesn't get show/hide callbacks again.

| Interface | Callback | Runs when |
|---|---|---|
| `IScreenShow` | `OnScreenShown()` | The layer becomes visible |
| `IScreenHide` | `OnScreenHidden()` | The layer is hidden |
| `IScreenLifecycle` | Both of the above | — |
| `IScreenParameter<T>` | `OnScreenParameter(T value)` | `GoTo(screen, value)` passes a `T`; runs before `OnScreenShown`. See [Pass a value to a screen](code-examples.md#pass-a-value-to-a-screen). |

Hidden layers stay active, so Unity's `OnEnable` and `OnDisable` don't run on show and hide.

## Events

```csharp
NavigationEvents.OnNavigationCompleted += screenId => Debug.Log($"Opened {screenId}");
NavigationEvents.OnNavigationFailed += (screenId, reason) => Debug.LogWarning($"{screenId}: {reason}");
```

`NavigationEvents` also has `OnNavigationStarted`, `OnScreenPushed`, `OnScreenPopped`, `OnStackCleared`, `OnBackAtRoot` and `OnQuitApplicationRequested`. These events are static, so unsubscribe when your object is destroyed.

Four more events follow a screen being shown and hidden:

| Event | Raised when |
|---|---|
| `OnTransitionInStarted` | A screen starts showing, before its layers load |
| `OnTransitionInCompleted` | Its layers and content are loaded and visible |
| `OnTransitionOutStarted` | A screen starts hiding |
| `OnTransitionOutCompleted` | Its layers are hidden |

For building screens from editor code (`ScreenAuthoring`), see [AI Coding Agents › Build screens from code](ai-agents.md#build-screens-from-code).

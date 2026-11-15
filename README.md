# Screen System

**Build Unity screens from reusable layers, like layers in an image editor. Ask for a screen by name, and the system loads it, orders it, hides what it covers and unloads it when the scene changes.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Tested on Unity 6000.3](https://img.shields.io/badge/Unity-6000.3%20tested-black?logo=unity)](#compatibility)
[![Version 0.3.0](https://img.shields.io/badge/version-0.3.0-blue.svg)](CHANGELOG.md)

Screen System is a free, open-source UI manager for Unity 6, for uGUI and UI Toolkit. Each screen is a stack of layers: a background, the screen itself, a top bar, a navigation bar. Screens share layers instead of copying them, and the system sets every layer's draw order from where the screen sits in the navigation stack.

Feature code calls `ScreenManager.Instance.GoTo("Shop")`. The system loads the screen from Resources or Addressables, keeps the back stack, renders only what's visible, and unloads everything the next scene doesn't need. Features never manage navigation, visibility or memory themselves.

<!-- Demo GIF: Documentation~/images/demo.gif -->

## Why use it

### Screens built from layers

A screen is a prefab plus an ordered list of layers. In the `LayerDefinition` inspector, you drag layer prefabs into a list and reorder them. Layers above the `SCREEN` entry draw behind the screen, and layers below it draw in front.

- **Reuse instead of duplicating.** A top bar, a navigation bar or a shop background is one prefab that any screen can list. Change it once, and every screen that uses it changes.
- **One instance per shared layer.** When two screens list the same layer, they share one instance. In a test project with 8 screens, 20 layer references resolved to 12 instances.
- **No sort orders to manage.** The system sets each layer's sorting order from its position in the list and its screen's depth in the stack. A shared layer takes the order of the topmost screen that uses it.
- **Shared layers stay put.** Moving between two screens that share a top bar keeps the bar visible and keeps its state. It doesn't get `OnScreenShown` or `OnScreenHidden` again, so anything it started keeps running.

### Memory that cleans itself up

All UI lives in one persistent object, so scenes hold no UI and load faster.

- **Automatic unloading.** On a single scene load, every screen is unloaded except those marked **Persist Over Scenes**, the splash screen and the quit confirmation. Their Addressables handles are released and their assets are unloaded.
- **No hitch on the next scene's first screen.** Unused assets are unloaded after the new scene's first screen is drawn, so its load doesn't wait for that work.
- **No leaks across scene changes.** Memory stays flat over 12 scene changes on a Pixel 5, with no Addressables handle left behind.
- **Loads only what's asked for.** A screen loads when it's requested, never ahead of time. Its missing layers load together, and layers already in the scene aren't loaded again.

### Only what's visible is rendered

- **At most three windows render at once:** one fullscreen screen, one modal over it and one alert over the modal. Opening a fullscreen screen hides the one under it.
- **Modals don't redraw the screen behind them.** When a modal or alert opens, a blurred, darkened snapshot of the screen takes its place and the screen stops rendering until the modal closes. Behind the modal there's one image instead of a whole screen. An alert over a modal only dims the modal.
- **Hiding keeps state.** A hidden uGUI layer only has its `Canvas` disabled, and a hidden UI Toolkit layer has its root set to `display: none`. Neither rebuilds its hierarchy or visual tree when it's shown again.
- **No per-frame work.** Nothing polls or runs on a timer, and navigation is event-driven.

### One API for uGUI and UI Toolkit

Each screen, and each layer, picks its framework. Both work in the same project at the same time when the canvas uses Screen Space - Overlay, which is the default.

### Tools included

Setup in one command, a wizard that creates screens ready to open, a registry that updates itself, a Screen Manager window with the live stack and metrics, a layer editor, EditMode tests and a benchmark that runs on your own screens.

## Performance

Measured with the [Benchmark sample](Samples~/Benchmark/README.md) on the screens of a test project: 8 screens mixing uGUI and UI Toolkit, two scenes, Addressables. IL2CPP Development builds at 30 fps.

| Measure | Pixel 5 (Android 14) | Galaxy A02s (Android 12) |
|---|---|---|
| Show a cached screen | 2.6 ms, no frame waited | 3.9 ms |
| Show a screen that isn't loaded | 15 ms median | 30 ms median |
| Go back | 2.4 ms | 3.1 ms |
| Open a modal, with its blurred snapshot | 36 ms (one frame) | 38 ms (one frame) |
| Capture and blur the snapshot (main thread) | 0.2 ms | 0.4 ms |
| Scene change until the new scene's first screen is visible | 133 ms, of which 76 ms is Unity's scene activation | 224 ms, of which 96 ms is Unity's |
| Allocations per cached show, uGUI and UI Toolkit | 33 to 40 KB | 26 to 30 KB |
| Memory after 12 scene changes | flat (207.3 MB to 208.1 MB) | flat (190.0 MB to 190.4 MB) |
| Addressables handles left behind | 0 | 0 |
| Fullscreen screens rendering under an open modal | 0 | 0 |
| Navigations that broke the three-window rule | 0 of 264 | 0 of 264 |

Run it on your project with **Tools > Screen System > Benchmark > Run In Play Mode**, or on a device from the sample. [What each metric means](Documentation~/benchmarks/README.md).

## Is it right for your project?

**Use Screen System if you:**

- Have many screens, overlays or dialogs that share parts
- Want one source of truth for the back stack
- Use uGUI, UI Toolkit or both
- Load content with Addressables and care about memory across scenes
- Want editor scaffolding, diagnostics and measurable performance

**Consider something else if you:**

- Have only a few screens and simple scene changes
- Need to pass data through navigation calls. `GoTo` takes only a screen ID for now.

## Features

- `GoTo`, `GoBack`, `PopTo` and `SetRoot` navigation, by screen name or ID
- Screens composed from ordered, shareable layers, with automatic draw order
- Fullscreen, modal and alert screen types
- Screens kept loaded and reused until their scene ends, and unloaded automatically after it
- Optional screens that persist across scene changes
- Guarded async navigation: a call made while another is running is ignored, logged and returns `false`
- uGUI and UI Toolkit screens, alone or together
- Loading from Resources or Addressables
- Back handling with Escape through the Input System
- Navigation and show/hide events, and lifecycle callbacks on layer components
- Profiler markers for each step, so the package shows up in the Unity Profiler
- A blurred, darkened snapshot behind modals and alerts that replaces the screen underneath
- Setup, feature scaffolding, a registry, a Screen Manager window and a layer editor
- A benchmark sample and EditMode tests

## Compatibility

| Requirement | Supported |
|---|---|
| Unity | Tested on Unity 6 (`6000.3.6f1`, `6000.3.16f1`). The package declares `2022.3` as its minimum, but older versions aren't tested yet. |
| Scripting backend | Mono and IL2CPP |
| Platforms | Editor (macOS) and Android, tested on a Pixel 5 (Android 14) and a Samsung Galaxy A02s (Android 12). Other platforms aren't verified yet. |
| UI frameworks | uGUI, UI Toolkit, or both at once when the canvas uses Screen Space - Overlay, which is the default |
| Enter Play Mode with domain reload disabled | Not verified yet. Keep domain reload on until you've tested it. |
| Dependencies | [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), uGUI, Addressables, Input System. No DI container is required. |

## Installation

Git must be installed on your machine.

### 1. Install Tools Core

This package requires [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), the shared code that defines the injection interfaces used by Fast IoC, Screen System and Recyclable Scroller. Unity doesn't install Git dependencies automatically, so add it first.

1. In Unity, open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

```text
https://github.com/sgonzalezrozo/unity-tools-core.git
```

### 2. Install this package

1. In Unity, open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

```text
https://github.com/sgonzalezrozo/unity-screen-system.git
```

To pin a release, add its tag:

```text
https://github.com/sgonzalezrozo/unity-screen-system.git#v0.3.0
```

## Setup

1. Run **Tools > Screen System > Setup**.
2. Confirm that its settings, registry and `ScreenSystem` prefab are in `Assets/Resources/Tools/Screen System/`.

Running Setup again repairs a project: it keeps your settings and prefab, and adds or rebuilds the backdrop behind modals.

The prefab is created automatically before the first scene loads, so you don't need to add it to a scene.

## Create your first screen

Two steps, about a minute:

1. In the Project window, right-click the folder that holds your features and select **Screen System > New Screen Feature**. Name the feature and its screens, and pick each screen's type: `fullscreen`, `modal` or `alert`.
2. Click **Create**, then call `ScreenManager.Instance.GoTo("ShopMain")` with your screen's name.

For a feature `Shop` with a screen `ShopMain`, the wizard creates:

```
Shop/
├── ShopMain/
│   └── ShopMain.prefab      Canvas or UIDocument, LayerDefinition. Put the screen's C# here too.
└── Services/
    └── ShopMain.asset       ScreenDefinition, ID "Shop_ShopMain"
```

It also marks the prefab Addressable, with its name as the address, and adds the screen to the registry. For UI Toolkit it creates an empty `ShopMain.uxml` and assigns it and the `GlobalPanelSettings` to the `UIDocument`.

To add a screen to an existing feature, right-click the feature folder, one of its screen folders or its `Services` folder and select **Screen System > Add Screen to Feature**.

The registry keeps itself up to date: creating, deleting, moving or renaming a `ScreenDefinition` updates it, and the console warns when two screens share a name or ID. **Tools > Screen System > Rebuild Registry Cache** is only needed to repair a registry.

### What's on a screen prefab

The wizard adds these for you. If you build a prefab by hand, add them yourself:

| Framework | Required on the prefab root |
|---|---|
| uGUI | `Canvas`, `GraphicRaycaster`, `CanvasGroup`, a stretched `RectTransform` and `LayerDefinition` |
| UI Toolkit | `UIDocument` and `LayerDefinition` |

`LayerComponent` is added when the screen loads, if the prefab doesn't have it. For a UI Toolkit prefab built by hand, also assign your UXML and the `GlobalPanelSettings` asset that Setup creates in `Assets/Resources/Tools/Screen System/Prefabs/`.

To use a prefab you built by hand, drag it onto the drop area of a `ScreenDefinition`. A prefab that isn't in `Resources` is marked Addressable.

## Compose a screen from layers

1. Make each reusable part a prefab with its own `Canvas` (uGUI) or `UIDocument` (UI Toolkit): a background, a top bar, a navigation bar.
2. Select the screen prefab and open its `LayerDefinition` in the Inspector.
3. Drag layer prefabs into the list. A prefab that isn't in `Resources` or Addressable is marked Addressable, with its name as the address. Keep the ones that should draw behind the screen above the `SCREEN` entry, and the ones that should draw in front below it.
4. Reorder the list to change the draw order.

Any screen that lists the same layer shares its instance.

### Layers and draw order

```mermaid
flowchart TB
    subgraph S2["Second screen: base order 30"]
        direction TB
        S2F["In front of the screen: 32"] --> S2M["SCREEN: 30"] --> S2B["Behind the screen: 28"]
    end
    subgraph S1["First screen: base order 10"]
        direction TB
        S1F["In front of the screen: 12"] --> S1M["SCREEN: 10"] --> S1B["Behind the screen: 8"]
    end
    S2 --> S1
```

Higher numbers draw on top, so the diagram reads from front to back. Each screen's base order is `10 + 20 × its depth`, and each step away from `SCREEN` adds or subtracts `2`.

### Shared layers

A shared layer:

- Uses the draw order of the topmost screen that's using it
- Stays visible when you navigate between two screens that use it
- Keeps its content, rather than loading a copy for each screen

## React to a screen opening or closing

Hidden layers stay active, so Unity's `OnEnable` and `OnDisable` don't run when a screen is shown or hidden. Implement these interfaces on any component of the prefab instead:

```csharp
using ScreenSystem;
using UnityEngine;

public class SettingsScreen : MonoBehaviour, IScreenLifecycle
{
    public void OnScreenShown()
    {
        Debug.Log("Settings shown");
    }

    public void OnScreenHidden()
    {
        Debug.Log("Settings hidden");
    }
}
```

Implement only `IScreenShow` or `IScreenHide` if you need just one callback. The callbacks run per layer: a layer shared with the previous screen stays visible and doesn't get them again.

## Open the first screen

`ScreenManager.Instance` is set in `Start()`, so wait for it before navigating:

```csharp
using System;
using System.Threading.Tasks;
using ScreenSystem;
using UnityEngine;

public class GameBoot : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            while (ScreenManager.Instance == null)
                await Task.Yield();

            await ScreenManager.Instance.WaitForInitializationAsync();
            await ScreenManager.Instance.SetRoot("ShopMain");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
```

`async void` is used because `Start()` can't return a `Task` to a caller, so keep the `try`/`catch`.

## Usage examples

### Connect buttons to screens

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

`ShopModal` opens as a modal because its `ScreenDefinition` type is `modal`.

### Check the result

```csharp
bool opened = await ScreenManager.Instance.GoTo("ShopDetail");

if (!opened)
    Debug.LogWarning("ShopDetail didn't open.");
```

### Change scenes

Load scenes as usual. Screen System cleans up automatically when a new scene loads in single mode, so you don't need to call anything first.

```csharp
SceneManager.LoadScene("MainMenu");
```

This needs `using UnityEngine.SceneManagement;`. Additive loads don't change your screens.

### Open a modal

Set the `ScreenDefinition` type to `modal`, then use the same navigation calls:

```csharp
await ScreenManager.Instance.GoTo("ShopModal");
await ScreenManager.Instance.GoBack(); // Closes the modal.
```

- One modal is open at a time. Calling `GoTo` for another new screen fails until the modal closes.
- Calling `GoTo` for a screen already in the stack closes the modal and returns to that screen.

### Switch tabs from a bottom bar

Make the bottom bar a layer that each tab screen lists, so it's shared and stays on screen. Its buttons clear the history above the root, so back from any tab returns home instead of walking through the tabs:

```csharp
public async void OnStoreTab()
{
    // From Inventory with A, B, C open, this leaves Home, Store.
    await ScreenManager.Instance.GoTo("Store", NavigationHistory.ClearToRoot);
}
```

Plain `GoTo("Store")` keeps the history instead: back from Store returns to C.

### Handle back at the root

`GoBack()` and the Escape key close an open modal first, then step back through the stack. At the root, Screen System opens a modal called `QuitConfirmation`. If that screen doesn't exist, the app quits straight away, and Play Mode stops in the Editor.

To confirm before quitting, create a modal screen named `QuitConfirmation`. You can change that name with **Quit Confirmation Modal Id** on the `ScreenManager`.

To react to the app quitting:

```csharp
NavigationEvents.OnQuitApplicationRequested += SaveGame;
```

### Keep a screen across scenes

Turn on **Persist Over Scenes** on the `ScreenDefinition`. This suits screens such as a loading overlay. The splash screen (**Splash Screen Id** on the `ScreenManager`) and the quit confirmation are kept across scenes without it.

When a scene loads in single mode, a persistent screen keeps its layers and its visibility, but the navigation history is cleared. A persistent screen that's showing when the scene loads, such as a splash, stays up until the new scene opens its first screen.

### Show an alert

Set the `ScreenDefinition` type to `alert`, then open it with `GoTo`:

```csharp
await ScreenManager.Instance.GoTo("ConnectionLost");

// From the alert's button:
await ScreenManager.Instance.DismissAlert();
```

- An alert opens over everything, including modals, and isn't added to the navigation stack.
- While it's open, `GoTo`, `GoBack`, `PopTo` and `SetRoot` fail, and the Escape key does nothing. Call `DismissAlert()` to close it.
- Use `IsAlertActive` to check whether one is open.
- A single scene load closes the alert.

### Show a screen over a modal or alert

Turn on **Ignore Navigation Rules** on the `ScreenDefinition` for splash screens and critical errors:

- A fullscreen screen opens on top and leaves the modal or alert open underneath. `GoBack()` closes that screen first.
- A modal screen replaces the open modal.

### Inject services into a screen

Components on a screen's layers are injected by whatever container you connect (see [Dependency injection](#dependency-injection)). With [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), add `internal` fields to any MonoBehaviour on the prefab or its children:

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

## Navigation reference

All navigation methods return `Task<bool>` and take an optional `CancellationToken`.

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
| `GoTo(screen)` | Open a screen. For a screen already in the stack, return to it like `PopTo`. |
| `GoTo(screen, NavigationHistory.ClearToRoot)` | Remove every screen above the root, then open a fullscreen screen over it. For tab bars. |
| `GoBack()` | Return to the previous screen |
| `PopTo(screen)` | Return to a screen already in the stack |
| `SetRoot(screen)` | Replace the root screen |
| `DismissAlert()` | Close the open alert |
| `CanGoBack` | Check whether there's a screen to return to |
| `IsAlertActive` | Check whether an alert is open |
| `IsTransitioning` | Check whether a navigation call is running |
| `GetCurrentScreen()` | Get the current screen ID |

Each `screen` argument can be the screen's name or its ID. The name comes from the prefab, such as `ShopMain`. The ID survives renames, and the wizard creates it from the feature and screen names, such as `Shop_ShopMain`. Names must be unique, because the first match wins.

## Navigation behavior

- **Going to a screen already in the stack:** `GoTo` works like `PopTo`. It removes every screen above that one from the stack, hides them, and shows it again. This is by design: the stack never holds the same screen twice.
- **Clearing to the root:** `GoTo(screen, NavigationHistory.ClearToRoot)` closes the open modal, hides every screen above the root and opens the screen over the root, without drawing the root on the way. Back then returns to the root. It works only for fullscreen screens, and fails while an alert is open.
- **While a navigation call is running:** another one is ignored, logs which call is running, and returns `false`.
- **Unknown screen ID:** an error or warning is logged, and the call returns `false`.
- **Active modal:** `GoTo` for a new screen fails until the modal closes. An alert or a screen with **Ignore Navigation Rules** can still open.
- **Active alert:** navigation fails until `DismissAlert()`, except for screens with **Ignore Navigation Rules**.
- **Back at the root:** `GoBack()` starts the [quit flow](#handle-back-at-the-root).
- **Escape key:** runs back navigation through the Input System.
- **Scene changes:** a single scene load cancels a navigation still loading, so the new scene can open its first screen right away.
- **Cancellation:** cancelling the token passed to a navigation call stops it before the screen shows and leaves the stack as it was. The call throws `OperationCanceledException`.
- **Before initialization:** a navigation call logs an error and returns `false`. Await `WaitForInitializationAsync()` first.
- **Asset loading:** a screen's asset path is tried in Resources first, then Addressables.

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

## How it works

Screen System runs from one persistent `ScreenSystem` object, marked `DontDestroyOnLoad`.

- **Loading:** a screen's layers load the first time it opens. The ones not in the scene yet load together, and prefabs in local Addressables bundles finish loading in the same frame.
- **Showing and hiding:** hidden screens stay loaded. A uGUI layer's `Canvas` is disabled, and a UI Toolkit layer's root is set to `display: none`. Reopening a screen reuses the same objects.
- **Draw order:** each layer's sorting order comes from its screen's depth in the stack and its position in the `LayerDefinition`.
- **Modals and alerts:** before a modal or alert becomes visible, the screen is captured at the end of the frame, blurred at a quarter of its resolution and darkened. That image is placed just below the modal, and the screen's layers stop rendering without being hidden, so their callbacks don't run. Layers the modal shares, such as a navigation bar, keep rendering. Blur strength, resolution, darkening and the alert dim are set in `ScreenSystemSettings`.
- **Scene changes:** screens without **Persist Over Scenes**, other than the splash and the quit confirmation, are destroyed and their Addressables handles released. Unused assets are unloaded once the new scene's first screen is drawn.
- **Addressables:** each address has one handle, held while a screen uses it. AssetBundles stay open for the session once a screen needs them, while their assets load and unload with the screens. Closing and reopening a bundle on every scene change cost about 29 MB of native memory per change on a Pixel 5.

### A screen's lifetime

```mermaid
stateDiagram-v2
    [*] --> Loading: First GoTo or SetRoot
    Loading --> Visible: Layers created and shown
    Visible --> Cached: Navigate away
    Cached --> Visible: Navigate back, or GoTo it again
    Visible --> Unloaded: Single scene load
    Cached --> Unloaded: Single scene load
    Unloaded --> [*]
```

Persistent screens, the splash and the quit confirmation skip the `Unloaded` state.

Going back to a screen with `GoBack`, `PopTo`, or `GoTo` for a screen already in the stack removes the screens above it from the stack. They stay cached, hidden, until they're opened again or the scene changes.

## Core concepts

| Concept | Meaning |
|---|---|
| Layer | A prefab with its own `Canvas` or `UIDocument`, drawn as part of one or more screens |
| `LayerDefinition` | The ordered list of a screen's layers, edited in the Inspector |
| `ScreenDefinition` | An asset holding a screen's ID, asset path, type, framework and behavior |
| `ScreenRegistry` | The lookup of available screens |
| `ScreenManager` | The entry point for navigation |
| `LayerComponent` | Handles a layer's runtime behavior |
| Screen feature | A folder with one folder per screen and a `Services` folder for its `ScreenDefinition` assets |
| Screen type | `fullscreen`, `modal` or `alert` |
| Framework | `uGUI`, `UIToolkit` or `Both` |

## Editor tools

| Command | Purpose |
|---|---|
| `Tools/Screen System/Setup` | Create the required project assets |
| `Tools/Screen System/Rebuild Registry Cache` | Repair the registry. It already updates when `ScreenDefinition` assets change. |
| `Tools/Screen System/Screen Manager Window` | Inspect the live stack, screens and metrics |
| `Tools/Screen System/Benchmark/Run In Play Mode` | Measure your screens (after importing the Benchmark sample) |
| `Assets/Screen System/New Screen Feature` | Create a feature with its screens, ready for `GoTo` |
| `Assets/Screen System/Add Screen to Feature` | Add a screen to an existing feature |
| `Tools/Screen System/Rebuild Modal Backdrop` | Add or rebuild the blurred backdrop behind modals. Setup also does it. |

In the Unity Profiler, the package's steps appear as `ScreenSystem.*` markers, such as `ScreenSystem.LoadPrefab` and `ScreenSystem.ActivateLayers`.

## Recommended usage

- Split repeated parts of your screens into layers and share them.
- Keep screen names unique and stable.
- Use a single root screen for your app's boot flow.
- Prefer stack-based navigation over direct scene loads when UI should stay mounted.

## Use it with AI coding agents

Coding agents write C# files, and this package is newer than their training data. Give them its rules so they use the real API:

1. Copy [`Documentation~/agent-rules.md`](Documentation~/agent-rules.md) into your project's agent rules, for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/screen-system.md`.
2. For a whole Unity architecture with a verify loop, add [Unity Architecture Agents](https://github.com/sgonzalezrozo/unity-architecture-agents). It detects this package and adapts its code.

There are no MCP tools for Screen System yet. The rules file works with any agent that reads project files.

## FAQ

### How do I manage UI screens, popups and modals in Unity?

Create each screen as a prefab with a `ScreenDefinition`, then navigate by name: `await ScreenManager.Instance.GoTo("ShopDetail")`. Screen System keeps the back stack and the modal and alert layers. See [Create your first screen](#create-your-first-screen).

### How do I reuse a top bar or background across screens?

Make it a layer prefab and add it to each screen's `LayerDefinition`. Screens that list it share one instance, and its draw order follows the topmost screen using it. See [Compose a screen from layers](#compose-a-screen-from-layers).

### How do I go back to the previous screen?

Call `await ScreenManager.Instance.GoBack()`. The Escape key does the same through the Input System. See [Navigation reference](#navigation-reference).

### Does it unload screens when the scene changes?

Yes. A single scene load unloads every screen except persistent ones, the splash and the quit confirmation, and releases their Addressables handles. See [How it works](#how-it-works).

### Does it work with uGUI and UI Toolkit?

Yes. Each `ScreenDefinition` sets its framework, and both can be used at once.

### Does it load screens with Addressables?

Yes. A screen's asset path is tried in Resources first, then Addressables.

### Does it work with Unity 6?

Yes. It's tested on Unity `6000.3`. The package declares `2022.3` as its minimum, but older versions aren't tested yet.

### Does it work with IL2CPP and mobile?

Yes on Android with IL2CPP, tested on a Pixel 5 and a Samsung Galaxy A02s. iOS and other platforms aren't verified yet. See [Compatibility](#compatibility).

### How fast is it?

A cached screen shows in 2.6 ms on a Pixel 5 and 3.9 ms on a Galaxy A02s, and memory stays flat across scene changes on both. See [Performance](#performance), and run the benchmark on your own screens.

### Can I pass parameters to a screen?

Not through navigation calls yet. Put shared data in a model the screen reads. See [Limitations](#limitations).

### Does it work with dependency injection? Do I need Fast IoC?

It works with any container, or none. Screen System depends only on the injection interfaces in Tools Core. Fast IoC registers itself; for Zenject, VContainer or your own container, implement those two interfaces. See [Dependency injection](#dependency-injection).

### Does it work with AI coding agents?

It ships a rules file you copy into your project, so agents use its real API instead of guessing. It hasn't been tested with agents beyond the author's own projects. See [Use it with AI coding agents](#use-it-with-ai-coding-agents).

### Should I use scenes or prefabs for UI screens?

Screen System uses prefabs, so UI stays loaded across navigation and a back stack is cheap. Load a scene when the game world changes, not for each menu. Screens marked **Persist Over Scenes** stay available across scene loads.

### How do I show a confirmation popup or an alert?

Give the screen a modal or alert type on its `ScreenDefinition`, then open it with `GoTo`. Close an alert with `DismissAlert()`. See [Navigation behavior](#navigation-behavior).

### How do I install it?

In Unity, open **Window > Package Manager**, select **+ > Add package from git URL** and enter `https://github.com/sgonzalezrozo/unity-screen-system.git`. Install [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) first, because Unity doesn't install Git dependencies automatically. See [Installation](#installation).

### Is it production-ready? Does anyone use it?

Not yet. Screen System is a pre-release package (`0.x`) by a single author. It isn't used in a shipped game, and it has no other users yet, so expect rough edges and API changes before `1.0.0`. Bug reports are very welcome. See [Project status](#project-status).

### Is it free for commercial use?

Yes. It's MIT licensed.

## Dependency injection

Screen System doesn't depend on any container. It only uses the two interfaces in [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), so it works with Fast IoC, Zenject (Extenject), VContainer or no container at all:

| Interface | What Screen System does with it |
|---|---|
| `IPrefabInstantiator` | Creates each layer from its prefab |
| `IMonoBehaviourInjector` | Injects the components of each new layer and its content |

- **No container:** the defaults use standard `Instantiate` and inject nothing.
- **[Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc):** registers itself, so screens are injected with no setup. The package exposes its internals to `Assembly-CSharp`, so Fast IoC `0.4.0` and later assign its `internal` fields directly, without reflection.
- **Any other container:** implement both interfaces with your container and assign them to `ToolInjectionServices` before the first scene loads. Tools Core has [a complete Zenject example](https://github.com/sgonzalezrozo/unity-tools-core#example-zenject).

## Troubleshooting

| Problem | Try this |
|---|---|
| `TryResolveScreenId FAILED` or `screen not found` | Check the screen name or ID, and the console for duplicate-name warnings. Run **Rebuild Registry Cache** to repair the registry. |
| `ScreenSystem prefab not found` | Run **Tools > Screen System > Setup** |
| `AssetPath is empty` | Set the screen's asset path on its `ScreenDefinition` |
| `GoTo` does nothing | Check the console for `ignored: ... is still running`, an open modal or an open alert |
| Escape quits the app | Create a `QuitConfirmation` modal, as shown in [Handle back at the root](#handle-back-at-the-root) |
| An `alert` screen won't close | Call `DismissAlert()`, because back navigation doesn't close alerts |
| `ScreenManager.Instance` is `null` | Wait for it as shown in [Open the first screen](#open-the-first-screen) |
| A layer doesn't appear | Check that its prefab is in `Resources` or Addressable under the name listed in the `LayerDefinition` |

## Limitations

- Navigation calls can't pass parameters to a screen, and modals can't return a result through `GoTo`.
- Screens are requested by string, so typos are only caught at runtime.
- Navigation calls made while another is running are ignored, not queued.

## Project status

This is a pre-release package at version `0.3.0`. Its API and behavior may change before `1.0.0`. Not yet used in a shipped game.

Until `1.0.0`, minor versions (`0.x.0`) can include breaking changes, and patch versions (`0.x.y`) don't. Breaking changes are marked **Breaking** in the [changelog](CHANGELOG.md).

There's no public roadmap yet. Known gaps are listed under [Limitations](#limitations), and planned work is tracked in [issues](https://github.com/sgonzalezrozo/unity-screen-system/issues).

## Contributing

Bug reports and pull requests are welcome. Please open an issue before starting a large change. See the [contributing guide](.github/CONTRIBUTING.md) and [security policy](.github/SECURITY.md).

## Related packages

| Package | Purpose |
|---|---|
| [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc) | Code-generated dependency injection |
| [UI Tweens](https://github.com/sgonzalezrozo/unity-ui-tweens) | ECS-based UI animation |
| [Recyclable Scroller](https://github.com/sgonzalezrozo/unity-recyclable-scroller) | Virtualized lists |
| [Enhanced Console](https://github.com/sgonzalezrozo/unity-enhanced-console) | Unity Editor log triage |
| [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) | Shared injection code, required by this package |

## License

Released under the [MIT License](LICENSE).

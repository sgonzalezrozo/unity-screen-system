# Getting Started

[← Back to README](../README.md)

From an empty project to a first screen on screen, in about five minutes.

- [Requirements](#requirements)
- [Installation](#installation)
- [Setup](#setup)
- [The two special screens](#the-two-special-screens)
- [Create your first screen](#create-your-first-screen)
- [Compose a screen from layers](#compose-a-screen-from-layers)
- [Open the first screen](#open-the-first-screen)
- [React to a screen opening or closing](#react-to-a-screen-opening-or-closing)
- [Next steps](#next-steps)

---

## Requirements

| Requirement | Supported |
|---|---|
| Unity | Tested on Unity 6 (`6000.3.6f1`, `6000.3.16f1`). The package declares `2022.3` as its minimum, but older versions aren't tested yet. |
| Scripting backend | Mono and IL2CPP |
| Platforms | Targets mobile and WebGL. Tested in the Editor (macOS), on iOS (iPhone Air with iOS 27, iPhone 13 mini with iOS 26.5) and on Android (Pixel 5 with Android 14, Samsung Galaxy A02s with Android 12). A WebGL Development build was run in a browser; there, layers always load asynchronously, because WebGL fetches every bundle with a web request. |
| UI frameworks | uGUI, UI Toolkit, or both at once when the canvas uses Screen Space - Overlay, which is the default |
| Enter Play Mode with domain reload disabled | Supported. Verified across repeated Play sessions with domain and scene reload off: static state, events and the registry reset at the start of each session. |
| Dependencies | [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), uGUI, Addressables, Input System. No DI container is required. |

## Installation

Git must be installed on your machine.

### 1. Install Tools Core

This package requires [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), the shared code that defines the injection interfaces Screen System uses to work with any container. Unity doesn't install Git dependencies automatically, so add it first.

1. In Unity, open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

```text
https://github.com/sgonzalezrozo/unity-tools-core.git
```

### 2. Install Screen System

1. In Unity, open **Window > Package Manager**.
2. Select **+ > Add package from git URL**.
3. Enter:

```text
https://github.com/sgonzalezrozo/unity-screen-system.git
```

## Setup

1. Run **Tools > Screen System > Setup**.
2. Confirm that its settings, registry and `ScreenSystem` prefab are in `Assets/Resources/Tools/Screen System/`.

Running Setup again repairs a project: it keeps your settings and prefab, and adds or rebuilds the backdrop behind modals.

The prefab is created automatically before the first scene loads, so you don't need to add it to a scene.

> Adding Screen System to a project that already has hand-made UI? Read [Migrating an existing project](migration.md) first.

## The two special screens

Screen System handles two screens by name. Every game designs its own, so Setup offers to create plain ones for you to redesign:

| Screen | Default name | What Screen System does with it |
|---|---|---|
| Splash | `SplashMain` (**Splash Screen Id** on the `ScreenManager`) | Shows it as soon as it starts, before your first `SetRoot`, and keeps it across scene changes. It stays up until the new scene opens its first screen. |
| Quit confirmation | `QuitConfirmation` (**Quit Confirmation Modal Id** on the `ScreenManager`) | Opens it when back is pressed at the root, and keeps it across scene changes. Without it, back at the root quits the app straight away, and stops Play Mode in the Editor. |

At the end of Setup, click **Create**. If you chose **Later**, the `ScreenManager` Inspector warns while one is missing and has a button to create them; running **Setup** again also offers it. Both go in `Assets/Boot`, as a feature like any other:

- `SplashMain`: a `Fullscreen` screen with **Ignore Navigation Rules** on, so it shows over anything while the game boots. It has a background and the title "Splash" to replace.
- `QuitConfirmation`: a `Modal` with a question and two buttons wired to the `QuitConfirmationLayer` component. **Quit** raises `NavigationEvents.OnQuitApplicationRequested` and quits, or stops Play Mode in the Editor. **Cancel** closes the modal, like back.

Screens that already exist are left alone, so running it again is safe. A screen whose prefab was deleted, for example with its `Resources` folder, is created again, and its old `ScreenDefinition` goes to the trash. The screens are uGUI; replace their contents, or build your own screens with the same names. To use your own buttons, point their **On Click** at `QuitConfirmationLayer.Quit` and `QuitConfirmationLayer.Cancel`.

On WebGL and iOS an app doesn't quit itself, so leave out the quit confirmation there and turn off **Quit On Back At Root** in `ScreenSystemSettings`.

## Create your first screen

Two steps, about a minute:

1. In the Project window, right-click the folder that holds your features and select **Screen System > New Screen Feature**. Name the feature and its screens, and pick each screen's type: `Fullscreen`, `Modal` or `Alert`.
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

The registry keeps itself up to date: creating, deleting, moving or renaming a `ScreenDefinition` updates it, and the console warns when two screens share a name or ID. **Rebuild Registry from Assets**, in the `ScreenRegistry` Inspector, is only needed to repair a registry.

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

Any screen that lists the same layer shares its instance. How the draw order is computed is explained in [Core Concepts › Layers](core-concepts.md#layers).

## Open the first screen

Wait for Screen System to be ready, then open your first screen:

```csharp
using System;
using ScreenSystem;
using UnityEngine;

public class GameBoot : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            ScreenManager screens = await ScreenManager.WhenReadyAsync();
            if (screens != null)
                await screens.SetRoot("ShopMain");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
```

`WhenReadyAsync()` returns once the manager exists and has finished initializing, so there's nothing to poll. It returns `null` if initialization failed or Screen System isn't set up, and logs why. You can call it from any `Awake` or `Start`, as many times as you need.

`ScreenManager.Instance` is already set when your scene's objects run `Awake`, because the `ScreenSystem` prefab is created before the first scene loads. It may still be initializing, so navigate after `WhenReadyAsync()`.

`async void` is used because `Start()` can't return a `Task` to a caller, so keep the `try`/`catch`.

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

The **Quick Start** sample (Package Manager > Screen System > Samples) contains both scripts above.

## Next steps

- [Code Examples](code-examples.md): buttons, modals, alerts, passing values, tabs, scene changes
- [Core Concepts](core-concepts.md): how layers, rendering and memory work
- [Editor Tools & Settings](editor-tools.md): the Screen Manager window and every setting

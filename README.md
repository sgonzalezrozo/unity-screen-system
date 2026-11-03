# Screen System

**Declarative UI navigation for Unity. Feature code asks for a screen, and the system handles the rest.**

[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

Screen System handles loading, layering and the back stack, so features don't have to manage navigation themselves. It provides fullscreen, modal and alert layers, and supports uGUI and UI Toolkit screens.

> **TODO:** Add a GIF showing navigation, a modal, a back action and an alert.

## Why I built it

Navigation code tends to spread. As a project grows, each feature ends up knowing too much about what is open, what loads next and what the back button should do.

Having led meta-game and live-ops client work on large mobile UI, I wanted features to make one simple request and leave loading, layering and history to one system.

> **TODO:** Replace this with your own story: the project or navigation problem that led you to build Screen System.

## Is it right for your project?

**Use Screen System if you:**

- Have many screens, overlays or dialogs
- Want one source of truth for the back stack
- Use uGUI, UI Toolkit or both
- Want editor scaffolding and diagnostics

**Consider something else if you:**

- Have only a few screens and simple scene transitions
- Need to pass data through navigation calls. `GoTo` currently takes only a screen ID.

## Features

- `GoTo`, `GoBack`, `SetRoot` and `PopTo` navigation
- Fullscreen, modal and alert screen types
- Automatic layer ordering, including layers shared between screens
- Screens kept loaded and reused until their scene ends
- Optional screens that persist across scene changes
- Guarded async navigation, which ignores calls made during a transition
- uGUI and UI Toolkit screen frameworks
- Screens loaded from Resources or Addressables
- Back handling with Escape through the Input System
- Navigation and transition events
- Optional background blur and glass effects
- Feature scaffolding, a registry and a Screen Manager window

## Compatibility

| Requirement | Supported |
|---|---|
| Unity | 2022.3 or newer. **TODO:** add tested versions. |
| Scripting backend | **TODO:** confirm Mono and IL2CPP |
| Platforms | **TODO:** list tested platforms |
| UI frameworks | uGUI, UI Toolkit |
| Enter Play Mode with domain reload disabled | **TODO:** confirm support |
| Dependencies | [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), uGUI, Addressables, Input System |

## Installation

Git must be installed on your machine.

### 1. Install Tools Core

Unity doesn't install Git dependencies automatically, so add [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) first.

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
https://github.com/sgonzalezrozo/unity-screen-system.git#v0.2.0
```

## Setup

1. Run **Tools > Screen System > Setup**.
2. Confirm that its settings, registry and `ScreenSystem` prefab are in `Assets/Resources/Tools/Screen System/`.

The prefab is created automatically before the first scene loads, so you don't need to add it to a scene.

## Create your first screen

1. In the Project window, select a folder for the feature.
2. Select **Assets > Screen System > New Screen Feature**.
3. Finish the wizard. It creates `Screens` and `Prefabs` folders with a `ScreenDefinition` and a prefab.
4. On the `ScreenDefinition`, check its screen type and framework. Its name, ID and asset path are filled in from the prefab.
5. Run **Tools > Screen System > Rebuild Registry Cache**.

To react when a screen opens or closes, add a component like this to its prefab:

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

Implement only `IScreenShow` or `IScreenHide` if you need just one callback. A component can also implement `IScreenContent` for `InitializeAsync()` and `Cleanup()`.

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

Load scenes as usual. Screen System cleans up automatically when a new scene loads in single mode.

```csharp
SceneManager.LoadScene("Frontend");
```

This needs `using UnityEngine.SceneManagement;`.

### Add editor shortcuts

This needs `using UnityEngine.InputSystem;`.

```csharp
#if UNITY_EDITOR
private void Update()
{
    if (Keyboard.current?.digit1Key.wasPressedThisFrame ?? false)
        ScreenManager.Instance.GoTo("ShopMain").Forget("Open ShopMain");

    if (Keyboard.current?.digit2Key.wasPressedThisFrame ?? false)
        ScreenManager.Instance.GoTo("ShopDetail").Forget("Open ShopDetail");
}
#endif
```

## Navigation reference

All navigation methods return `Task<bool>` and take an optional `CancellationToken`.

```csharp
ScreenManager screens = ScreenManager.Instance;

await screens.GoTo("ShopDetail");
await screens.GoBack();
await screens.PopTo("ShopMain");
await screens.SetRoot("Lobby");
```

| Operation | Purpose |
|---|---|
| `GoTo(screenId)` | Open a screen |
| `GoBack()` | Return to the previous screen |
| `PopTo(screenId)` | Return to a screen already in the stack |
| `SetRoot(screenId)` | Replace the root screen |
| `CanGoBack` | Check whether there's a screen to return to |
| `GetCurrentScreen()` | Get the current screen ID |

## Navigation behavior

- **During a transition:** another navigation call is ignored and returns `false`.
- **Unknown screen ID:** an error or warning is logged, and the call returns `false`.
- **Active alert or modal:** `GoTo` is blocked until it's dismissed. Screens with **Ignore Navigation Rules** can show over modals.
- **Back at the root:** `GoBack()` starts the quit flow, which can call `Application.Quit()`.
- **Escape key:** runs back navigation through the Input System.
- **Scene changes:** cleanup runs automatically when a scene loads in single mode. Additive loads don't change your screens. Screens with **Persist Over Scenes** stay available.
- **Asset loading:** a screen's asset path is tried in Resources first, then Addressables.

To open a modal, set its `ScreenDefinition` type to `modal` and call `GoTo` with its ID.

> **TODO:** Add examples for showing an alert and customizing transitions.

## Events

```csharp
NavigationEvents.OnNavigationCompleted += screenId => Debug.Log($"Opened {screenId}");
NavigationEvents.OnNavigationFailed += (screenId, reason) => Debug.LogWarning($"{screenId}: {reason}");
```

`NavigationEvents` also has `OnNavigationStarted`, `OnTransitionInStarted`, `OnTransitionInCompleted`, `OnTransitionOutStarted` and `OnTransitionOutCompleted`. These events are static, so unsubscribe when your object is destroyed.

## Dependency injection

Screen System works without a container, using standard `Instantiate`. With [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc), its components are injected automatically. See [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) for using another container.

## How it works

Screen System runs from one persistent `ScreenSystem` object, marked `DontDestroyOnLoad`.

- **Loading:** a screen's layers load the first time it opens, then stay cached.
- **Showing and hiding:** hidden screens stay loaded, but their `Canvas` or `UIDocument` is disabled. Reopening one reuses the same objects.
- **Layers:** each screen is made of one or more layers, and each layer has its own `Canvas` or `UIDocument`. Layers can be shared between screens.
- **Draw order:** the system sets each layer's sort order from its position in the navigation stack and its `LayerDefinition`, so you don't set sort orders by hand.
- **Scene changes:** screens without **Persist Over Scenes** are destroyed, their cached resources are released, and unused assets are unloaded. Persistent screens are hidden and kept.

Each `ScreenDefinition` in the registry stores the settings that control this, so features don't manage these lifetimes themselves.

## Core concepts

| Concept | Meaning |
|---|---|
| Screen feature | A folder of screens created by the wizard |
| `ScreenDefinition` | An asset holding a screen's ID, asset path, type, framework and behavior |
| `ScreenRegistry` | The lookup of available screens |
| `ScreenManager` | The entry point for navigation |
| `LayerComponent` | Handles a layer's runtime behavior |
| `LayerDefinition` | Defines a screen's layers and their order |
| Screen type | `fullscreen`, `modal` or `alert` |
| Framework | `uGUI`, `UIToolkit` or `Both` |

## Editor tools

| Command | Purpose |
|---|---|
| `Tools/Screen System/Setup` | Create the required project assets |
| `Tools/Screen System/Rebuild Registry Cache` | Rebuild the registry from `ScreenDefinition` assets |
| `Tools/Screen System/Screen Manager Window` | Inspect screens and metrics |
| `Assets/Screen System/New Screen Feature` | Scaffold a new screen feature |
| `Assets/Screen System/Add Screen to Feature` | Add a screen to an existing feature |
| `GameObject/UI/Blur Background Layer` | Add a blur background layer |

## Recommended usage

- Keep screen names unique and stable.
- Use a single root screen for your app's boot flow.
- Prefer stack-based transitions over direct scene loads when UI should stay mounted.
- Rebuild the registry after adding or renaming screens.

## Troubleshooting

| Problem | Try this |
|---|---|
| `TryResolveScreenId FAILED` or `screen not found` | Run **Rebuild Registry Cache** and check the screen name or ID |
| `ScreenSystem prefab not found` | Run **Tools > Screen System > Setup** |
| `AssetPath is EMPTY` | Set the screen's asset path on its `ScreenDefinition` |
| `GoTo` does nothing | Check whether a transition, modal or alert is blocking it |
| `ScreenManager.Instance` is `null` | Wait for it as shown in [Open the first screen](#open-the-first-screen) |

## Limitations

- Navigation calls can't pass parameters to a screen.
- Modals can't return a result through `GoTo`.
- Screens are requested by string, so typos are only caught at runtime.
- Calls made during a transition are ignored, not queued.

## Project status

This is a pre-release package at version `0.2.0`. Its API and behavior may change before `1.0.0`.

> **TODO:** Add a roadmap link and breaking-change policy.

## Contributing

Bug reports and pull requests are welcome. Please open an issue before starting a large change.

## Related packages

| Package | Purpose |
|---|---|
| [Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc) | Code-generated dependency injection |
| [UI Tweens](https://github.com/sgonzalezrozo/unity-ui-tweens) | ECS-based UI animation |
| [Recyclable Scroller](https://github.com/sgonzalezrozo/unity-recyclable-scroller) | Virtualized lists |
| [Enhanced Console](https://github.com/sgonzalezrozo/unity-enhanced-console) | Unity Editor log triage |
| [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core) | Shared injection interfaces |

## License

Released under the [MIT License](LICENSE).

# Troubleshooting & Limitations

[← Back to README](../README.md)

- [Troubleshooting](#troubleshooting)
- [Limitations](#limitations)
- [What's verified](#whats-verified)
- [Future development](#future-development)

---

## Troubleshooting

| Problem | Try this |
|---|---|
| `ScriptableSingleton already exists` or `assets located in immutable packages were unexpectedly altered` after adding packages | A package was added while Unity was still importing another. Nothing is broken: clear the Console. Next time, add Tools Core first and wait for the compile before adding the next package. If they come back when you open the project without adding a package, report it |
| `TryResolveScreenId FAILED` or `screen not found` | Check the screen name or ID, and the console for duplicate-name warnings. Click **Rebuild Registry from Assets** in the `ScreenRegistry` Inspector to repair it. |
| `ScreenSystem prefab not found` | Run **Tools > Screen System > Setup** |
| `AssetPath is empty` | Set the screen's asset path on its `ScreenDefinition` |
| `GoTo` does nothing | Check the console for `ignored: ... is still running`, an open modal or an open alert. The returned [`NavigationResult.Status`](api-reference.md#navigationresult) says which. |
| Escape quits the app | Create a `QuitConfirmation` modal, as shown in [Handle back at the root](code-examples.md#handle-back-at-the-root), or turn off **Quit On Back At Root** while migrating |
| An `Alert` screen won't close | Call `DismissAlert()`, because back navigation doesn't close alerts |
| `ScreenManager.Instance` is `null` or a navigation call says it isn't initialized | Await `ScreenManager.WhenReadyAsync()` first, as shown in [Open the first screen](getting-started.md#open-the-first-screen). If it returns `null`, run **Tools > Screen System > Setup** and check the console. |
| A layer doesn't appear | Check that its prefab is in `Resources` or Addressable under the name listed in the `LayerDefinition` |

For more diagnostics, open the Screen Manager window from the `ScreenManager` Inspector in Play Mode, or define `SCREEN_SYSTEM_VERBOSE` for informational logs. See [Editor Tools & Settings](editor-tools.md).

## Limitations

- Only Screen Space - Overlay canvases are verified; the blurred snapshot behind modals assumes them.
- The package declares Unity `2022.3` as its minimum but is only tested on Unity 6.
- Each tab of a bottom bar doesn't keep its own back stack; `ClearToRoot` returns to the root.

Some constraints are intentional rather than missing features. See [Core Concepts › By design](core-concepts.md#by-design).

## What's verified

**Verified:** the navigation rules, back stack, parameters, modal answers, results and injection order, covered by Edit Mode tests (a GitHub Actions workflow runs them once a Unity license is added to the repository); memory, handles, render budget, draw order and timings on an iPhone Air, an iPhone 13 mini, a Pixel 5 and a Samsung Galaxy A02s with IL2CPP; a WebGL build in a browser; and Enter Play Mode with domain reload off.

**Not verified yet:** Screen Space - Camera canvases, Unity versions before 6, a shipped game, and the VContainer and Zenject adapters in a running game (they're compiled against those containers).

## Future development

Screen System targets mobile and WebGL. For PC and consoles, these would be added:

- **Back from a gamepad:** today back comes from the Escape key and the Android back button. Mapping the Input System's UI Cancel action (gamepad B) to `GoBack()` would cover gamepads.
- **Focus when returning to a screen:** remembering the selected button when a screen hides and selecting it again when it returns, so gamepad and keyboard navigation pick up where they left off.

Planned work is tracked in [issues](https://github.com/sgonzalezrozo/unity-screen-system/issues).

# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

### Added

- `GoTo(screen, value)` passes a typed value to the screen. Every component on its layers that implements `IScreenParameter<T>` gets it once, before the layers are shown, including shared layers that are already visible. Back doesn't deliver it again, and Screen System doesn't keep it.
- `CloseAll()` hides every screen, modal and alert and empties the stack, to return to UI that Screen System doesn't manage.
- **Quit On Back At Root** in `ScreenSystemSettings`. Off, back at the root only raises `NavigationEvents.OnBackAtRoot`, for projects migrating one menu at a time.
- README guide to adopting Screen System in a project that already has hand-made UI.
- `GoTo(screen, NavigationHistory.ClearToRoot)` removes every screen above the root before opening a fullscreen screen, for tab bars. The root isn't shown on the way, and back returns to it.

### Fixed

- The back key with no Screen System screen open quit the app, so a project with its own UI closed on Escape or Android back. It now does nothing.
- Addressables handles leaked: layers loaded through Addressables were never released, so their assets stayed in memory for the whole session. Each address now has one handle, released on a single scene load unless a kept screen uses it. Cancelled and failed loads release their handle.
- Content loaders released their handle while the instance was alive, or never released it.
- **Persist Over Scenes** screens lost their layers on a scene change, and a persistent screen that was hidden came back visible.
- A navigation still loading when a scene changed blocked the new scene's first `SetRoot` and left its handle behind. Scene changes now cancel it.
- `ScreenDefinition` assets saved with the older nested `Behaviors` list lost **Persist Over Scenes** and **Ignore Navigation Rules**. They're read again.
- The navigation stack reversed the order of persistent screens on a scene change.
- `GoTo` for a screen already lower in the stack returned `false` after popping back to it. It now returns `true`.
- `GoBack` ignored during a transition at the root started the quit flow, which quits the app without a `QuitConfirmation` modal.
- Navigation calls made before initialization threw a `NullReferenceException`. They now log an error and return `false`.
- The `CancellationToken` passed to a navigation call was ignored once the screen started loading. Cancelling now stops the show, undoes the stack change and throws `OperationCanceledException`.
- UI Toolkit layers always counted as hidden, so a visible shared UI Toolkit layer was shown again with its callbacks.
- With domain reload disabled, the registry kept its index from the previous Play session, so screens added in between weren't found. Static state and `NavigationEvents` subscribers are reset at the start of each Play session.
- On a scene change with a modal open, the blur could stay on and its cache kept the destroyed layer.
- Hiding a UI Toolkit layer disabled its `UIDocument`, which rebuilt its visual tree on every show (about 330 KB per show on a Pixel 5). Its root is now hidden with `display: none`.
- Unused assets were unloaded during the scene change, which made the new scene's first screen wait for that work. They're unloaded once that screen is drawn.
- Screens made with the wizard couldn't open: the prefab wasn't loadable, the asset path was empty and the screen wasn't in the registry. A prefab dragged onto a `ScreenDefinition` or a `LayerDefinition` that wasn't Addressable failed at runtime or was ignored.

### Changed

- **Breaking:** the unused `ScreenParameters` component, `IScreenParameters` and the key/value `ScreenParameter` class are removed. Use `GoTo(screen, value)` and `IScreenParameter<T>`.
- The screen wizard creates `Feature/Screen/Screen.prefab` and `Feature/Services/Screen.asset` instead of `Screens/` and `Prefabs/` folders, sets each screen's type, marks the prefab Addressable, fills in its asset path and registers it. For UI Toolkit it creates the UXML and assigns it and the `GlobalPanelSettings`.
- The registry updates itself when a `ScreenDefinition` is created, deleted, moved or renamed, and warns about duplicate names and IDs. **Rebuild Registry Cache** is now a repair tool.
- Prefabs dragged onto a `ScreenDefinition` or `LayerDefinition` are marked Addressable, with their name as the address, unless they're in `Resources`.
- New `ScreenDefinition` assets get a `Feature_Screen` ID instead of a GUID. Existing IDs don't change.
- **Breaking:** `BlurBackgroundLayer` was rebuilt as the snapshot backdrop. The old fake glass overlay, its inspector, the **GameObject > UI > Blur Background Layer** menu and `BlurFadeController` are removed. Run Setup to rebuild the backdrop.

- On a single scene load, every screen is unloaded except those with **Persist Over Scenes**, the splash and the quit confirmation modal.
- AssetBundles stay open for the session once a screen needs them, while their assets are unloaded on scene changes. Closing and reopening a bundle on each scene change leaked about 29 MB of native memory per change on a Pixel 5.
- A call ignored during a transition now logs which call was running.
- A screen's layers that aren't loaded yet start loading together.
- Prefabs in local bundles finish loading in the same frame instead of waiting for later frames. A cold show on a Pixel 5 at 30 fps went from 99 ms (3 frames) to 15 ms. Remote bundles still load asynchronously.

### Added

- A blurred, darkened snapshot behind modals and alerts. The screen underneath stops rendering while a modal or alert is open, and an alert over a modal only dims the modal. Setup adds the backdrop to the `ScreenSystem` prefab, and running it again repairs an existing one. **Tools > Screen System > Rebuild Modal Backdrop** does the same. Settings: `BlurResolutionScale`, `AlertDimColor`, and the existing blur strength and tint.

- `ScreenManager.IsTransitioning`.
- `ScreenSystem.*` profiler markers for each synchronous step of showing, hiding, loading and scene cleanup.
- Benchmark metrics for scene change phases, time per step, allocations per screen and garbage collections, plus an Editor report of allocations by profiler marker.
- EditMode tests for navigation, the stack, the registry, layer order, the definition migration and the handle cache.
- A Benchmark sample that measures navigation time, allocations, idle cost, memory and handles across scene changes, and the overdraw window rule, with the project's own screens.

### Documentation

- An FAQ, rules for AI coding agents in `Documentation~/agent-rules.md`, `llms.txt` and more package keywords.

### Documentation

- Said that the package isn't used in production or in a shipped game yet, and described Tools Core as a required dependency.

### Changed

- The runtime assemblies expose their internals to `Assembly-CSharp`, so Fast IoC `0.4.0` assigns injected fields directly, without reflection.
- Console, Editor and setup messages use plain text instead of emojis. The setup-required message box is realigned.

## [0.3.0] - 2026-11-03

### Added

- `ScreenManager.DismissAlert()` and `ScreenManager.IsAlertActive`.
- **Ignore Navigation Rules** on `ScreenDefinition`.

### Fixed

- **Breaking:** `alert` screens now open as alerts. They used to behave like fullscreen screens.
- The `OnTransition...` events and `OnStackCleared` are now raised.

### Changed

- Updated to Tools Core `0.3.0`, which removes `IMaskSource`.

### Documentation

- Corrected the documentation for back at the root.
- Added recipes for modals, the quit flow, persistent screens, animation and injection.
- Added prefab requirements, screen lifetime and draw order diagrams, and shared layer behavior.
- Added contributing, security and issue template files, and a Quick Start sample.
- Added XML documentation to `ScreenManager`.

## [0.2.0] - Unreleased

### Changed

- **Breaking:** Renamed the namespaces from `Tools.ScreenSystem.Scripts` to `ScreenSystem`.
- **Breaking:** Renamed the assemblies to `ScreenSystem` and `ScreenSystem.Editor`.

## [0.1.2] - Unreleased

- Updated to Tools Core `0.2.0`.

## [0.1.1] - Unreleased

- Fixed Setup failing in a new project because the `Tools` folder wasn't created.
- Corrected the paths and steps in the setup dialog.

## [0.1.0] - Unreleased

First pre-release for private testing.

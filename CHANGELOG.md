# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

### Fixed

- Addressables handles leaked: layers loaded through Addressables were never released, so their assets stayed in memory for the whole session. Each address now has one handle, released on a single scene load unless a kept screen uses it. Cancelled and failed loads release their handle.
- Content loaders released their handle while the instance was alive, or never released it.
- **Persist Over Scenes** screens lost their layers on a scene change, and a persistent screen that was hidden came back visible.
- A navigation still loading when a scene changed blocked the new scene's first `SetRoot` and left its handle behind. Scene changes now cancel it.
- `ScreenDefinition` assets saved with the older nested `Behaviors` list lost **Persist Over Scenes** and **Ignore Navigation Rules**. They're read again.
- The navigation stack reversed the order of persistent screens on a scene change.

### Changed

- On a single scene load, every screen is unloaded except those with **Persist Over Scenes**, the splash and the quit confirmation modal.
- AssetBundles stay open for the session once a screen needs them, while their assets are unloaded on scene changes. Closing and reopening a bundle on each scene change leaked about 29 MB of native memory per change on a Pixel 5.
- A call ignored during a transition now logs which call was running.

### Added

- `ScreenManager.IsTransitioning`.
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

# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

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

# Changelog

All notable changes to this package are documented in this file.

## [Unreleased]

## [0.6.2] - 2026-11-19

### Changed

- `package.json` lists the author's email, santiago.gonzalezrozo@outlook.com, next to the name and GitHub URL.

## [0.6.1] - 2026-11-19

### Documentation

- The README version badge shows the current version; it had stayed at 0.5.1.

## [0.6.0] - 2026-11-19

### Changed

- **New Screen Feature** no longer has a Feature field. The folder you right-click is the feature: the screens go in it (`<Folder>/<Screen>/<Screen>.prefab`, `<Folder>/Services/<Screen>.asset`) and its name prefixes the screen IDs. Before, the wizard made a new feature folder inside the one you picked. Opened on a feature that already has screens, or from **Add Screen to Feature…**, it lists them and adds the new ones next to them; `Assets` itself isn't accepted as a feature.
- The wizard is a compact window that grows with its screen rows (a long list scrolls), with **Cancel** and **Create** buttons. New screens are named after the feature (`ShopMain`, then `ShopScreen2`...), so `GoTo` names stay unique. A screen created without a skipped one closes the window without a dialog and selects its prefab.

### Documentation

- The README Quickstart and Getting Started create the feature's folder first and right-click it. The old wizard screenshot is removed until a new one is captured.

## [0.5.3] - 2026-11-19

### Documentation

- The installation steps note that Tools Core is needed only by Screen System, Fast IoC and UI Scroller, and is added once.

## [0.5.2] - 2026-11-19

### Fixed

- `OnScreenShown` and `OnScreenHidden` no longer run on components whose GameObject is inactive, where starting a coroutine threw an error. Destroyed components are skipped too. See [Lifecycle interfaces](Documentation~/api-reference.md#lifecycle-interfaces).
- The ScreenManager Inspector no longer clips the TOP badge, a layer's icon or its Z order.

## [0.5.1] - 2026-11-18

### Documentation

- Screenshots of the Assets menu, the New Screen Feature wizard and the New Layer popup in the README's Quickstart and in Getting Started.

## [0.5.0] - 2026-11-18

### Added

- **Assets > Screen System > New Layer**: asks for a name and creates a layer prefab in the selected folder: a Canvas with a GraphicRaycaster, a CanvasGroup and a `LayerComponent`, and a full-screen Image inside. The prefab is marked Addressable with its name as the address.
- An **Edit** section in the `ScreenDefinition` Inspector, under Configuration: **Edit Prefab** opens the screen's prefab, and **Add Screen to Feature…** opens the screen wizard on the definition's feature.

### Removed

- The **Assets > Screen System > Add Screen to Feature** menu item. It showed on any folder; use **Add Screen to Feature…** in a `ScreenDefinition`'s Inspector instead. `FeatureCreatorWizard.AddScreenToFeature(path)` opens the wizard from code.

### Documentation

- The README's description is plain text instead of a quote, and short enough for two lines.

## [0.4.4] - 2026-11-18

### Fixed

- **Setup** warned `Addressables Settings not initialized` in a project that had never used Addressables. Setup now creates the Addressables settings first, so the screens it marks Addressable have somewhere to go.

### Removed

- The `ScreenContent` Addressables label that Setup added. Nothing used it.

## [0.4.3] - 2026-11-18

### Removed

- The welcome dialog shown once per project after the package is imported.

### Documentation

- The README is now a short overview with installation, a quickstart and links. Its content moved into guides in `Documentation~/`: Getting Started, Code Examples, Core Concepts, API Reference, Editor Tools & Settings, Performance, Migrating an Existing Project, Dependency Injection, AI Coding Agents, Troubleshooting & Limitations, FAQ and Project Status.
- Installation instructions no longer pin a release tag.

## [0.4.2] - 2026-11-17

### Changed

- `ScreenSystemSettings`: **Blur Fade Duration** and **Default Log Level** are hidden in the Inspector and marked obsolete. Neither did anything: the blurred snapshot appears with the modal, and informational logs depend on the `SCREEN_SYSTEM_VERBOSE` scripting define symbol. They'll be removed in a later version.
- `ScreenSystemSettings` tooltips say what each setting does, including that **Mobile Blur Strength** replaces **Blur Strength** on Android and iOS.

### Documentation

- README: a **Settings** section with a screenshot and every `ScreenSystemSettings` setting, and how to turn on informational logs.

### Fixed

- Two EventSystems at once, and a "There are 2 event systems in the scene" warning every frame, when an EventSystem appeared after Screen System had added its own, for example from a UI prefab or a scene object turned on later. Screen System now turns its own off as soon as another one is active, and its own is never counted in that warning. It still adds one whenever none is active.

## [0.4.1] - 2026-11-17

### Added

- After the package is imported, a dialog links to the documentation, once per project. **Tools > Screen System > Documentation** opens it at any time.
- `documentationUrl` in `package.json`, so the Package Manager links to the documentation.

### Changed

- **Tools > Screen System** only has **Setup** and **Documentation**. The other commands are in the Inspectors: **Create Special Screens** and **Open Screen Manager Window** on the `ScreenManager`, **Rebuild Registry from Assets** on the `ScreenRegistry`. Setup already rebuilds the modal backdrop, and `ScreenIds` regenerates on its own. The Benchmark sample runs from **Run Benchmark In Play Mode** in the `ScreenManager` Inspector instead of a menu.
- The splash screen that Setup creates says "Splash" instead of the product name.

### Fixed

- **New Screen Feature** puts the feature inside the folder you right-clicked (it showed that folder's parent as the parent folder). On a feature folder, the new feature goes next to it. With nothing selected, it uses the folder open in the Project window.
- `LayerDefinition`: layer prefabs can be dropped anywhere on the layers list, not only on its header.
- `ScreenManager` Inspector: the Select button no longer cuts off the **TOP** badge of the top screen.
- A splash or quit confirmation whose prefab was deleted (for example with its `Resources` folder) counted as existing, so Setup didn't offer to create it and Play Mode failed to load it. Setup and the `ScreenManager` Inspector now offer to create it again, and move its old `ScreenDefinition` to the trash.
- Buttons did nothing in a scene without an `EventSystem`, such as a new project's, so the quit confirmation's Quit and Cancel didn't respond. When no loaded scene has one, `ScreenManager` adds its own (with `InputSystemUIInputModule`) and turns it off while a scene brings its own.

## [0.4.0] - 2026-11-17

### Added

- Setup offers to create the splash (`SplashMain`) and quit confirmation (`QuitConfirmation`) screens, in `Assets/Boot`. Later, the `ScreenManager` Inspector warns while one is missing and creates them with a button. Screens that already exist are left alone.
- `QuitConfirmationLayer`: the Quit and Cancel buttons of a quit confirmation modal. Quit raises `NavigationEvents.OnQuitApplicationRequested` and quits; Cancel closes the modal.
- `GoToForResult<TResult>(screen)` opens a modal or alert and awaits its answer as a `ModalResult<TResult>` (`Returned`, `Dismissed` or `NotOpened`); the screen answers with `CloseWithResult(value)`. The result completes once the screen has closed, whatever closed it, so the caller's next navigation goes through. An overload passes a value to the screen's `IScreenParameter<T>` components.
- `ScreenIds`: a generated class with one constant per screen (`GoTo(ScreenIds.ShopMain)`), so typos and renamed or deleted screens are caught at compile time. It's regenerated when `ScreenDefinition`s change; turn it off or move it in `ScreenSystemSettings`.
- `ScreenAuthoring` (Editor) builds screens from code, for editor scripts and AI agents with editor tools: `CreateScreen`, `CreateLayer`, `AddLayer` with a placement, `RemoveLayer`, `GetLayers`, and `-executeMethod` entry points.
- `ScreenManager.WhenReadyAsync()` waits until Screen System is ready and returns the manager, or `null` if it failed or isn't set up. It replaces polling `ScreenManager.Instance` and then awaiting `WaitForInitializationAsync()`.
- `GoTo(screen, value)` passes a typed value to the screen. Every component on its layers that implements `IScreenParameter<T>` gets it once, before the layers are shown, including shared layers that are already visible. Back doesn't deliver it again, and Screen System doesn't keep it.
- `CloseAll()` hides every screen, modal and alert and empties the stack, to return to UI that Screen System doesn't manage.
- **Quit On Back At Root** in `ScreenSystemSettings`. Off, back at the root only raises `NavigationEvents.OnBackAtRoot`, for projects migrating one menu at a time.
- README guide to adopting Screen System in a project that already has hand-made UI.
- `GoTo(screen, NavigationHistory.ClearToRoot)` removes every screen above the root before opening a fullscreen screen, for tab bars. The root isn't shown on the way, and back returns to it.

### Fixed

- Screen System started a frame later than needed several times: initialization waited three frames and every show waited one more when it ran after an `await` (boot, chained navigation). They now run in the same frame. On a Galaxy A02s the splash shows 190 ms sooner, in the first frame instead of the fourth.
- The first show in the Editor, Development builds and `SCREEN_SYSTEM_METRICS` builds took over half a second longer on Android: metrics called `DateTime.Now`, whose first call loads the time zone database. They use `DateTime.UtcNow`.
- A layer could draw behind layers with a lower sorting order, such as a menu hidden under a full-screen background on the first screen after boot. Its order was assigned while its canvas was disabled, and Unity didn't always re-sort it when it was enabled. Showing a layer now re-applies its order so Unity re-sorts it. The benchmark checks it as `render.draw-order-mismatches`.
- A screen that couldn't be shown (its prefab didn't load, or an exception was thrown) made `GoTo` return `true` and stay in the stack. It now returns `Failed` and the stack is undone, so a failed modal isn't left open and a failed alert doesn't block navigation. `GoTo` for a screen already in the stack that pops back to it returns success.
- Layer components ran `Awake` and `OnEnable` before their dependencies were injected, and layers that already existed were injected again every time another screen used them. New layers are now created inactive, injected once, then activated. An instantiator marked `IInjectingPrefabInstantiator` (Tools Core 0.4.0, such as its VContainer and Zenject adapters) owns injection and isn't followed by a second pass; any other instantiator creates the copy and Screen System injects it. In the Editor benchmark, injection per cold show went from 10.4 ms to 0.01 ms (median).
- On WebGL, local Addressables loads tried to finish in the same frame with `WaitForCompletion`, which WebGL doesn't support for bundles. They stay asynchronous there.
- The package didn't compile in a project without the Screen Capture and UI Elements built-in modules. They're now declared as dependencies.
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

- **Breaking:** `ScreenType` values are `Fullscreen`, `Modal` and `Alert`, and the field is `ScreenDefinition.ScreenType`. Assets keep their values (they store the number, and the field reads its old name). The old `screenType` property still works, marked obsolete.
- **Breaking:** the internal services are `internal`: navigator, navigation and display services, caches, loaders, factories, hiders and their interfaces. The public API is `ScreenManager`, the assets and components (`ScreenDefinition`, `ScreenRegistry`, `ScreenSystemSettings`, `LayerDefinition`, `LayerComponent`, `BlurBackgroundLayer`), the lifecycle and parameter interfaces, `NavigationEvents`, `NavigationResult`, `NavigationHistory`, `ScreenType`, `ScreenVisibility` and `ScreenMetrics`.
- Metrics calls are compiled out of release builds, with their arguments, unless `SCREEN_SYSTEM_METRICS` is defined; they stay in the Editor and Development builds. Informational logs are compiled out unless `SCREEN_SYSTEM_VERBOSE` is defined. Warnings and errors are unchanged.
- Requires Tools Core 0.4.0.
- **Breaking:** unused types and services are removed: `LayerPrefabInstantiator`, `LayerPrefabInstantiatorFromAddressable`, the old glass effect (`ModalGlassEffect`, `GlassmorphismPanel`, `BlurEffectRenderer`), `IScreenContent`, `IScreenContentEvents`, `ScreenContentEvents`, `IShellTemplateProvider`, `NavigationTracker`, `ScreenIndexEntry`, `SerializableDictionary`, and `HideAllScreensAsync`, `RestoreAllScreensAsync` and `NavigateFromModalToScreenAsync` with their classes. With Fast IoC, regenerate its code after updating.
- **Breaking:** navigation calls return `NavigationResult` instead of `bool`. It converts to `bool`, so `if (await GoTo(...))` and `bool ok = await GoTo(...)` still compile; code that types the task as `Task<bool>` needs `Task<NavigationResult>`. `Status` says why a call didn't happen.
- The Screen Manager window shows what the next back does, what's rendering from front to back (including the blurred snapshot and the dim), and the back stack down to the root with what back does at each step. Layers show whether they render, are hidden or are suspended behind the blur, and which are shared. Buttons for Back, Dismiss Alert, Close All and Pop To.
- `ScreenManager.Instance` is set in `Awake` instead of `Start`, so it's available when the first scene's objects run `Awake`.
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

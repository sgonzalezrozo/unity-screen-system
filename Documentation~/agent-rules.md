# Screen System rules for AI coding agents

Copy this file into your project's agent rules (for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/screen-system.md`).

## How navigation works here

- This project navigates UI with **Screen System** (`com.sgonzalezrozo.screen-system`). Don't load scenes, toggle panels with `SetActive`, or instantiate screen prefabs yourself to show a screen.
- The entry point is `ScreenManager.Instance` (namespace `ScreenSystem`). `ScreenSystem` is the namespace, not a class.
- Each screen is a prefab with a `ScreenDefinition` asset. Screens are created with **Assets > Screen System > New Screen Feature**, which puts the prefab in `Feature/Screen/` and the `ScreenDefinition` in `Feature/Services/`, marks the prefab Addressable and registers it. The registry updates itself when definitions change.
- Never write `.prefab` or `.asset` files as text. Describe the prefab and the Inspector steps instead. With Unity editor tools (an MCP server or `-executeMethod`), create screens by calling `ScreenSystem.Editor.FeatureCreatorWizard.CreateScreen(featureDir, feature, screenName, type, framework)`, which does everything the wizard does.
- In a project that still has hand-made UI, migrate one menu at a time: `GoTo`/`GoBack` replace `SetActive` for migrated menus, and `NavigationEvents.OnBackAtRoot` + `ScreenManager.Instance.CloseAll()` return to the hand-made menu. Don't touch the menus not being migrated.

## Layers

- A screen is a prefab plus the layers listed in its `LayerDefinition` (edited in the Inspector). Repeated parts, such as a top bar or a background, are layer prefabs shared by name, not copied into each screen.
- Don't set `Canvas.sortingOrder` or UI Toolkit sorting by hand on screens or layers. The system sets it.
- Don't unload screens, release Addressables handles or disable covered screens yourself. Scene changes and navigation do it.

## Navigate

All methods return `Task<bool>` (`false` when blocked or unknown) and take an optional `CancellationToken`:

```csharp
ScreenManager screens = ScreenManager.Instance;

await screens.GoTo("ShopDetail");
await screens.GoTo("Store", NavigationHistory.ClearToRoot); // tab bar: Root, Store
await screens.GoBack();
await screens.PopTo("ShopMain");
await screens.SetRoot("Lobby");
```

Also: `CanGoBack`, `GetCurrentScreen()`, `DismissAlert()`, `IsAlertActive`, `IsTransitioning`. A screen argument is its name (from the prefab) or its ID.

- At boot, `ScreenManager screens = await ScreenManager.WhenReadyAsync();` then `SetRoot` if it isn't `null`. Don't poll `ScreenManager.Instance` in a loop. `Instance` is set before the first scene's `Awake`, but may still be initializing.
- Wrap `async void` Unity methods in `try`/`catch` and log the exception.
- A tab bar is a layer shared by the tab screens. Its buttons call `GoTo(tab, NavigationHistory.ClearToRoot)`, so back returns to the root instead of walking through tabs. It works for fullscreen screens only.
- Pass what a navigation is about with `GoTo(screen, value)`; components on any of the screen's layers receive it by implementing `IScreenParameter<T>` (`OnScreenParameter(T value)`), once, before `OnScreenShown`. It isn't delivered again on back and Screen System doesn't keep it, so the component stores what it needs. Keep state shared by several screens in a Model.

## React to a screen

Components on the screen prefab implement `IScreenShow` (`OnScreenShown()`), `IScreenHide` (`OnScreenHidden()`) or both through `IScreenLifecycle`. Hidden screens stay active (only their `Canvas` or UI Toolkit root is hidden), so don't rely on `OnEnable` or `OnDisable` for show and hide.

Static events live on `NavigationEvents`, such as `OnNavigationCompleted` and `OnNavigationFailed`. Unsubscribe when the object is destroyed.

## Don't

- Don't call `GoTo` while another navigation call is running and expect it to run: it returns `false`. Check `IsTransitioning`.
- Screen System has no screen transitions or animations. Animate inside layers from `OnScreenShown()`/`OnScreenHidden()`; a layer shared with the previous screen doesn't get them again.
- Don't expect `GoBack()` to close an alert. Call `DismissAlert()`.
- Don't invent methods. If a member isn't listed here or in the README, read the package source.

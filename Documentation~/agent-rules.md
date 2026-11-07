# Screen System rules for AI coding agents

Copy this file into your project's agent rules (for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/screen-system.md`).

## How navigation works here

- This project navigates UI with **Screen System** (`com.sgonzalezrozo.screen-system`). Don't load scenes, toggle panels with `SetActive`, or instantiate screen prefabs yourself to show a screen.
- The entry point is `ScreenManager.Instance` (namespace `ScreenSystem`). `ScreenSystem` is the namespace, not a class.
- Each screen is a prefab with a `ScreenDefinition` asset. Screens are created with **Assets > Screen System > New Screen Feature**. After adding or renaming a screen, the developer runs **Tools > Screen System > Rebuild Registry Cache**.
- Never write `.prefab` or `.asset` files as text. Describe the prefab and the Inspector steps instead, or use Unity editor tools if you have them.

## Navigate

All methods return `Task<bool>` (`false` when blocked or unknown) and take an optional `CancellationToken`:

```csharp
ScreenManager screens = ScreenManager.Instance;

await screens.GoTo("ShopDetail");
await screens.GoBack();
await screens.PopTo("ShopMain");
await screens.SetRoot("Lobby");
```

Also: `CanGoBack`, `GetCurrentScreen()`, `DismissAlert()`, `IsAlertActive`. A screen argument is its name (from the prefab) or its ID.

- `ScreenManager.Instance` is set in `Start()`. At boot, wait until it's not `null`, then `await ScreenManager.Instance.WaitForInitializationAsync()` before the first `SetRoot`.
- Wrap `async void` Unity methods in `try`/`catch` and log the exception.
- Navigation can't pass parameters to a screen. Put shared data in a Model the screen reads.

## React to a screen

Components on the screen prefab implement `IScreenShow` (`OnScreenShown()`), `IScreenHide` (`OnScreenHidden()`) or both through `IScreenLifecycle`. Use `IScreenContent` for `InitializeAsync()` and `Cleanup()`.

Static events live on `NavigationEvents`, such as `OnNavigationCompleted` and `OnNavigationFailed`. Unsubscribe when the object is destroyed.

## Don't

- Don't call `GoTo` during a transition and expect it to run: it returns `false`.
- Don't expect `GoBack()` to close an alert. Call `DismissAlert()`.
- Don't invent methods. If a member isn't listed here or in the README, read the package source.

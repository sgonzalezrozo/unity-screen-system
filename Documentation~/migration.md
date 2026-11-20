# Migrating an Existing Project

[← Back to README](../README.md)

You don't have to migrate everything at once. Screen System stays out of the way until you open a screen, so your hand-made menus and its screens can live side by side while you move them over one at a time.

- [What installing it changes](#what-installing-it-changes)
- [Migrate one menu](#migrate-one-menu)
- [While both kinds of UI are on screen](#while-both-kinds-of-ui-are-on-screen)
- [When the last menu is migrated](#when-the-last-menu-is-migrated)

---

## What installing it changes

One `ScreenSystem` object is created in DontDestroyOnLoad. It opens nothing until you call `GoTo` or `SetRoot`, ignores the back key while none of its screens is open, uses your scene's `EventSystem` (or adds its own when no scene has one), and only unloads its own screens on scene changes. Your scene UI is untouched.

## Migrate one menu

1. Run **Tools > Screen System > Setup**, then turn off **Quit On Back At Root** in `ScreenSystemSettings`. While your main menu is still hand-made, back at the first Screen System screen should return to it, not quit.
2. Create the screen with **Assets > Screen System > New Screen** and move the panel's content into its prefab. Move its `OnEnable`/`OnDisable` code to `IScreenShow`/`IScreenHide`.
3. Replace the code that showed and hid the panel:

```csharp
// Before
shopPanel.SetActive(true);
shopPanel.SetActive(false);

// After
await ScreenManager.Instance.GoTo("Shop");
await ScreenManager.Instance.GoBack();
```

4. Hand back to your UI when the player backs out of the first Screen System screen:

```csharp
private void OnEnable() => NavigationEvents.OnBackAtRoot += ReturnToMenu;
private void OnDisable() => NavigationEvents.OnBackAtRoot -= ReturnToMenu;

private async void ReturnToMenu()
{
    try
    {
        await ScreenManager.Instance.CloseAll(); // hides every screen and empties the stack
        mainMenuPanel.SetActive(true);           // your hand-made menu
    }
    catch (Exception ex)
    {
        Debug.LogException(ex);
    }
}
```

Closed screens stay loaded until the next scene change, so opening them again is fast.

## While both kinds of UI are on screen

- **Draw order:** Screen System's canvases are Screen Space Overlay with sorting orders from 10 up. Keep your overlay canvases below 10 to draw behind its screens, or well above to draw over them, such as a HUD or a loading curtain.
- **Back key:** while a Screen System screen is open, it handles Escape and Android back. Your own back handler should skip when `ScreenManager.Instance.GetCurrentScreen()` isn't `null`.
- **Visibility:** don't `SetActive` a screen's objects yourself. Open and close it through `ScreenManager`.

## When the last menu is migrated

Open your first screen with `SetRoot` at boot, turn **Quit On Back At Root** back on, add a [`QuitConfirmation` modal](code-examples.md#handle-back-at-the-root), and remove the `OnBackAtRoot` handler.

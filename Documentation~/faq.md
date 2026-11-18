# FAQ

[← Back to README](../README.md)

**About the project**
- [Is it right for my project?](#is-it-right-for-my-project)
- [Is it production-ready? Does anyone use it?](#is-it-production-ready-does-anyone-use-it)
- [Why was it built?](#why-was-it-built)
- [Is it free for commercial use?](#is-it-free-for-commercial-use)

**Compatibility**
- [Does it work with Unity 6? With older versions?](#does-it-work-with-unity-6-with-older-versions)
- [Does it work with uGUI and UI Toolkit?](#does-it-work-with-ugui-and-ui-toolkit)
- [Does it work with IL2CPP, mobile, WebGL and iOS?](#does-it-work-with-il2cpp-mobile-webgl-and-ios)
- [Does it work with Screen Space - Camera or World Space canvases?](#does-it-work-with-screen-space---camera-or-world-space-canvases)
- [Do I have to use Addressables?](#do-i-have-to-use-addressables)
- [Does it work with dependency injection? Do I need Fast IoC?](#does-it-work-with-dependency-injection-do-i-need-fast-ioc)
- [Does it work with AI coding agents?](#does-it-work-with-ai-coding-agents)

**Usage**
- [How do I manage screens, popups and modals?](#how-do-i-manage-screens-popups-and-modals)
- [How do I reuse a top bar or background across screens?](#how-do-i-reuse-a-top-bar-or-background-across-screens)
- [Should I use scenes or prefabs for UI screens?](#should-i-use-scenes-or-prefabs-for-ui-screens)
- [Can I pass parameters to a screen?](#can-i-pass-parameters-to-a-screen)
- [Can a modal return a result, like a confirmation?](#can-a-modal-return-a-result-like-a-confirmation)
- [How do I switch tabs from a bottom bar?](#how-do-i-switch-tabs-from-a-bottom-bar)
- [How do I animate a screen?](#how-do-i-animate-a-screen)
- [Can I migrate a project that already has hand-made UI?](#can-i-migrate-a-project-that-already-has-hand-made-ui)
- [Any recommended practices?](#any-recommended-practices)

**Design and internals**
- [Why a single ScreenManager? How do I test my code without it?](#why-a-single-screenmanager-how-do-i-test-my-code-without-it)
- [Why are navigation calls ignored instead of queued?](#why-are-navigation-calls-ignored-instead-of-queued)
- [Why only one modal and one alert at a time?](#why-only-one-modal-and-one-alert-at-a-time)
- [Does it unload screens when the scene changes?](#does-it-unload-screens-when-the-scene-changes)
- [What happens to memory and AssetBundles?](#what-happens-to-memory-and-assetbundles)
- [How fast is it?](#how-fast-is-it)
- [How do I see what's on screen and what back will do?](#how-do-i-see-whats-on-screen-and-what-back-will-do)

---

## About the project

### Is it right for my project?

**Use Screen System if you:**

- Have many screens, overlays or dialogs that share parts
- Want one source of truth for the back stack
- Use uGUI, UI Toolkit or both
- Load content with Addressables and care about memory across scenes
- Want editor scaffolding, diagnostics and measurable performance
- Have a project in progress with hand-made UI: you can [migrate one menu at a time](migration.md)

**Consider something else if you** have only a few screens and simple scene changes.

### Is it production-ready? Does anyone use it?

Not yet. Screen System is a pre-release package (`0.x`) by a single author. It isn't used in a shipped game, and it has no other users yet, so expect rough edges and API changes before `1.0.0`. Bug reports are very welcome. See [What's verified](troubleshooting.md#whats-verified).

### Why was it built?

Most projects I worked on had no formal layer controller. Their screens repeated other prefabs instead of reusing them, even with nested and variant prefabs.

My first time using one was on a recent project, with a custom layer system. That experience, and remembering how apps like Photoshop handle layers, made me want my own tool: a more robust system that covers all my use cases without time-based loops or other detection routines that hurt the game's performance.

I built it for uGUI. With the canvas set to overlay, it also works with uGUI and UI Toolkit at the same time.

I built and refined it on my own time in personal projects. In November 2026 I extracted it into this standalone package and documented it. Earlier history lives in a private repository.

### Is it free for commercial use?

Yes. It's MIT licensed.

## Compatibility

### Does it work with Unity 6? With older versions?

It's tested on Unity `6000.3`. The package declares `2022.3` as its minimum, but older versions aren't tested yet. See [Requirements](getting-started.md#requirements).

### Does it work with uGUI and UI Toolkit?

Yes. Each `ScreenDefinition` sets its framework, and both can be used at once when the canvas uses Screen Space - Overlay.

### Does it work with IL2CPP, mobile, WebGL and iOS?

Yes. It's benchmarked with IL2CPP on iOS (iPhone Air, iPhone 13 mini) and Android (Pixel 5, Samsung Galaxy A02s). A WebGL Development build was run in a browser: screens load from Addressables and navigation works, with layers always loading asynchronously there. On WebGL and iOS, apps don't quit themselves, so leave out the quit confirmation and turn off **Quit On Back At Root**.

### Does it work with Screen Space - Camera or World Space canvases?

It's built and tested for Screen Space - Overlay, the default **Canvas Render Mode** in `ScreenSystemSettings`. Other render modes aren't verified, and the blurred snapshot behind modals assumes overlay canvases.

### Do I have to use Addressables?

Addressables is a dependency, but screens and layers can also load from a `Resources` folder: an asset path is tried in Resources first. The wizard marks prefabs Addressable unless they're in a `Resources` folder.

### Does it work with dependency injection? Do I need Fast IoC?

It works with any container, or none. Screen System depends only on the injection interfaces in Tools Core. Fast IoC registers itself, VContainer takes `builder.UseToolsCore()` and Zenject takes `ToolsCoreInstaller`, both from Tools Core 0.4.0. Layers are injected once, before their `Awake`. See [Dependency Injection](dependency-injection.md).

### Does it work with AI coding agents?

It ships a rules file you copy into your project, so agents use its real API instead of guessing. See [AI Coding Agents](ai-agents.md).

## Usage

### How do I manage screens, popups and modals?

Create each screen as a prefab with a `ScreenDefinition`, then navigate by name: `await ScreenManager.Instance.GoTo("ShopDetail")`. Screen System keeps the back stack and the modal and alert layers. See [Create your first screen](getting-started.md#create-your-first-screen).

### How do I reuse a top bar or background across screens?

Make it a layer prefab and add it to each screen's `LayerDefinition`. Screens that list it share one instance, and its draw order follows the topmost screen using it. See [Core Concepts › Layers](core-concepts.md#layers).

### Should I use scenes or prefabs for UI screens?

Screen System uses prefabs, so UI stays loaded across navigation and a back stack is cheap. Load a scene when the game world changes, not for each menu. Screens marked **Persist Over Scenes** stay available across scene loads.

### Can I pass parameters to a screen?

Yes. Pass any type with `GoTo(screen, value)`, and implement `IScreenParameter<T>` on the components that need it, on any layer of the screen. See [Pass a value to a screen](code-examples.md#pass-a-value-to-a-screen).

### Can a modal return a result, like a confirmation?

Yes. Open it with `GoToForResult<bool>(screen)` and await a `ModalResult<bool>`; the modal answers with `CloseWithResult(true)`. Back or any other close completes it as `Dismissed`. See [Get an answer from a modal](code-examples.md#get-an-answer-from-a-modal).

### How do I switch tabs from a bottom bar?

Make the bar a layer shared by the tab screens, and call `GoTo(tab, NavigationHistory.ClearToRoot)` from its buttons. Back then returns to the root instead of walking through the tabs. See [Switch tabs from a bottom bar](code-examples.md#switch-tabs-from-a-bottom-bar).

### How do I animate a screen?

Transitions depend on each game's art direction, so animations belong to the layers, not to Screen System: start an intro from `OnScreenShown()`, and play an outro before calling `GoTo`. See [Animate a screen](code-examples.md#animate-a-screen).

### Can I migrate a project that already has hand-made UI?

Yes, one menu at a time. Screen System stays out of the way until you open one of its screens, ignores the back key while none is open, and `CloseAll()` hands the screen back to your own UI. See [Migrating an existing project](migration.md).

### Any recommended practices?

- Split repeated parts of your screens into layers and share them.
- Keep screen names unique and stable, and navigate with the generated [`ScreenIds`](api-reference.md#screen-ids).
- Use a single root screen for your app's boot flow.
- Prefer stack-based navigation over direct scene loads when UI should stay mounted.

## Design and internals

### Why a single ScreenManager? How do I test my code without it?

A game has one UI, so there's one `ScreenManager`, created before the first scene loads and kept in DontDestroyOnLoad. To test your own code, depend on `IScreenNavigationService`, which `ScreenManager` implements, and pass a fake in your tests. The package's own navigation logic is tested with fakes too, without the singleton.

### Why are navigation calls ignored instead of queued?

A navigation either happens now or it doesn't. A queue would replay taps the player made while a screen was loading, and could open screens they no longer want. A call made during a transition returns `Transitioning`; check `IsTransitioning` or the result if you need to know.

### Why only one modal and one alert at a time?

So at most one fullscreen screen, one modal and one alert render at once, and back always has one obvious thing to close. To show a next step, change the content of the open modal or close it first. See [One modal and one alert at a time](code-examples.md#one-modal-and-one-alert-at-a-time).

### Does it unload screens when the scene changes?

Yes. A single scene load unloads every screen except persistent ones, the splash and the quit confirmation, and releases their Addressables handles. See [Core Concepts › Memory](core-concepts.md#memory).

### What happens to memory and AssetBundles?

Each Addressables address has one handle, held while a screen uses it and released on a single scene load unless a kept screen needs it. AssetBundles stay open for the session once a screen needs them, while their assets are unloaded on scene changes, because closing and reopening a bundle on every scene change leaked native memory on Android.

### How fast is it?

A cached screen shows in about 1 ms on an iPhone Air or iPhone 13 mini, 3.0 ms on a Pixel 5 and 5.0 ms on a Galaxy A02s, and memory stays flat across scene changes on all four. See [Performance](performance.md), and run the benchmark on your own screens.

### How do I see what's on screen and what back will do?

In Play Mode, click **Open Screen Manager Window** in the `ScreenManager` Inspector. It shows what the next back does, what's rendering from front to back, and the back stack down to the root with each screen's layers. Each step also has a `ScreenSystem.*` profiler marker. See [Editor Tools](editor-tools.md).

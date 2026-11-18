# AI Coding Agents

[← Back to README](../README.md)

Coding agents write C# files, and this package is newer than their training data. Give them its rules so they use the real API:

1. Copy [`agent-rules.md`](agent-rules.md) into your project's agent rules, for example `AGENTS.md`, `CLAUDE.md` or `.cursor/rules/screen-system.md`.
2. For a whole Unity architecture with a verify loop, add [Unity Architecture Agents](https://github.com/sgonzalezrozo/unity-architecture-agents). It detects this package and adapts its code.

It hasn't been tested with agents beyond the author's own projects.

## Build screens from code

An agent connected to the Unity Editor (through an MCP server such as Coplay, or with `-executeMethod`) can build complete screens with `ScreenSystem.Editor.ScreenAuthoring`, which does what the wizard and the `LayerDefinition` Inspector do:

```csharp
using ScreenSystem;
using ScreenSystem.Editor;

ScreenDefinition shop = ScreenAuthoring.CreateScreen("Assets/Client/Shop", "ShopMain");
GameObject background = ScreenAuthoring.CreateLayer("Assets/Client/Shop/ShopMain", "ShopBackground");
GameObject topBar = ScreenAuthoring.CreateLayer("Assets/Client/Shared", "TopBar");

ScreenAuthoring.AddLayer(shop, background, LayerPlacement.Back);
ScreenAuthoring.AddLayer(shop, topBar, LayerPlacement.Front);

// SCREEN marks where the screen prefab draws: ShopBackground, SCREEN, TopBar
List<string> order = ScreenAuthoring.GetLayers(shop);
```

| Member | Purpose |
|---|---|
| `CreateScreen(featureDir, name, type, framework)` | Create a screen in the rules layout, loadable and registered. Returns the existing one if it's already there. |
| `CreateLayer(folder, name, framework)` | Create a layer prefab with its own `Canvas` or `UIDocument`, loadable |
| `AddLayer(screen, layerPrefab, placement)` | Add a layer at `Back`, `BehindScreen`, `InFrontOfScreen` or `Front`. Adding it twice does nothing. |
| `RemoveLayer(screen, layerName)` | Remove a layer |
| `GetLayers(screen)` | The layers from back to front, with `SCREEN` for the screen prefab |
| `FindScreen(nameOrId)`, `FindScreenPrefabPath(screen)` | Look up a screen and its prefab |

### From the command line

Without an editor connection, the same steps run in batch mode:

```bash
Unity -batchmode -quit -projectPath . -executeMethod ScreenSystem.Editor.ScreenAuthoring.AddLayerFromCommandLine -screen ShopMain -layer Assets/Client/Shared/TopBar.prefab -placement Front
```

`CreateScreenFromCommandLine` and `CreateLayerFromCommandLine` work the same way.

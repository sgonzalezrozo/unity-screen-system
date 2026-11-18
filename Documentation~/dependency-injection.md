# Dependency Injection

[← Back to README](../README.md)

Screen System doesn't depend on any container. It only uses the two interfaces in [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core), so it works with Fast IoC, Zenject (Extenject), VContainer or no container at all:

| Interface | What Screen System does with it |
|---|---|
| `IPrefabInstantiator` | Creates each layer from its prefab |
| `IMonoBehaviourInjector` | Injects the components of each new layer and its content, once, before their `Awake` runs |

A new layer is created under an inactive holder, injected, then moved into place, so its components' `Awake` and `OnEnable` already have their dependencies. Layers that already exist aren't injected again. When the instantiator is marked `IInjectingPrefabInstantiator` (it injects as it creates, like a container's `InstantiatePrefab`), Screen System skips its own pass, so nothing is injected twice.

## Connect your container

- **No container:** the defaults use standard `Instantiate` and inject nothing.
- **[Fast IoC](https://github.com/sgonzalezrozo/unity-fast-ioc):** registers itself, so screens are injected with no setup. The package exposes its internals to `Assembly-CSharp`, so Fast IoC `0.4.0` and later assign its `internal` fields directly, without reflection.
- **VContainer:** call `builder.UseToolsCore()` in your root `LifetimeScope`. See [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core#use-vcontainer).
- **Zenject (Extenject):** add `ToolsCoreInstaller` to your `ProjectContext`. See [Tools Core](https://github.com/sgonzalezrozo/unity-tools-core#use-zenject-extenject).
- **Any other container:** implement both interfaces with your container and assign them to `ToolInjectionServices` before the first scene loads. Mark the instantiator `IInjectingPrefabInstantiator` if it injects what it creates.

The VContainer and Zenject adapters are compiled against those containers but not yet verified in a running game.

For an example screen with an injected service, see [Inject services into a screen](code-examples.md#inject-services-into-a-screen).

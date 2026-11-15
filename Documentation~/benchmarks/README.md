# Benchmarks

The [Benchmark sample](../../Samples~/Benchmark/README.md) measures these metrics with a project's own screens. Results from the Editor are only for comparing changes on one machine. Published numbers come from IL2CPP builds on a device, with the commit recorded.

## Base metrics

| Metric | Meaning | Goal |
|---|---|---|
| `cold-show.ms` | `GoTo` until it returns, for a screen that isn't loaded. Median and p95. | Lower |
| `cold-show.<screen>.ms` | The same, per screen | Lower |
| `warm-show.ms` | `GoTo` for a cached screen | Same frame |
| `warm-back.ms` | `GoBack` to a cached screen | Same frame |
| `modal-open-cold.ms`, `modal-open-warm.ms`, `modal-close.ms` | Modal open and close | Lower |
| `*.frames` | Frames between the call and its return | 0 for cached screens |
| `*.alloc` | Bytes allocated on the main thread during the call | 0 B for cached screens |
| `*.max-frame-ms` | Longest frame during any call of that kind | Under one frame budget |
| `*.failures` | Calls that returned `false` | 0 |
| `idle.root-only.*`, `idle.all-cached.*` | Frame time, allocations per frame, batches and SetPass calls with nothing happening | No difference between the two |
| `scene-change.load` | Single scene load until the scene's start flow settles | Lower |
| `scene-change.layers-after-cleanup` | Layer instances left after a scene change | Persistent layers only |
| `scene-change.*-growth` | Managed memory, native memory and layer count, last cycle minus first | 0 |
| `addressables.bundles-reopened-total` | AssetBundles closed and opened again across scene cycles. Each reopen leaked about 29 MB of native memory on a Pixel 5. | 0 |
| `addressables.bundles-held` | AssetBundles kept open. A bundle opens the first time a screen needs it and stays open for the session. | Only bundles in use |
| `addressables.orphan-handles` | Addressables handles held for an address that no loaded screen uses. Any value above 0 is a leak. | 0 |
| `scene-change.handles-after-cleanup`, `scene-change.handle-growth` | Handles held after a scene change, and the change from the first cycle to the last | Only kept screens; growth 0 |
| `persistent.layers-alive-ratio` | Layers of a persistent screen still alive after a scene change | 1 |
| `persistent.visible-after-scene-start` | 1 when a persistent screen shown before the load is still visible after the new scene opens its root | 0 |
| `render.window-rule-violations` | Navigations that left more than one fullscreen, modal or alert rendering, or a covered layer rendering | 0 |
| `render.layers-rendering` | Layers rendering after a navigation. Median and max. | Lower |
| `layers.instances-all-loaded` | Layer instances with every screen loaded. Lower than the sum of each screen's layers when layers are shared. | Lower |
| `logs.per-navigation` | Console lines per navigation call | 0 in Release |
| `quit-requests` | Times the quit flow ran | 0 |

## Layer metrics

Layers are the package's main feature, so the benchmark reports how much sharing saves. `layers.instances-all-loaded` against the sum of each screen's layer count gives the instances saved by shared layers.

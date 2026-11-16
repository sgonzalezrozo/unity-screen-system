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
| `render.fullscreens-under-modal` | Fullscreen screens still rendering while a modal is open. With the backdrop, the modal's blurred snapshot replaces the screen. | 0 |
| `blur.last-snapshot-ms`, `*.step.Snapshot.ms` | Main thread cost of capturing and blurring the snapshot when a modal opens | Lower |
| `alert.snapshot-over-fullscreen` | 1 when an alert over a fullscreen screen gets the blurred snapshot | 1 |
| `alert.dim-over-modal`, `alert.modal-rendering-under-alert`, `alert.dim-between-modal-and-alert` | An alert over a modal: the dim shows, the modal keeps rendering, and the dim sits above every modal layer and below every alert layer | 1 |
| `alert.dim-hidden-after-dismiss` | 1 when dismissing the alert removes the dim | 1 |
| `result.returned`, `result.dismissed-by-back` | `GoToForResult` on a real modal: `CloseWithResult(true)` returns `true`, and back completes it as `Dismissed` | 1 |
| `result.next-goto-after-answer` | 1 when the caller's `GoTo` right after the answer goes through, because the result completes once the modal has closed | 1 |
| `render.layers-rendering` | Layers rendering after a navigation. Median and max. | Lower |
| `layers.instances-all-loaded` | Layer instances with every screen loaded. Lower than the sum of each screen's layers when layers are shared. | Lower |
| `*.gc-collections`, `*.gc-ms` | Garbage collections and time spent in GC during a kind of call, an idle window or a scene change | 0 for cached screens and idle |
| `gc.collections-total`, `gc.ms-total`, `gc.max-frame-ms` | GC over the whole run, without the benchmark's own `GC.Collect` calls, and the longest GC frame | Lower |
| `scene-change.activation-ms`, `cleanup-ms`, `until-root-start-ms`, `root-show-ms`, `total-to-root-ms` | A scene change split into Unity's activation, the package's cleanup, the wait for the scene to ask for its root, and the root's load. `total-to-root-ms` is what the player waits for. | Lower |
| `*.step.<Marker>.ms` | Time in each `ScreenSystem.*` profiler marker during a kind of call: `LoadPrefab`, `InstantiateLayer`, `InjectLayer`, `ReassignLayers`, `PrepareLayers`, `ActivateLayers`, `HideLayers`, `LifecycleCallbacks`, `Metrics` | Lower |
| `warm-show.<Screen>.alloc` | Allocations per warm show of each screen, to compare UI Toolkit and uGUI screens | 0 |
| `allocations-*.md` (Editor only) | Warm show allocations split by profiler marker, from Profiler frames recorded during two warm repetitions | — |
| `logs.per-navigation` | Console lines per navigation call | 0 in Release |
| `quit-requests` | Times the quit flow ran | 0 |

## Layer metrics

Layers are the package's main feature, so the benchmark reports how much sharing saves. `layers.instances-all-loaded` against the sum of each screen's layer count gives the instances saved by shared layers.

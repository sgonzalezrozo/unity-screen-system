# Performance

[← Back to README](../README.md)

Measured with the [Benchmark sample](../Samples~/Benchmark/README.md) on the screens of a test project: 8 screens mixing uGUI and UI Toolkit, two scenes, Addressables. IL2CPP Development builds at 30 fps.

| Measure | iPhone Air (iOS 27) | iPhone 13 mini (iOS 26.5) | Pixel 5 (Android 14) | Galaxy A02s (Android 12) |
|---|---|---|---|---|
| Show a cached screen | 0.95 ms | 1.1 ms | 3.0 ms | 5.0 ms |
| Show a screen that isn't loaded | 5.5 ms median | 7.1 ms median | 12 ms median | 22 ms median |
| Inject a new layer's components | 0.02 ms | 0.02 ms | 0.05 ms | 0.08 ms |
| Go back | 0.9 ms | 1.0 ms | 2.8 ms | 4.0 ms |
| Open a modal, with its blurred snapshot | 34 ms (one frame) | 34 ms (one frame) | 36 ms (one frame) | 38 ms (one frame) |
| Capture and blur the snapshot (main thread) | 0.09 ms | 0.12 ms | 0.2 ms | 0.3 ms |
| Scene change until the new scene's first screen is visible | 85 ms, of which 67 ms is Unity's scene activation | 85 ms, of which 67 ms is Unity's | 134 ms, of which 76 ms is Unity's | 215 ms, of which 96 ms is Unity's |
| Allocations per cached show, uGUI and UI Toolkit | 12 to 21 KB | 12 to 21 KB | 12 to 21 KB | 6 to 12 KB |
| Allocations when opening a modal / an alert | 15 KB / 20 KB | 15 KB / 20 KB | 15 KB / 20 KB | 8 KB / 10 KB |
| Memory after 12 scene changes | flat (170.7 MB to 171.0 MB) | flat (170.6 MB to 171.1 MB) | flat (207.0 MB to 207.4 MB) | flat (190.0 MB to 190.2 MB) |
| Addressables handles left behind | 0 | 0 | 0 | 0 |
| Fullscreen screens rendering under an open modal | 0 | 0 | 0 | 0 |
| Navigations that broke the three-window rule | 0 of 284 | 0 of 284 | 0 of 284 | 0 of 284 |
| `GoToForResult`: answer returned, back dismissed, next `GoTo` right after | all pass | all pass | all pass | all pass |

Measured one run per device, iOS on commit `035b7bd` and Android on `b0a9a6c` (the only runtime change between them re-sorts canvases on show). Take the numbers as a guide: run the benchmark on your own screens.

## Run it on your project

- **In the Editor:** import the Benchmark sample (Package Manager > Screen System > Samples), then click **Run Benchmark In Play Mode** in the `ScreenManager` Inspector.
- **On a device:** add `ScreenSystemBenchmarkRunner` to an object in your first build scene and make an IL2CPP build. See the [sample's README](../Samples~/Benchmark/README.md).

[What each metric means](benchmarks/README.md).

## What a navigation costs

On a Pixel 5, a screen whose prefabs are already loaded shows in about 3 ms, and one loaded for the first time in about 12 ms, plus your own components' work. Opening a modal adds a blurred snapshot of the screen below, about 0.3 ms of main-thread work, and the modal appears one frame later. Metrics and informational logs are compiled out of release builds.

Why it stays cheap is covered in [Core Concepts › Rendering](core-concepts.md#rendering) and [Memory](core-concepts.md#memory).

# Benchmark sample

Runs your project's own screens through `ScreenManager` and measures navigation time, allocations, frame spikes, idle cost, memory across scene changes and console noise. It reads the screens from the `ScreenRegistry` and the scenes from the build settings, so it needs no extra assets.

## What it does

1. Waits for `ScreenManager` and for the first scene's own start flow to settle.
2. **Idle, root only:** 300 frames with only the root screen loaded.
3. **Cold pass,** 3 runs: loads the next build scene, which unloads every non-persistent screen, then opens each fullscreen screen that isn't loaded yet, returns to the root and opens and closes each modal.
4. **Warm pass,** 20 repetitions: from the root, opens each fullscreen screen and goes back, then opens and closes each modal. Every screen is cached by then.
5. **Idle, all cached:** 300 frames with every screen loaded.
6. **Scene cycles,** 5 cycles: opens the first persistent screen, loads the next scene, checks the persistent screen's layers survived, and records memory and layer count after cleanup.

It never calls `GoBack()` at the root, so a project without a `QuitConfirmation` modal doesn't quit. `quit-requests` must stay at 0. Alerts and screens with **Ignore Navigation Rules** aren't navigated, except the persistent screen.

## Run it in the Editor

Select **Tools > Screen System > Benchmark > Run In Play Mode**. It opens the first enabled build scene, enters Play Mode and leaves Play Mode when it ends.

From the command line, with the project closed in the Editor:

```bash
Unity -batchmode -projectPath <project> -executeMethod ScreenSystem.Benchmark.Editor.ScreenSystemBenchmarkLauncher.RunFromCommandLine -logFile -
```

Results from the Editor are marked as not valid for publishing. Use them to compare changes on the same machine.

## Run it on a device

Add an empty GameObject with `ScreenSystemBenchmarkRunner` to the first build scene, set **Commit Hash**, and make an IL2CPP build. Use a Development build for allocations and a Release build for timings.

## Results

Each run writes a JSON file and a Markdown summary to:

- Editor: `<project>/Logs/ScreenSystemBenchmarks/`
- Player: `Application.persistentDataPath/ScreenSystemBenchmarks/`

The files are rewritten after each phase, so a stopped run keeps what it measured. Metric names are described in the package's `Documentation~/benchmarks/README.md`.

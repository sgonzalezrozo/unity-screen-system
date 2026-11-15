using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using Debug = UnityEngine.Debug;

namespace ScreenSystem.Benchmark.Editor
{
    /// <summary>Starts the benchmark from the first enabled build scene, from the menu or the command line.</summary>
    [InitializeOnLoad]
    public static class ScreenSystemBenchmarkLauncher
    {
        private const string CommandLineKey = "ScreenSystem.Benchmark.CommandLine";

        static ScreenSystemBenchmarkLauncher()
        {
            if (SessionState.GetBool(CommandLineKey, false))
                EditorApplication.playModeStateChanged += ExitWhenBackInEditMode;
        }

        [MenuItem("Tools/Screen System/Benchmark/Run In Play Mode")]
        public static void RunFromMenu()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Launch();
        }

        /// <summary>Entry point for -executeMethod. Quits the Editor when the run ends.</summary>
        public static void RunFromCommandLine()
        {
            SessionState.SetBool(CommandLineKey, true);
            EditorApplication.playModeStateChanged += ExitWhenBackInEditMode;
            Launch();
        }

        private static void Launch()
        {
            EditorBuildSettingsScene firstScene = EditorBuildSettings.scenes.FirstOrDefault(s => s.enabled);
            if (firstScene == null)
            {
                Debug.LogError("[ScreenSystemBenchmark] Add at least one enabled scene to the build settings.");
                return;
            }

            EditorSceneManager.OpenScene(firstScene.path);
            SessionState.SetBool(ScreenSystemBenchmarkRunner.AutoRunKey, true);
            SessionState.SetString(ScreenSystemBenchmarkRunner.CommitKey, ReadPackageCommit());
            EditorApplication.EnterPlaymode();
        }

        private static void ExitWhenBackInEditMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode)
                return;

            SessionState.EraseBool(CommandLineKey);
            EditorApplication.Exit(0);
        }

        internal static string ReadPackageCommit()
        {
            try
            {
                UnityEditor.PackageManager.PackageInfo package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ScreenManager).Assembly);
                if (package == null)
                    return "";

                ProcessStartInfo info = new("git", "rev-parse --short HEAD")
                {
                    WorkingDirectory = package.resolvedPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(info);
                string output = process?.StandardOutput.ReadToEnd().Trim() ?? "";
                process?.WaitForExit(2000);

                bool dirty = false;
                ProcessStartInfo status = new("git", "status --porcelain -- Runtime")
                {
                    WorkingDirectory = package.resolvedPath,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process statusProcess = Process.Start(status))
                {
                    dirty = !string.IsNullOrWhiteSpace(statusProcess?.StandardOutput.ReadToEnd());
                    statusProcess?.WaitForExit(2000);
                }

                return dirty ? output + "-dirty" : output;
            }
            catch (Exception)
            {
                return "";
            }
        }
    }
}

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace ScreenSystem.Benchmark.Editor
{
    /// <summary>Writes the package commit into Resources before a build, so device results record it.</summary>
    public sealed class ScreenSystemBenchmarkCommitWriter : IPreprocessBuildWithReport
    {
        private const string Folder = "Assets/ScreenSystemBenchmark/Resources";
        private const string FilePath = Folder + "/ScreenSystemBenchmarkCommit.txt";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(FilePath, ScreenSystemBenchmarkLauncher.ReadPackageCommit());
            AssetDatabase.ImportAsset(FilePath);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Editor.Build
{
    /// <summary>
    /// Windows playtest build of <c>Assets/_Project/Scenes/Main.unity</c>: StandaloneWindows64, Mono, release (no
    /// development build, no test assemblies), product/company from ProjectSettings. Every builder (Main Scene last)
    /// runs first so the build always reflects source. Writes <c>build_info.txt</c> next to the exe.
    /// Normally run through <c>python tools/build_player.py</c>; directly:
    /// <code>python tools/unity_batch.py exec --method MoonProject.Editor.Build.PlayerBuild.BuildWindows
    ///     [--arg out=Builds/LofiLunar-Windows] [--arg builders=false]</code>
    /// </summary>
    public static class PlayerBuild
    {
        public const string DefaultOutput = "Builds/LofiLunar-Windows";
        public const string InfoFileName = "build_info.txt";

        /// <summary>Suffix of folders Unity writes next to the player that must never ship (debug symbols).</summary>
        private const string DoNotShipSuffix = "_DoNotShip";

        public static void BuildWindows()
        {
            BatchRunner.Run(nameof(BuildWindows), args => Build(
                BatchArgs.ProjectPath(args.GetString("out", DefaultOutput)), args.GetBool("builders", true)));
        }

        /// <summary>Builds into <paramref name="outputDirectory"/> (emptied first). Returns true on success.</summary>
        public static bool Build(string outputDirectory, bool runBuilders)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            {
                Debug.LogError("Windows build support is not installed for this Unity editor.");
                return false;
            }

            ScriptingImplementation backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
            if (backend != ScriptingImplementation.Mono2x)
            {
                Debug.LogError($"Standalone scripting backend is {backend}; playtest builds expect Mono.");
                return false;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            BuildVersion version = BuildVersion.FromGit(projectRoot);
            BatchRunner.Log($"player build {version.Stamp} -> {outputDirectory}");

            if (runBuilders && !BuilderRegistry.DiscoverAndRun(string.Empty))
            {
                Debug.LogError("Builders failed; the player was not built.");
                return false;
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainSceneBuilder.ScenePath) == null)
            {
                Debug.LogError($"{MainSceneBuilder.ScenePath} does not exist; run the Main Scene builder.");
                return false;
            }

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            Directory.CreateDirectory(outputDirectory);
            string exePath = Path.Combine(outputDirectory, ExeName() + ".exe");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainSceneBuilder.ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            var topLevelAssets = new HashSet<string>(AssetDatabase.GetSubFolders("Assets"), StringComparer.Ordinal);
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            finally
            {
                RemoveBuildLeftovers(topLevelAssets);
            }

            BuildSummary summary = report.summary;
            BatchRunner.Log($"player build: {summary.result} in {summary.totalTime.TotalSeconds:0}s, " +
                            $"{summary.totalErrors} error(s), {summary.totalWarnings} warning(s), " +
                            $"{summary.totalSize / (1024f * 1024f):0.0} MB");
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"Player build {summary.result}: see the errors above.");
                return false;
            }

            RemoveDoNotShipFolders(outputDirectory);
            string infoPath = Path.Combine(outputDirectory, InfoFileName);
            File.WriteAllText(infoPath, Info(version, summary), new UTF8Encoding(false));
            BatchRunner.Log($"player build: {exePath}");
            return true;
        }

        /// <summary>Exe name: the product name without spaces or punctuation ("Lofi Lunar" -> "LofiLunar").</summary>
        public static string ExeName()
        {
            var name = new StringBuilder();
            foreach (char c in PlayerSettings.productName)
            {
                if (char.IsLetterOrDigit(c))
                {
                    name.Append(c);
                }
            }

            if (name.Length == 0)
            {
                throw new InvalidOperationException("PlayerSettings.productName has no letters or digits.");
            }

            return name.ToString();
        }

        /// <summary>
        /// Build callbacks of packages (link.xml merging, the performance test framework's Resources files) create
        /// top-level folders under Assets and only remove them when the build succeeds; never leave them behind.
        /// </summary>
        private static void RemoveBuildLeftovers(HashSet<string> topLevelBefore)
        {
            foreach (string folder in AssetDatabase.GetSubFolders("Assets"))
            {
                if (!topLevelBefore.Contains(folder))
                {
                    AssetDatabase.DeleteAsset(folder);
                    BatchRunner.Log($"player build: removed build leftover {folder}");
                }
            }
        }

        private static void RemoveDoNotShipFolders(string outputDirectory)
        {
            foreach (string directory in Directory.GetDirectories(outputDirectory, "*" + DoNotShipSuffix))
            {
                Directory.Delete(directory, true);
            }
        }

        private static string Info(BuildVersion version, BuildSummary summary)
        {
            var info = new StringBuilder();
            info.Append("product=").AppendLine(PlayerSettings.productName);
            info.Append("company=").AppendLine(PlayerSettings.companyName);
            info.Append("version=").AppendLine(version.Stamp);
            info.Append("commit=").AppendLine(version.Commit);
            info.Append("commitDate=").AppendLine(version.CommitDate);
            info.Append("dirty=").AppendLine(version.Dirty ? "true" : "false");
            info.Append("builtAtUtc=")
                .AppendLine(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture));
            info.Append("unity=").AppendLine(Application.unityVersion);
            info.AppendLine("target=StandaloneWindows64");
            info.AppendLine("scriptingBackend=Mono");
            info.AppendLine("development=false");
            info.Append("exe=").AppendLine(ExeName() + ".exe");
            info.Append("buildSeconds=")
                .AppendLine(summary.totalTime.TotalSeconds.ToString("0", CultureInfo.InvariantCulture));
            return info.ToString();
        }
    }
}

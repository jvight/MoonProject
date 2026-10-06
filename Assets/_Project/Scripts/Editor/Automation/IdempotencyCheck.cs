using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Proves builders are idempotent: runs the selected builders twice, each from a fresh empty scene, and requires
    /// every file under Generated/ and Scenes/ (assets and .meta files) to be byte-identical after the second pass.
    /// Also flags builders that leave objects in, or dirty, the open scene (they should build temporary objects in a
    /// <see cref="BuilderScratchScene"/>), and reports what the first pass changed relative to the files on disk
    /// (committed output that was stale).
    /// <code>python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.CheckIdempotency
    ///     [--arg filter=^Art/]</code>
    /// </summary>
    public static class IdempotencyCheck
    {
        public static readonly string[] WatchedFolders =
        {
            GeneratedAssets.Root, Path.GetDirectoryName(MainSceneBuilder.ScenePath)?.Replace('\\', '/'),
        };

        /// <summary>Runs the check; appends a human-readable report. Returns true if every rule held.</summary>
        public static bool Run(string filter, StringBuilder report)
        {
            IReadOnlyList<BuilderInfo> all = BuilderRegistry.Discover(out IReadOnlyList<string> problems);
            foreach (string problem in problems)
            {
                report.AppendLine("PROBLEM: " + problem);
            }

            IReadOnlyList<BuilderInfo> builders = BuilderRegistry.Filter(all, filter);
            if (builders.Count == 0)
            {
                report.AppendLine($"No builder path matches '{filter}'.");
                return false;
            }

            Dictionary<string, string> before = Snapshot();
            var polluting = new SortedSet<string>(StringComparer.Ordinal);
            bool built = RunPass(builders, polluting, report, 1);
            Dictionary<string, string> afterFirst = Snapshot();
            built &= RunPass(builders, polluting, report, 2);
            Dictionary<string, string> afterSecond = Snapshot();

            List<string> stale = Differences(before, afterFirst);
            report.AppendLine($"pass 1 changed {stale.Count} file(s) on disk" +
                              (stale.Count > 0 ? " (committed generated output was stale):" : "."));
            AppendLimited(report, stale);

            List<string> unstable = Differences(afterFirst, afterSecond);
            report.AppendLine(unstable.Count == 0
                ? $"pass 2 reproduced all {afterSecond.Count} watched file(s) byte for byte."
                : $"NOT IDEMPOTENT: pass 2 changed {unstable.Count} file(s):");
            AppendLimited(report, unstable);

            foreach (string entry in polluting)
            {
                report.AppendLine($"TOUCHES THE OPEN SCENE: {entry}; use a BuilderScratchScene for temporary objects.");
            }

            return built && problems.Count == 0 && unstable.Count == 0 && polluting.Count == 0;
        }

        private static bool RunPass(IReadOnlyList<BuilderInfo> builders, ISet<string> polluting, StringBuilder report,
            int pass)
        {
            bool succeeded = true;
            foreach (BuilderInfo builder in builders)
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BuilderResult result = BuilderRegistry.Run(new[] { builder })[0];
                if (!result.Succeeded)
                {
                    report.AppendLine($"pass {pass}: {builder.Path} FAILED: {result.FirstError}");
                    succeeded = false;
                }

                if (!builder.ReplacesOpenScene && (scene.isDirty || scene.rootCount > 0))
                {
                    polluting.Add($"{builder.Path} ({scene.rootCount} object(s) left in the open scene" +
                                  (scene.isDirty ? ", scene marked dirty)" : ")"));
                }
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.SaveAssets();
            return succeeded;
        }

        /// <summary>Project-relative path to SHA-256 of every file below the watched folders.</summary>
        private static Dictionary<string, string> Snapshot()
        {
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            using (SHA256 sha = SHA256.Create())
            {
                foreach (string folder in WatchedFolders)
                {
                    if (!Directory.Exists(folder))
                    {
                        continue;
                    }

                    foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories))
                    {
                        string path = file.Replace('\\', '/');
                        hashes[path] = Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(file)));
                    }
                }
            }

            return hashes;
        }

        private static List<string> Differences(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            var changes = new List<string>();
            foreach (KeyValuePair<string, string> entry in a)
            {
                if (!b.TryGetValue(entry.Key, out string hash))
                {
                    changes.Add("removed  " + entry.Key);
                }
                else if (hash != entry.Value)
                {
                    changes.Add("changed  " + entry.Key);
                }
            }

            foreach (string path in b.Keys)
            {
                if (!a.ContainsKey(path))
                {
                    changes.Add("added    " + path);
                }
            }

            changes.Sort(StringComparer.Ordinal);
            return changes;
        }

        private static void AppendLimited(StringBuilder report, List<string> lines)
        {
            const int MaxLines = 60;
            for (int i = 0; i < lines.Count && i < MaxLines; i++)
            {
                report.Append("  ").AppendLine(lines[i]);
            }

            if (lines.Count > MaxLines)
            {
                report.AppendLine($"  ... and {lines.Count - MaxLines} more");
            }
        }
    }
}

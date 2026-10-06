using System;
using System.Diagnostics;
using System.Globalization;
using UnityEditor;

namespace MoonProject.Editor.Build
{
    /// <summary>
    /// Version stamp of a player build, taken from git:
    /// "&lt;bundleVersion&gt;-&lt;commit date yyyyMMdd&gt;-&lt;short hash&gt;", with "-dirty" when the working tree had
    /// uncommitted changes before the build. The same commit always gets the same stamp, so a playtest report maps
    /// straight back to source.
    /// </summary>
    public sealed class BuildVersion
    {
        private BuildVersion(string bundleVersion, string commit, string commitDate, bool dirty)
        {
            BundleVersion = bundleVersion;
            Commit = commit;
            CommitDate = commitDate;
            Dirty = dirty;
        }

        /// <summary>PlayerSettings.bundleVersion, e.g. "0.1.0".</summary>
        public string BundleVersion { get; }

        /// <summary>Short commit hash of HEAD.</summary>
        public string Commit { get; }

        /// <summary>Commit date of HEAD, UTC, yyyyMMdd.</summary>
        public string CommitDate { get; }

        /// <summary>True if source files differed from HEAD when the stamp was taken.</summary>
        public bool Dirty { get; }

        public string Stamp => $"{BundleVersion}-{CommitDate}-{Commit}{(Dirty ? "-dirty" : string.Empty)}";

        /// <summary>Reads the stamp from git in <paramref name="repositoryRoot"/>; throws if git fails.</summary>
        public static BuildVersion FromGit(string repositoryRoot)
        {
            string commit = Git(repositoryRoot, "rev-parse --short HEAD");
            string epoch = Git(repositoryRoot, "log -1 --format=%ct HEAD");
            string date = DateTimeOffset.FromUnixTimeSeconds(long.Parse(epoch, CultureInfo.InvariantCulture))
                .UtcDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            bool dirty = HasSourceChanges(Git(repositoryRoot, "status --porcelain"));
            return new BuildVersion(PlayerSettings.bundleVersion, commit, date, dirty);
        }

        /// <summary>
        /// True if <c>git status --porcelain</c> lists anything except untracked .meta files, which Unity generates
        /// for new files in every worktree and the Director commits from the main editor.
        /// </summary>
        public static bool HasSourceChanges(string porcelain)
        {
            foreach (string line in porcelain.Split('\n'))
            {
                string entry = line.Trim();
                if (entry.Length == 0)
                {
                    continue;
                }

                bool untrackedMeta = entry.StartsWith("?? ", StringComparison.Ordinal) &&
                                     entry.TrimEnd('"', '/').EndsWith(".meta", StringComparison.Ordinal);
                if (!untrackedMeta)
                {
                    return true;
                }
            }

            return false;
        }

        private static string Git(string workingDirectory, string arguments)
        {
            var start = new ProcessStartInfo("git", "-c safe.directory=* " + arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using (Process process = Process.Start(start))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Could not start git.");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"git {arguments} failed ({process.ExitCode}): {error.Trim()}");
                }

                return output.Trim();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MoonProject.Editor.Automation;
using Debug = UnityEngine.Debug;

namespace MoonProject.Editor.Builders
{
    /// <summary>
    /// Finds every <see cref="MoonBuilderAttribute"/> method in the loaded editor assemblies (via TypeCache, so domain
    /// editor assemblies need no registration code) and runs them in a stable order with per-builder error tracking.
    /// </summary>
    public static class BuilderRegistry
    {
        /// <summary>
        /// All valid builders sorted by order then path. Malformed declarations (non-static, parameters, non-void,
        /// empty or duplicate path) are reported in <paramref name="problems"/>; callers must surface them.
        /// </summary>
        public static IReadOnlyList<BuilderInfo> Discover(out IReadOnlyList<string> problems)
        {
            var found = new List<BuilderInfo>();
            var issues = new List<string>();
            var paths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<MoonBuilderAttribute>())
            {
                var attribute = method.GetCustomAttribute<MoonBuilderAttribute>();
                string name = $"{method.DeclaringType?.FullName}.{method.Name}";
                string problem = Validate(method, attribute);
                if (problem != null)
                {
                    issues.Add($"[MoonBuilder] {name}: {problem}");
                    continue;
                }

                string path = attribute.Path.Trim('/');
                if (paths.TryGetValue(path, out string existing))
                {
                    issues.Add($"[MoonBuilder] {name}: path '{path}' is already used by {existing}.");
                    continue;
                }

                paths.Add(path, name);
                found.Add(new BuilderInfo(path, attribute.Order, attribute.ReplacesOpenScene, method));
            }

            found.Sort(CompareBuilders);
            problems = issues;
            return found;
        }

        /// <summary>Builders whose path matches <paramref name="pattern"/> (case-insensitive regex; empty = all).</summary>
        public static IReadOnlyList<BuilderInfo> Filter(IReadOnlyList<BuilderInfo> builders, string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return builders;
            }

            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var matches = new List<BuilderInfo>();
            foreach (BuilderInfo builder in builders)
            {
                if (regex.IsMatch(builder.Path))
                {
                    matches.Add(builder);
                }
            }

            return matches;
        }

        /// <summary>
        /// Runs <paramref name="builders"/> in the given order, saving assets after each. A builder fails if it throws
        /// or logs an error; later builders still run so one report shows every failure.
        /// </summary>
        public static IReadOnlyList<BuilderResult> Run(IReadOnlyList<BuilderInfo> builders)
        {
            var results = new List<BuilderResult>(builders.Count);
            bool interactive = !Application.isBatchMode;
            try
            {
                for (int i = 0; i < builders.Count; i++)
                {
                    BuilderInfo builder = builders[i];
                    if (interactive)
                    {
                        EditorUtility.DisplayProgressBar("MoonProject Build", builder.Path, (float)i / builders.Count);
                    }

                    results.Add(RunOne(builder));
                }
            }
            finally
            {
                if (interactive)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            return results;
        }

        /// <summary>True if any of <paramref name="builders"/> replaces the open scene.</summary>
        public static bool AnyReplacesOpenScene(IReadOnlyList<BuilderInfo> builders)
        {
            foreach (BuilderInfo builder in builders)
            {
                if (builder.ReplacesOpenScene)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// For menu and window runs: if a selected builder replaces the open scene, asks the user to save modified
        /// scenes first. Returns false if the user cancelled (nothing must run then).
        /// </summary>
        public static bool ConfirmInteractiveRun(IReadOnlyList<BuilderInfo> builders)
        {
            if (Application.isBatchMode || !AnyReplacesOpenScene(builders))
            {
                return true;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return true;
            }

            Debug.LogWarning("Build cancelled: the open scene has unsaved changes and a selected builder replaces it.");
            return false;
        }

        /// <summary>Discovers, logs discovery problems, filters and runs. Returns true if everything succeeded.</summary>
        public static bool DiscoverAndRun(string pattern)
        {
            IReadOnlyList<BuilderInfo> all = Discover(out IReadOnlyList<string> problems);
            foreach (string problem in problems)
            {
                Debug.LogError(problem);
            }

            IReadOnlyList<BuilderInfo> selected = Filter(all, pattern);
            if (selected.Count == 0)
            {
                Debug.LogError($"[MoonBuilder] no builder path matches '{pattern}' ({all.Count} registered).");
                return false;
            }

            IReadOnlyList<BuilderResult> results = Run(selected);
            int failed = 0;
            foreach (BuilderResult result in results)
            {
                if (!result.Succeeded)
                {
                    failed++;
                }
            }

            BatchRunner.Log($"builders: {results.Count - failed}/{results.Count} succeeded");
            return failed == 0 && problems.Count == 0;
        }

        private static BuilderResult RunOne(BuilderInfo builder)
        {
            BatchRunner.Log($"build {builder.Path} ({builder.MethodName})");
            var stopwatch = Stopwatch.StartNew();
            using (var capture = new LogCapture())
            {
                try
                {
                    builder.Invoke();
                    AssetDatabase.SaveAssets();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                bool succeeded = capture.ErrorCount == 0;
                double seconds = stopwatch.Elapsed.TotalSeconds;
                BatchRunner.Log($"build {builder.Path}: {(succeeded ? "OK" : "FAILED")} in {seconds:0.00}s" +
                                (succeeded ? string.Empty : $" ({capture.ErrorCount} error(s))"));
                return new BuilderResult(builder.Path, succeeded, seconds, capture.ErrorCount, capture.FirstMessage);
            }
        }

        private static string Validate(MethodInfo method, MoonBuilderAttribute attribute)
        {
            if (!method.IsStatic)
            {
                return "builders must be static.";
            }

            if (method.GetParameters().Length != 0)
            {
                return "builders take no parameters.";
            }

            if (method.ReturnType != typeof(void))
            {
                return "builders must return void.";
            }

            if (method.ContainsGenericParameters)
            {
                return "builders cannot be generic.";
            }

            if (string.IsNullOrWhiteSpace(attribute.Path) || attribute.Path.Trim('/').Length == 0)
            {
                return "path is empty.";
            }

            return attribute.Path.Contains("//") ? "path contains an empty segment." : null;
        }

        private static int CompareBuilders(BuilderInfo a, BuilderInfo b)
        {
            int byOrder = a.Order.CompareTo(b.Order);
            return byOrder != 0 ? byOrder : string.CompareOrdinal(a.Path, b.Path);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Builders;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Build commands for a running, interactive editor (the Director's main editor, driven through
    /// <c>tools/unity_mcp.py build|scene|builders</c>). Unlike the batch entry points they never exit Unity and never
    /// open dialogs: each returns a plain-text report whose last line is <c>RESULT: PASS</c> or <c>RESULT: FAIL</c>,
    /// and also writes it to <c>reportPath</c> (when given) once finished, so a caller whose request timed out while a
    /// long build blocked the editor can still collect the outcome.
    /// </summary>
    public static class EditorCommands
    {
        private const string Pass = "RESULT: PASS";
        private const string Fail = "RESULT: FAIL";

        /// <summary>Runs every builder whose path matches <paramref name="filter"/> (regex, empty = all).</summary>
        public static string Build(string filter, string reportPath)
        {
            return Report(reportPath, report => RunBuilders(filter ?? string.Empty, report));
        }

        /// <summary>Rebuilds Assets/_Project/Scenes/Main.unity.</summary>
        public static string BuildMainScene(string reportPath)
        {
            return Report(reportPath, report => RunBuilders(MainSceneBuilder.BuilderPattern, report));
        }

        /// <summary>Lists registered builders (order, path, method).</summary>
        public static string ListBuilders(string reportPath)
        {
            return Report(reportPath, report =>
            {
                IReadOnlyList<BuilderInfo> builders = BuilderRegistry.Discover(out IReadOnlyList<string> problems);
                foreach (BuilderInfo builder in builders)
                {
                    report.AppendLine($"{builder.Order,5}  {builder.Path}  ({builder.MethodName})" +
                                      (builder.ReplacesOpenScene ? "  [replaces open scene]" : string.Empty));
                }

                foreach (string problem in problems)
                {
                    report.AppendLine("PROBLEM: " + problem);
                }

                report.AppendLine($"{builders.Count} builder(s)");
                return problems.Count == 0;
            });
        }

        private static bool RunBuilders(string filter, StringBuilder report)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                report.AppendLine("The editor is in play mode; exit play mode first.");
                return false;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                report.AppendLine("The editor is compiling or importing; retry when it is idle.");
                return false;
            }

            IReadOnlyList<BuilderInfo> all = BuilderRegistry.Discover(out IReadOnlyList<string> problems);
            foreach (string problem in problems)
            {
                report.AppendLine("PROBLEM: " + problem);
            }

            IReadOnlyList<BuilderInfo> selected = BuilderRegistry.Filter(all, filter);
            if (selected.Count == 0)
            {
                report.AppendLine($"No builder path matches '{filter}' ({all.Count} registered).");
                return false;
            }

            if (BuilderRegistry.AnyReplacesOpenScene(selected) && TryFindDirtyScene(out string dirty))
            {
                report.AppendLine($"Scene '{dirty}' has unsaved changes and a selected builder replaces the open " +
                                  "scene. Save or discard them in the editor, then retry.");
                return false;
            }

            bool succeeded = problems.Count == 0;
            foreach (BuilderResult result in BuilderRegistry.Run(selected))
            {
                report.Append(result.Succeeded ? "OK      " : "FAILED  ")
                    .Append($"{result.Seconds,7:0.00}s  {result.Path}");
                if (!result.Succeeded)
                {
                    report.Append($"  ({result.ErrorCount} error(s)) first: {FirstLine(result.FirstError)}");
                    succeeded = false;
                }

                report.AppendLine();
            }

            return succeeded;
        }

        private static string Report(string reportPath, Func<StringBuilder, bool> body)
        {
            var report = new StringBuilder();
            bool succeeded;
            try
            {
                succeeded = body(report);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                report.AppendLine("EXCEPTION: " + exception);
                succeeded = false;
            }

            report.Append(succeeded ? Pass : Fail);
            string text = report.ToString();
            if (!string.IsNullOrEmpty(reportPath))
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(reportPath));
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string temporary = reportPath + ".tmp";
                File.WriteAllText(temporary, text, new UTF8Encoding(false));
                if (File.Exists(reportPath))
                {
                    File.Delete(reportPath);
                }

                File.Move(temporary, reportPath);
            }

            return text;
        }

        private static bool TryFindDirtyScene(out string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                {
                    name = string.IsNullOrEmpty(scene.path) ? "Untitled" : scene.path;
                    return true;
                }
            }

            name = null;
            return false;
        }

        private static string FirstLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            int newline = text.IndexOf('\n');
            return newline < 0 ? text : text.Substring(0, newline);
        }
    }
}

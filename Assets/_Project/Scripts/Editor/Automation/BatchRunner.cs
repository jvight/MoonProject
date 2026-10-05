using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Wraps a <c>-executeMethod</c> entry point: parses <see cref="BatchArgs"/>, runs the body, prints a
    /// <c>[moon]</c> summary line and, in batch mode, exits Unity with 0 (body returned true and nothing logged an
    /// error) or 1. Entry points stay one-liners:
    /// <code>public static void BuildRocks() => BatchRunner.Run("BuildRocks", args => RockBuilder.Build(args));</code>
    /// In the interactive editor the same call just runs and logs, so menu items can share entry points.
    /// </summary>
    public static class BatchRunner
    {
        /// <summary>Prefix of every automation summary line; tools/unity_batch.py echoes these live.</summary>
        public const string LogPrefix = "[moon] ";

        public static void Run(string name, Func<BatchArgs, bool> body)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            StackTraceLogType previousLogTrace = Application.GetStackTraceLogType(LogType.Log);
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
            bool ok;
            var stopwatch = Stopwatch.StartNew();
            using (var capture = new LogCapture())
            {
                try
                {
                    ok = body(BatchArgs.FromCommandLine());
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    ok = false;
                }

                ok = ok && capture.ErrorCount == 0;
                Log($"{name}: {(ok ? "OK" : "FAILED")} in {stopwatch.Elapsed.TotalSeconds:0.0}s, " +
                    $"{capture.ErrorCount} error(s) logged" +
                    (capture.ErrorCount > 0 ? $"; first: {FirstLine(capture.FirstMessage)}" : string.Empty));
            }

            Application.SetStackTraceLogType(LogType.Log, previousLogTrace);
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(ok ? 0 : 1);
            }
        }

        /// <summary>Logs a <c>[moon]</c> line (shown live by unity_batch.py).</summary>
        public static void Log(string message)
        {
            Debug.Log(LogPrefix + message);
        }

        private static string FirstLine(string text)
        {
            int newline = text.IndexOf('\n');
            return newline < 0 ? text : text.Substring(0, newline);
        }
    }
}

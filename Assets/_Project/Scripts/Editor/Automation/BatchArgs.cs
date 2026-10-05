using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Arguments of a batch run started by <c>tools/unity_batch.py exec</c>: every <c>--arg key=value</c> arrives as
    /// <c>-moonArg key=value</c>, and the run's output folder as <c>-moonOutDir path</c>. Keys are case-insensitive;
    /// the last occurrence of a key wins. Typed getters throw <see cref="ArgumentException"/> on malformed values so
    /// a typo fails the run loudly instead of silently using a default.
    /// </summary>
    public sealed class BatchArgs
    {
        public const string ArgumentFlag = "-moonArg";
        public const string OutputDirectoryFlag = "-moonOutDir";

        private static readonly char[] ListSeparators = { ';', ',' };

        private readonly Dictionary<string, string> _values;

        private BatchArgs(Dictionary<string, string> values, string outputDirectory)
        {
            _values = values;
            OutputDirectory = outputDirectory;
        }

        /// <summary>Absolute folder for this run's outputs (captures, reports). Created on demand by writers.</summary>
        public string OutputDirectory { get; }

        public IReadOnlyCollection<string> Keys => _values.Keys;

        /// <summary>Parses the current process command line.</summary>
        public static BatchArgs FromCommandLine()
        {
            return Parse(Environment.GetCommandLineArgs());
        }

        /// <summary>Parses a command line; without <c>-moonOutDir</c> outputs go to <c>Logs/batch/manual</c>.</summary>
        public static BatchArgs Parse(IReadOnlyList<string> commandLine)
        {
            if (commandLine == null)
            {
                throw new ArgumentNullException(nameof(commandLine));
            }

            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string outputDirectory = null;
            for (int i = 0; i < commandLine.Count; i++)
            {
                string token = commandLine[i];
                bool isArgument = string.Equals(token, ArgumentFlag, StringComparison.OrdinalIgnoreCase);
                bool isOutput = string.Equals(token, OutputDirectoryFlag, StringComparison.OrdinalIgnoreCase);
                if (!isArgument && !isOutput)
                {
                    continue;
                }

                if (i + 1 >= commandLine.Count)
                {
                    throw new ArgumentException($"{token} is missing its value.");
                }

                string value = commandLine[++i];
                if (isOutput)
                {
                    outputDirectory = value;
                    continue;
                }

                int split = value.IndexOf('=');
                if (split <= 0)
                {
                    throw new ArgumentException($"{ArgumentFlag} expects key=value, got '{value}'.");
                }

                values[value.Substring(0, split).Trim()] = value.Substring(split + 1).Trim();
            }

            return new BatchArgs(values, Path.GetFullPath(outputDirectory ?? DefaultOutputDirectory()));
        }

        /// <summary>Builds arguments from <c>key=value</c> pairs, e.g. for menu items that reuse batch entry points.</summary>
        public static BatchArgs FromPairs(string outputDirectory, params string[] pairs)
        {
            var commandLine = new List<string> { OutputDirectoryFlag, outputDirectory ?? DefaultOutputDirectory() };
            foreach (string pair in pairs)
            {
                commandLine.Add(ArgumentFlag);
                commandLine.Add(pair);
            }

            return Parse(commandLine);
        }

        public bool Has(string key)
        {
            return _values.ContainsKey(key);
        }

        public string GetString(string key, string fallback)
        {
            return _values.TryGetValue(key, out string value) ? value : fallback;
        }

        public string GetRequiredString(string key)
        {
            if (!_values.TryGetValue(key, out string value) || value.Length == 0)
            {
                throw new ArgumentException($"Missing required argument '{key}' (pass --arg {key}=...).");
            }

            return value;
        }

        public int GetInt(string key, int fallback)
        {
            if (!_values.TryGetValue(key, out string value))
            {
                return fallback;
            }

            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                throw new ArgumentException($"Argument '{key}' must be an integer, got '{value}'.");
            }

            return result;
        }

        public float GetFloat(string key, float fallback)
        {
            if (!_values.TryGetValue(key, out string value))
            {
                return fallback;
            }

            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
            {
                throw new ArgumentException($"Argument '{key}' must be a number, got '{value}'.");
            }

            return result;
        }

        /// <summary>Accepts true/false, 1/0, yes/no, on/off (case-insensitive).</summary>
        public bool GetBool(string key, bool fallback)
        {
            if (!_values.TryGetValue(key, out string value))
            {
                return fallback;
            }

            switch (value.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "on":
                    return true;
                case "false":
                case "0":
                case "no":
                case "off":
                    return false;
                default:
                    throw new ArgumentException($"Argument '{key}' must be true or false, got '{value}'.");
            }
        }

        /// <summary>Items of a ';' or ',' separated value (trimmed, empties dropped); empty when the key is absent.</summary>
        public string[] GetList(string key)
        {
            if (!_values.TryGetValue(key, out string value))
            {
                return Array.Empty<string>();
            }

            var items = new List<string>();
            foreach (string item in value.Split(ListSeparators))
            {
                string trimmed = item.Trim();
                if (trimmed.Length > 0)
                {
                    items.Add(trimmed);
                }
            }

            return items.ToArray();
        }

        /// <summary>Resolves a project-relative path (e.g. <c>Assets/...</c> or <c>Logs/...</c>) to an absolute one.</summary>
        public static string ProjectPath(string relativeOrAbsolute)
        {
            return Path.GetFullPath(Path.IsPathRooted(relativeOrAbsolute)
                ? relativeOrAbsolute
                : Path.Combine(ProjectRoot(), relativeOrAbsolute));
        }

        private static string ProjectRoot()
        {
            return Path.GetDirectoryName(Application.dataPath);
        }

        private static string DefaultOutputDirectory()
        {
            return Path.Combine(ProjectRoot(), "Logs", "batch", "manual");
        }
    }
}

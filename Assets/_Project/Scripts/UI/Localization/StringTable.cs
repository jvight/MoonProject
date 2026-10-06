using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// One language's strings by key, parsed once from its JSON table. Malformed tables throw (content bug).
    /// </summary>
    internal sealed class StringTable
    {
        private readonly Dictionary<string, string> _strings;

        private StringTable(string language, string name, Dictionary<string, string> strings)
        {
            Language = language;
            Name = name;
            _strings = strings;
        }

        public string Language { get; }

        public string Name { get; }

        public int Count => _strings.Count;

        public IEnumerable<string> Keys => _strings.Keys;

        public bool TryGet(string key, out string text)
        {
            return _strings.TryGetValue(key, out text);
        }

        /// <summary>Parses <paramref name="json"/>; <paramref name="source"/> names it in error messages.</summary>
        public static StringTable Parse(string json, string source)
        {
            StringTableData data;
            try
            {
                data = JsonUtility.FromJson<StringTableData>(json);
            }
            catch (ArgumentException exception)
            {
                throw new FormatException($"String table {source} is not valid JSON: {exception.Message}");
            }

            if (data == null || string.IsNullOrWhiteSpace(data.language) || string.IsNullOrWhiteSpace(data.name))
            {
                throw new FormatException($"String table {source} needs a language code and a name.");
            }

            var strings = new Dictionary<string, string>(data.entries?.Length ?? 0, StringComparer.Ordinal);
            if (data.entries != null)
            {
                foreach (StringEntryData entry in data.entries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.text == null)
                    {
                        throw new FormatException($"String table {source} has an entry without a key or text.");
                    }

                    if (strings.ContainsKey(entry.key))
                    {
                        throw new FormatException($"String table {source} defines '{entry.key}' twice.");
                    }

                    strings.Add(entry.key, entry.text);
                }
            }

            return new StringTable(data.language, data.name, strings);
        }
    }
}

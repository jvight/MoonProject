using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// <see cref="ILocalization"/> over the string tables in Assets/_Project/Data/Localization. The first table is the
    /// primary language (English): the source of every key and the starting language. A key missing from the chosen
    /// language is shown in the primary language and a key missing everywhere shows as itself; either is a content
    /// bug, logged once per key (the parity test keeps both from shipping).
    /// </summary>
    internal sealed class LocalizationService : ILocalization
    {
        private readonly EventBus _events;
        private readonly StringTable[] _tables;
        private readonly string[] _languages;
        private readonly HashSet<string> _reported = new HashSet<string>(StringComparer.Ordinal);
        private StringTable _current;

        /// <param name="events">Where <see cref="LanguageChanged"/> is published.</param>
        /// <param name="tables">Parsed tables, the primary language first; codes must be unique.</param>
        public LocalizationService(EventBus events, IReadOnlyList<StringTable> tables)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (tables == null || tables.Count == 0)
            {
                throw new ArgumentException("Localization needs at least the primary string table.", nameof(tables));
            }

            _tables = new StringTable[tables.Count];
            _languages = new string[tables.Count];
            for (int i = 0; i < tables.Count; i++)
            {
                StringTable table = tables[i] ?? throw new ArgumentException($"String table {i} is missing.");
                if (Array.IndexOf(_languages, table.Language, 0, i) >= 0)
                {
                    throw new ArgumentException($"Language '{table.Language}' has two string tables.", nameof(tables));
                }

                _tables[i] = table;
                _languages[i] = table.Language;
            }

            _current = _tables[0];
        }

        public string Language => _current.Language;

        public IReadOnlyList<string> Languages => _languages;

        /// <summary>The primary language (the first table).</summary>
        public string PrimaryLanguage => _tables[0].Language;

        public string GetLanguageName(string language)
        {
            StringTable table = Find(language);
            if (table == null)
            {
                throw new ArgumentException($"No string table for language '{language}'.", nameof(language));
            }

            return table.Name;
        }

        public string Get(string key)
        {
            if (key != null && _current.TryGet(key, out string text))
            {
                return text;
            }

            if (key != null && _current != _tables[0] && _tables[0].TryGet(key, out string primary))
            {
                Report(key, $"'{key}' has no {_current.Language} text; showing {_tables[0].Language}.");
                return primary;
            }

            Report(key ?? string.Empty, $"'{key}' is not in any string table.");
            return key ?? string.Empty;
        }

        public bool TryGet(string key, out string text)
        {
            if (key == null)
            {
                text = null;
                return false;
            }

            return _current.TryGet(key, out text) || _tables[0].TryGet(key, out text);
        }

        public void SetLanguage(string language)
        {
            StringTable table = Find(language);
            if (table == null)
            {
                throw new ArgumentException($"No string table for language '{language}'.", nameof(language));
            }

            if (table == _current)
            {
                return;
            }

            _current = table;
            _events.Publish(new LanguageChanged(table.Language));
        }

        /// <summary>True when <paramref name="language"/> has a table.</summary>
        public bool Supports(string language)
        {
            return Find(language) != null;
        }

        private StringTable Find(string language)
        {
            for (int i = 0; i < _tables.Length; i++)
            {
                if (string.Equals(_tables[i].Language, language, StringComparison.Ordinal))
                {
                    return _tables[i];
                }
            }

            return null;
        }

        private void Report(string key, string message)
        {
            if (_reported.Add(key))
            {
                Debug.LogError($"{nameof(LocalizationService)}: {message}");
            }
        }
    }
}

using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Small English and Vietnamese string tables made in the test, so rules can be checked without depending on the
    /// shipped tables' content.
    /// </summary>
    internal static class TestTables
    {
        public const string English = "en";
        public const string Vietnamese = "vi";

        /// <summary>
        /// A localization over two tables built from (key, English text, Vietnamese text) triples, English first.
        /// </summary>
        public static LocalizationService Localization(EventBus events, params string[] triples)
        {
            if (triples.Length % 3 != 0)
            {
                throw new ArgumentException("Entries come as (key, English, Vietnamese) triples.", nameof(triples));
            }

            return new LocalizationService(events, new[]
            {
                Table(English, "English", triples, 1),
                Table(Vietnamese, "Tiếng Việt", triples, 2),
            });
        }

        private static StringTable Table(string language, string name, string[] triples, int column)
        {
            var data = new StringTableData
            {
                language = language,
                name = name,
                entries = new StringEntryData[triples.Length / 3],
            };
            for (int i = 0; i < data.entries.Length; i++)
            {
                data.entries[i] = new StringEntryData { key = triples[i * 3], text = triples[i * 3 + column] };
            }

            return StringTable.Parse(JsonUtility.ToJson(data), language + " test table");
        }
    }
}

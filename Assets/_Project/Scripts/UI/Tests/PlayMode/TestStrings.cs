using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The shipped string tables topped up with the entries the tests and captures read but the tables do not carry:
    /// test-only ticker lines, and the M3-05 story and UI keys until the Director adds them to the tables. A key a
    /// shipped table already has keeps its shipped text, so the tables always win.
    /// </summary>
    internal static class TestStrings
    {
        public const string SignalKey = "ticker.bell.signal";
        public const string HomeKey = "ticker.bell.home";

        /// <summary>(key, English, Vietnamese) triples.</summary>
        private static readonly string[] Entries =
        {
            SignalKey, "Bell's picking something up… bearing {0}.",
            "Bell đang bắt được tín hiệu gì đó… hướng {0}.",
            HomeKey, "Bell got home before you. She says the porch light was on.",
            "Bell về nhà trước bạn rồi. Cô ấy bảo đèn hiên vẫn sáng.",
        };

        /// <summary>
        /// Copies of <paramref name="tables"/> (English first) with every missing entry added; destroy them after use.
        /// </summary>
        public static TextAsset[] Load(params TextAsset[] tables)
        {
            var merged = new TextAsset[tables.Length];
            for (int i = 0; i < tables.Length; i++)
            {
                var data = JsonUtility.FromJson<StringTableData>(tables[i].text);
                int column = Column(data.language);
                var keys = new HashSet<string>(StringComparer.Ordinal);
                var entries = new List<StringEntryData>(data.entries);
                foreach (StringEntryData entry in data.entries)
                {
                    keys.Add(entry.key);
                }

                for (int e = 0; e < Entries.Length; e += 3)
                {
                    if (!keys.Contains(Entries[e]))
                    {
                        entries.Add(new StringEntryData { key = Entries[e], text = Entries[e + column] });
                    }
                }

                data.entries = entries.ToArray();
                merged[i] = new TextAsset(JsonUtility.ToJson(data)) { name = tables[i].name };
            }

            return merged;
        }

        private static int Column(string language)
        {
            switch (language)
            {
                case "en":
                    return 1;
                case "vi":
                    return 2;
                default:
                    throw new ArgumentException($"No test strings for language '{language}'.", nameof(language));
            }
        }
    }
}

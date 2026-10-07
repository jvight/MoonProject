using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

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
        public const string FirstLog = "ro_1";
        public const string FirstTape = "after_dark_1";
        public const string SecondTape = "dust_and_honey";

        /// <summary>(key, English, Vietnamese) triples.</summary>
        private static readonly string[] Entries =
        {
            SignalKey, "Bell's picking something up… bearing {0}.",
            "Bell đang bắt được tín hiệu gì đó… hướng {0}.",
            HomeKey, "Bell got home before you. She says the porch light was on.",
            "Bell về nhà trước bạn rồi. Cô ấy bảo đèn hiên vẫn sáng.",
            "log." + FirstLog,
            "Night one of Lumen After Dark. Three listeners and a rover who rolls closer when I play the slow ones. "
            + "Best audience I've ever had.",
            "Đêm đầu tiên của Lumen After Dark. Ba thính giả và một chiếc rover cứ lăn lại gần mỗi khi tôi mở mấy bài "
            + "chậm. Khán giả tuyệt nhất tôi từng có.",
            "cassette." + FirstTape + ".title", "Lumen After Dark, Vol. 1", "Lumen After Dark, Tập 1",
            "cassette." + FirstTape + ".note",
            "First night show. Audience: three crew, one rover, one basil plant. The basil had requests.",
            "Buổi phát sóng đêm đầu tiên. Khán giả: ba phi hành gia, một chiếc rover, một chậu húng quế. Chậu húng quế "
            + "còn xin bài.",
            "cassette." + SecondTape + ".title", "Dust & Honey", "Bụi & Mật ong",
            "cassette." + SecondTape + ".note",
            "Recorded with the mic taped to the airlock. If you hear a thump, that's Kenji.",
            "Thu âm với chiếc micro dán băng keo vào cửa khoang. Nếu nghe tiếng thịch, đó là Kenji.",
            UiKeys.CrewLogCaption, "Crew log", "Nhật ký phi hành đoàn",
            UiKeys.LinerCaption, "Ro's liner notes", "Lời Ro ghi trên vỏ băng",
            UiKeys.TapeCount, "{0}/{1}", "{0}/{1}",
            UiKeys.RadioChannelName(RadioChannel.LumenAfterDark), "Lumen After Dark", "Lumen After Dark",
            UiKeys.RadioChannelName(RadioChannel.TapeDeck), "Tape Deck", "Đầu băng",
            UiKeys.RadioChannelName(RadioChannel.QuietHours), "Quiet Hours", "Giờ tĩnh lặng",
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

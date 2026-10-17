using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Verifies story-v4 letter routing (`letter.mast_1`…`10`, `letter.after_1`…`10`, `ticker.ending.answer`), header
    /// stamp &amp; signoff parsing, typewriter reveal with rate-limited <see cref="UiTypewriterTick"/> (`UiTypeKind.Card`)
    /// audio cues, voice-line hold extension, and early dismissal.
    /// </summary>
    public sealed class LetterCardTests
    {
        private const string UxmlPath = "Assets/_Project/UI/GameUI.uxml";
        private const float Frame = 1f / 60f;

        private EventBus _events;
        private LocalizationService _localization;
        private LetterCardSettings _settings;
        private UiLayout _layout;
        private LetterCard _card;
        private readonly List<UiTypeKind> _ticks = new List<UiTypeKind>();
        private readonly List<UiCueKind> _cues = new List<UiCueKind>();
        private readonly List<string> _shownKeys = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _ticks.Clear();
            _cues.Clear();
            _shownKeys.Clear();
            _events.Subscribe<UiTypewriterTick>(t => _ticks.Add(t.Kind));
            _events.Subscribe<UiCue>(c => _cues.Add(c.Kind));
            _events.Subscribe<TickerLineShown>(s => _shownKeys.Add(s.Key));

            _localization = TestTables.Localization(_events,
                UiKeys.CardClose, "Close", "Đóng",
                UiKeys.CrewLogCaption, "CREW LOG", "NHẬT KÝ ĐỘI",
                "ui.station.radio_tower", "RADIO TOWER", "THÁP RADIO",
                "letter.mast_1",
                "LOG 0311 · You asked where I put the solder. Third drawer down, under the spare fuses — don't mix the spools again. — K.",
                "NHẬT KÝ 0311 · Anh hỏi em để cuộn thiếc hàn ở đâu. Ngăn kéo thứ ba từ trên xuống, dưới đám cầu chì dự phòng — đừng trộn lẫn các cuộn nữa nhé. — K.",
                "letter.after_1",
                "L4 ARK · S. · 07, that first mast was right where Kenji said it'd be. The whole bay cheered when your ping came through.",
                "TÀU ARK L4 · S. · 07, cột tiếp sóng đầu tiên nằm đúng chỗ Kenji nói. Cả khoang reo lên khi tín hiệu của em vọng về.",
                "ticker.ending.answer",
                "Seven... is that you? The board just lit up all at once — we hear you. Come on, keep the line open, tell us everything.",
                "Bảy... em đấy à? Cả bảng mạch vừa sáng rực lên cùng một lúc — tụi chị nghe thấy em rồi. Nào, giữ đường truyền mở nhé, kể hết cho tụi chị nghe đi.");

            _settings = new LetterCardSettings();
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            Assert.IsNotNull(uxml, $"Missing {UxmlPath}");
            VisualElement root = uxml.CloneTree();
            _layout = new UiLayout(root);
            _card = new LetterCard(_layout, _settings, _localization, _events);
        }

        [Test]
        public void IsLetterKey_RoutesStoryV4LettersAndEndingAnswer_LeavesShortTickerLinesOnToast()
        {
            for (int i = 1; i <= 10; i++)
            {
                Assert.IsTrue(LetterCard.IsLetterKey("letter.mast_" + i));
                Assert.IsTrue(LetterCard.IsCrewLetterKey("letter.mast_" + i));
                Assert.IsTrue(LetterCard.IsLetterKey("letter.after_" + i));
                Assert.IsFalse(LetterCard.IsCrewLetterKey("letter.after_" + i));
            }

            Assert.IsTrue(LetterCard.IsLetterKey("ticker.ending.answer"));
            Assert.IsFalse(LetterCard.IsCrewLetterKey("ticker.ending.answer"));

            Assert.IsFalse(LetterCard.IsLetterKey("ticker.home"));
            Assert.IsFalse(LetterCard.IsLetterKey("ticker.static_1"));
            Assert.IsFalse(LetterCard.IsLetterKey("ticker.relay_1"));
            Assert.IsFalse(LetterCard.IsLetterKey("ticker.playing"));
        }

        [Test]
        public void ParseLetter_ExtractsStampBodyAndSignoff_InEnglishAndVietnamese()
        {
            LetterCard.ParseLetter("letter.mast_1", _localization.Get("letter.mast_1"),
                out string stamp, out string body, out string sender);
            Assert.AreEqual("LOG 0311", stamp);
            Assert.AreEqual(
                "You asked where I put the solder. Third drawer down, under the spare fuses — don't mix the spools again.",
                body);
            Assert.AreEqual("— K.", sender);

            LetterCard.ParseLetter("letter.after_1", _localization.Get("letter.after_1"),
                out stamp, out body, out sender);
            Assert.AreEqual("L4 ARK · S.", stamp);
            Assert.AreEqual(
                "07, that first mast was right where Kenji said it'd be. The whole bay cheered when your ping came through.",
                body);
            Assert.AreEqual(string.Empty, sender);

            LetterCard.ParseLetter("ticker.ending.answer", _localization.Get("ticker.ending.answer"),
                out stamp, out body, out sender);
            Assert.AreEqual("L4 ARK", stamp);
            Assert.IsTrue(body.StartsWith("Seven... is that you?"));
            Assert.AreEqual(string.Empty, sender, "mid-sentence em-dash in ending.answer is not a signoff");
        }

        [Test]
        public void TypewriterReveal_PublishesRateLimitedCardTicks_HoldsUntilRead_AndCanDismiss()
        {
            _card.Enqueue(new TickerLine("letter.mast_1"));
            Assert.IsTrue(_card.IsBusy);

            Step(_settings.AppearDelay + Frame);
            Assert.IsTrue(_card.IsVisible);
            Assert.IsTrue(_card.CanDismiss);
            CollectionAssert.AreEqual(new[] { UiCueKind.CardShown }, _cues);
            CollectionAssert.AreEqual(new[] { "letter.mast_1" }, _shownKeys);
            Assert.AreEqual("CREW LOG", _layout.LetterCardCaption.text);
            Assert.AreEqual("LOG 0311", _layout.LetterCardStamp.text);
            Assert.AreEqual("— K.", _layout.LetterCardSender.text);

            // Step 1.5 seconds of typewriter reveal.
            Step(1.5f);
            Assert.Greater(_ticks.Count, 5, "publishes UiTypewriterTick(UiTypeKind.Card) as characters reveal");
            Assert.LessOrEqual(_ticks.Count, Mathf.CeilToInt(1.5f / _settings.TickIntervalSeconds) + 2,
                "ticks are rate-limited");
            Assert.That(_ticks, Has.All.EqualTo(UiTypeKind.Card));
            Assert.IsFalse(_card.IsFullyRevealed);

            // Step until the typewriter completes.
            Step(4f);
            Assert.IsTrue(_card.IsFullyRevealed);
            Assert.AreEqual(
                "You asked where I put the solder. Third drawer down, under the spare fuses — don't mix the spools again.",
                _layout.LetterCardText.text);

            int ticksAfterReveal = _ticks.Count;
            Step(1f);
            Assert.AreEqual(ticksAfterReveal, _ticks.Count, "no typewriter ticks once fully revealed");

            // Relocalize in place preserves full reveal.
            _localization.SetLanguage("vi");
            _card.Relocalize();
            Assert.AreEqual("NHẬT KÝ ĐỘI", _layout.LetterCardCaption.text);
            Assert.AreEqual("NHẬT KÝ 0311", _layout.LetterCardStamp.text);
            Assert.IsTrue(_layout.LetterCardText.text.StartsWith("Anh hỏi em để cuộn thiếc hàn"));

            // Dismiss early.
            _card.Dismiss();
            Assert.AreEqual(UiCueKind.Back, _cues[_cues.Count - 1]);
            Step(_settings.Reveal.FadeOut + 0.2f);
            Assert.IsFalse(_card.IsVisible);
            Assert.IsFalse(_card.IsBusy);
        }

        private void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _card.Tick(Frame, "Esc", InputDeviceKind.KeyboardMouse, true);
            }
        }
    }
}

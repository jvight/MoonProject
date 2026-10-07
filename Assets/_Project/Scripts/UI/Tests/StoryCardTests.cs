using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The story card's M3-05 kinds, built over GameUI.uxml with test tables: a crew log under its caption without a
    /// name, a cassette's liner notes on the smaller, warmer card with its tape count, one queue for every kind, wiring
    /// bugs logged, and a new language re-read in place.
    /// </summary>
    public sealed class StoryCardTests
    {
        private const float Frame = 1f / 60f;
        private const string Log = "test_log";
        private const string Tape = "test_tape";

        private LocalizationService _localization;
        private UiLayout _layout;
        private MemoryCard _card;
        private UiTuning _tuning;
        private string _relic;
        private int _relicCount;

        [SetUp]
        public void SetUp()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var catalog = AssetDatabase.LoadAssetAtPath<RelicCatalog>(UiAssetPaths.RelicCatalog);
            Assert.IsNotNull(uxml, UiAssetPaths.Uxml);
            Assert.IsNotNull(catalog, UiAssetPaths.RelicCatalog);
            _relic = catalog.Relics[0].Id;
            _relicCount = catalog.Relics.Count;
            var events = new EventBus();
            _localization = TestTables.Localization(events,
                UiKeys.CardClose, "Close", "Đóng",
                UiKeys.CardCaption, "Memory {0} of {1}", "Ký ức thứ {0} trong {1}",
                UiKeys.CrewLogCaption, "Crew log", "Nhật ký phi hành đoàn",
                UiKeys.LinerCaption, "Ro's liner notes", "Lời Ro ghi trên vỏ băng",
                UiKeys.TapeCount, "{0}/{1}", "{0}/{1}",
                UiKeys.RelicName(_relic), "A relic", "Một kỷ vật",
                UiKeys.RelicMemory(_relic), "It remembers Earth.", "Nó nhớ Trái Đất.",
                UiKeys.CrewLog(Log), "Night one of the show.", "Đêm đầu tiên của chương trình.",
                UiKeys.CassetteTitle(Tape), "Slow Orbit", "Quỹ đạo chậm",
                UiKeys.CassetteNote(Tape), "It isn't. I checked.", "Không đâu. Tôi thử rồi.");
            _tuning = ScriptableObject.CreateInstance<UiTuning>();
            _layout = new UiLayout(uxml.Instantiate());
            _card = new MemoryCard(_layout, _tuning.MemoryCard, _localization, events, catalog, new NoFriends());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void ACrewLog_ShowsItsText_UnderTheCrewLogCaption_WithoutAName()
        {
            _card.EnqueueCrewLog(Log);
            RunUntilVisible();
            Assert.AreEqual(Log, _card.Current);
            Assert.AreEqual("Crew log", _layout.MemoryCardCaption.text);
            Assert.AreEqual("Night one of the show.", _layout.MemoryCardText.text);
            Assert.IsTrue(_layout.MemoryCard.ClassListContains(MemoryCard.UntitledClass), "no name line");
            Assert.IsFalse(_layout.MemoryCard.ClassListContains(MemoryCard.LinerClass));
        }

        [Test]
        public void ACassette_ShowsRosLinerNotes_AndTheTapeCount_OnTheWarmerCard()
        {
            _card.EnqueueCassette(Tape, 1, 3);
            RunUntilVisible();
            Assert.AreEqual(Tape, _card.Current);
            Assert.AreEqual("Ro's liner notes", _layout.MemoryCardCaption.text);
            Assert.AreEqual("1/3", _layout.MemoryCardCount.text, "the real total, never a hard-coded 8");
            Assert.AreEqual("Slow Orbit", _layout.MemoryCardName.text);
            Assert.AreEqual("It isn't. I checked.", _layout.MemoryCardText.text);
            Assert.IsTrue(_layout.MemoryCard.ClassListContains(MemoryCard.LinerClass));
            Assert.IsFalse(_layout.MemoryCard.ClassListContains(MemoryCard.UntitledClass));
        }

        [Test]
        public void EveryKind_WaitsItsTurn_InOneQueue_AndWearsItsOwnLook()
        {
            _card.EnqueueCassette(Tape, 2, 3);
            _card.EnqueueCrewLog(Log);
            _card.Enqueue(_relic, 4);
            var shown = new List<string>();
            for (float t = 0f; t < 120f && shown.Count < 3; t += Frame)
            {
                Step(Frame);
                if (_card.Current != null && (shown.Count == 0 || shown[shown.Count - 1] != _card.Current))
                {
                    shown.Add(_card.Current);
                    Assert.AreEqual(_card.Current == Tape,
                        _layout.MemoryCard.ClassListContains(MemoryCard.LinerClass), _card.Current);
                    Assert.AreEqual(_card.Current == Log,
                        _layout.MemoryCard.ClassListContains(MemoryCard.UntitledClass), _card.Current);
                }
            }

            CollectionAssert.AreEqual(new[] { Tape, Log, _relic }, shown, "one at a time, in arrival order");
            Assert.AreEqual("Memory 4 of " + _relicCount, _layout.MemoryCardCaption.text);
            Assert.AreEqual("A relic", _layout.MemoryCardName.text, "the relic card has its name line back");
        }

        [Test]
        public void ALogOrTapeWithoutText_IsLogged_AndShowsNoCard()
        {
            LogAssert.Expect(LogType.Error, new Regex("crew log 'missing' has no 'log.missing' text"));
            _card.EnqueueCrewLog("missing");
            LogAssert.Expect(LogType.Error, new Regex("cassette 'missing' needs 'cassette.missing.title'"));
            _card.EnqueueCassette("missing", 1, 3);
            Run(5f);
            Assert.IsFalse(_card.IsBusy);
            Assert.IsFalse(_card.IsVisible);
        }

        [Test]
        public void ANewLanguage_RereadsTheLinerCardInPlace()
        {
            _card.EnqueueCassette(Tape, 1, 3);
            RunUntilVisible();
            _localization.SetLanguage(TestTables.Vietnamese);
            _card.Relocalize();
            Assert.AreEqual("Lời Ro ghi trên vỏ băng", _layout.MemoryCardCaption.text);
            Assert.AreEqual("Quỹ đạo chậm", _layout.MemoryCardName.text);
            Assert.AreEqual("Không đâu. Tôi thử rồi.", _layout.MemoryCardText.text);
            Assert.AreEqual("1/3", _layout.MemoryCardCount.text);
        }

        private void RunUntilVisible()
        {
            for (float t = 0f; t < 10f && !_card.IsVisible; t += Frame)
            {
                Step(Frame);
            }

            Assert.IsTrue(_card.IsVisible, "the card appeared");
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                Step(Frame);
            }
        }

        private void Step(float deltaTime)
        {
            _card.Tick(deltaTime, "Esc", InputDeviceKind.KeyboardMouse, true);
        }
    }
}

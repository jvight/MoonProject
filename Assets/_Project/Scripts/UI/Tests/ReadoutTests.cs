using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// 07's readout (docs/design/opening-v2.md sections 2, 10): the protocol line follows the opening's milestones,
    /// each system's line reads exactly as designed in English and Vietnamese, the cards take turns and the Sun's arc
    /// follows the Sun's height.
    /// </summary>
    public sealed class ReadoutTests
    {
        private const float Frame = 1f / 60f;

        /// <summary>07's start kit, one line per system in the order of <see cref="RoverSystem"/>.</summary>
        private static readonly string[] EnglishSystems =
        {
            "WHEELS ········ OK (1 MISMATCHED)",
            "LAMPS ········· OK",
            "LENS · SCAN ··· OK",
            "ASSEMBLY ······ OK · 0 BLUEPRINTS",
            "CUTTER ········ FAULT · NO IGNITER",
            "SOLAR WING ···· JAMMED",
            "ANTENNA ······· RECEIVE ONLY",
            "SOCKETS 1-2 ··· EMPTY",
        };

        private static readonly string[] VietnameseSystems =
        {
            "BÁNH XE ········ OK (1 BÁNH KHÁC BỘ)",
            "ĐÈN ············ OK",
            "ỐNG KÍNH · QUÉT  OK",
            "LẮP RÁP ········ OK · 0 BẢN VẼ",
            "MỎ CẮT ········· LỖI · MẤT BỘ ĐÁNH LỬA",
            "CÁNH PIN ······· KẸT",
            "ĂNG-TEN ········ CHỈ NHẬN",
            "Ổ LẮP 1–2 ······ TRỐNG",
        };

        private LocalizationService _localization;
        private ReadoutText _text;

        [SetUp]
        public void SetUp()
        {
            var en = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("en"));
            var vi = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("vi"));
            _localization = new LocalizationService(new EventBus(),
                new[] { StringTable.Parse(en.text, "en"), StringTable.Parse(vi.text, "vi") });
            _text = new ReadoutText(_localization);
        }

        [Test]
        public void TheProtocolLine_ReadsPowerOffline_ThenPowerOn_ThenTheWestLink()
        {
            var protocol = new OpeningProtocol();
            Assert.AreEqual(ProtocolLine.PowerOffline, protocol.Line);
            Assert.IsFalse(protocol.IsPowerOn);

            Assert.IsFalse(protocol.Reach(OpeningMilestone.FirstCharge), "the first charge does not change the line");
            Assert.IsTrue(protocol.Reach(OpeningMilestone.PowerOn));
            Assert.AreEqual(ProtocolLine.PowerOn, protocol.Line);
            Assert.IsTrue(protocol.IsPowerOn);
            Assert.IsFalse(protocol.Reach(OpeningMilestone.NightReady), "nor does the battery bank");
            Assert.IsFalse(protocol.Reach(OpeningMilestone.PowerOn), "a milestone twice changes nothing");

            Assert.IsTrue(protocol.Reach(OpeningMilestone.SignalWest));
            Assert.AreEqual(ProtocolLine.LinkSignalWest, protocol.Line);
            Assert.IsTrue(protocol.IsPowerOn, "the base keeps its power");
        }

        [Test]
        public void ASavedGame_ReplaysItsMilestones_IntoTheSameLine_InAnyOrder()
        {
            var protocol = new OpeningProtocol();
            protocol.Reach(OpeningMilestone.SignalWest);
            protocol.Reach(OpeningMilestone.NightReady);
            protocol.Reach(OpeningMilestone.PowerOn);
            protocol.Reach(OpeningMilestone.FirstCharge);
            Assert.AreEqual(ProtocolLine.LinkSignalWest, protocol.Line);
        }

        [Test]
        public void TheProtocolLines_ReadAsDesigned_InBothLanguages()
        {
            Assert.AreEqual("POWER · OFFLINE", _localization.Get(OpeningProtocol.KeyOf(ProtocolLine.PowerOffline)));
            Assert.AreEqual("POWER · ON", _localization.Get(OpeningProtocol.KeyOf(ProtocolLine.PowerOn)));
            Assert.AreEqual("LINK · 0/1 · SIGNAL WEST",
                _localization.Get(OpeningProtocol.KeyOf(ProtocolLine.LinkSignalWest)));
            _localization.SetLanguage("vi");
            foreach (ProtocolLine line in System.Enum.GetValues(typeof(ProtocolLine)))
            {
                Assert.IsNotEmpty(_localization.Get(OpeningProtocol.KeyOf(line)));
            }
        }

        [Test]
        public void TheSystemLines_ReadExactlyAsDesigned_InEnglish()
        {
            AssertSystems(EnglishSystems);
        }

        [Test]
        public void TheSystemLines_ReadExactlyAsDesigned_InVietnamese()
        {
            _localization.SetLanguage("vi");
            AssertSystems(VietnameseSystems);
        }

        [Test]
        public void TheCards_FitTheReadoutPlate_InBothLanguages()
        {
            const int plateCharacters = 52;
            var systems = new FakeSystems();
            foreach (string language in new[] { "en", "vi" })
            {
                _localization.SetLanguage(language);
                foreach (RoverSystem system in System.Enum.GetValues(typeof(RoverSystem)))
                {
                    string line = _text.SystemLine(system, systems.StateOf(system), 12);
                    Assert.LessOrEqual(line.Length, plateCharacters, $"{language}: '{line}'");
                }

                var content = new ReadoutContent();
                _text.Story(content, "story.airlock_panel");
                Assert.AreEqual(2, content.Count, $"{language}: the panel's text is two lines");
                for (int i = 0; i < content.Count; i++)
                {
                    Assert.LessOrEqual(content.TextAt(i).Length, plateCharacters, $"{language}: '{content.TextAt(i)}'");
                }
            }
        }

        [TestCase("en", 1, "Solar Array · blueprint 1/3")]
        [TestCase("en", 3, "Solar Array · blueprint 3/3 · learned")]
        [TestCase("vi", 1, "Giàn pin mặt trời · bản vẽ 1/3")]
        [TestCase("vi", 3, "Giàn pin mặt trời · bản vẽ 3/3 · đã học")]
        public void AScanSetsProgress_ReadsPlainly_AndFitsThePlate(string language, int step, string expected)
        {
            const int plateCharacters = 52;
            _localization.SetLanguage(language);
            var content = new ReadoutContent();
            _text.ScanSet(content, "structure.solar_array.name", step, 3);
            Assert.AreEqual(1, content.Count, "one short line");
            Assert.AreEqual(expected, content.TextAt(0));
            Assert.LessOrEqual(content.TextAt(0).Length, plateCharacters);
        }

        [Test]
        public void AFlippedLine_ReadsOk_AndTheBlueprintsCountIsInTheAssemblyLine()
        {
            Assert.AreEqual("CUTTER ········ OK",
                _text.SystemLine(RoverSystem.Cutter, RoverSystemState.Ok, 0));
            Assert.AreEqual("ASSEMBLY ······ OK · 1 BLUEPRINT",
                _text.SystemLine(RoverSystem.Assembly, RoverSystemState.Ok, 1));
            Assert.AreEqual("ASSEMBLY ······ OK · 3 BLUEPRINTS",
                _text.SystemLine(RoverSystem.Assembly, RoverSystemState.Ok, 3));
        }

        [Test]
        public void ABrokenPartReadsWarm_AndOneThatWaitsReadsDim()
        {
            Assert.AreEqual(ReadoutLineStyle.Plain, ReadoutText.StyleOf(RoverSystemState.Ok));
            Assert.AreEqual(ReadoutLineStyle.Fault, ReadoutText.StyleOf(RoverSystemState.Fault));
            Assert.AreEqual(ReadoutLineStyle.Fault, ReadoutText.StyleOf(RoverSystemState.Jammed));
            Assert.AreEqual(ReadoutLineStyle.Dim, ReadoutText.StyleOf(RoverSystemState.ReceiveOnly));
            Assert.AreEqual(ReadoutLineStyle.Dim, ReadoutText.StyleOf(RoverSystemState.Empty));
        }

        [Test]
        public void TheReserveCard_ReadsTheLandersReserve()
        {
            var content = new ReadoutContent();
            _text.Reserve(content, 0);
            Assert.AreEqual("LANDER RESERVE · 0%", content.TextAt(0));
        }

        [Test]
        public void TheAirlockPanel_ReadsAsTwoLines_WithoutAFragmentNote()
        {
            var content = new ReadoutContent();
            _text.Story(content, "story.airlock_panel");
            Assert.AreEqual(2, content.Count);
            Assert.AreEqual("HABITAT SEAL · LOCKED · 28 OCT 1988", content.TextAt(0));
            Assert.AreEqual("SUIT 01-04 · OFFLINE · 29 OCT 1988 · 04:12", content.TextAt(1));
        }

        [Test]
        public void ACardWaitsForTheSlot_ThenRests_ThenFades_ThenTheNextOneComes()
        {
            var settings = new GuidanceSettings();
            var queue = new ReadoutQueue(settings);
            Assert.IsFalse(queue.Wants);

            queue.Offer(new ReadoutRequest(ReadoutKind.Protocol));
            Assert.IsTrue(queue.Wants, "an asked-for card wants the slot");
            Assert.IsFalse(queue.Tick(Frame, false, true), "it does not start before the slot lets it");
            Assert.AreEqual(ReadoutKind.None, queue.Current.Kind);

            Assert.IsTrue(queue.Tick(Frame, true, true), "it starts once the slot allows");
            Assert.AreEqual(ReadoutKind.Protocol, queue.Current.Kind);
            Assert.IsTrue(queue.IsShowing);

            queue.Offer(new ReadoutRequest(ReadoutKind.Reserve));
            Run(queue, settings.StatusHoldSeconds - 0.1f, true);
            Assert.IsTrue(queue.IsShowing, "the hold is not over");
            Run(queue, 0.2f, true);
            Assert.IsFalse(queue.IsShowing, "the hold ended: it fades");
            Assert.AreEqual(ReadoutKind.Protocol, queue.Current.Kind, "and stays until the fade is over");

            Assert.IsFalse(queue.Tick(Frame, true, false), "the next card does not start over a fading one");
            Assert.IsFalse(queue.Tick(Frame, true, true), "the fade is over: the screen is clear");
            Assert.AreEqual(ReadoutKind.None, queue.Current.Kind);
            Assert.IsTrue(queue.Tick(Frame, true, true), "then the reserve card comes");
            Assert.AreEqual(ReadoutKind.Reserve, queue.Current.Kind);
        }

        [Test]
        public void AScanSetsNextPiece_TakesItsCardsPlace_WaitingOrShowing_SoCardsNeverPileUp()
        {
            var settings = new GuidanceSettings();
            var queue = new ReadoutQueue(settings);
            const string array = "structure.solar_array.name";
            Assert.IsFalse(queue.Offer(new ReadoutRequest(ReadoutKind.ScanSet, textKey: array, step: 1, of: 3)));
            Assert.IsFalse(queue.Offer(new ReadoutRequest(ReadoutKind.ScanSet, textKey: array, step: 2, of: 3)),
                "the newer piece replaces the waiting card");
            Assert.IsTrue(queue.Tick(Frame, true, true));
            Assert.AreEqual(2, queue.Current.Step, "only the newest shows");
            Run(queue, settings.ScanSetHoldSeconds - 0.5f, true);
            Assert.IsTrue(queue.Offer(new ReadoutRequest(ReadoutKind.ScanSet, textKey: array, step: 3, of: 3)),
                "the last piece lands on the card that shows");
            Assert.AreEqual(3, queue.Current.Step);
            Run(queue, settings.ScanSetHoldSeconds - 0.5f, true);
            Assert.IsTrue(queue.IsShowing, "with a fresh, short hold");
            Run(queue, 1f, true);
            Assert.IsFalse(queue.IsShowing);
            Assert.IsFalse(queue.Offer(new ReadoutRequest(ReadoutKind.ScanSet, textKey: "structure.charging_pad.name",
                step: 1, of: 2)), "another set is another card");
        }

        [Test]
        public void ACardAskedForTwice_QueuesOnce_AndOneThatShowsRestsAgain()
        {
            var settings = new GuidanceSettings();
            var queue = new ReadoutQueue(settings);
            var cutter = new ReadoutRequest(ReadoutKind.System, RoverSystem.Cutter);
            Assert.IsFalse(queue.Offer(cutter));
            Assert.IsFalse(queue.Offer(cutter), "asking again while it waits is the same card");
            queue.Tick(Frame, true, true);
            Run(queue, settings.StatusHoldSeconds - 0.5f, true);
            Assert.IsTrue(queue.Offer(cutter), "asking while it shows keeps it and says so");
            Run(queue, settings.StatusHoldSeconds - 0.5f, true);
            Assert.IsTrue(queue.IsShowing, "the hold started again");

            Assert.IsFalse(queue.Offer(new ReadoutRequest(ReadoutKind.System, RoverSystem.Wheels)),
                "another system is another card");
        }

        [Test]
        public void ACardPausedBySlot_KeepsItsHold()
        {
            var settings = new GuidanceSettings();
            var queue = new ReadoutQueue(settings);
            queue.Offer(new ReadoutRequest(ReadoutKind.Story, textKey: "story.airlock_panel"));
            queue.Tick(Frame, true, true);
            Run(queue, settings.StoryHoldSeconds * 3f, false);
            Assert.IsTrue(queue.IsShowing, "no hold passes while the slot is not the readout's");
            Run(queue, settings.StoryHoldSeconds + 0.1f, true);
            Assert.IsFalse(queue.IsShowing);
        }

        [Test]
        public void TheSunsArc_FollowsTheSunsHeight()
        {
            const float full = 17.3f;
            Assert.AreEqual(0f, SunArc.Fraction(Direction(0f), full), 1e-4f, "at the rim");
            Assert.AreEqual(0f, SunArc.Fraction(Direction(-5f), full), 1e-4f, "below it");
            Assert.AreEqual(1f, SunArc.Fraction(Direction(17.3f), full), 1e-3f, "at the top of its path");
            Assert.AreEqual(1f, SunArc.Fraction(Direction(40f), full), 1e-4f, "never beyond");
            Assert.AreEqual(3f / full, SunArc.Fraction(Direction(3f), full), 1e-3f, "last light is a short arc");
            Assert.IsTrue(SunArc.IsUp(Direction(0.5f)));
            Assert.IsFalse(SunArc.IsUp(Direction(0f)));
            Assert.IsFalse(SunArc.IsUp(Direction(-1.5f)));
            Assert.AreEqual(20f, SunArc.ElevationDegrees(Direction(20f)), 1e-3f);
        }

        [Test]
        public void TheSunsTime_ShowsOnlyWhileItIsReallyGoingDown()
        {
            Assert.IsFalse(SunArc.CountsDown(float.PositiveInfinity), "the opening holds the Sun: no countdown");
            Assert.IsFalse(SunArc.HasSet(float.PositiveInfinity));
            Assert.IsFalse(SunArc.CountsDown(0f), "set");
            Assert.IsTrue(SunArc.HasSet(0f));
            Assert.IsTrue(SunArc.CountsDown(90f));
            Assert.AreEqual(2, SunArc.MinutesLeft(61f), "rounded up");
            Assert.AreEqual(1, SunArc.MinutesLeft(5f), "never 0 while it is still up");
            Assert.AreEqual(12, SunArc.MinutesLeft(720f));
        }

        private void AssertSystems(string[] expected)
        {
            var systems = new FakeSystems();
            var all = (RoverSystem[])System.Enum.GetValues(typeof(RoverSystem));
            Assert.AreEqual(expected.Length, all.Length, "one designed line per system");
            for (int i = 0; i < all.Length; i++)
            {
                Assert.AreEqual(expected[i], _text.SystemLine(all[i], systems.StateOf(all[i]), 0), all[i].ToString());
            }
        }

        private static void Run(ReadoutQueue queue, float seconds, bool granted)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                queue.Tick(Frame, granted, false);
            }
        }

        private static Vector3 Direction(float elevationDegrees)
        {
            float radians = elevationDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
        }

        /// <summary>07's start kit (opening-v2 §10).</summary>
        private sealed class FakeSystems : IRoverSystems
        {
            public bool CanFitIgniter => false;

            public string IgniterPartId => "part.igniter";

            public RoverSystemState StateOf(RoverSystem system)
            {
                switch (system)
                {
                    case RoverSystem.Cutter:
                        return RoverSystemState.Fault;
                    case RoverSystem.SolarWing:
                        return RoverSystemState.Jammed;
                    case RoverSystem.Antenna:
                        return RoverSystemState.ReceiveOnly;
                    case RoverSystem.Sockets:
                        return RoverSystemState.Empty;
                    default:
                        return RoverSystemState.Ok;
                }
            }

            public bool TryFitIgniter()
            {
                return false;
            }
        }
    }
}

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
    /// the boot diagnostics read exactly as designed in English and Vietnamese, the cards take turns and the Sun's arc
    /// follows the Sun's height.
    /// </summary>
    public sealed class ReadoutTests
    {
        private const float Frame = 1f / 60f;

        private static readonly string[] EnglishBoot =
        {
            "UNIT 07 · SSR-5 · CONTINUE · 13 148 DAYS",
            "WHEELS ········ OK (1 MISMATCHED)",
            "LAMPS ········· OK",
            "LENS · SCAN ··· OK",
            "ASSEMBLY ······ OK · 0 BLUEPRINTS",
            "CUTTER ········ FAULT · NO IGNITER",
            "SOLAR WING ···· JAMMED",
            "ANTENNA ······· RECEIVE ONLY",
            "SOCKETS 1-2 ··· EMPTY",
            "BATTERY ······· 50%",
            "POWER · OFFLINE",
        };

        private static readonly string[] VietnameseBoot =
        {
            "ĐƠN VỊ 07 · SSR-5 · TIẾP TỤC · 13 148 NGÀY",
            "BÁNH XE ········ OK (1 BÁNH KHÁC BỘ)",
            "ĐÈN ············ OK",
            "ỐNG KÍNH · QUÉT  OK",
            "LẮP DỰNG ······· OK · 0 BẢN VẼ",
            "MỎ CẮT ········· LỖI · MẤT BỘ ĐÁNH LỬA",
            "CÁNH PIN ······· KẸT",
            "ĂNG-TEN ········ CHỈ NHẬN",
            "Ổ LẮP 1–2 ······ TRỐNG",
            "PIN ············ 50%",
            "NGUỒN · NGOẠI TUYẾN",
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
        public void TheBootDiagnostics_ReadExactlyAsDesigned_InEnglish()
        {
            AssertBoot(EnglishBoot);
        }

        [Test]
        public void TheBootDiagnostics_ReadExactlyAsDesigned_InVietnamese()
        {
            _localization.SetLanguage("vi");
            AssertBoot(VietnameseBoot);
        }

        [Test]
        public void TheBootDiagnostics_FitTheReadoutPlate_InBothLanguages()
        {
            const int plateCharacters = 52;
            foreach (string language in new[] { "en", "vi" })
            {
                _localization.SetLanguage(language);
                var content = new ReadoutContent();
                _text.Boot(content, new FakeSystems(), 12, 100, ProtocolLine.LinkSignalWest);
                for (int i = 0; i < content.Count; i++)
                {
                    Assert.LessOrEqual(content.TextAt(i).Length, plateCharacters, $"{language}: '{content.TextAt(i)}'");
                }

                _text.Story(content, "story.airlock_panel");
                Assert.AreEqual(2, content.Count, $"{language}: the panel's text is two lines");
                for (int i = 0; i < content.Count; i++)
                {
                    Assert.LessOrEqual(content.TextAt(i).Length, plateCharacters, $"{language}: '{content.TextAt(i)}'");
                }
            }
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
        public void TheBootDiagnostics_GoBeforeEveryOtherCard()
        {
            var queue = new ReadoutQueue(new GuidanceSettings());
            queue.Offer(new ReadoutRequest(ReadoutKind.Protocol));
            queue.Offer(new ReadoutRequest(ReadoutKind.Boot));
            Assert.IsTrue(queue.Tick(Frame, true, true));
            Assert.AreEqual(ReadoutKind.Boot, queue.Current.Kind);
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

        private void AssertBoot(string[] expected)
        {
            var content = new ReadoutContent();
            _text.Boot(content, new FakeSystems(), 0, 50, ProtocolLine.PowerOffline);
            Assert.AreEqual(expected.Length, content.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], content.TextAt(i), $"line {i}");
            }

            Assert.AreEqual(ReadoutLineStyle.Fault, content.StyleAt(1 + (int)RoverSystem.Cutter));
            Assert.AreEqual(ReadoutLineStyle.Dim, content.StyleAt(1 + (int)RoverSystem.Antenna));
            Assert.AreEqual((int)RoverSystem.Cutter, content.SystemAt(1 + (int)RoverSystem.Cutter));
            Assert.AreEqual(1 + (int)RoverSystem.Cutter, content.IndexOfSystem((int)RoverSystem.Cutter));
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

        /// <summary>07's start kit as the boot diagnostics list it.</summary>
        private sealed class FakeSystems : IRoverSystems
        {
            public bool CanFitIgniter => false;

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

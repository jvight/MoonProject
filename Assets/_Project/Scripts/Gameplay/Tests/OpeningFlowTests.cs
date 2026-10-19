using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The opening's objective flow (docs/design/opening-v2.md §3): the lines in order as the game moves on, the lines
    /// that wait for the world, nothing stored (a loaded game's state shows its step at once), and the milestones
    /// published once, replayed as Restored on a load.
    /// </summary>
    public sealed class OpeningFlowTests
    {
        private EventBus _events;
        private List<string> _lines;
        private List<OpeningMilestoneReached> _milestones;
        private FakeProgress _game;
        private OpeningFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _lines = new List<string>();
            _milestones = new List<OpeningMilestoneReached>();
            _events.Subscribe<NextStepChanged>(changed => _lines.Add(changed.LineKey));
            _events.Subscribe<OpeningMilestoneReached>(_milestones.Add);
            _game = new FakeProgress();
            _flow = new OpeningFlow(_events, _game);
        }

        [Test]
        public void TheLines_FollowTheOpening_OneAtATime_AndEachClearsWhenItsStepIsDone()
        {
            Expect(OpeningKeys.DriveToLander);
            _game.ReachedLander = true;
            Expect(OpeningKeys.ScanLander);
            _game.Known.Add(StructureIds.ChargingPad);
            Expect(OpeningKeys.CollectParts);
            _game.Covered.Add(StructureIds.ChargingPad);
            Expect(OpeningKeys.BuildPad, "the charger kit alone covers the pad: no igniter in the lander's crate");
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            Expect(OpeningKeys.Charge);
            _game.FirstCharge = true;
            Expect(string.Empty, "the reserve is not spent yet: the lander's 0% says it first");
            _game.ReserveSpent = true;
            Expect(OpeningKeys.FindSolar);
            _game.Known.Add(StructureIds.SolarArray);
            Expect(OpeningKeys.CutOptics, "the cutter is FAULT, but the line asks to cut: the rack is in plain view");
            _game.CutterSputtered = true;
            Expect(OpeningKeys.TakeIgniter, "the cutter sputtered: the line points at the rack");
            _game.CarriesIgniter = true;
            _game.CanFitIgniter = true;
            Expect(OpeningKeys.FitIgniter, "the igniter in the hold: a field swap, fitted where 07 stands");
            _game.CarriesIgniter = false;
            _game.IgniterFitted = true;
            Expect(OpeningKeys.CutOptics, "the hold does not cover the array yet");
            _game.Covered.Add(StructureIds.SolarArray);
            Expect(OpeningKeys.BuildSolar);
            _game.PowerOn = true;
            Expect(OpeningKeys.BuildBatteryBank, "Night is coming shows at PowerOn");
            _game.NightReady = true;
            Expect(string.Empty, "the first nightfall says nothing");
            _game.NightFallen = true;
            Expect(OpeningKeys.ScanTower);
            _game.Known.Add(StructureIds.AntennaMast);
            Expect(OpeningKeys.BuildMast);
            _game.BuiltIds.Add(StructureIds.AntennaMast);
            Expect(string.Empty, "the pillar in the west is the message");
        }

        [Test]
        public void WithAPadPartInTheHold_TheLineAsksToParkOffThePad_AndToFitOnIt()
        {
            ReachTheArray();
            _game.CarriesIgniter = true;
            _game.CanFitIgniter = false;
            Expect(OpeningKeys.ParkOnPad, "a part that needs the pad: the line says to park there");
            _game.CanFitIgniter = true;
            Expect(OpeningKeys.FitIgniter, "on the plate (or anywhere, for a field swap) the cutter takes it");
            _game.CanFitIgniter = false;
            Expect(OpeningKeys.ParkOnPad, "driving off the plate asks again");
        }

        [Test]
        public void TheFitLine_ShowsExactlyWhenTheScreenWillTakeTheIgniter()
        {
            ReachTheArray();
            foreach (bool carries in new[] { false, true })
            {
                foreach (bool canFit in new[] { false, true })
                {
                    foreach (bool fitted in new[] { false, true })
                    {
                        _game.CarriesIgniter = carries;
                        _game.CanFitIgniter = canFit;
                        _game.IgniterFitted = fitted;
                        _flow.Tick();
                        bool screenTakesIt = !fitted && carries && canFit;
                        Assert.AreEqual(screenTakesIt, _flow.Current == OpeningKeys.FitIgniter,
                            $"carries {carries}, can fit {canFit}, fitted {fitted}: '{_flow.Current}'");
                    }
                }
            }
        }

        [Test]
        public void AnIgniterTakenUnasked_IsFittedWithNoRackLine()
        {
            ReachTheArray();
            Assert.AreEqual(OpeningKeys.CutOptics, _flow.Current);
            _game.CarriesIgniter = true;
            _game.CanFitIgniter = true;
            Expect(OpeningKeys.FitIgniter, "taken from the rack before the cutter ever sputtered");
            _game.CarriesIgniter = false;
            _game.IgniterFitted = true;
            Expect(OpeningKeys.CutOptics);
            CollectionAssert.DoesNotContain(_lines, OpeningKeys.TakeIgniter, "the rack line is for a stalled player");
        }

        [Test]
        public void AnIgniterFittedBeforeTheScan_PassesOverTheIgniterStep()
        {
            ReachTheArray();
            _game.Known.Remove(StructureIds.SolarArray);
            _game.IgniterFitted = true;
            Expect(OpeningKeys.FindSolar);
            _game.Known.Add(StructureIds.SolarArray);
            Expect(OpeningKeys.CutOptics);
            CollectionAssert.DoesNotContain(_lines, OpeningKeys.FitIgniter);
        }

        /// <summary>B7: the reserve spent and the array scanned; the cutter still has no igniter.</summary>
        private void ReachTheArray()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Covered.Add(StructureIds.ChargingPad);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
            _game.ReserveSpent = true;
            _game.Known.Add(StructureIds.SolarArray);
            _flow.Tick();
        }

        [Test]
        public void TheCollectLine_IsPassedOver_WhenTheHoldAlreadyHoldsTheParts()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Covered.Add(StructureIds.ChargingPad);
            Expect(OpeningKeys.BuildPad);
            CollectionAssert.DoesNotContain(_lines, OpeningKeys.CollectParts);
        }

        [Test]
        public void ALoadedGame_ShowsItsStepAtOnce_AndReplaysItsMilestonesAsRestored()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Known.Add(StructureIds.SolarArray);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
            _game.IgniterFitted = true;
            _game.ReserveSpent = true;
            _game.PowerOn = true;
            _flow.Announce();
            _flow.Tick();
            Assert.AreEqual(new[] { OpeningKeys.BuildBatteryBank }, _lines.ToArray(), "no stored step: read at once");
            Assert.AreEqual(2, _milestones.Count);
            Assert.AreEqual(OpeningMilestone.FirstCharge, _milestones[0].Milestone);
            Assert.AreEqual(OpeningMilestone.PowerOn, _milestones[1].Milestone);
            Assert.IsTrue(_milestones[0].Restored && _milestones[1].Restored, "replayed, not reached now");
        }

        [Test]
        public void EachMilestone_IsPublishedOnce_AsTheOpeningReachesIt()
        {
            _flow.Tick();
            Assert.IsEmpty(_milestones, "a new game has reached none");
            _game.FirstCharge = true;
            _flow.Tick();
            _flow.Tick();
            _game.PowerOn = true;
            _flow.Tick();
            _game.NightReady = true;
            _flow.Tick();
            _game.BuiltIds.Add(StructureIds.AntennaMast);
            _flow.Tick();
            Assert.AreEqual(4, _milestones.Count, "once each");
            Assert.AreEqual(OpeningMilestone.FirstCharge, _milestones[0].Milestone);
            Assert.AreEqual(OpeningMilestone.PowerOn, _milestones[1].Milestone);
            Assert.AreEqual(OpeningMilestone.NightReady, _milestones[2].Milestone);
            Assert.AreEqual(OpeningMilestone.SignalWest, _milestones[3].Milestone);
            Assert.IsFalse(_milestones[3].Restored);
            Assert.IsTrue(_flow.HasReached(OpeningMilestone.SignalWest));
            Assert.IsFalse(_flow.HasReached(OpeningMilestone.CallAnswered), "the answer publishes its own milestone");
        }

        [Test]
        public void ALoadedGame_AtTheOpeningsEnd_ReplaysOnlyTheMilestonesTheFlowDerives()
        {
            _game.FirstCharge = true;
            _game.PowerOn = true;
            _game.NightReady = true;
            _game.BuiltIds.Add(StructureIds.AntennaMast);
            Assert.DoesNotThrow(() => _flow.Announce(), "every milestone the enum has is safe to replay past");
            Assert.AreEqual(4, _milestones.Count);
            foreach (OpeningMilestoneReached reached in _milestones)
            {
                Assert.AreNotEqual(OpeningMilestone.CallAnswered, reached.Milestone);
            }
        }

        [Test]
        public void ABankBuiltOffTheGrid_AsksToMoveItCloser_ThenNightReadyClearsTheLine()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Known.Add(StructureIds.SolarArray);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
            _game.IgniterFitted = true;
            _game.ReserveSpent = true;
            _game.PowerOn = true;
            Expect(OpeningKeys.BuildBatteryBank);
            _game.BuiltIds.Add(StructureIds.BatteryBank);
            Expect(OpeningKeys.MoveBatteryBank, "built, but the grid does not reach it");
            _game.NightReady = true;
            Expect(string.Empty, "moved within reach: the first nightfall says nothing");
        }

        private void Expect(string key, string why = "the next step")
        {
            _flow.Tick();
            Assert.AreEqual(key, _flow.Current, why);
            Assert.AreEqual(key, _lines[_lines.Count - 1], "published as the line");
        }

        private sealed class FakeProgress : IOpeningProgress
        {
            public readonly HashSet<string> Known = new HashSet<string>();
            public readonly HashSet<string> Covered = new HashSet<string>();
            public readonly HashSet<string> BuiltIds = new HashSet<string>();

            public bool ReachedLander { get; set; }
            public bool CarriesIgniter { get; set; }
            public bool CutterSputtered { get; set; }
            public bool CanFitIgniter { get; set; }
            public bool IgniterFitted { get; set; }
            public bool FirstCharge { get; set; }
            public bool ReserveSpent { get; set; }
            public bool PowerOn { get; set; }
            public bool NightReady { get; set; }
            public bool NightFallen { get; set; }

            public bool Knows(string structureId)
            {
                return Known.Contains(structureId);
            }

            public bool Covers(string structureId)
            {
                return Covered.Contains(structureId);
            }

            public bool Built(string structureId)
            {
                return BuiltIds.Contains(structureId);
            }
        }
    }
}

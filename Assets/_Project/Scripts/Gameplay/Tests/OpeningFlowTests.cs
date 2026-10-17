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
            Expect(OpeningKeys.CutCrates);
            _game.Covered.Add(StructureIds.ChargingPad);
            Expect(OpeningKeys.BuildPad);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            Expect(OpeningKeys.Charge);
            _game.FirstCharge = true;
            Expect(string.Empty, "the reserve is not spent yet: the lander's 0% says it first");
            _game.ReserveSpent = true;
            Expect(OpeningKeys.FindSolar);
            _game.Known.Add(StructureIds.SolarArray);
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
        public void TheCratesLine_IsPassedOver_WhenTheHoldAlreadyCoversThePad()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Covered.Add(StructureIds.ChargingPad);
            Expect(OpeningKeys.BuildPad);
            CollectionAssert.DoesNotContain(_lines, OpeningKeys.CutCrates);
        }

        [Test]
        public void ALoadedGame_ShowsItsStepAtOnce_AndReplaysItsMilestonesAsRestored()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Known.Add(StructureIds.SolarArray);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
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

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The opening's objective flow (docs/design/opening-v2.md §3, opening-v3 guidance audit): the lines in order as
    /// the game moves on, the lines that wait for the world, nothing stored (a loaded game's state shows its step at
    /// once), the next line after each success with its key's glyph while the key is new or idle (G2), the reminder
    /// after a stall, moving or still, once more later, then never (G4), and the milestones published once, replayed as
    /// Restored on a load.
    /// </summary>
    public sealed class OpeningFlowTests
    {
        private const int NextLine = 6;
        private const int Stall = 25;
        private const int Again = 90;
        private const int Reminder = 20;
        private const int KeyFresh = 300;
        private const float Closing = 0.5f;
        private const float Arrival = 15f;

        private EventBus _events;
        private List<string> _lines;
        private List<NextStepChanged> _changes;
        private List<OpeningMilestoneReached> _milestones;
        private FakeProgress _game;
        private KeyUse _keys;
        private OpeningFlow _flow;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _lines = new List<string>();
            _changes = new List<NextStepChanged>();
            _milestones = new List<OpeningMilestoneReached>();
            _events.Subscribe<NextStepChanged>(changed =>
            {
                _lines.Add(changed.LineKey);
                _changes.Add(changed);
            });
            _events.Subscribe<OpeningMilestoneReached>(_milestones.Add);
            _game = new FakeProgress();
            _keys = new KeyUse();
            _flow = new OpeningFlow(_events, _game, _keys, new GuidanceTiming(NextLine, Stall, Again, Reminder, KeyFresh,
                Closing, Arrival));
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
            Expect(OpeningKeys.DockForDusk, "power on: the dock that ends the day is named, wherever 07 roams (payoff audit §4)");
            _game.DuskBegun = true;
            Expect(OpeningKeys.ScanModule, "night is coming: the habitat's power module can store it");
            _game.Known.Add(StructureIds.BatteryBank);
            Expect(OpeningKeys.HookModule, "the module is the bank's frame: hook it and tow it home (B9a)");
            _game.ModuleHooked = true;
            Expect(OpeningKeys.TowModuleHome, "the module is on the cable: tow it home (G6)");
            _game.ModuleHooked = false;
            _game.ModuleHome = true;
            Expect(OpeningKeys.BuildBatteryBank, "home: build the bank on it");
            _game.NightReady = true;
            Expect(OpeningKeys.DockForNight, "night-ready: dock on the pad for the night");
            _game.NightFallen = true;
            Expect(OpeningKeys.ScanTower);
            _game.Known.Add(StructureIds.AntennaMast);
            Expect(OpeningKeys.BuildMast);
            _game.BuiltIds.Add(StructureIds.AntennaMast);
            Expect(OpeningKeys.Transmit, "the mast stands: transmit from it (G6)");
            _game.AnswerStarted = true;
            Expect(string.Empty, "transmitting: the answer's moment says nothing");
            _game.AnswerDone = true;
            Expect(OpeningKeys.GoToRelay, "relay.1 answered: drive to it");
            _game.AtRelay = true;
            Expect(string.Empty, "at relay.1: its cabinet's and socket's chips lead");
            _game.RelayRelit = true;
            Expect(string.Empty, "relay.1 lit: the opening's lines are done; 06 and the headlamp are a pull, no line");
        }

        [Test]
        public void TheStepIndexes_FollowTheOpening_AsOpeningStepsNamesThem()
        {
            Assert.AreEqual(OpeningSteps.DriveToLander, _flow.CurrentIndex);
            _game.ReachedLander = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.ScanLander, _flow.CurrentIndex);
            _game.Known.Add(StructureIds.ChargingPad);
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.CollectParts, _flow.CurrentIndex);
            _game.Covered.Add(StructureIds.ChargingPad);
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.BuildPad, _flow.CurrentIndex);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.Charge, _flow.CurrentIndex);
            _game.FirstCharge = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.FindSolar, _flow.CurrentIndex, "a step with no line yet still has its place");
            _game.ReserveSpent = true;
            _game.Known.Add(StructureIds.SolarArray);
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.FitIgniter, _flow.CurrentIndex);
            _game.IgniterFitted = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.BuildSolar, _flow.CurrentIndex);
            _game.PowerOn = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.BuildBatteryBank, _flow.CurrentIndex);
            _game.NightReady = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.ScanTower, _flow.CurrentIndex);
            _game.Known.Add(StructureIds.AntennaMast);
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.BuildMast, _flow.CurrentIndex);
            _game.BuiltIds.Add(StructureIds.AntennaMast);
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.Transmit, _flow.CurrentIndex);
            _game.AnswerStarted = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.GoToRelay, _flow.CurrentIndex, "the transmit step ends as 07 transmits");
            _game.AnswerDone = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.GoToRelay, _flow.CurrentIndex);
            _game.RelayRelit = true;
            _flow.Tick();
            Assert.AreEqual(OpeningSteps.Count, _flow.CurrentIndex, "the index is the step count once the opening is over");
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

        [Test]
        public void ShortOfTheRelayTrip_TheLineSendsToThePadFirst_KeyLess_ThenToTheRelayOnceCharged()
        {
            _game.RelayTripCovered = false;
            ReachTheArray();
            _game.IgniterFitted = true;
            _game.PowerOn = true;
            _game.NightReady = true;
            _game.NightFallen = true;
            _game.Known.Add(StructureIds.AntennaMast);
            _game.BuiltIds.Add(StructureIds.AntennaMast);
            _game.AnswerStarted = true;
            _game.AnswerDone = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningSteps.GoToRelay, _flow.CurrentIndex);
            Assert.AreEqual(OpeningKeys.ChargeForRelay, _flow.Current, "relay.1 answered, but 07 would arrive too low");
            Assert.AreEqual(OpeningKeys.ChargeForRelay, _flow.Shown);
            Assert.AreEqual(default(RoverAction), _changes[_changes.Count - 1].Key, "one element: no key chip");
            Tick(NextLine);
            int said = _lines.Count;
            _events.Publish(new ChargingChanged(true, ChargerKind.Pad, Vector3.zero));
            Tick(Stall + Again + Reminder * 2);
            Assert.AreEqual(said, _lines.Count, "07 charges as asked: the line is not said again, nor as a reminder");

            _game.RelayTripCovered = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.GoToRelay, _flow.Shown, "charged enough: the relay line comes as the next line");
            Assert.AreEqual(NextLineReason.Next, _flow.ShownReason);
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
            Assert.AreNotEqual(OpeningKeys.CollectParts, _flow.Current);
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
            _game.DuskBegun = true;
            _game.Known.Add(StructureIds.BatteryBank);
            _game.ModuleHome = true;
            _flow.Announce();
            _flow.Tick();
            Assert.AreEqual(OpeningKeys.BuildBatteryBank, _flow.Current, "no stored step: read at once");
            Assert.IsEmpty(_lines, "the line a session starts on is no success: no next line");
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
            Assert.AreEqual(2, _milestones.Count, "the base night-ready, 07 off the pad: the night waits for the dock");
            _events.Publish(new RoverDockChanged(true, Vector3.zero, Quaternion.identity));
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
            _game.NightFallen = true;
            _game.BuiltIds.Add(StructureIds.AntennaMast);
            _game.RelayRelit = true;
            Assert.DoesNotThrow(() => _flow.Announce(), "every milestone the enum has is safe to replay past");
            Assert.AreEqual(5, _milestones.Count);
            foreach (OpeningMilestoneReached reached in _milestones)
            {
                Assert.AreNotEqual(OpeningMilestone.CallAnswered, reached.Milestone);
            }

            Assert.AreEqual(OpeningMilestone.OpeningComplete, _milestones[4].Milestone);
            Assert.IsTrue(_milestones[4].Restored);
        }

        [Test]
        public void RelayOneLit_CompletesTheOpening_Once()
        {
            _flow.Tick(1f);
            Assert.IsFalse(_flow.HasReached(OpeningMilestone.OpeningComplete));
            _game.RelayRelit = true;
            _flow.Tick(1f);
            Assert.IsTrue(_flow.HasReached(OpeningMilestone.OpeningComplete), "relay.1 fixed and the ring grown: the end");
            Assert.AreEqual(1, _milestones.FindAll(m => m.Milestone == OpeningMilestone.OpeningComplete).Count);
            _flow.Tick(1f);
            Assert.AreEqual(1, _milestones.FindAll(m => m.Milestone == OpeningMilestone.OpeningComplete).Count,
                "once");
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
            _game.DuskBegun = true;
            _game.Known.Add(StructureIds.BatteryBank);
            _game.ModuleHome = true;
            Expect(OpeningKeys.BuildBatteryBank);
            _game.BuiltIds.Add(StructureIds.BatteryBank);
            Expect(OpeningKeys.MoveBatteryBank, "built, but the grid does not reach it");
            _game.NightReady = true;
            Expect(OpeningKeys.DockForNight, "moved within reach: dock on the pad for the night");
        }

        [Test]
        public void ASuccess_ShowsTheNextLine_ForItsSeconds_ButNotTheLineASessionStartsOn()
        {
            _flow.Tick(1f);
            Assert.IsEmpty(_lines, "the first line of a session is no success");
            _game.ReachedLander = true;
            _flow.Tick(1f);
            CollectionAssert.AreEqual(new[] { OpeningKeys.ScanLander }, _lines, "at the lander: the next small action");
            Assert.AreEqual(NextLineReason.Next, _changes[0].Reason);
            Assert.AreEqual(NextLineReason.Next, _flow.ShownReason);
            Tick(NextLine);
            CollectionAssert.AreEqual(new[] { OpeningKeys.ScanLander, string.Empty }, _lines, "then it goes");
        }

        [Test]
        public void TheEndOfANewGamesWake_ShowsTheFirstLine_AsTheNextLine()
        {
            _flow.Tick(1f);
            Assert.IsEmpty(_lines);
            _events.Publish(new BootCheckFinished());
            _flow.Tick(1f);
            CollectionAssert.AreEqual(new[] { OpeningKeys.DriveToLander }, _lines, "the wake is done: drive to it");
            Assert.AreEqual(NextLineReason.Next, _changes[0].Reason);
            Tick(NextLine);
            _flow.Tick(1f);
            Assert.AreEqual(2, _lines.Count, "once");
        }

        [Test]
        public void WatchingANewGamesWake_IsNoStall_TheReminderCountsFromTheHandOver()
        {
            _events.Publish(new RoverWakeBeatReached(WakeBeat.FadedOut));
            _flow.Tick(1f);
            _events.Publish(new RoverWakeBeatReached(WakeBeat.ChecksBegin));
            Tick(Stall + Again);
            Assert.IsEmpty(_lines, "the wake plays: no line nags a player who only watches it");

            _events.Publish(new BootCheckFinished());
            _flow.Tick(1f);
            Tick(NextLine);
            CollectionAssert.AreEqual(new[] { OpeningKeys.DriveToLander, string.Empty }, _lines, "the hand-over line");
            Tick(Stall - 2);
            Assert.AreEqual(2, _lines.Count, "the stall counts from the hand-over, not from the wake");
            Tick(2);
            Assert.AreEqual(NextLineReason.Reminder, _changes[_changes.Count - 1].Reason);
        }

        [Test]
        public void AKeyLine_LeadsWithItsKey_OnlyWhereTheKeyWouldAct()
        {
            _game.OutOfReach.Add(OpeningKeys.ScanLander);
            _flow.Tick(1f);
            _game.ReachedLander = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.ScanLander, _flow.Shown);
            Assert.AreEqual(RoverAction.Excavate, _changes[0].Key);
            Assert.IsFalse(_changes[0].ShowsKey, "no scan target in reach: E would do nothing yet");

            _game.OutOfReach.Clear();
            Tick(NextLine + Stall);
            Assert.AreEqual(NextLineReason.Reminder, _changes[_changes.Count - 1].Reason);
            Assert.IsTrue(_changes[_changes.Count - 1].ShowsKey, "in reach and E never pressed: the reminder leads with E");
        }

        [Test]
        public void TheNextLine_LeadsWithItsKey_WhileTheKeyIsNewOrIdle()
        {
            _flow.Tick(1f);
            _game.ReachedLander = true;
            _flow.Tick(1f);
            Assert.AreEqual(RoverAction.Excavate, _changes[0].Key);
            Assert.IsTrue(_changes[0].ShowsKey, "E has never been pressed");

            _keys.Pressed(RoverAction.Excavate);
            _game.Known.Add(StructureIds.ChargingPad);
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.CollectParts, _changes[_changes.Count - 1].LineKey);
            Assert.IsFalse(_changes[_changes.Count - 1].ShowsKey, "E was just pressed: the words alone");

            _game.Covered.Add(StructureIds.ChargingPad);
            _flow.Tick(1f);
            Assert.AreEqual(RoverAction.Build, _changes[_changes.Count - 1].Key);
            Assert.IsTrue(_changes[_changes.Count - 1].ShowsKey, "B, never pressed: B  Build the charging pad");

            _keys.Step(KeyFresh);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.Charge, _changes[_changes.Count - 1].LineKey);
            Assert.AreEqual(default(RoverAction), _changes[_changes.Count - 1].Key, "a drive takes no action key");
            Assert.IsFalse(_changes[_changes.Count - 1].ShowsKey);
            Assert.IsFalse(_keys.UsedWithin(RoverAction.Excavate, KeyFresh), "five idle minutes make E new again");
        }

        [Test]
        public void TheLine_CarriesThePlaceOfItsAction_WhenItHasOne()
        {
            var apron = new Vector3(3f, 0f, 9f);
            _game.Places[OpeningKeys.BuildPad] = apron;
            _flow.Tick(1f);
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Covered.Add(StructureIds.ChargingPad);
            _flow.Tick(1f);
            NextStepChanged build = _changes[_changes.Count - 1];
            Assert.AreEqual(OpeningKeys.BuildPad, build.LineKey);
            Assert.IsTrue(build.HasTarget, "B  Build the charging pad: over there");
            Assert.AreEqual("place." + OpeningKeys.BuildPad, build.TargetId);
            Assert.AreEqual(apron, build.TargetPosition);

            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _flow.Tick(1f);
            Assert.IsFalse(_changes[_changes.Count - 1].HasTarget, "a line with no place carries none");
        }

        [Test]
        public void NoProgress_MovingOrStill_BringsTheLineBack_ThenOnceMore_ThenNever()
        {
            _flow.Tick(1f);
            Tick(Stall - 1);
            Assert.IsEmpty(_lines, "a second short of the stall is not yet a stall");
            _flow.Tick(1f);
            CollectionAssert.AreEqual(new[] { OpeningKeys.DriveToLander }, _lines, "the reminder after the stall");
            Assert.AreEqual(NextLineReason.Reminder, _changes[0].Reason);
            Tick(Reminder);
            Assert.AreEqual(string.Empty, _flow.Shown, "it goes after its time");
            Tick(Again - 1);
            Assert.AreEqual(2, _lines.Count, "not yet again");
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.DriveToLander, _flow.Shown, "still stuck: once more");
            Tick(Reminder + 600);
            Assert.AreEqual(4, _lines.Count, "then never again for that step");
        }

        [Test]
        public void ProgressWithinAStep_AScanACutABuildAHook_KeepsTheReminderAway()
        {
            _flow.Tick(1f);
            _game.ReachedLander = true;
            _flow.Tick(1f);
            Tick(NextLine);
            int before = _lines.Count;
            Tick(Stall - 5);
            _events.Publish(new FragmentScanned("ruin.cargo.0", "charging_pad", 0, 2, false, 0));
            Tick(Stall - 5);
            Assert.AreEqual(before, _lines.Count, "the first fragment scanned: 07 is getting on with it");
            Tick(6);
            Assert.AreEqual(OpeningKeys.ScanLander, _flow.Shown, "then a full stall with nothing done brings it back");
            Assert.AreEqual(NextLineReason.Reminder, _flow.ShownReason);
        }

        [Test]
        public void DrivingTowardTheLinesPlace_IsProgress_NoReminderOnTheWay()
        {
            var lander = new Vector3(0f, 0f, 200f);
            _game.Places[OpeningKeys.DriveToLander] = lander;
            _flow.Tick(1f, Vector3.zero);
            for (int second = 1; second <= Stall * 3; second++)
            {
                _flow.Tick(1f, new Vector3(0f, 0f, second));
            }

            Assert.IsEmpty(_lines, "closing on the lander at 1 m/s for three stalls: on its way, never reminded");
            for (int second = 0; second < Stall; second++)
            {
                _flow.Tick(1f, new Vector3(0f, 0f, Stall * 3));
            }

            Assert.AreEqual(OpeningKeys.DriveToLander, _flow.Shown, "stopped short with no progress: reminded");
        }

        [Test]
        public void Resting07OnACharger_SaysTheStepsLineOnceMore_OncePerStep()
        {
            _flow.Tick(1f);
            _game.ReachedLander = true;
            _flow.Tick(1f);
            Tick(NextLine);
            Assert.AreEqual(string.Empty, _flow.Shown, "the next line has gone");

            _events.Publish(new ChargingChanged(true, ChargerKind.Mast, Vector3.zero));
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.ScanLander, _flow.Shown, "07 rests on a charger: what now, once more");
            Assert.AreEqual(NextLineReason.Next, _flow.ShownReason);

            Tick(NextLine);
            _events.Publish(new ChargingChanged(false, ChargerKind.Mast, Vector3.zero));
            _events.Publish(new ChargingChanged(true, ChargerKind.Mast, Vector3.zero));
            _flow.Tick(1f);
            Assert.AreEqual(string.Empty, _flow.Shown, "not again for the same step");
        }

        [Test]
        public void TheFitLine_LeavesTheTabChipToTeachIt_ButStillReminds()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Covered.Add(StructureIds.ChargingPad);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
            _game.ReserveSpent = true;
            _game.Known.Add(StructureIds.SolarArray);
            _flow.Tick(1f);
            _game.CarriesIgniter = true;
            _game.CanFitIgniter = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.FitIgniter, _flow.Current);
            Assert.AreEqual(string.Empty, _flow.Shown, "no next line over the Tab chip on 07, which shows the key");
            Tick(Stall);
            Assert.AreEqual(OpeningKeys.FitIgniter, _flow.Shown, "a stalled player still gets the reminder");
            Assert.AreEqual(NextLineReason.Reminder, _flow.ShownReason);
        }

        [Test]
        public void TakingTheIgniter_ClearsTheTakeLine_AtOnce_WhetherItIsANextLineOrAReminder()
        {
            ReachTheArray();
            _flow.Tick(1f);
            _game.CutterSputtered = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.TakeIgniter, _flow.Shown, "the sputter names the rack");
            _game.CarriesIgniter = true;
            _game.CanFitIgniter = true;
            _flow.Tick(1f);
            Assert.AreEqual(string.Empty, _flow.Shown, "taken: the take line goes at once, the Tab chip leads");
            Assert.AreEqual(string.Empty, _lines[_lines.Count - 1], "and the screen is told so");

            _game.CarriesIgniter = false;
            _flow.Tick(1f);
            Tick(NextLine);
            _game.CutterSputtered = true;
            _events.Publish(new StoryPartCollected("part.igniter", Vector3.zero));
            Assert.AreEqual(string.Empty, _flow.Shown, "the take event alone clears the line, whatever the hold says");
            Tick(NextLine + Stall);
            Assert.AreEqual(OpeningKeys.TakeIgniter, _flow.Shown, "dropped back: the take line, reminded");
            _game.CarriesIgniter = true;
            _flow.Tick(1f);
            Assert.AreEqual(string.Empty, _flow.Shown, "a reminder goes on the pickup too");
            Assert.AreEqual(string.Empty, _lines[_lines.Count - 1]);
        }

        [Test]
        public void TheNightfall_PlaysOnThePad_WithNothingSaid()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
            _game.ReserveSpent = true;
            _game.Known.Add(StructureIds.SolarArray);
            _game.IgniterFitted = true;
            _game.PowerOn = true;
            _game.DuskBegun = true;
            _game.NightReady = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.DockForNight, _flow.Current);
            _events.Publish(new RoverDockChanged(true, Vector3.zero, Quaternion.identity));
            _events.Publish(new ChargingChanged(true, ChargerKind.Pad, Vector3.zero));
            _flow.Tick(1f);
            _flow.Tick(1f);
            Assert.IsTrue(_flow.HasReached(OpeningMilestone.NightReady), "docked: the nightfall begins");
            Assert.AreEqual(string.Empty, _flow.Current, "the night falls with nothing said");
            Tick(Stall * 10);
            Assert.IsEmpty(_lines, "nothing to do but watch: no line, no reminder");
        }

        [Test]
        public void Progress_StartsTheStallAgain_AndTheNewLineGetsItsOwnReminders()
        {
            _flow.Tick(1f);
            Tick(Stall - 1);
            _game.ReachedLander = true;
            _flow.Tick(1f);
            CollectionAssert.AreEqual(new[] { OpeningKeys.ScanLander }, _lines, "the next line, no reminder");
            Tick(NextLine + Stall);
            CollectionAssert.AreEqual(new[] { OpeningKeys.ScanLander, string.Empty, OpeningKeys.ScanLander }, _lines,
                "the new line's own reminder, counted from when its next line went");
            Assert.AreEqual(NextLineReason.Reminder, _changes[2].Reason);
        }

        [Test]
        public void ScanningThePowerModule_PublishesTheHookLineWithItsKeyAndHitch_WhichHoldsUntilHooked()
        {
            var hitch = new Vector3(14f, 0f, 22f);
            _game.Places[OpeningKeys.HookModule] = hitch;
            _game.OutOfReach.Add(OpeningKeys.HookModule);
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Known.Add(StructureIds.SolarArray);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
            _game.IgniterFitted = true;
            _game.ReserveSpent = true;
            _game.PowerOn = true;
            _game.DuskBegun = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.ScanModule, _flow.Current);

            _game.Known.Add(StructureIds.BatteryBank);
            _flow.Tick(1f);
            NextStepChanged hook = _changes[_changes.Count - 1];
            Assert.AreEqual(OpeningKeys.HookModule, hook.LineKey);
            Assert.AreEqual(NextLineReason.Next, hook.Reason);
            Assert.AreEqual(RoverAction.Tether, hook.Key);
            Assert.IsFalse(hook.ShowsKey, "07 is far from the hitch: RMB would hook nothing, so the words and the " +
                                          "module's mark alone; the hitch's own chip teaches RMB on arrival");
            Assert.IsTrue(hook.HasTarget);
            Assert.AreEqual(hitch, hook.TargetPosition);

            Tick(NextLine + 10);
            Assert.AreEqual(OpeningKeys.HookModule, _flow.Shown,
                "holds on screen while 07 cuts the cable reel and backs up to the hitch");

            _game.ModuleHooked = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.TowModuleHome, _flow.Shown);
            Assert.AreEqual(NextLineReason.Next, _changes[_changes.Count - 1].Reason);
            Tick(NextLine);
            Assert.AreEqual(string.Empty, _flow.Shown, "the tow-home line clears after its normal duration");

            _game.ModuleHooked = false;
            _game.ModuleHome = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.BuildBatteryBank, _flow.Shown);
        }

        [Test]
        public void TheModuleCaughtPastTheSlack_SaysBackUpToFreeTheCable_AndOnceFreeTheTowSaysNothingMore()
        {
            _game.ReachedLander = true;
            _game.Known.Add(StructureIds.ChargingPad);
            _game.Known.Add(StructureIds.SolarArray);
            _game.BuiltIds.Add(StructureIds.ChargingPad);
            _game.FirstCharge = true;
            _game.IgniterFitted = true;
            _game.ReserveSpent = true;
            _game.PowerOn = true;
            _game.DuskBegun = true;
            _game.Known.Add(StructureIds.BatteryBank);
            _game.ModuleHooked = true;
            _flow.Tick(1f);
            Tick(NextLine);
            Assert.AreEqual(string.Empty, _flow.Shown, "towing home, the tow line has had its say");

            _game.ModuleSnagged = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.FreeCable, _flow.Shown, "caught past the slack: back up to free the cable");
            Assert.AreEqual(NextLineReason.Next, _changes[_changes.Count - 1].Reason);
            Assert.AreEqual(default(RoverAction), _changes[_changes.Count - 1].Key, "plain words, no key chip");
            Tick(NextLine * 3);
            Assert.AreEqual(OpeningKeys.FreeCable, _flow.Shown, "it holds while the module is still caught");

            int lines = _lines.Count;
            _game.ModuleSnagged = false;
            _flow.Tick(1f);
            Assert.AreEqual(string.Empty, _flow.Shown, "freed: the line goes");
            Tick(Again + Stall);
            Assert.IsFalse(_lines.GetRange(lines, _lines.Count - lines).Contains(OpeningKeys.TowModuleHome),
                "and the tow-home line is never said again on this tow");

            _game.ModuleHooked = false;
            _game.ModuleHome = true;
            _flow.Tick(1f);
            Assert.AreEqual(OpeningKeys.BuildBatteryBank, _flow.Shown, "home: the next step speaks as usual");
        }

        private void Tick(int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                _flow.Tick(1f);
            }
        }

        private string FirstStepKey()
        {
            _flow.Tick();
            return _flow.Current;
        }

        private void Expect(string key, string why = "the next step")
        {
            _flow.Tick();
            Assert.AreEqual(key, _flow.Current, why);
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
            public bool DuskBegun { get; set; }
            public bool NightReady { get; set; }
            public bool NightFallen { get; set; }
            public bool ModuleHooked { get; set; }
            public bool ModuleSnagged { get; set; }
            public bool ModuleAtHome { get; set; }
            public bool ModuleHome { get; set; }
            public bool AnswerDone { get; set; }
            public bool AnswerStarted { get; set; }
            public bool AtRelay { get; set; }
            public bool RelayRelit { get; set; }
            public bool RelayTripCovered { get; set; } = true;
            public readonly Dictionary<string, Vector3> Places = new Dictionary<string, Vector3>();
            public readonly HashSet<string> OutOfReach = new HashSet<string>();

            public bool CanAct(string lineKey)
            {
                return !OutOfReach.Contains(lineKey);
            }

            public bool TryGetPlace(string lineKey, out string placeId, out Vector3 position)
            {
                bool has = Places.TryGetValue(lineKey, out position);
                placeId = has ? "place." + lineKey : string.Empty;
                return has;
            }

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

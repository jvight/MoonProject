using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// 07's battery rules (docs/features/M3-20 §2-§3, VISION ruling 15) on <see cref="BatteryModel"/> with the default
    /// <see cref="BatteryTuning"/> but M3-20's reference numbers (120 m a cell, a full battery at the start), so the
    /// rules read in whole cells: every drain, the drums, the reserve and the crawl, the trickle, the chargers, the
    /// warnings and the save. The opening's balance (opening-v2 §10) is checked on the shipping defaults.
    /// </summary>
    public sealed class BatteryModelTests
    {
        private const float Dt = 1f / 60f;
        private const float Cruise = 6f;
        private const float Tolerance = 2e-3f;
        private const string ReferenceNumbers = "{\"_metresPerCell\":120,\"_startingCharge01\":1}";

        private readonly List<BatteryChanged> _cells = new List<BatteryChanged>();
        private readonly List<LowPowerChanged> _lowPower = new List<LowPowerChanged>();
        private readonly List<ChargingChanged> _charging = new List<ChargingChanged>();
        private readonly List<BatteryWarned> _warnings = new List<BatteryWarned>();
        private readonly List<BatteryArcRevealed> _arcs = new List<BatteryArcRevealed>();
        private BatteryTuning _tuning;
        private EventBus _events;
        private BatteryModel _battery;

        [SetUp]
        public void SetUp()
        {
            _cells.Clear();
            _lowPower.Clear();
            _charging.Clear();
            _warnings.Clear();
            _arcs.Clear();
            _tuning = ScriptableObject.CreateInstance<BatteryTuning>();
            JsonUtility.FromJsonOverwrite(ReferenceNumbers, _tuning);
            _events = new EventBus();
            _events.Subscribe<BatteryChanged>(_cells.Add);
            _events.Subscribe<LowPowerChanged>(_lowPower.Add);
            _events.Subscribe<ChargingChanged>(_charging.Add);
            _events.Subscribe<BatteryWarned>(_warnings.Add);
            _events.Subscribe<BatteryArcRevealed>(_arcs.Add);
            _battery = new BatteryModel(_tuning, _events);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void A_NewGame_StartsWithFourFullCells_AndDrivesFree()
        {
            Assert.AreEqual(4, _battery.Cells);
            Assert.AreEqual(4f, _battery.Charge);
            Assert.AreEqual(1f, _battery.Charge01);
            Assert.AreEqual(1f, _battery.DriveCap01);
            Assert.IsFalse(_battery.IsLowPower);
            Assert.IsTrue(_battery.CanLeap);
            Assert.AreEqual(0f, _battery.Drums01, "no Boost Coils, no drums");
        }

        [Test]
        public void Driving120MetresOnTheFlat_CostsOneCell()
        {
            Drive(120f, 0f);

            Assert.AreEqual(3f, _battery.Charge, Tolerance);
        }

        [Test]
        public void Uphill_CostsHalfAsMuchAgain_AndDownhill_Half()
        {
            Drive(20f, _tuning.FullSlope);
            float before = _battery.Charge;
            Drive(60f, _tuning.FullSlope);
            Assert.AreEqual(0.5f * 1.5f, before - _battery.Charge, Tolerance, "60 m uphill");

            Drive(20f, -_tuning.FullSlope);
            before = _battery.Charge;
            Drive(60f, -_tuning.FullSlope);
            Assert.AreEqual(0.5f * 0.5f, before - _battery.Charge, Tolerance, "60 m downhill");
        }

        [Test]
        public void TheSlopeMultiplier_EasesIn_SoABumpNeverJumpsTheCost()
        {
            Drive(1f, _tuning.FullSlope);

            Assert.Less(4f - _battery.Charge, 1.5f / 120f, "one metre onto a hill costs less than the full uphill rate");
            Assert.Greater(4f - _battery.Charge, 1f / 120f);
        }

        [Test]
        public void Towing_CostsAQuarterMore_AndTheTetherDrawsNoBeam()
        {
            Drive(120f, 0f, towing: true);

            Assert.AreEqual(4f - 1.25f, _battery.Charge, Tolerance);
        }

        [Test]
        public void SixtySecondsOfBeamWork_CostOneCell()
        {
            Work(60f);

            Assert.AreEqual(3f, _battery.Charge, Tolerance);
        }

        [TestCase(1f, 0.25f)]
        [TestCase(0f, 0.05f)]
        [TestCase(0.5f, 0.15f)]
        public void A_Leap_IsPaidAtTheLeap_ByItsStrength(float strength, float cost)
        {
            _battery.PayLeap(strength);

            Assert.AreEqual(4f - cost, _battery.Charge, 1e-5f);
        }

        [Test]
        public void Boosting_DoublesTheDrivingCost()
        {
            Drive(60f, 0f, boosting: true);

            Assert.AreEqual(3f, _battery.Charge, Tolerance);
        }

        [Test]
        public void TheDrums_HoldAQuarterCell_AndPayBoostsAndLeapsFirst()
        {
            Step(new BatteryStep { DrumsFitted = true, OnCharger = true, Charger = ChargerKind.Dock }, 10f);
            Assert.AreEqual(1f, _battery.Drums01, 1e-5f, "a charger fills the drums");
            Assert.AreEqual(_tuning.DrumCells, _battery.Drums, 1e-5f);

            _battery.PayLeap(1f);
            Assert.AreEqual(4f, _battery.Charge, 1e-5f, "the full leap came out of the drums");
            Assert.AreEqual(0f, _battery.Drums01, 1e-5f);

            Step(new BatteryStep { DrumsFitted = true, OnCharger = true, Charger = ChargerKind.Dock }, 10f);
            Drive(30f, 0f, boosting: true, drums: true);
            Assert.AreEqual(4f - 0.25f, _battery.Charge, Tolerance, "30 m of boost cost 0.5 cell, the drums paid 0.25");
        }

        [Test]
        public void RollingDownhillAtSpeed_RefillsTheDrums()
        {
            Step(new BatteryStep { DrumsFitted = true, OnCharger = true, Charger = ChargerKind.Dock }, 10f);
            _battery.PayLeap(1f);

            Drive(25f, -10f, drums: true);

            Assert.AreEqual(1f, _battery.Drums01, 1e-3f, "25 m of downhill rolling refills a quarter cell");
        }

        [Test]
        public void StandingStill_NeverDrains()
        {
            Step(new BatteryStep { Still = true }, 600f);

            Assert.AreEqual(4f, _battery.Charge);
        }

        [Test]
        public void AtEmpty_07Crawls_AtAThirdOfItsSpeed_EasedOverASecondAndAHalf()
        {
            Empty();
            Assert.IsTrue(_battery.IsLowPower);
            Assert.AreEqual(1, _lowPower.Count);
            Assert.IsTrue(_lowPower[0].LowPower);

            Step(new BatteryStep(), 0.5f);
            Assert.Greater(_battery.DriveCap01, 0.5f, "the cap eases down, it never steps");

            Step(new BatteryStep(), 1f);
            Assert.AreEqual(_tuning.CrawlCap, _battery.DriveCap01, 0.04f);
            Assert.AreEqual(0.35f, _tuning.CrawlCap);
        }

        [Test]
        public void TheCrawl_NeverRunsOut()
        {
            Empty();
            Drive(2000f, 0f, towing: true);

            Assert.IsTrue(_battery.IsLowPower);
            Assert.AreEqual(0f, _battery.Charge);
            Assert.AreEqual(1, _lowPower.Count, "the crawl began once and never ended");
        }

        [Test]
        public void A_VerbInProgress_Completes_OnTheReserve_ThenTheCrawlStarts()
        {
            Drive(479f, 0f);
            Work(30f);

            Assert.AreEqual(0f, _battery.Charge);
            Assert.IsFalse(_battery.IsLowPower, "the cut finishes on the reserve");
            Assert.IsTrue(_battery.OnReserve);
            Assert.AreEqual(1f, _battery.DriveCap01);

            Step(new BatteryStep(), Dt);
            Assert.IsTrue(_battery.IsLowPower, "the crawl starts after the verb");
        }

        [Test]
        public void TheLeap_Refuses_WhenTheBatteryCannotPayAFullLeap()
        {
            for (int i = 0; i < 15; i++)
            {
                _battery.PayLeap(1f);
            }

            Assert.AreEqual(0.25f, _battery.Charge);
            Assert.IsTrue(_battery.CanLeap);

            Drive(1f, 0f);
            Assert.IsFalse(_battery.CanLeap);

            Drive(100f, 0f);
            Assert.IsTrue(_battery.IsLowPower);
            Assert.IsFalse(_battery.CanLeap, "never during a crawl");
        }

        [Test]
        public void Tilly_Leads_AfterFiveSecondsOfCrawl_AndTheCrawlRises()
        {
            Empty();
            Step(new BatteryStep { FriendCanLead = true }, 4.9f);
            Assert.IsFalse(_battery.FriendLeads);

            Step(new BatteryStep { FriendCanLead = true }, 3f);
            Assert.IsTrue(_battery.FriendLeads);
            Assert.AreEqual(0.55f, _battery.DriveCap01, 0.01f);
        }

        [Test]
        public void WithoutTillyHome_TheCrawlStaysAtItsOwnPace()
        {
            Empty();
            Step(new BatteryStep(), 30f);

            Assert.IsFalse(_battery.FriendLeads);
            Assert.AreEqual(0.35f, _battery.DriveCap01, 0.01f);
        }

        [Test]
        public void Scanning_CostsItsSeconds_AndAWeld_ItsBuildSecondsAtTheWeldRate()
        {
            float before = _battery.Charge;
            Step(new BatteryStep { ScanSeconds = Dt }, 10f);
            Assert.AreEqual(10f / _tuning.ScanSecondsPerCell, before - _battery.Charge, Tolerance,
                "every second of an open lens costs (opening-v2 §10)");

            before = _battery.Charge;
            Step(new BatteryStep { WeldSeconds = Dt }, 3f);
            Assert.AreEqual(_tuning.WeldCells(3f), before - _battery.Charge, Tolerance,
                "the charging pad's 3 s weld costs its build seconds at the weld rate");
            Assert.AreEqual(3f / _tuning.WeldSecondsPerCell, _tuning.WeldCells(3f), 1e-6f);
        }

        [Test]
        public void TheOpening_FromTheHalfFullWake_LeavesAQuarterToAThird_AfterThePadWeld()
        {
            var shipping = ScriptableObject.CreateInstance<BatteryTuning>();
            var fresh = new BatteryModel(shipping, _events);
            Assert.AreEqual(0.5f, fresh.Charge01, 1e-5f, "07 wakes half full");
            Drive(fresh, 200f);
            Step(fresh, new BatteryStep { ScanSeconds = Dt }, 5f);
            Step(fresh, new BatteryStep { WeldSeconds = Dt }, 3f);
            Assert.That(fresh.Charge01, Is.InRange(0.25f, 0.3f),
                "the drive to the lander, its two scans and the pad weld (opening-v2 §10 balance)");
            Assert.Less(fresh.Charge01, shipping.LowThreshold, "the Low warning has landed by the pad");
            UnityEngine.Object.DestroyImmediate(shipping);
        }

        [Test]
        public void WithTheSolarWingJammed_AStill07NeverTrickles()
        {
            Empty();
            Step(new BatteryStep { Still = true, SolarFitted = false }, 600f);
            Assert.AreEqual(0f, _battery.Charge, 1e-5f, "only chargers refill 07 (opening-v2 §10)");
        }

        [Test]
        public void TheSolarCell_TricklesAStill07BackUpToOneCell_NeverMore()
        {
            Empty();
            Step(new BatteryStep { Still = true, SolarFitted = true }, 3f);
            Assert.AreEqual(0f, _battery.Charge, 1e-3f, "07 must stand still three seconds first");

            Step(new BatteryStep { Still = true, SolarFitted = true }, 50f);
            Assert.AreEqual(50f / 180f, _battery.Charge, 0.01f, "a cell per 180 s");
            Assert.IsFalse(_battery.IsLowPower, "the trickle lifted it past the exit margin");

            Step(new BatteryStep { Still = true, SolarFitted = true }, 600f);
            Assert.AreEqual(1f, _battery.Charge, 1e-5f);
        }

        [Test]
        public void TheTrickle_NeedsTheSolarCell_AndNeverTopsUpAboveOneCell()
        {
            Empty();
            Step(new BatteryStep { Still = true }, 600f);
            Assert.AreEqual(0f, _battery.Charge, "no gift, no trickle");

            var fresh = new BatteryModel(_tuning, _events);
            for (int i = 0; i < 4; i++)
            {
                fresh.PayLeap(1f);
            }

            fresh.Step(new BatteryStep { Still = true, SolarFitted = true }, 600f);
            Assert.AreEqual(3f, fresh.Charge, 1e-5f, "above one cell the trickle does nothing");
        }

        [Test]
        public void TheCrawl_EndsOnlyPastTheExitMargin_SoItNeverFlickers()
        {
            Empty();
            Step(new BatteryStep { Still = true, SolarFitted = true }, 3f + 40f);

            Assert.Less(_battery.Charge, 0.25f);
            Assert.IsTrue(_battery.IsLowPower);
        }

        [TestCase(ChargerKind.Dock, 4f)]
        [TestCase(ChargerKind.Mast, 5f)]
        public void A_Charger_FillsCellByCell_AtItsSecondsPerCell_OneRisingNoteEach(ChargerKind charger,
            float secondsPerCell)
        {
            Empty();
            _cells.Clear();
            _lowPower.Clear();
            var position = new Vector3(3f, 0f, 4f);
            var step = new BatteryStep { OnCharger = true, Charger = charger, ChargerPosition = position };
            float hold = _tuning.ChargeStopHold;

            Step(step, hold + secondsPerCell - 0.1f);
            Assert.AreEqual(0, _battery.FullCells, "the first cell is not full yet");
            Assert.IsTrue(_battery.IsCharging);

            Step(step, 0.2f);
            Assert.AreEqual(1, _battery.FullCells, "one cell per its seconds");

            Step(step, 3f * secondsPerCell - 0.2f);
            Assert.Less(_battery.Charge, 4f);
            Assert.IsTrue(_battery.IsCharging);

            Step(step, 0.2f);
            Assert.AreEqual(4f, _battery.Charge, 1e-5f);
            Assert.IsFalse(_battery.IsCharging, "full: charging ends on the charger");
            Assert.AreEqual(2, _charging.Count);
            Assert.IsTrue(_charging[0].Charging);
            Assert.AreEqual(charger, _charging[0].Charger);
            Assert.AreEqual(position, _charging[0].Position);
            Assert.IsFalse(_charging[1].Charging);
            Assert.AreEqual(4, _cells.Count, "one rising note per cell");
            for (int i = 0; i < _cells.Count; i++)
            {
                Assert.AreEqual(i + 1, _cells[i].FullCells);
                Assert.IsTrue(_cells[i].Rising);
            }

            Assert.AreEqual(1, _lowPower.Count);
            Assert.IsFalse(_lowPower[0].LowPower, "the charger ended the crawl");
        }

        [Test]
        public void TheChargerDefaults_AreFourSecondsADockCell_FiveAMastCell_AndAHalfSecondStop()
        {
            Assert.AreEqual(4f, _tuning.DockSecondsPerCell);
            Assert.AreEqual(5f, _tuning.MastSecondsPerCell);
            Assert.AreEqual(0.3f, _tuning.ChargeStopSpeed);
            Assert.AreEqual(0.5f, _tuning.ChargeStopHold);
        }

        [Test]
        public void MoreCells_TakeLonger_SixEmptyCellsFillInTwentyFourSecondsAtTheDock()
        {
            _battery.GrowTo(6);
            Empty();
            var step = new BatteryStep { OnCharger = true, Charger = ChargerKind.Dock };

            Step(step, _tuning.ChargeStopHold + 24f - 0.1f);
            Assert.Less(_battery.Charge, 6f);
            Assert.IsTrue(_battery.IsCharging);

            Step(step, 0.2f);
            Assert.AreEqual(6f, _battery.Charge, 1e-5f);
        }

        [Test]
        public void Charging_StartsOnlyOnceStopped_AtOrUnderPointThreeMetresPerSecond_ForHalfASecond()
        {
            Drive(240f, 0f);
            Step(new BatteryStep { OnCharger = true, Charger = ChargerKind.Dock, Speed = 0.5f }, 5f);
            Assert.IsFalse(_battery.IsCharging, "rolling over the dock is no stop");
            Assert.AreEqual(0, _charging.Count);

            var stopped = new BatteryStep { OnCharger = true, Charger = ChargerKind.Dock, Speed = 0.3f };
            Step(stopped, 0.4f);
            Step(new BatteryStep { OnCharger = true, Charger = ChargerKind.Dock, Speed = 0.31f }, Dt);
            Step(stopped, 0.4f);
            Assert.IsFalse(_battery.IsCharging, "a moment over the stop speed starts the wait again");
            Assert.AreEqual(2f, _battery.Charge, Tolerance);

            Step(stopped, 0.15f);
            Assert.IsTrue(_battery.IsCharging, "half a second stopped");
            Assert.AreEqual(1, _charging.Count);
            Assert.IsTrue(_charging[0].Charging);
        }

        [Test]
        public void DrivingOff_StopsTheCharge()
        {
            Drive(240f, 0f);
            Step(new BatteryStep { OnCharger = true, Charger = ChargerKind.Mast }, 1f);
            Step(new BatteryStep(), Dt);

            Assert.IsFalse(_battery.IsCharging);
            Assert.AreEqual(2, _charging.Count);
            Assert.IsFalse(_charging[1].Charging);
            Assert.AreEqual(ChargerKind.Mast, _charging[1].Charger);
        }

        [Test]
        public void Leaving_KeepsThePartOfACellAlreadyFilled_AndTheNextChargeGoesOnFromIt()
        {
            Drive(240f, 0f);
            var dock = new BatteryStep { OnCharger = true, Charger = ChargerKind.Dock };
            Step(dock, _tuning.ChargeStopHold + 2f);
            float kept = _battery.Charge;
            Assert.AreEqual(2.5f, kept, 0.02f, "half a cell in 2 s");
            Assert.AreEqual(1, _charging.Count);

            Step(new BatteryStep { Still = true }, 30f);
            Assert.IsFalse(_battery.IsCharging);
            Assert.AreEqual(kept, _battery.Charge, "nothing resets the half cell");

            Step(dock, _tuning.ChargeStopHold + 2.5f);
            Assert.AreEqual(3.125f, _battery.Charge, 0.02f, "the next charge goes on from the half cell");
            Assert.AreEqual(3, _battery.FullCells);
        }

        [Test]
        public void TheCellEvent_FiresOnCellBoundariesOnly()
        {
            Drive(1f, 0f);
            Assert.AreEqual(1, _cells.Count, "the fourth cell began to be drawn");
            Assert.AreEqual(3, _cells[0].FullCells);
            Assert.AreEqual(4, _cells[0].Cells);
            Assert.IsFalse(_cells[0].Rising);

            Drive(100f, 0f);
            Assert.AreEqual(1, _cells.Count, "nothing while the cell drains");

            Drive(20f, 0f);
            Assert.AreEqual(2, _cells.Count);
            Assert.AreEqual(2, _cells[1].FullCells);
        }

        [Test]
        public void TheWarnings_FireOncePerDischarge_AtThirtyAndFifteenPercent()
        {
            var lit = new Vector3(30f, 0f, 40f);
            Drive(335f, 0f, lit: lit);
            Assert.AreEqual(0, _warnings.Count, "1.2 cells left is 30%: not below it yet");

            Drive(2f, 0f, lit: lit);
            Assert.AreEqual(1, _warnings.Count);
            Assert.AreEqual(BatteryWarning.Low, _warnings[0].Warning);
            Assert.AreEqual(lit, _warnings[0].NearestLitCharger);

            Drive(75f, 0f, lit: lit);
            Assert.AreEqual(2, _warnings.Count);
            Assert.AreEqual(BatteryWarning.VeryLow, _warnings[1].Warning);

            Step(new BatteryStep { Still = true, SolarFitted = true }, 120f);
            Drive(50f, 0f);
            Assert.AreEqual(2, _warnings.Count, "the trickle re-arms nothing");
        }

        [Test]
        public void Charging_ReArmsTheWarnings()
        {
            var lit = new Vector3(30f, 0f, 40f);
            Drive(420f, 0f, lit: lit);
            Assert.AreEqual(2, _warnings.Count);

            Step(new BatteryStep { OnCharger = true, Charger = ChargerKind.Dock }, 20f);
            Drive(420f, 0f, lit: lit);

            Assert.AreEqual(4, _warnings.Count);
        }

        [TestCase(200f, 1)]
        [TestCase(170f, 0)]
        public void TheLowWarning_ComesEarly_WhenTheRangeLeftIsUnderOneAndAHalfTimesTheWay(float distance,
            int expected)
        {
            Assert.AreEqual(1.5f, _tuning.LowRangeMargin);
            Assert.AreEqual(1.2f, _tuning.LowRouteFactor);
            var lit = new Vector3(distance, 0f, 0f);
            Drive(150f, 0f, lit: lit);

            Assert.AreEqual(expected, _warnings.Count,
                "2.75 cells left is 330 m: under 1.5 x 1.2 x 200 m = 360 m, over 1.5 x 1.2 x 170 m = 306 m");
            if (expected > 0)
            {
                Assert.AreEqual(BatteryWarning.Low, _warnings[0].Warning, "at about 70%: the distance spoke first");
                Assert.AreEqual(lit, _warnings[0].NearestLitCharger);
            }
        }

        [Test]
        public void TheLowWarning_NeverPlaysWithinFortyMetresOfALitCharger_AndWaitsUntil07IsFarther()
        {
            Assert.AreEqual(40f, _tuning.LowChargerClearance);
            Drive(400f, 0f, lit: new Vector3(39f, 0f, 0f));
            Assert.AreEqual(0, _warnings.Count, "17% beside a lit mast: no Low warning");

            Drive(1f, 0f, lit: new Vector3(41f, 0f, 0f));
            Assert.AreEqual(1, _warnings.Count);
            Assert.AreEqual(BatteryWarning.Low, _warnings[0].Warning);
        }

        [Test]
        public void TheLowWarning_NeverPlaysOnACharger_AndAChargeLeftUnderThirtyPercentKeepsItDue()
        {
            Drive(390f, 0f, lit: new Vector3(20f, 0f, 0f));
            Assert.AreEqual(0, _warnings.Count, "under 30%, but near the lit charger");

            var far = new Vector3(0f, 0f, 300f);
            var charger = new BatteryStep { OnCharger = true, Charger = ChargerKind.Mast, NearestLitCharger = far };
            Step(charger, _tuning.ChargeStopHold + 0.5f);
            Assert.IsTrue(_battery.IsCharging);
            Assert.AreEqual(0, _warnings.Count, "never on a charger or while charging");

            Step(new BatteryStep { NearestLitCharger = far }, Dt);
            Assert.AreEqual(1, _warnings.Count, "left under 30%, the warning is still due");
            Assert.AreEqual(BatteryWarning.Low, _warnings[0].Warning);
        }

        [Test]
        public void TheArc_RevealsItselfOnce_BelowSeventyPercent()
        {
            Drive(140f, 0f);
            Assert.AreEqual(0, _arcs.Count);

            Drive(10f, 0f);
            Step(new BatteryStep { OnCharger = true, Charger = ChargerKind.Dock }, 20f);
            Drive(200f, 0f);

            Assert.AreEqual(1, _arcs.Count);
            Assert.IsFalse(_arcs[0].Restored);
            Assert.IsTrue(_battery.ArcRevealed);
        }

        [Test]
        public void TheLowLine_IsShownTheFirstThreeTimesOnly()
        {
            Assert.IsTrue(_battery.TakeLowLine());
            Assert.IsTrue(_battery.TakeLowLine());
            Assert.IsTrue(_battery.TakeLowLine());
            Assert.IsFalse(_battery.TakeLowLine());
            Assert.AreEqual(3, _battery.LowLinesShown);
        }

        [Test]
        public void CellPackTwo_AddsTwoFullCells_WithARisingNote()
        {
            Drive(120f, 0f);
            _cells.Clear();

            _battery.GrowTo(6);

            Assert.AreEqual(6, _battery.Cells);
            Assert.AreEqual(5f, _battery.Charge, Tolerance);
            Assert.AreEqual(1, _cells.Count);
            Assert.AreEqual(6, _cells[0].Cells);
            Assert.IsTrue(_cells[0].Rising);

            _battery.GrowTo(4);
            Assert.AreEqual(6, _battery.Cells, "a pack never shrinks the battery");
        }

        [Test]
        public void TakingThePackOff_FallsBackTo07sOwnCells_KeepingWhatChargeFits()
        {
            _battery.GrowTo(6);
            _cells.Clear();
            _battery.ShrinkTo(_tuning.StartingCells);
            Assert.AreEqual(_tuning.StartingCells, _battery.Cells);
            Assert.AreEqual(_tuning.StartingCells, _battery.Charge, Tolerance, "full cells stay full, the rest go");
            Assert.AreEqual(1, _cells.Count);
            Assert.IsFalse(_cells[0].Rising);

            _battery.ShrinkTo(6);
            Assert.AreEqual(_tuning.StartingCells, _battery.Cells, "shrinking never grows the battery");
        }

        [Test]
        public void TheSave_RoundTrips_AndALoadedCrawlResumes()
        {
            _battery.GrowTo(6);
            Empty();
            _battery.TakeLowLine();
            BatterySaveData data = _battery.Capture();

            var loaded = new BatteryModel(_tuning, _events);
            loaded.Restore(data);
            _cells.Clear();
            _lowPower.Clear();
            _arcs.Clear();
            loaded.Announce();

            Assert.AreEqual(6, loaded.Cells);
            Assert.AreEqual(0f, loaded.Charge);
            Assert.IsTrue(loaded.IsLowPower);
            Assert.AreEqual(_tuning.CrawlCap, loaded.DriveCap01);
            Assert.AreEqual(1, loaded.LowLinesShown);
            Assert.IsTrue(loaded.ArcRevealed);
            Assert.AreEqual(1, _cells.Count);
            Assert.IsTrue(_cells[0].Restored);
            Assert.AreEqual(1, _lowPower.Count);
            Assert.IsTrue(_lowPower[0].LowPower);
            Assert.IsTrue(_lowPower[0].Restored);
            Assert.AreEqual(1, _arcs.Count);
            Assert.IsTrue(_arcs[0].Restored);

            _warnings.Clear();
            loaded.Step(new BatteryStep(), Dt);
            Assert.AreEqual(0, _warnings.Count, "a loaded low battery does not warn again");
        }

        [Test]
        public void A_SaveWithNoCapacity_LoadsTheStartingCells()
        {
            _battery.Restore(new BatterySaveData { charge = 4f });

            Assert.AreEqual(4, _battery.Cells);
            Assert.AreEqual(4f, _battery.Charge);
            Assert.IsFalse(_battery.IsLowPower);
        }

        [Test]
        public void Stepping_AllocatesNothing()
        {
            var battery = new BatteryModel(_tuning, new EventBus());
            var step = new BatteryStep { GroundDistance = 0.1f, Speed = Cruise, SlopeDegrees = 3f, Towing = true };
            for (int i = 0; i < 100; i++)
            {
                battery.Step(step, Dt);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 6000; i++)
            {
                battery.Step(step, Dt);
                battery.Step(new BatteryStep { OnCharger = true, Charger = ChargerKind.Mast }, Dt);
                battery.PayLeap(0.5f);
            }

            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before, "bytes allocated by 18000 steps");
        }

        /// <summary>Drives <paramref name="metres"/> at cruise on a constant slope, one frame at a time.</summary>
        private void Drive(float metres, float slope, bool towing = false, bool boosting = false, bool drums = false,
            Vector3 lit = default)
        {
            float perFrame = Cruise * Dt;
            int frames = Mathf.RoundToInt(metres / perFrame);
            var step = new BatteryStep
            {
                GroundDistance = perFrame,
                Speed = Cruise,
                SlopeDegrees = slope,
                Towing = towing,
                Boosting = boosting,
                DrumsFitted = drums,
                NearestLitCharger = lit,
            };
            for (int i = 0; i < frames; i++)
            {
                _battery.Step(step, Dt);
            }
        }

        /// <summary>Spends every cell with full leaps (exact quarters), then one frame: the crawl begins.</summary>
        private void Empty()
        {
            for (int i = 0; i < _battery.Cells * 4; i++)
            {
                _battery.PayLeap(1f);
            }

            _battery.Step(new BatteryStep(), Dt);
        }

        /// <summary>Works the beam, standing, for <paramref name="seconds"/>.</summary>
        private void Work(float seconds)
        {
            Step(new BatteryStep { BeamSeconds = Dt }, seconds);
        }

        private static void Drive(BatteryModel battery, float metres)
        {
            float perFrame = Cruise * Dt;
            var step = new BatteryStep { GroundDistance = perFrame, Speed = Cruise };
            Step(battery, step, metres / Cruise);
        }

        private static void Step(BatteryModel battery, BatteryStep step, float seconds)
        {
            int frames = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < frames; i++)
            {
                battery.Step(step, Dt);
            }
        }

        private void Step(BatteryStep step, float seconds)
        {
            int frames = Mathf.RoundToInt(seconds / Dt);
            for (int i = 0; i < frames; i++)
            {
                _battery.Step(step, Dt);
            }
        }
    }
}

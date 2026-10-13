using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay.Lift;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The golden path through the real game, as a regression and a pacing measurement: the built Main scene (on its
    /// own save slot) boots, 07 wakes, and a calm autopilot holds the wheel while the real input actions do the rest.
    /// First-hour v2 §4: drive first to the sleeping tower (power first), then to the supply depot, cut it piece by
    /// piece with the beam until it is picked clean, and pick up the tower's coil its last cut revealed in its heart
    /// (M3-16 P1); on the tower pad 07 hands the coil to the port, then crafts level 1 from the depot's materials
    /// through the UI's hold. The tower's first sweep raises Tilly's pillar; 07 listens when it rests. Then Tilly
    /// (M3-02): gather her parts (the last one dug from the dust), repair her, drive home with her, be greeted, and let
    /// her spot Kenji's garage on the next trip, the tether still asleep. Cut the garage's three seams in one trip; its
    /// port fits the tether ring, then Ro's tape deck dug beside it and held on the tether, and 07 is home with at least
    /// a quarter of its charge. Dig Kenji's duck out of the garage's heart, which holds the Cargo Cradle's
    /// blueprint, and tow it home, where the jammed belly lift asks for its stitch (M3-19): stitch it from beside it,
    /// ride up with the duck beside 07, let the lab's reader decode the Cradle's tube (its hand-off pulse waits at the
    /// sleeping bay) and place the duck on the keepsake shelf: the first memory (step 4), then ride down facing out.
    /// Tilly spots Bell's light across the chasm (P2): drive to the lip, look across with no way over, pick up
    /// Hover-Jump's blueprint tube waiting there (M3-21), then back to the garage, where the transformer now shows.
    /// Find the Rover Bay still asleep (no pad, no prompt, a "not yet" answer on approach), hand in the transformer and
    /// craft tower level 2 to wake it (its plotter draws the Cradle's waiting sheet), ride up for the reader to decode
    /// Hover-Jump's tube, find that holding Jump does nothing yet, craft Hover-Jump in the bay through the same hold
    /// and the Cargo Cradle from its new blueprint, dig the gnome from the garage, lift it into the rack with the
    /// tether button, drive home, ride the lift up and set it down on the keepsake shelf from the rack, and take the
    /// first leap. Then Bell (M3-04/05): drive up the mouth lane, leap the chasm, find her lying at the terminus with
    /// Ro's log cache and her tape, gather her three parts from the alcoves, repair her (the tape slides in, her dial
    /// wakes, she stands), drive home by the one-way exit while she makes her own way, be greeted by her, turn her dial
    /// through every station, and follow her first signal to the basin tape onto her rack. Then the relay network
    /// (M3-06): pick up the mound relay's part, restore it at its foot with materials and watch it come online, pick up
    /// the relay spare on its pad (level 3's part), drive out past the tower's reach and find home still reaching 07
    /// there, then radio-hop home from the mast's pad and back, and rest on the dock: the dock's evening opens, the
    /// places home can see blink seen on the crew's map, and the tower's sweep raises the next pillar (P6). Inside the
    /// canyon 07 drives a line found over drivable ground (the same planner Bell walks by). The base wakes step by step
    /// (M3-16, M3-19 §7): it opens fully dark, power comes back with each tower level, no station is usable before its
    /// step, and the soft "next" line teaches steps 0-5, then speaks only of the recovery targets, never of anything
    /// found by roaming (M3-21): the panel's Power row lights at each tower level and its Link row at the mound mast.
    /// Step 8: with the rest of the basin's salvage stood in, hand the relay spare to the tower and craft level 3,
    /// which wakes the lab's survey chart; ride up, where the map opens by itself and the chart pen inks what 07 has
    /// visited in reach, come away and ride down: 0.5's wake-up, end to end. Every step is timed
    /// (Logs/gameplay-captures/playthrough.md) and captured, with review frames of every M3-05 placement; any error or
    /// exception in the log fails it. Slow: run on demand with --category Playthrough.
    /// </summary>
    [Explicit("Slow end-to-end playthrough of the real Main scene; run on demand with --category Playthrough.")]
    [Category("Playthrough")]
    public sealed class GoldenPathPlaythrough : InputTestFixture
    {
        private const string Tower = "radio_tower";
        private const string HoverJump = "rover.hover_jump";
        private const string CargoCradle = "rover.cargo_cradle";
        private const string CellPack = "rover.cell_pack";
        private const string TillyId = "tilly";
        private const string BellId = "bell";
        private const string AfterDark = "after_dark_1";
        private const string DustAndHoney = "dust_and_honey";
        private const string SlowOrbit = "slow_orbit";

        /// <summary>The run at the chasm starts this far (m) back from the lip on the mouth lane.</summary>
        private const float RunUp = 45f;

        /// <summary>A full charge for the chasm: its charge time and a little more, as a player holds it.</summary>
        private const float ChasmCharge = 1.2f;

        /// <summary>Jump is let go this far (m) before the lip.</summary>
        private const float ReleaseLead = 0.5f;

        /// <summary>The chasm is 18.5 m from the lip to the far face; a crossing lands past it, up on the apron.
        /// </summary>
        private const float ChasmWidth = 18.5f;
        private const float ApronRise = 1f;

        /// <summary>Canyon waypoints count as reached this close (m); the last one is driven to a stop.</summary>
        private const float WaypointArrive = 3f;
        private const float CanyonThrottle = 0.7f;

        /// <summary>Seconds between two turns of Bell's dial (the needle settles in between).</summary>
        private const float DialPause = 1.5f;

        /// <summary>Hold Jump this much longer than the rover's full charge time (s), as a player would.</summary>
        private const float ChargeMargin = 0.25f;

        /// <summary>A full-charge leap rises at least this high (m); the rover spec asks for ~6-8 m.</summary>
        private const float MinLeapApex = 5f;

        /// <summary>
        /// The first leap's spot (m) ahead of the charging dock, in 07's lane on the base pad: open ground, well past
        /// the dock's rest radius, so standing still to charge the leap never docks 07.
        /// </summary>
        private const float LeapSpotAhead = 6f;

        /// <summary>Close enough (m) to ping Tilly's crater, and the stand-off 07 repairs her from.</summary>
        private const float TillyPingDistance = 35f;
        private const float RepairStandOff = 3f;

        /// <summary>07 waits this far (m) short of an undiscovered salvage site for Tilly to spot it.</summary>
        private const float SpotStandOff = 25f;

        /// <summary>Where the review camera stands relative to Tilly (m away from 07, m up).</summary>
        private const float WitnessDistance = 4f;
        private const float WitnessHeight = 1.5f;

        /// <summary>Where the review camera stands to watch the Rover Bay (m out in front of it, m aside, m up).
        /// </summary>
        private const float BayViewDistance = 7.5f;
        private const float BayViewSide = 3f;
        private const float BayViewHeight = 2.6f;
        private const float SparkDelay = 0.3f;

        /// <summary>Where the low review camera watches the coils rise under 07 (m out front, m up, m up the target).
        /// </summary>
        private const float PitViewDistance = 3.6f;
        private const float PitViewHeight = 0.45f;
        private const float PitViewTarget = 0.35f;

        /// <summary>Seconds the bay's arms may take to set a bought piece on 07 (the rover's install moment).</summary>
        private const float BayFitTimeout = 25f;

        /// <summary>07 lines up this far (m) in front of the turntable to drive in; it backs out as far.</summary>
        private const float BayApproach = 6f;

        /// <summary>Close enough (m) to the turntable's centre to stop on it; the gentle throttle in and out.</summary>
        private const float BayParkRadius = 0.6f;
        private const float BayThrottle = 0.4f;

        /// <summary>Where the review camera stands to watch the tower's service port (m out in front, aside, up).
        /// </summary>
        private const float PortViewDistance = 5.5f;
        private const float PortViewSide = 3f;
        private const float PortViewHeight = 2.5f;
        private const float StopSpeed = 0.4f;

        /// <summary>Metres in one frame that can only be a radio-hop's placement, never driving.</summary>
        private const float HopJump = 3f;
        private const float ApproachOffset = 3f;
        private const float RetreatDistance = 16f;

        /// <summary>Seconds 07 may take to creep up on the surfaced relic and latch on.</summary>
        private const float LatchTimeout = 40f;

        /// <summary>07 creeps up on the surfaced relic no closer than this (m).</summary>
        private const float LatchReach = 5f;

        /// <summary>07 stops this far (m) short of the depot's centre on its way in, clear of the wreck.</summary>
        private const float SiteApproach = 18f;

        /// <summary>07 circles a wreck this far (m) outside its footprint, so it never drives through it.</summary>
        private const float RingMargin = 5f;

        /// <summary>Metres wider 07 circles a wreck with a relic in tow, so it trails clear of the wreck.</summary>
        private const float TowRingExtra = 6f;

        /// <summary>Seconds of reeling that take the tether from one end of the winch to the other.</summary>
        private const float ReelSeconds = 3f;

        /// <summary>Degrees between two points 07 drives through when circling a wreck.</summary>
        private const float RingStep = 50f;

        /// <summary>07 lines up this far (m) out from a piece's cut, then eases in to cut it from...</summary>
        private const float CutLineUp = 11f;

        /// <summary>...this far (m), facing it.</summary>
        private const float CutStandOff = 4.5f;

        /// <summary>A cut face looking mostly up or down is approached from the site's centre outward.</summary>
        private const float MinCutFacing = 0.35f;

        /// <summary>Seconds 07 waits, parked in front of a piece, for the aim to pick something.</summary>
        private const float AimSettle = 3f;

        /// <summary>07 digs a relic from this far (m) from the heart, within the beam's reach.</summary>
        private const float DigStandOff = 3.5f;

        /// <summary>Metres nearer the meant relic than any other in its heart that 07 parks to dig.</summary>
        private const float HeartMargin = 0.5f;

        /// <summary>Sphere (m) that must be free of the wreck where 07 parks to dig.</summary>
        private const float ParkClearance = 1.6f;

        /// <summary>07 parks this far (m) in front of a relay mast's junction box to restore it.</summary>
        private const float RelayFoot = 3f;

        /// <summary>07 first swings this far (m) to the side of the mast, clear of it and its guy wires.</summary>
        private const float RelayAside = 12f;

        /// <summary>How far past the mound relay (m, away from home) 07 drives to find home still in reach.</summary>
        private static readonly Vector2 PastTheMast = new Vector2(55f, 95f);

        /// <summary>07 lines up this far (m) in front of the charging dock, then stops this close to its anchor.
        /// </summary>
        private const float DockApproach = 5f;
        private const float DockParkRadius = 0.5f;

        /// <summary>Close enough (m) to a hop pad's centre to park on it.</summary>
        private const float PadArrive = 1f;

        /// <summary>
        /// A drive whose line passes this close (m) to the lander's centre goes round it, by a point this far out.
        /// </summary>
        private const float LanderKeepOut = 9f;

        /// <summary>
        /// A long tow reels its relic in this far (m) out from home, on open ground, so it follows 07's own line past
        /// the Rover Bay and the lander instead of swinging wide into them.
        /// </summary>
        private const float TowReelIn = 25f;

        /// <summary>Home by radio-hop, 07 rolls this far (m) out from the dock, away from the tower, to look out.</summary>
        private const float LookOutDistance = 10f;
        private const float LanderDetour = 13f;

        /// <summary>Height (m) over 07 of the review frame taken when a wait times out.</summary>
        private const float StuckReviewHeight = 9f;

        /// <summary>Close enough (m) to the base pad centre to turn toward the shelf or the tower.</summary>
        private const float PadArrival = 5f;

        /// <summary>
        /// Seconds the golden path may take: steps 0-8 end to end measured 1461 s on 2026-10-12 at the battery's spec
        /// tuning (M3-20's charger-to-charger route: docks between trips, the garage's cuts over two trips, the mound,
        /// mouth and terminus masts and their rings, Cell Pack II, the radio-hops), with the same ~8% margin the path
        /// has always had.
        /// </summary>
        private const float TotalBudget = 1580f;

        /// <summary>
        /// NUnit's hard stop: the budget plus the scene boot and teardown, so the budget's own assertion (with its
        /// per-step report) fails first, never a bare timeout (the tower's online moments and pickups on E made the path
        /// longer in 0.5.3).
        /// </summary>
        private const int TestTimeoutMs = 1800000;

        /// <summary>Close enough (m) to a part resting on a mast's pad to draw it in.</summary>
        private const float PartArrive = 1.5f;

        /// <summary>
        /// 0.5.5 §5.1: a small piece, press to crack (s): the brace, one stage and the held breath (1.07 s), then
        /// 1.2 s of cut.
        /// </summary>
        private const float FirstCut = 2.3f;

        /// <summary>
        /// 0.5.5 §5.1: nothing in the first half hour takes longer, press to crack (s): a fresh medium piece's two
        /// stages (1.52 s of charge) and 2.5 s of cut.
        /// </summary>
        private const float FirstHalfHourCut = 4.1f;

        /// <summary>The first half hour of play (s).</summary>
        private const float FirstHalfHour = 1800f;

        /// <summary>Room for 07 easing to a stop as the beam asks it to, before the charge starts (s).</summary>
        private const float EaseToStop = 0.6f;

        /// <summary>07 stops this far (m) back from the chasm's lip, well inside where it looks across.</summary>
        private const float LipStandOff = 8f;

        private const int CoilPart = 0;
        private const int TransformerPart = 1;
        private const int SparePart = 2;
        private const string Duck = "rubber_duck";

        /// <summary>07 lines up this far (m) out from the belly lift, toward the dock, then drives onto it.</summary>
        private const float LiftLineUp = 5f;

        /// <summary>Close enough (m) to the platform's ride spot to stop on it.</summary>
        private const float BoardRadius = 0.5f;

        /// <summary>Seconds the lift may take between two of its phases.</summary>
        private const float LiftPhaseTimeout = 40f;

        /// <summary>Seconds the lab's turntable may take to come to rest at the station that wants what 07 carries.
        /// </summary>
        private const float LabTurnTimeout = 6f;

        /// <summary>Seconds the reader may take to feed a tube in and decode it.</summary>
        private const float ReaderTimeout = 12f;

        /// <summary>Seconds 07 rests on the map while the chart pen inks before coming away.</summary>
        private const float MapSeconds = 4f;

        /// <summary>Seconds the look at the tech board settles (the browse camera's ease, the labels' fade).</summary>
        private const float BoardSeconds = 2.5f;

        /// <summary>07 faces out toward the dock after a ride down: its heading within this of the way out.</summary>
        private const float FacesOut = 0.9f;

        private const float KeySeconds = 0.25f;
        /// <summary>Ro's tape deck: the relic Kenji's port fits on 07's flank (first-hour v2 §3).</summary>
        private const string Deck = "cassette_player";

        /// <summary>The keepsake carried home in the Cargo Cradle once it is built.</summary>
        private const string Gnome = "garden_gnome";

        /// <summary>The share of the battery 07 must still have at the dock after the garage's one trip (§4).</summary>
        private const float OneTripArrival = 0.25f;

        /// <summary>Metres above the ground the straight way to a line-up is checked for anything solid (wreck, prop or
        /// a rise of the ground).</summary>
        private const float WayCheckHeight = 1.2f;

        /// <summary>Half 07's width and a margin: the straight way must be this wide.</summary>
        private const float WayCheckRadius = 0.9f;

        /// <summary>Metres from the tower's pad that count as having driven to the tower.</summary>
        private const float TowerVisit = 5f;

        /// <summary>Metres from the port pad's middle 07 parks within.</summary>
        private const float PortPark = 1f;

        /// <summary>Metres 07 backs out from the port's pad before it turns, clear of the garage's walls.</summary>
        private const float PortBackOut = 5f;

        /// <summary>Tether releases before the tow home began (Kenji's port took the tape deck off the tether).</summary>
        private int _releasesBeforeTow;

        private static readonly string ReportPath = Path.Combine(GameplayFixture.CaptureFolder, "playthrough.md");

        private readonly List<(string Step, float Seconds, string Notes, Vector3 Battery)> _steps =
            new List<(string, float, string, Vector3)>();

        /// <summary>The range lessons' charges (docs/features/M3-20 §6, §12): where, the charge, the target.</summary>
        private readonly List<(string Where, float Charge, Vector2 Target)> _lessons =
            new List<(string, float, Vector2)>();

        private readonly List<string> _problems = new List<string>();
        private int _warnings;

        private Keyboard _keyboard;
        private Mouse _mouse;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private Autopilot _pilot;
        private EventRecorder _events;
        private IDisposable _awoke;
        private IDisposable _crawled;
        private IDisposable _lines;
        private readonly List<string> _tierTwoLines = new List<string>();
        private int _crawls;
        private float _stepMetres;
        private float _stepBeam;
        private float _stepLowest;
        private int _cuts;
        private float _awokeAt = -1f;
        private float _firstCut = -1f;
        private float _slowestEarlyCut;
        private float _stepStart;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            Application.logMessageReceived += OnLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _rover?.SetDriveSource(null);
            _events?.Dispose();
            _awoke?.Dispose();
            _crawled?.Dispose();
            _lines?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(TestTimeoutMs)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator FreshGame_FirstRelicHome_AndTheTowerAwake()
        {
            float started = Time.time;
            yield return Boot();
            yield return WakeUp();
            AssertAsleep();
            yield return ExpectStep(ProgressionKeys.NextWake, "07 has just woken on its dock: power first");
            yield return DriveToTheTower();
            yield return ExpectStep(ProgressionKeys.NextSalvage, "the tower says what it needs; the depot has some");

            SalvageSite depot = _gameplay.Salvage.Find(WorldAnchorIds.SitePrefix + "depot");
            Assert.IsNotNull(depot, "the supply depot stands near home");
            Assert.IsEmpty(depot.Relics, "the depot's heart holds the tower's coil, not a memory (P1)");
            yield return DriveToSite(depot);
            Assert.IsFalse(depot.HeartRevealed, "the depot's heart lies under its pieces (P10)");
            Assert.IsFalse(_gameplay.TowerPartField.IsResting(CoilPart), "no coil before the heart is open");
            yield return SalvageTheDepot(depot);
            Assert.IsTrue(depot.HeartRevealed, "the last cut reveals it");
            yield return ExpectStep(ProgressionKeys.NextTowerPart, "the coil glows in the open heart: bring it to the tower");
            Assert.IsTrue(_gameplay.Materials.Has(_gameplay.Upgrades.Find(Tower).Levels[0].Recipe),
                "the depot's materials cover level 1...");
            Assert.IsTrue(_gameplay.TowerParts.NeedsPart(Tower, 1), "...but power is earned by a find (P1)");
            yield return PickUpPartInTheHeart(depot, CoilPart);
            Assert.IsFalse(depot.AnswersSonar, "picked clean and its coil taken: the depot falls silent");
            yield return BuyTowerLevel(1);
            yield return ExpectStep(ProgressionKeys.NextSignal, "home is powered: the tower hears Tilly");
            yield return TheSweepRaisesTillysPillar();
            yield return ChargeAtTheDock("before Tilly's crater");
            yield return FindTilly();
            yield return GatherTillyParts();
            yield return RepairTilly();
            yield return DriveHomeWithTilly();
            Lesson("home after Tilly's parts", 0.2f, 0.35f);
            yield return ChargeAtTheDock("the dock charges");
            yield return ExpectStep(ProgressionKeys.NextGarage, "Tilly is home: Kenji's garage comes next");
            AssertTetherAsleep();
            SalvageSite garage = _gameplay.Salvage.Find(WorldAnchorIds.SitePrefix + "garage");
            yield return TillySpotsOnTheNextTrip(garage);
            Relic duck = RelicIn(garage, Duck);
            Relic deck = RelicIn(garage, Deck);
            Relic gnome = RelicIn(garage, Gnome);
            yield return SalvageTheGarage(garage, int.MaxValue, "the garage's three seams, one trip");
            yield return ExpectStep(ProgressionKeys.NextPort, "the last cut opened Kenji's port");
            yield return FitTheTetherRing();
            yield return Excavate(garage, deck);
            yield return LatchTether(deck);
            yield return FitTheTapeDeck();
            Lesson("the garage in one trip", 0.25f, 0.4f);
            yield return ChargeAtTheDock("home from the garage in one trip", OneTripArrival);
            yield return Excavate(garage, duck);
            Assert.IsTrue(LogFound("kenji_1"), "Kenji's log tin by his bench opens while 07 digs");
            Assert.IsTrue(_gameplay.Blueprints.IsCarrying, "the garage's heart held the Cradle's blueprint tube");
            Assert.IsFalse(_gameplay.Blueprints.IsPinned(CargoCradle), "it rides home to the lab's reader first");
            Assert.IsFalse(_gameplay.TowerPartField.IsResting(TransformerPart), "the transformer waits for the chasm");
            Assert.LessOrEqual(_gameplay.Lift.Phase, LiftPhase.Called,
                "home's power woke the belly lift jammed; it still waits for its repair");
            yield return LatchTether(duck);
            yield return TowHome(garage, duck);
            yield return StitchTheLift();
            yield return RideUpWithTheFirstFinds(duck);
            yield return ChargeAtTheDock("before the mound relay");
            yield return ExpectStep(ProgressionKeys.NextChasm, "the first memory is home: Tilly sees Bell's light");
            AssertNotYetPastTheChasm();
            yield return StandInSalvage();
            Assert.AreEqual(0, _gameplay.Recovery.LitLamps(RecoveryTarget.Link), "no chain mast is lit yet");
            yield return RestoreTheMoundRelay();
            yield return Until(() => _gameplay.Recovery.LitLamps(RecoveryTarget.Link) == 1, 2f,
                "the mound mast lights the Link row's first lamp: Link begins");
            Assert.AreEqual(TechNodeState.Built, _gameplay.Tech.StateOf(TechNodeId.RelayNetwork));
            yield return ChargeOnTheRing(0);
            yield return ToTheRunUp();
            yield return LookAcrossTheChasm();
            Lesson("the chasm lip", 0.3f, 0.45f);
            yield return PickUpTheLipTube();
            yield return ExpectStep(ProgressionKeys.NextBayWake, "the chasm is seen: the bay waits for the tower");
            yield return RestoreTheMouthRelay();
            yield return ChargeOnTheRing(2);
            yield return HopFrom(WorldAnchorIds.RelayPrefix + 2, StationReach.HomeId);
            yield return RideUpToDecodeTheTubes();
            yield return ChargeAtTheDock("home from the mouth relay");
            yield return BackToTheGarageForTheTransformer(garage);
            yield return TheBaySleepsUntilTheTowerWakesIt();
            yield return BuyTowerLevel(2);
            yield return ExpectStep(ProgressionKeys.NextBay, "the bay has power: the turntable is waiting");
            yield return ChargeAtTheDock("home with the transformer");
            yield return BuyHoverJumpAtTheBench();
            yield return ExpectStep(ProgressionKeys.NextPowerPart, "a kit is fitted: only the targets speak now");
            yield return CraftTheCargoCradle();
            yield return CraftTheCellPack();
            yield return Excavate(garage, gnome);
            yield return StowInTheCradle(gnome);
            yield return CarryHomeInTheCradle(garage, gnome);
            yield return ChargeAtTheDock("before the leap");
            yield return FirstLeap();
            yield return HopFrom(StationReach.HomeId, WorldAnchorIds.RelayPrefix + 2);
            yield return ChargeOnTheRing(2);
            yield return ToTheRunUp();
            yield return LeapTheChasm();
            yield return FindBell();
            yield return RestoreTheTerminusRelay();
            yield return ChargeOnTheRing(3);
            yield return GatherBellsParts();
            yield return RepairBell();
            yield return HomeFromTheTerminus();
            yield return ChargeAtTheDock("home from the canyon");
            yield return TurnBellsDial();
            yield return FollowBellsSignal();
            yield return ExpectStep(ProgressionKeys.NextPowerPart, "Bell is home: the tower still needs its last part");
            Assert.IsTrue(_gameplay.TowerParts.NeedsPart(Tower, 3), "level 3 waits for the relay spare");
            yield return ToTheMoundRelay();
            Assert.IsFalse(_gameplay.Power.HasReached(BasePowerStage.Chart), "the chart stays dark before its step");
            yield return PickUpTheRelaySpare();
            yield return DriveOutInReach();
            yield return HopHomeAndBack();
            yield return RestOnTheDock();
            AssertReadyForTheChart();
            yield return StockUpForTheChart();
            yield return BuyTowerLevel(3);
            yield return LookAtTheChart();
            yield return Until(
                () => _gameplay.Progression.NextStep.StartsWith(ProgressionKeys.NextLink, StringComparison.Ordinal),
                2f, "Power is restored: the line speaks of the masts toward the Peak");
            CollectionAssert.IsEmpty(_tierTwoLines, "past the teaching steps the line names only the targets");

            float total = Time.time - started;
            WriteReport(total);
            Assert.AreEqual(0, _crawls, "the golden path never runs the battery down to a crawl (M3-20 §12)");
            Assert.Less(total, TotalBudget, "the whole golden path fits a generous time budget");
            Assert.IsEmpty(_problems, "no errors or exceptions anywhere in the log during the playthrough");
        }

        /// <summary>
        /// What each step asks of the battery (docs/features/M3-20 §3): metres on the ground and seconds of beam work,
        /// counted every frame for the report's tuning columns (a radio-hop's jump is not driving), and the lowest charge
        /// within the step (a charger later in it hides a dip from its end charge).
        /// </summary>
        private IEnumerator TrackTheBattery(IBeamStatus beam)
        {
            Vector3 last = _rover.Position;
            while (true)
            {
                yield return null;
                Vector3 now = _rover.Position;
                float metres = SurfaceRules.HorizontalDistance(now, last);
                last = now;
                _stepMetres += _rover.IsGrounded && metres < HopJump ? metres : 0f;
                _stepBeam += beam.Working ? Time.deltaTime : 0f;
                _stepLowest = Mathf.Min(_stepLowest, _gameplay.Battery.Model.Charge01);
            }
        }

        private void OnLowPower(LowPowerChanged lowPower)
        {
            _crawls += lowPower.LowPower && !lowPower.Restored ? 1 : 0;
        }

        /// <summary>Notes any line past the teaching steps that is not about a recovery target (M3-21 §3).</summary>
        private void OnNextLine(NextStepChanged changed)
        {
            if (_gameplay.Progression.PastTeaching && !ProgressionKeys.SpeaksOfTargets(changed.LineKey))
            {
                _tierTwoLines.Add(changed.LineKey);
            }
        }

        /// <summary>Notes the charge at one of the range lessons (docs/features/M3-20 §6) for the report.</summary>
        private void Lesson(string where, float from, float to)
        {
            _lessons.Add((where, _gameplay.Battery.Model.Charge01, new Vector2(from, to)));
        }

        /// <summary>
        /// Home from a trip: drive onto the charging dock, rest while it fills the cells (each a rising note), and
        /// leave as soon as they are full, before the evening's sweep (the dock's evening still opens with news).
        /// </summary>
        /// <summary>
        /// Power first (first-hour v2 §4): 07 drives from the dock to the sleeping tower, whose card and recipe say what
        /// it needs; the next line moves on to the depot.
        /// </summary>
        private IEnumerator DriveToTheTower()
        {
            Begin();
            RadioTower tower = _gameplay.Tower;
            yield return DriveTo(Flat(tower.PadCentre), TowerVisit, 0.6f, 60f, "the sleeping radio tower");
            yield return Until(() => _gameplay.Progression.NextStep != ProgressionKeys.NextWake, 3f,
                "near the tower, the wake line gives way");
            End("Drive to the radio tower", "power first: the tower is asleep");
        }

        /// <summary>Tower level 1's first sweep raises Tilly's pillar a moment later (first-hour v2 §4).</summary>
        private IEnumerator TheSweepRaisesTillysPillar()
        {
            Begin();
            Friend tilly = _gameplay.Friends.Find(TillyId);
            yield return Until(() => tilly.Progress.Discovered,
                _gameplay.Progression.Tuning.TowerSweepDelay + 3f, "the tower's sweep raises Tilly's pillar");
            End("The tower's first sweep", "a faint signal out past the dune");
        }

        /// <summary>Before Kenji's port, holding the tether does nothing: the ring is gone (first-hour v2 §3).</summary>
        private void AssertTetherAsleep()
        {
            Assert.IsFalse(_context.Get<IRoverAbilities>().Has(RoverAbility.Tether), "no tether before the port");
            Assert.AreEqual(0, _events.TetherAttached.Count, "the tether never latched before the ring");
            Assert.AreEqual(TetherAimState.Asleep, _gameplay.Tether.State, "nothing to hold with: no reticle, no icon");
        }

        /// <summary>
        /// The garage's last cut opened Kenji's port: park on its pad and its arm fits the spare tether ring round 07's
        /// eye; the tether wakes.
        /// </summary>
        private IEnumerator FitTheTetherRing()
        {
            Begin();
            GaragePort port = _gameplay.Port;
            Assert.IsTrue(port.IsOpen, "the last cut opened the port");
            int fitted = 0;
            using IDisposable fits = _context.Events.Subscribe<RoverKitFitted>(fit =>
                fitted += fit.Piece == RoverKitPiece.TetherRing ? 1 : 0);
            yield return DriveTo(Flat(port.PadCentre), PortPark, 0.35f, 40f, "Kenji's port pad");
            yield return Until(() => port.RingFitted, 6f, "parked on the pad, the arm fits the ring");
            yield return Until(() => fitted == 1, 6f, "the ring is on 07's eye");
            Assert.IsTrue(_context.Get<IRoverAbilities>().Has(RoverAbility.Tether), "07 can hold things again");
            Capture("12c-the-tether-ring");
            Witness(_rover.Position, "12c-the-tether-ring-closeup");
            _pilot.Reverse = BayThrottle;
            yield return Until(() => SurfaceRules.HorizontalDistance(_rover.Position, port.PadCentre) > PortBackOut,
                6f, "07 backs out of Kenji's bay the way it came in");
            _pilot.Reverse = 0f;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 stops outside the port");
            End("Kenji's port fits the tether ring", "the tether wakes");
        }

        /// <summary>
        /// Ro's tape deck, dug up beside the port and held on the tether: resting by the port, 07 has it taken onto the
        /// tray and fitted on its flank; the first tape plays.
        /// </summary>
        private IEnumerator FitTheTapeDeck()
        {
            Begin();
            GaragePort port = _gameplay.Port;
            int fitted = 0;
            using IDisposable fits = _context.Events.Subscribe<RoverKitFitted>(fit =>
                fitted += fit.Piece == RoverKitPiece.TapeDeck ? 1 : 0);
            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 rests by the port, the deck on its tether");
            yield return Until(() => port.DeckFitted, 10f, "the port takes the deck onto its tray and fits it");
            Release(_mouse.rightButton);
            yield return Until(() => fitted == 1, 6f, "the deck is on 07's flank");
            Capture("12d-the-tape-deck");
            End("Kenji's port fits Ro's tape deck", "the first music, earned");
        }

        private IEnumerator ChargeAtTheDock(string why, float leastOnArrival = 0f)
        {
            Begin();
            BatteryModel battery = _gameplay.Battery.Model;
            ChargingDock dock = _gameplay.Home.Dock;
            Vector3 facing = Flat(dock.Rotation * Vector3.forward).normalized;
            yield return DriveTo(Flat(dock.Position) - facing * DockApproach, 1.5f, 0.6f, 60f,
                "the front of the charging dock");
            yield return DriveTo(Flat(dock.Position), DockParkRadius, 0.35f, 30f, "the charging dock");
            float before = battery.Charge01;
            Assert.GreaterOrEqual(before, leastOnArrival, $"07 reaches the dock with charge to spare ({why})");
            yield return Until(() => dock.Docked, _gameplay.Home.Tuning.DockDelay + 3f, "07 rests on the dock");
            float docked = Time.time;
            yield return Until(() => battery.FullCells == battery.Cells && !battery.IsCharging,
                _gameplay.Battery.Tuning.ChargeStopHold
                + battery.Cells * _gameplay.Battery.Tuning.DockSecondsPerCell + 2f,
                "the dock fills the cells");
            float fill = Time.time - docked;
            _pilot.Reverse = BayThrottle;
            yield return Until(() => !dock.Docked, 2f, "a touch of the throttle leaves the dock");
            _pilot.Reverse = 0f;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 stops off the dock");
            End($"Charge at the dock ({why})", string.Format(CultureInfo.InvariantCulture,
                "{0:P0} -> {1:P0} in {2:F1} s", before, battery.Charge01, fill));
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
            {
                _warnings++;
            }
            else if (type != LogType.Log)
            {
                _problems.Add($"{type}: {message}");
            }
        }

        private IEnumerator Boot()
        {
            Begin();
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The playthrough loads the scene through the editor.");
#endif
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            GameBootstrap bootstrap = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            _rover = (RoverController)_context.Get<IRoverRig>();
            _events = new EventRecorder(_context.Events);
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awokeAt = Time.time);
            _crawled = _context.Events.Subscribe<LowPowerChanged>(OnLowPower);
            _lines = _context.Events.Subscribe<NextStepChanged>(OnNextLine);
            _gameplay.StartCoroutine(TrackTheBattery(_context.Get<IBeamStatus>()));
            _pilot = new Autopilot(_rover);
            _rover.SetDriveSource(_pilot);
            Assert.AreEqual(0, _gameplay.Materials.Total, "a fresh game (its own save slot)");
            SalvageField salvage = _gameplay.Salvage;
            int units = 0;
            foreach (SalvagePiece piece in salvage.Pieces)
            {
                units += piece.Units;
            }

            End("Boot the real Main scene", $"{salvage.Sites.Count} salvage sites with {salvage.Pieces.Count} " +
                                            $"pieces: {units} units of salvage");
        }

        private IEnumerator WakeUp()
        {
            Begin();
            yield return Until(() => _awokeAt >= 0f, 20f, "07 wakes up on its own");
            yield return new WaitForSeconds(2f);
            Capture("01-awake");
            End("07 wakes", "woke on its own (not by player input)");
        }

        private IEnumerator DriveToSite(SalvageSite site)
        {
            Begin();
            float distance = SurfaceRules.HorizontalDistance(site.Position, _rover.Position);
            Vector3 toSite = Flat(site.Position - _rover.Position).normalized;
            yield return DriveTo(Flat(site.Position - toSite * SiteApproach), 3f, 1f, 60f, "the supply depot");
            Capture("01b-the-depot");
            End($"Drive to the supply depot ({distance:F0} m)", $"{site.Pieces.Count} pieces on the wreck");
        }

        /// <summary>
        /// Every piece of the depot in turn: circle the wreck to it, line up in front of its cut, ease in facing it and
        /// hold Excavate until it folds into 07. A piece the aim cannot pick from there is retried on a second pass.
        /// </summary>
        private IEnumerator SalvageTheDepot(SalvageSite site)
        {
            Begin();
            yield return CutPieces(site, "02", int.MaxValue);
            int cuts = _cuts;
            Assert.IsTrue(site.IsPickedClean, "every piece of the depot is salvaged");
            Assert.IsTrue(site.Root.Find("Skeleton").gameObject.activeSelf, "its weathered skeleton stays");
            int topStep = 0;
            foreach (EventRecorder.Timed<MaterialSalvaged> piece in _events.MaterialSalvaged)
            {
                Assert.AreEqual(site.Id, piece.Value.SiteId);
                topStep = Mathf.Max(topStep, piece.Value.ComboStep);
            }

            Assert.Greater(topStep, 0, "consecutive pieces climb the salvage melody");
            IMaterialStock stock = _gameplay.Materials;
            Review(site.Position - Flat(site.Root.forward) * 16f + Vector3.up * 6f, site.Position + Vector3.up,
                "02d-depot-picked-clean");
            End("Salvage the depot", $"{site.Pieces.Count} pieces in {cuts} cuts, melody up to step {topStep}; " +
                                     $"stock metal {stock.Metal}, wiring {stock.Wiring}, optics {stock.Optics}; " +
                                     $"first small cut {_firstCut:F2} s, slowest so far {_slowestEarlyCut:F2} s");
        }

        /// <summary>
        /// 0.5.5 §5.1, measured: a small piece about 2.3 s from press to crack, and nothing in the first half hour over
        /// about 4.1 s (each with a little room for 07 easing to a stop as the beam asks it to).
        /// </summary>
        private void TimeTheCut(float took, PieceSize size)
        {
            if (size == PieceSize.Small)
            {
                _firstCut = _firstCut < 0f ? took : _firstCut;
                Assert.LessOrEqual(took, FirstCut + EaseToStop, "a small piece cuts snappily");
            }

            if (Time.time - _awokeAt <= FirstHalfHour)
            {
                _slowestEarlyCut = Mathf.Max(_slowestEarlyCut, took);
                Assert.LessOrEqual(took, FirstHalfHourCut + EaseToStop, "nothing in the first half hour drags");
            }
        }

        /// <summary>
        /// Cuts every piece of <paramref name="site"/> the beam can cut (drag pieces wait for the tether), capturing
        /// the first cut as <paramref name="capture"/>; the cuts started are left in <see cref="_cuts"/>.
        /// </summary>
        private IEnumerator CutPieces(SalvageSite site, string capture, int most)
        {
            SalvageField salvage = _gameplay.Salvage;
            int started = _events.SalvageCutStarted.Count;
            int cuts = started;
            bool captured = false;
            for (int pass = 0; pass < 2 && !site.IsPickedClean && cuts - started < most; pass++)
            {
                foreach (SalvagePiece piece in InSweepOrder(site))
                {
                    if (cuts - started >= most)
                    {
                        break;
                    }

                    yield return FacePiece(site, piece);
                    float settle = Time.time + AimSettle;
                    while (salvage.Candidate == null && Time.time < settle)
                    {
                        yield return null;
                    }

                    if (salvage.Candidate == null)
                    {
                        continue;
                    }

                    int salvaged = _events.MaterialSalvaged.Count;
                    int completed = CompletedCuts();
                    float cutSeconds = salvage.Candidate.CutSeconds;
                    PieceSize size = salvage.Candidate.Size;
                    float pressed = Time.time;
                    Press(_keyboard.eKey);
                    if (!captured)
                    {
                        // Mid charge-up (the Anticipate): the charge run's lamps on their way to the emitter (M3-26).
                        yield return Until(() => salvage.Cutting != null, 6f, "the emitter starts to charge");
                        yield return new WaitForSeconds(_context.Get<ICutter>().ChargeSeconds * 0.5f);
                        Capture(capture + "a-charge-run");
                        Witness(salvage.Cutting.CutPosition, capture + "a-charge-run-closeup");
                    }

                    yield return Until(() => _events.SalvageCutStarted.Count > cuts, 6f, "the beam starts cutting");
                    Assert.IsTrue(_rover.IsHeldStill, "07 is asked to hold still while it cuts");
                    if (!captured)
                    {
                        yield return new WaitForSeconds(cutSeconds * 0.5f);
                        Capture(capture + "b-salvage-cut");
                        Witness(salvage.Cutting != null ? salvage.Cutting.CutPosition : site.Position,
                            capture + "c-salvage-cut-closeup");
                        captured = true;
                    }

                    yield return Until(() => CompletedCuts() > completed, cutSeconds + 4f, "the beam cuts it free");
                    Release(_keyboard.eKey);
                    TimeTheCut(Time.time - pressed, size);
                    yield return Until(() => _events.MaterialSalvaged.Count > salvaged, 6f,
                        "the piece comes away and folds into 07");
                    cuts = _events.SalvageCutStarted.Count;
                    yield return null;
                }
            }

            _cuts = cuts - started;
        }

        /// <summary>
        /// The wreck's cuttable pieces in the order one sweep round it reaches them from where 07 stands, turning the
        /// shorter way, as a player works round a wreck rather than criss-crossing it.
        /// </summary>
        private List<SalvagePiece> InSweepOrder(SalvageSite site)
        {
            Vector3 centre = Flat(site.Position);
            float here = Bearing(_rover.Position - centre);
            var pieces = new List<SalvagePiece>();
            var offsets = new List<float>();
            float lowest = 360f;
            float highest = 0f;
            foreach (SalvagePiece piece in site.Pieces)
            {
                if (!piece.IsCuttable)
                {
                    continue;
                }

                float offset = Mathf.Repeat(Bearing(piece.CutPosition - centre) - here, 360f);
                pieces.Add(piece);
                offsets.Add(offset);
                lowest = Mathf.Min(lowest, offset);
                highest = Mathf.Max(highest, offset);
            }

            bool clockwise = highest <= 360f - lowest;
            var order = new List<int>();
            for (int i = 0; i < pieces.Count; i++)
            {
                order.Add(i);
            }

            order.Sort((a, b) => clockwise ? offsets[a].CompareTo(offsets[b]) : offsets[b].CompareTo(offsets[a]));
            var swept = new List<SalvagePiece>();
            foreach (int i in order)
            {
                swept.Add(pieces[i]);
            }

            return swept;
        }

        /// <summary>
        /// The garage on the way to the duck: its wiring pays the tower's second level (M3-16 P9). Up to
        /// <paramref name="most"/> cuts a trip, so each trip out from the dock and back fits a charge.
        /// </summary>
        private IEnumerator SalvageTheGarage(SalvageSite site, int most, string what)
        {
            Begin();
            yield return DriveToSite(site);
            yield return CutPieces(site, "11", most);
            IMaterialStock stock = _gameplay.Materials;
            End($"Salvage {site.Id}: {what}", $"{_cuts} cuts; stock metal {stock.Metal}, wiring " +
                                             $"{stock.Wiring}, optics {stock.Optics}");
        }

        /// <summary>Circles the wreck to the piece's side, lines up in front of its cut and eases in.</summary>
        private IEnumerator FacePiece(SalvageSite site, SalvagePiece piece)
        {
            Vector3 cut = piece.CutPosition;
            Vector3 facing = Flat(piece.CutNormal);
            Vector3 outward = facing.magnitude >= MinCutFacing
                ? facing.normalized
                : Flat(cut - site.Position).normalized;
            yield return CircleTo(site, Flat(cut + outward * CutLineUp), 0f);
            yield return DriveTo(Flat(cut + outward * CutLineUp), 2f, 0.6f, 40f, "a line-up in front of a piece");
            yield return DriveTo(Flat(cut + outward * CutStandOff), 1f, 0.35f, 30f, "the piece's cut");
        }

        /// <summary>
        /// Drives round the wreck, outside its footprint (and <paramref name="extra"/> metres more), to the side facing
        /// the target; when nothing solid stands on the straight way there, it goes straight, as a player would.
        /// </summary>
        private IEnumerator CircleTo(SalvageSite site, Vector3 target, float extra)
        {
            if (StraightWayIsOpen(target))
            {
                yield break;
            }

            float ring = Anchor(site.Id).Radius + RingMargin + extra;
            Vector3 centre = Flat(site.Position);
            float from = Bearing(_rover.Position - centre);
            float to = Bearing(target - centre);
            float turn = Mathf.DeltaAngle(from, to);
            int steps = Mathf.CeilToInt(Mathf.Abs(turn) / RingStep);
            for (int i = 0; i <= steps; i++)
            {
                float bearing = from + (steps == 0 ? 0f : turn * i / steps);
                Vector3 point = centre + SurfaceRules.BearingDirection(bearing) * ring;
                yield return DriveTo(point, 3f, 0.7f, 40f, "the way round the wreck");
            }
        }

        /// <summary>True when no wreck or prop stands between 07 and <paramref name="target"/> at wheel height.</summary>
        private bool StraightWayIsOpen(Vector3 target)
        {
            Vector3 from = _rover.Position + Vector3.up * WayCheckHeight;
            Vector3 way = new Vector3(target.x, from.y, target.z) - from;
            int solid = ~(Layers.RoverMask | Layers.RelicMask | Layers.PickupMask);
            return !Physics.SphereCast(from, WayCheckRadius, way.normalized, out _, way.magnitude, solid,
                QueryTriggerInteraction.Ignore);
        }

        private static float Bearing(Vector3 direction)
        {
            return SurfaceRules.Bearing(Flat(direction));
        }

        /// <summary>
        /// Dig the relic out of the heart: park within the beam's reach on open ground round the heart, facing it, and
        /// hold Excavate until it is free.
        /// </summary>
        /// <summary>
        /// The nearest spot <see cref="DigStandOff"/> from <paramref name="heart"/> with open ground for 07 that
        /// <paramref name="fits"/> accepts, and the way out from the heart to it.
        /// </summary>
        private bool OpenSpotBy(Vector3 heart, Func<Vector3, bool> fits, out Vector3 park, out Vector3 outward)
        {
            park = Vector3.zero;
            outward = Vector3.zero;
            float best = float.MaxValue;
            for (float bearing = 0f; bearing < 360f; bearing += 30f)
            {
                Vector3 direction = SurfaceRules.BearingDirection(bearing);
                Vector3 spot = Flat(heart + direction * DigStandOff);
                float ground = _context.Get<ITerrainQuery>().SampleHeight(spot.x, spot.z);
                bool open = !Physics.CheckSphere(new Vector3(spot.x, ground + ParkClearance + 0.2f, spot.z),
                    ParkClearance, Layers.PropMask, QueryTriggerInteraction.Ignore);
                float distance = SurfaceRules.HorizontalDistance(spot, _rover.Position);
                if (open && fits(spot) && distance < best)
                {
                    best = distance;
                    park = spot;
                    outward = direction;
                }
            }

            return best < float.MaxValue;
        }

        /// <summary>
        /// The tower's component <paramref name="index"/> waits in <paramref name="site"/>'s open heart: round the
        /// wreck to open ground beside it, ease up and press E: it spirals into 07 (P1).
        /// </summary>
        private IEnumerator PickUpPartInTheHeart(SalvageSite site, int index)
        {
            Begin();
            TowerPartField field = _gameplay.TowerPartField;
            string part = _gameplay.TowerParts.PartAt(index).PartId;
            yield return Until(() => field.IsResting(index), _gameplay.Progression.Tuning.PartAppearSeconds + 2f,
                $"{part} eases into view in the heart of {site.Id}");
            Vector3 rest = field.RestPosition(index);
            Assert.IsTrue(OpenSpotBy(rest, _ => true, out Vector3 park, out Vector3 outward),
                $"open ground beside {part} in the heart of {site.Id}");
            yield return CircleTo(site, Flat(rest + outward * CutLineUp), 0f);
            yield return DriveTo(Flat(rest + outward * CutLineUp), 2f, 0.6f, 40f, $"a line-up facing {part}");
            yield return DriveTo(park, 1f, 0.35f, 30f, $"{part} in the heart");
            yield return PickupPress.Take(this, _gameplay, 4f, part);
            yield return Until(() => _gameplay.TowerParts.StateOf(index) == TowerPartState.Carried, 8f,
                $"{part} spirals into 07");
            Capture("03-part-" + part);
            End($"Pick up {part} in the heart of {site.Id}", "it rides home on 07's back");
        }

        /// <summary>The relay spare waits on the first lit mast's pad: drive up to it and press E (P1, level 3).</summary>
        private IEnumerator PickUpTheRelaySpare()
        {
            Begin();
            TowerPartField field = _gameplay.TowerPartField;
            yield return Until(() => field.IsResting(SparePart) ||
                                     _gameplay.TowerParts.StateOf(SparePart) != TowerPartState.Waiting,
                _gameplay.Progression.Tuning.PartAppearSeconds + 2f, "the relay spare shows on the lit mast's pad");
            if (_gameplay.TowerParts.StateOf(SparePart) == TowerPartState.Waiting)
            {
                yield return DriveTo(Flat(field.RestPosition(SparePart)), PartArrive, 0.5f, 60f, "the relay spare");
                yield return PickupPress.Take(this, _gameplay, 4f, "the relay spare");
            }

            yield return Until(() => _gameplay.TowerParts.StateOf(SparePart) == TowerPartState.Carried, 8f,
                "the relay spare spirals into 07");
            End("Pick up the relay spare on the mast's pad", "level 3's part rides on 07's back");
        }

        /// <summary>
        /// Before the chasm is seen (P2): Bell's light already stands across it, but the garage's transformer is not
        /// out yet, so tower level 2, the bay and its Hover-Jump cannot come first.
        /// </summary>
        private void AssertNotYetPastTheChasm()
        {
            Assert.IsFalse(_gameplay.Progression.ChasmSeen);
            Assert.IsTrue(BellHeard(), "Tilly spotted Bell's light across the chasm");
            Assert.IsFalse(_gameplay.TowerPartField.IsResting(TransformerPart), "no transformer yet");
            Assert.IsTrue(_gameplay.TowerParts.NeedsPart(Tower, 2), "so level 2 cannot be built");
            Assert.IsFalse(_gameplay.Workshop.Awake, "and the bay sleeps");
        }

        private bool BellHeard()
        {
            foreach (Friend friend in _gameplay.Friends.Friends)
            {
                if (friend.Id == BellId)
                {
                    return friend.Progress.Discovered;
                }
            }

            return false;
        }

        /// <summary>
        /// Up to the chasm's lip with no Hover-Jump: 07 stops, holds still and looks across at Bell's light while it
        /// pulses; it can't cross yet (P2, ruling 10).
        /// </summary>
        private IEnumerator LookAcrossTheChasm()
        {
            Begin();
            WorldAnchor lip = Anchor(WorldAnchorIds.CanyonLip);
            BaseProgression progression = _gameplay.Progression;
            yield return DriveTo(Flat(lip.Position - lip.Forward * LipStandOff), 2f, 0.5f, 60f, "the chasm's lip");
            yield return Until(() => progression.ChasmSeen, 4f, "07 sees the chasm");
            Assert.AreEqual(0, _gameplay.Upgrades.LevelOf(HoverJump), "it cannot leap yet");
            Capture("08-looking-across-the-chasm");
            yield return Until(() => !progression.LookingAcross, progression.Tuning.ChasmLookSeconds + 2f,
                "07 looks across, then lets go");
            Assert.IsFalse(_rover.IsHeldStill, "free to drive back");
            End("Look across the chasm at Bell's light", $"held still {progression.Tuning.ChasmLookSeconds:F1} s");
        }

        /// <summary>
        /// Hover-Jump's blueprint tube waits at the chasm's near lip (M3-21 §4), where 07 stopped to look across: drive
        /// through it and it rides home on 07's back, for the lab's reader.
        /// </summary>
        private IEnumerator PickUpTheLipTube()
        {
            Begin();
            GroundTubes tubes = _gameplay.GroundTubes;
            if (tubes.IsResting(HoverJump))
            {
                yield return DriveTo(Flat(tubes.RestPosition(HoverJump)), PartArrive, 0.4f, 30f,
                    "Hover-Jump's tube at the lip");
                yield return PickupPress.Take(this, _gameplay, 4f, "Hover-Jump's tube");
            }

            yield return Until(() => !_gameplay.Blueprints.IsWaiting(HoverJump), 8f, "the tube spirals into 07");
            Assert.AreEqual(HoverJump, _gameplay.Blueprints.NextCarried, "it rides home to the lab's reader");
            Assert.AreEqual(TechNodeState.Unknown, _gameplay.Tech.StateOf(TechNodeId.HoverJump), "not read yet");
            Capture("08b-the-lip-tube");
            End("Pick up Hover-Jump's tube at the chasm's lip", "no sheet was pinned from the start");
        }

        /// <summary>
        /// Home from the mouth relay by radio-hop: up the belly lift with Hover-Jump's tube from the lip and Cell Pack
        /// II's from relay.2's cabinet on 07's back. The turntable turns to the reader, which decodes one, then the
        /// other, sending each hand-off pulse to the bay (still asleep); then back down (M3-19 §5, M3-20, M3-21 §4).
        /// </summary>
        private IEnumerator RideUpToDecodeTheTubes()
        {
            Begin();
            LiftStation lift = _gameplay.Lift;
            int decoded = 0;
            using IDisposable finds = _context.Events.Subscribe<FindDecoded>(find =>
                decoded += find.KitId == HoverJump || find.KitId == CellPack ? 1 : 0);
            yield return DriveOntoTheLift("the belly lift's platform, the tubes on 07's back");
            yield return Until(() => lift.Phase == LiftPhase.Inside, LiftPhaseTimeout,
                "resting on the platform carries 07 up into the lab");
            yield return Until(() => decoded > 1, LabTurnTimeout + 2f * ReaderTimeout,
                "the turntable turns to the reader, whose beam feeds both tubes in to decode, one after the other");
            Assert.IsTrue(_gameplay.Blueprints.IsPinned(HoverJump), "decoded: the bay can build Hover-Jump");
            Assert.IsTrue(_gameplay.Blueprints.IsPinned(CellPack), "decoded: the bay can build Cell Pack II");
            yield return null;
            Assert.AreEqual(TechNodeState.Decoded, _gameplay.Tech.StateOf(TechNodeId.HoverJump),
                "its sheet is on the tech board");
            Assert.AreEqual(TechNodeState.Decoded, _gameplay.Tech.StateOf(TechNodeId.CellPackII));
            yield return RideDown("08c-down-from-the-reader");
            End("Ride up to decode the two tubes", "the reader decoded both; the hand-offs ran to the bay");
        }

        /// <summary>Having seen the chasm, back to the garage: Tilly spots the transformer (P1, L2).</summary>
        private IEnumerator BackToTheGarageForTheTransformer(SalvageSite garage)
        {
            Begin();
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 90f, "the base pad");
            Vector3 toGarage = Flat(garage.Position - _rover.Position).normalized;
            yield return DriveTo(Flat(garage.Position - toGarage * SiteApproach), 3f, 1f, 90f, "the garage");
            End("Back to the garage", "Tilly spots the transformer in its heart");
            yield return PickUpPartInTheHeart(garage, TransformerPart);
        }

        /// <summary>
        /// Where step 8 begins (the crew's lift, the rover session's slice): home powered to the bay, the lift still
        /// dark, and level 3's relay spare riding on 07, ready to hand to the tower.
        /// </summary>
        private void AssertReadyForTheChart()
        {
            Assert.AreEqual(BasePowerStage.Bay, _gameplay.Power.Stage);
            Assert.AreEqual(TowerPartState.Carried, _gameplay.TowerParts.StateOf(SparePart));
            Assert.AreEqual(ProgressionKeys.NextPowerCarried, _gameplay.Progression.NextStep,
                "the tower's last part rides with 07");
        }

        private bool LogFound(string logId)
        {
            foreach (EventRecorder.Timed<CrewLogFound> found in _events.CrewLogFound)
            {
                if (found.Value.LogId == logId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The salvage of the rest of the basin, which the golden path skips: the stock is topped up to the tower's
        /// third level.
        /// </summary>
        private IEnumerator StockUpForTheChart()
        {
            Begin();
            Recipe need = _gameplay.Upgrades.Find(Tower).Levels[2].Recipe;
            IMaterialStock stock = _gameplay.Materials;
            var topUp = new Recipe(Mathf.Max(0, need.Metal - stock.Metal), Mathf.Max(0, need.Wiring - stock.Wiring),
                Mathf.Max(0, need.Optics - stock.Optics));
            _gameplay.Materials.Deliver(SalvageMaterial.Metal, topUp.Metal);
            _gameplay.Materials.Deliver(SalvageMaterial.Wiring, topUp.Wiring);
            _gameplay.Materials.Deliver(SalvageMaterial.Optics, topUp.Optics);
            yield return null;
            End("The rest of the basin's salvage (not driven)", $"topped up by {topUp} to {need}");
        }

        /// <summary>
        /// Home with the duck in tow and the Cradle's tube on its back, the jammed belly lift asked for its stitch: to
        /// the spot just off its lit platform, the duck still in tow, and rest while 07's beam stitches the cable and
        /// the platform levels.
        /// </summary>
        private IEnumerator StitchTheLift()
        {
            Begin();
            LiftStation lift = _gameplay.Lift;
            Assert.AreEqual(LiftPhase.Called, lift.Phase, "home with finds for the lab: the lift asks for the stitch");
            yield return ExpectStep(ProgressionKeys.NextLiftStitch, "the lift is stuck: stitch the cable");
            yield return DriveTo(Flat(lift.StitchSpot), 1f, 0.5f, 60f, "the spot by the jammed platform");
            yield return Until(() => lift.Phase >= LiftPhase.Ready, LiftPhaseTimeout,
                "resting by it, 07 stitches the cable and the platform eases level");
            Assert.IsTrue(lift.Capture().restored, "the restored lift is saved");
            Capture("05-lift-stitched");
            End("Stitch the belly lift", "from beside its lit platform, the duck still in tow");
        }

        /// <summary>
        /// The first ride: up the ramp onto the restored platform, which carries 07 up into the lab with the duck
        /// beside it. The turntable turns to the reader, which takes the Cradle's tube and decodes it (its hand-off
        /// pulse runs to the bay, still asleep), then to the keepsake shelf, where the press places the duck: the first
        /// memory. Back brings the platform down, turned so 07 drives out toward the dock.
        /// </summary>
        private IEnumerator RideUpWithTheFirstFinds(Relic duck)
        {
            Begin();
            LiftStation lift = _gameplay.Lift;
            int decoded = 0;
            int handOffs = 0;
            using IDisposable finds = _context.Events.Subscribe<FindDecoded>(find =>
                decoded += find.KitId == CargoCradle ? 1 : 0);
            using IDisposable pulses = _context.Events.Subscribe<DecodeHandOff>(_ => handOffs++);
            yield return DriveTo(Flat(lift.RideSpot), BoardRadius, 0.35f, 30f, "up its ramp onto the platform");
            yield return Until(() => lift.Phase == LiftPhase.Inside, LiftPhaseTimeout,
                "resting on the platform, it carries 07 up into the lab");
            Assert.AreSame(duck, lift.Rider, "the towed duck rode up beside 07");
            Assert.IsNull(_gameplay.Tether.Towed, "the tether let it go aboard");
            yield return ExpectStep(ProgressionKeys.NextLiftRide, "up in the lab, the duck waits to be placed");
            yield return Until(() => decoded > 0, LabTurnTimeout + ReaderTimeout,
                "the turntable turns to the reader, whose beam feeds the Cradle's tube in to decode");
            Capture("06-lab-reader");
            Assert.AreEqual(CargoCradle, _events.BlueprintDelivered[0].Value.KitId, "the reader took the tube");
            Assert.IsTrue(_gameplay.Blueprints.IsPinned(CargoCradle), "decoded: the bay can build the Cradle");
            Assert.AreEqual(1, handOffs, "the hand-off pulse runs down to the bay");
            Assert.IsFalse(_gameplay.Blueprints.IsDrawn(CargoCradle), "the bay sleeps: its sheet waits for level 2");
            yield return Until(() => _gameplay.Home.KeepsakesFaced, LabTurnTimeout,
                "then the turntable turns to the keepsake shelf, for the duck");
            yield return Deposit(duck);
            yield return RideDown("08-down-from-the-lab");
            End("Ride up with the first finds", "the tube decoded at the reader, the duck on the keepsake shelf");
        }

        /// <summary>Back the stick, as a player does, and wait for the platform to settle down, 07 facing
        /// out.</summary>
        private IEnumerator RideDown(string capture)
        {
            LiftStation lift = _gameplay.Lift;
            yield return Tap(_keyboard.sKey);
            yield return Until(() => lift.Phase == LiftPhase.Lowering, LiftPhaseTimeout, "back brings the lift down");
            yield return Until(() => lift.Phase == LiftPhase.Ready, LiftPhaseTimeout,
                "the platform settles at the bottom stop");
            Vector3 heading = Flat(_rover.Rotation * Vector3.forward).normalized;
            Assert.Greater(Vector3.Dot(heading, -Flat(lift.RideHeading).normalized), FacesOut,
                "the platform turned while lowering: 07 faces out toward the dock");
            yield return new WaitForSeconds(1f);
            Capture(capture);
        }

        /// <summary>From the apron in front of the belly lift, onto its platform, and stop there.</summary>
        private IEnumerator DriveOntoTheLift(string what)
        {
            LiftStation lift = _gameplay.Lift;
            Vector3 heading = Flat(lift.RideHeading).normalized;
            yield return DriveTo(Flat(lift.RideSpot - heading * LiftLineUp), 1f, 0.5f, 60f, "the apron before the lift");
            yield return DriveTo(Flat(lift.RideSpot), BoardRadius, 0.35f, 30f, what);
        }

        /// <summary>A short press of <paramref name="key"/>, as a player taps the stick in the lab.</summary>
        private IEnumerator Tap(KeyControl key)
        {
            Press(key);
            yield return new WaitForSeconds(KeySeconds);
            Release(key);
            yield return new WaitForSeconds(KeySeconds);
        }

        /// <summary>
        /// Step 8: the tower's third level woke the survey chart. Ride up carrying nothing: 07 arrives facing the chart
        /// and the Peak, the map opens by itself and the chart pen inks what 07 has visited in reach; back comes away
        /// from the map, a look at the tech board's recovery panel (M3-21), and back again brings the platform down.
        /// </summary>
        private IEnumerator LookAtTheChart()
        {
            Begin();
            LiftStation lift = _gameplay.Lift;
            Assert.IsTrue(_gameplay.Power.HasReached(BasePowerStage.Chart), "the tower's third level wakes the chart");
            int looks = 0;
            int inks = 0;
            int ended = 0;
            using IDisposable started = _context.Events.Subscribe<MapLookStarted>(_ => looks++);
            using IDisposable inked = _context.Events.Subscribe<MapInkCued>(_ => inks++);
            using IDisposable closed = _context.Events.Subscribe<MapLookEnded>(_ => ended++);
            yield return DriveOntoTheLift("the belly lift's platform");
            yield return Until(() => lift.Phase == LiftPhase.Inside, LiftPhaseTimeout,
                "resting on the platform carries 07 up into the lab");
            Assert.AreEqual(LabStation.Chart, lift.Lab.Facing, "carrying nothing, 07 arrives facing the chart");
            yield return Until(() => looks > 0, lift.Tuning.ChartBreath + LabTurnTimeout,
                "with news on it, the map opens by itself");
            yield return Until(() => inks > 0, LabTurnTimeout, "the chart pen inks what 07 has visited in reach");
            yield return new WaitForSeconds(MapSeconds);
            Capture("39-the-survey-chart");
            yield return Tap(_keyboard.sKey);
            yield return Until(() => ended > 0, 3f, "back comes away from the map");
            Assert.AreEqual(LiftPhase.Inside, lift.Phase, "coming away from the map keeps 07 in the lab");
            yield return LookAtTheBoard();
            yield return RideDown("40-down-from-the-chart");
            End("Look at the survey chart", $"the map opened by itself, {inks} places inked");
        }

        /// <summary>
        /// Power 3/3 (M3-21): round to the tech board, seen whole from the ring camera with the recovery panel at its
        /// head, then looked at. The panel's Power row is lit whole and the Link row's first lamp too, the board's
        /// labels show; then come away again.
        /// </summary>
        private IEnumerator LookAtTheBoard()
        {
            LiftStation lift = _gameplay.Lift;
            yield return Tap(_keyboard.dKey);
            yield return Until(() => lift.Lab.Facing == LabStation.Reader && !lift.Turning, LabTurnTimeout,
                "right turns the turntable to the reader");
            yield return Tap(_keyboard.dKey);
            yield return Until(() => lift.Lab.Facing == LabStation.Board && !lift.Turning, LabTurnTimeout,
                "and on to the tech board");
            yield return new WaitForSeconds(BoardSeconds);
            Capture("39a-the-recovery-panel");
            yield return Tap(_keyboard.wKey);
            Assert.IsTrue(lift.Lab.IsLooking, "forward looks at the board");
            Assert.IsTrue(_gameplay.Recovery.IsComplete(RecoveryTarget.Power), "Power 3/3 on the recovery panel");
            Assert.AreEqual(1, _gameplay.Recovery.LitLamps(RecoveryTarget.Link), "the mound mast's lamp: Link begins");
            yield return new WaitForSeconds(BoardSeconds);
            Capture("39b-the-tech-board");
            yield return Tap(_keyboard.sKey);
            Assert.IsFalse(lift.Lab.IsLooking, "back comes away from the board");
        }

        private static Relic RelicIn(SalvageSite site, string id)
        {
            foreach (Relic relic in site.Relics)
            {
                if (relic.Definition.Id == id)
                {
                    return relic;
                }
            }

            Assert.Fail($"{site.Id}'s heart holds no '{id}'");
            return null;
        }

        private IEnumerator Excavate(SalvageSite site, Relic relic)
        {
            Begin();
            Vector3 heart = relic.Site.Position;
            Assert.IsTrue(OpenSpotBy(heart, spot => NearestOfItsHeart(site, relic, spot), out Vector3 park,
                out Vector3 outward), $"open ground to dig from round the heart of {site.Id}");
            yield return CircleTo(site, Flat(heart + outward * CutLineUp), 0f);
            yield return DriveTo(Flat(heart + outward * CutLineUp), 2f, 0.6f, 40f, "a line-up facing the heart");
            yield return DriveTo(park, 1f, 0.35f, 30f, "the site's heart");
            yield return Until(() => _gameplay.Excavation.Candidate == relic, 4f, "the relic is in the beam's reach");
            Press(_keyboard.eKey);
            yield return Until(() => _events.ExcavationStarted.Count > 0, 6f, "the tractor beam takes hold");
            Assert.IsTrue(_rover.IsHeldStill, "07 is asked to hold still while the beam is held");
            float duration = _gameplay.Excavation.Tuning.DurationFor(relic.Definition.Mass);
            yield return new WaitForSeconds(duration * 0.5f);
            Capture("03-excavating-" + relic.Definition.Id);
            yield return Until(() => Surfaced(relic.Definition.Id), duration + 8f, "the relic surfaces");
            Release(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.IsFalse(_rover.IsHeldStill, "07 is free again once the relic is up");
            Assert.AreEqual(RelicState.Loose, relic.State);
            yield return new WaitForSeconds(1.5f);
            End($"Dig '{relic.Definition.Id}' from the heart of {site.Id}",
                $"lift took {duration:F1} s of beam for {relic.Definition.Mass:F0} kg");
        }

        /// <summary>
        /// The duck hopped out in front of 07, against the wreck: back out the way 07 came in, turn and creep up on it
        /// with the tether held until it latches.
        /// </summary>
        private IEnumerator LatchTether(Relic relic)
        {
            Begin();
            int latched = _events.TetherAttached.Count;
            Vector3 back = Flat(_rover.Position - relic.transform.position).normalized;
            yield return DriveTo(Flat(relic.transform.position + back * RetreatDistance), 3f, 0.6f, 40f,
                "a spot to turn back and aim from");
            Press(_mouse.rightButton);
            float deadline = Time.time + LatchTimeout;
            while (_gameplay.Tether.State != TetherAimState.Towing && Time.time < deadline)
            {
                _pilot.GoTo(relic.transform.position, LatchReach, 0.45f);
                yield return null;
            }

            _pilot.Target = null;
            Assert.AreEqual(TetherAimState.Towing, _gameplay.Tether.State,
                $"the tether latches onto the relic ({relic.State}, " +
                $"{Vector3.Distance(relic.transform.position, _rover.Position):F1} m from 07)");
            Assert.AreSame(relic, _gameplay.Tether.Towed);
            Assert.AreEqual(latched + 1, _events.TetherAttached.Count);
            End("Turn back and latch the tether", $"latched at {_gameplay.Tether.Length:F1} m");
        }

        /// <summary>
        /// Reel the relic in close, pull it straight out from the wreck and round it, wide, to its home side; then home
        /// to the apron in front of the belly lift.
        /// </summary>
        /// <summary>The towed relic is still on the tether; if not, where and how it let go.</summary>
        private void AssertTowHolds(string where)
        {
            if (_events.TetherReleased.Count == _releasesBeforeTow)
            {
                return;
            }

            TetherReleased released = _events.TetherReleased[_releasesBeforeTow].Value;
            Assert.Fail($"the tether holds all the way home, but it let go {where} " +
                        $"({(released.Snapped ? "snapped" : "released")} at {released.Position}, 07 at " +
                        $"{_rover.Position})");
        }

        private IEnumerator TowHome(SalvageSite site, Relic relic)
        {
            Begin();
            _releasesBeforeTow = _events.TetherReleased.Count;
            yield return Reel(1f, ReelSeconds);
            Assert.Less(_gameplay.Tether.Length, _gameplay.Tether.Tuning.MinLength + 0.5f, "reeled in close");
            Vector3 centre = Flat(site.Position);
            float ring = Anchor(site.Id).Radius + RingMargin + TowRingExtra;
            yield return DriveTo(centre + Flat(_rover.Position - centre).normalized * ring, 3f, 0.6f, 40f,
                "open ground out from the wreck, the relic in tow");
            Vector3 toHome = Flat(_context.Get<IWorldLayout>().BasePosition - site.Position).normalized;
            yield return CircleTo(site, centre + toHome * ring, TowRingExtra);
            Assert.AreEqual(_releasesBeforeTow, _events.TetherReleased.Count, "the relic follows 07 out round the wreck");
            yield return Reel(-1f, ReelSeconds);
            LiftStation lift = _gameplay.Lift;
            Vector3 stand = Flat(lift.RideSpot - Flat(lift.RideHeading).normalized * LiftLineUp);
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, stand);
            Vector3 basePad = _context.Get<IWorldLayout>().BasePosition;
            Vector3 outside = Flat(basePad) + Flat(_rover.Position - basePad).normalized * TowReelIn;
            yield return DriveTo(outside, WaypointArrive, 0.8f, 60f, "open ground just out from home");
            AssertTowHolds("on the long tow across the basin");
            yield return Reel(1f, ReelSeconds);
            yield return DriveTo(basePad, PadArrival, 0.6f, 60f, "the base pad, clear of the lander");
            AssertTowHolds("on the way to the base pad");
            _pilot.GoTo(stand, 2.5f, 0.6f);
            float deadline = Time.time + 90f;
            bool captured = false;
            while ((!_pilot.Arrived || lift.Phase != LiftPhase.Called) && Time.time < deadline)
            {
                AssertTowHolds("on the last stretch to the belly lift");
                if (!captured && _pilot.Distance < distance * 0.5f)
                {
                    Capture("04-towing-home");
                    captured = true;
                }

                yield return null;
            }

            _pilot.Target = null;
            Assert.AreSame(relic, _gameplay.Tether.Towed, "the relic is towed home to the belly lift");
            Assert.AreEqual(LiftPhase.Called, lift.Phase, "the lift asks for its stitch");
            Assert.IsFalse(_gameplay.Hints.TryGet(InteractionKind.Deposit, out _),
                "nothing to place outside: the keepsakes are up in the lab");
            yield return new WaitForSeconds(0.5f);
            Capture("05-at-the-lift");
            End($"Tow it home ({distance:F0} m)", $"tether at {_gameplay.Tether.Length:F1} m on arrival");
        }

        /// <summary>
        /// Facing the keepsake shelf, the Tether press lifts the relic that rode up into its outline (the button still
        /// held from the tow comes up first).
        /// </summary>
        private IEnumerator Deposit(Relic relic)
        {
            Begin();
            int materials = _events.MaterialsChanged.Count;
            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Deposit, out _), "the Deposit prompt is offered");
            Release(_mouse.rightButton);
            yield return null;
            Press(_mouse.rightButton);
            yield return Until(() => _events.RelicDeposited.Count > 0, 8f, "the relic floats onto the shelf");
            Release(_mouse.rightButton);
            RelicDeposited deposited = _events.RelicDeposited[0].Value;
            Assert.AreEqual(Duck, deposited.RelicId, "Kenji's bath duck is the first memory (step 4)");
            Assert.AreEqual(relic.Definition.Id, deposited.RelicId);
            Assert.AreEqual(1, deposited.DisplayedCount);
            Assert.AreEqual(RelicState.Displayed, relic.State);
            yield return null;
            Assert.AreEqual(materials, _events.MaterialsChanged.Count, "memories are not paid for");
            yield return new WaitForSeconds(1f);
            Capture("07-on-the-keepsake-shelf");
            End("Place it on the keepsake shelf", $"{deposited.DisplayedCount} memory on display");
        }

        private IEnumerator BuyTowerLevel(int level)
        {
            Begin();
            RadioTower tower = _gameplay.Tower;
            Assert.AreEqual(BasePower.StageFor(level - 1), _gameplay.Power.Stage, "power stops where the tower stands");
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.6f, 60f,
                "the base pad, clear of the lander");
            yield return DriveTo(tower.PadCentre, 1f, 0.6f, 60f, "the tower pad");
            yield return Until(() => _gameplay.Shop.IsAtStation, 3f, "07 is parked on the pad");
            string part = _gameplay.TowerParts.PartAt(level - 1).PartId;
            yield return Until(() => !_gameplay.TowerParts.NeedsPart(Tower, level),
                _gameplay.Progression.Tuning.PartFeedSeconds + 3f, $"07 hands {part} to the port");
            yield return Until(() => _gameplay.TowerPartField.Feeding < 0,
                _gameplay.Progression.Tuning.PartFeedSeconds + 1f, $"{part} glides into the hopper");
            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, $"the materials so far cover level {level}");
            int purchasesBefore = _events.SignalRadiusChanged.Count;
            int purchases = _events.UpgradePurchased.Count;
            int cues = _events.StationCued.Count;
            Press(_keyboard.eKey);
            yield return Until(() => _events.UpgradePurchased.Count > purchases, 15f,
                "holding confirm buys the level");
            Release(_keyboard.eKey);
            UpgradePurchased purchase = _events.UpgradePurchased[_events.UpgradePurchased.Count - 1].Value;
            Assert.AreEqual(Tower, purchase.UpgradeId);
            Assert.AreEqual(level, purchase.Level);
            Assert.Greater(_events.SignalRadiusChanged.Count, purchasesBefore);
            float radius = _events.SignalRadiusChanged[_events.SignalRadiusChanged.Count - 1].Value.Radius;
            Assert.AreEqual(_gameplay.Upgrades.Find(Tower).SignalRadiusAt(level), radius, 1e-3f);
            Assert.IsTrue(tower.Feeding, "07's beam feeds the service port's hopper");
            yield return new WaitForSeconds(0.4f);
            Vector3 port = tower.HopperMouth;
            Vector3 front = Flat(tower.PadCentre - port).normalized;
            Vector3 aside = Vector3.Cross(Vector3.up, front);
            Review(port + front * PortViewDistance + aside * PortViewSide + Vector3.up * PortViewHeight, port,
                $"07a-tower-port-feed-l{level}");
            yield return Until(() => Cued(StationCue.StitchStarted, cues), 6f, "the hatch opens and the beam stitches");
            yield return new WaitForSeconds(tower.Tuning.FlareDuration);
            Review(port + front * PortViewDistance * 1.6f + aside * PortViewSide + Vector3.up * PortViewHeight,
                Vector3.Lerp(port, tower.BeaconPosition, 0.5f), $"07b-tower-port-stitch-l{level}");
            yield return Until(() => !tower.Crafting, 6f, "the hatch shuts and the moment ends");
            Recipe recipe = _gameplay.Upgrades.Find(Tower).Levels[level - 1].Recipe;
            var beats = new List<StationCue> { StationCue.FeedStarted };
            foreach (int units in new[] { recipe.Metal, recipe.Wiring, recipe.Optics })
            {
                if (units > 0)
                {
                    beats.Add(StationCue.BundleDropped);
                }
            }

            beats.Add(StationCue.Fed);
            beats.Add(StationCue.HatchOpened);
            beats.Add(StationCue.StitchStarted);
            beats.Add(StationCue.HatchClosed);
            AssertCues(cues, Tower, beats.ToArray());
            Assert.AreEqual(BasePower.StageFor(level), _gameplay.Power.Stage, "power reaches one stage further");
            Assert.AreEqual(level >= 2, _gameplay.Workshop.Awake, "the Rover Bay wakes with the second level only");
            yield return null;
            Assert.AreEqual(level, _gameplay.Recovery.LitLamps(RecoveryTarget.Power),
                "the recovery panel's Power row lights a lamp per level");
            Assert.AreEqual(level == _gameplay.Upgrades.Find(Tower).MaxLevel,
                _gameplay.Recovery.IsComplete(RecoveryTarget.Power), "Power is restored with the last level only");
            yield return new WaitForSeconds(1f);
            Capture($"07-tower-awake-l{level}");
            End($"Park on the pad, hand in {part} and craft tower level {level}",
                $"signal radius {radius:F0} m, recipe {recipe}, power {_gameplay.Power.Stage}");
        }

        /// <summary>
        /// The base as a fresh game opens (docs/features/M3-16, M3-21): no power, the bay asleep with its lamps dark, no
        /// sheet on Kenji's board (Hover-Jump's tube waits at the chasm's lip), the recovery panel dark, nothing to buy.
        /// </summary>
        private void AssertAsleep()
        {
            Assert.AreEqual(BasePowerStage.Asleep, _gameplay.Power.Stage, "a fresh game opens fully dark");
            Assert.IsFalse(_gameplay.Workshop.Awake, "the Rover Bay sleeps");
            Assert.AreEqual(0f, _gameplay.Workshop.LampLevel, 1e-3f, "its lamps are dark");
            Assert.IsFalse(_gameplay.Blueprints.IsPinned(HoverJump), "no sheet is pinned from the start");
            Assert.IsTrue(_gameplay.GroundTubes.IsResting(HoverJump), "Hover-Jump's tube glints at the chasm's lip");
            Assert.AreEqual(0, _gameplay.Tech.KnownCount, "the porthole's lamps are dark");
            Assert.AreEqual(0, _gameplay.Recovery.LitLamps(RecoveryTarget.Power), "the recovery panel is dark");
            Assert.IsFalse(_gameplay.Blueprints.IsPinned(CargoCradle), "the Cradle's sheet is still out there");
            Assert.AreEqual(PurchaseResult.NotPinned, _gameplay.Shop.Purchase(HoverJump), "no sheet, no kit");
            Assert.AreEqual(PurchaseResult.NotAtStation, _gameplay.Shop.Purchase(Tower), "nothing to buy yet");
        }

        /// <summary>The soft "next" line shows <paramref name="key"/> (M3-16 step flow).</summary>
        private IEnumerator ExpectStep(string key, string why)
        {
            yield return Until(() => _gameplay.Progression.NextStep == key, 2f,
                $"the next line is '{key}' ({why})");
        }

        /// <summary>
        /// Home is powered but the bay is not: at its mouth it offers nothing (no pad, no prompt, no panel), and coming
        /// near it, it answers "not yet".
        /// </summary>
        private IEnumerator TheBaySleepsUntilTheTowerWakesIt()
        {
            Begin();
            Workshop bay = _gameplay.Workshop;
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 90f,
                "the base pad, clear of the lander");
            Vector3 entrance = Flat(bay.PadCentre) + Flat(bay.BayForward).normalized * BayApproach;
            yield return DriveTo(entrance, 1.5f, 0.6f, 60f, "the mouth of the sleeping Rover Bay");
            Assert.IsFalse(bay.Awake, "tower level 1 powers home, not the bay");
            Assert.IsFalse(_gameplay.Shop.IsAtStation, "no panel");
            Assert.IsFalse(_gameplay.Hints.TryGet(InteractionKind.Upgrade, out _), "no prompt");
            Assert.AreEqual(PurchaseResult.NotAtStation, _gameplay.Shop.Purchase(CargoCradle), "nothing to buy");
            yield return Until(() => AnsweredNotYet(0, ProgressionKeys.NotYetBay), 2f,
                "the sleeping bay has answered 'not yet' as 07 came near (once a session)");
            End("The Rover Bay sleeps until the tower wakes it", "no pad, no prompt, a soft 'not yet'");
        }

        private bool AnsweredNotYet(int from, string reasonKey)
        {
            for (int i = from; i < _events.StationNotYet.Count; i++)
            {
                if (_events.StationNotYet[i].Value.ReasonKey == reasonKey)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator FindTilly()
        {
            Begin();
            Friend tilly = _gameplay.Friends.Find(TillyId);
            Assert.IsNotNull(tilly, "Tilly lies somewhere in the basin");
            Vector3 site = tilly.Site.Position;
            float fromHome = SurfaceRules.HorizontalDistance(site, _context.Get<IWorldLayout>().BasePosition);
            Assert.That(fromHome, Is.InRange(60f, 110f), "Tilly lies 60-110 m from home");
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 60f, "the base pad");
            Vector3 toSite = site - _rover.Position;
            toSite.y = 0f;
            Vector3 near = site - toSite.normalized * TillyPingDistance;
            yield return DriveTo(near, 3f, 1f, 60f, "the rim of Tilly's crater");
            int answersBefore = _events.FriendAnswered.Count;
            yield return Until(() => _events.FriendAnswered.Count > answersBefore,
                _gameplay.Sonar.Tuning.RestSeconds + 6f, "07 rests and listens: a broken chirp answers");
            Assert.AreEqual(TillyId, _events.FriendAnswered[_events.FriendAnswered.Count - 1].Value.FriendId);
            yield return new WaitForSeconds(1f);
            Capture("08-tilly-answers");
            End($"Find Tilly ({fromHome:F0} m from home)", "her broken chirp answered the ping");
        }

        private IEnumerator GatherTillyParts()
        {
            Begin();
            Friend tilly = _gameplay.Friends.Find(TillyId);
            int total = tilly.Progress.PartCount;
            float travelled = 0f;
            while (tilly.Progress.Collected < total)
            {
                int next = NearestMissingPart(tilly);
                Vector3 part = tilly.PartRest[next];
                travelled += SurfaceRules.HorizontalDistance(_rover.Position, part);
                int before = tilly.Progress.Collected;
                yield return DriveTo(new Vector3(part.x, 0f, part.z), 1.5f, 0.8f, 60f, "one of Tilly's parts");
                if (next == tilly.BuriedPart)
                {
                    yield return DigTillysLastPart();
                }
                else
                {
                    yield return PickupPress.Take(this, _gameplay, 4f, "one of Tilly's parts");
                }
                yield return Until(() => tilly.Progress.Collected > before, 6f, "the part drifts into 07");
            }

            Assert.AreEqual(total, _events.FriendPartCollected.Count);
            FriendPartCollected last = _events.FriendPartCollected[total - 1].Value;
            Assert.AreEqual(total, last.Collected);
            Assert.AreEqual(total, last.Total);
            Assert.IsTrue(tilly.Progress.CanRepair);
            End("Gather Tilly's parts", $"{total} parts over {travelled:F0} m of driving");
        }

        /// <summary>
        /// Her last part lies half under the dust (first-hour v2 §4): driving over it is not enough; the line says so,
        /// the Dig prompt shows, and holding the beam digs it free.
        /// </summary>
        private IEnumerator DigTillysLastPart()
        {
            yield return Until(() => _gameplay.Progression.NextStep == ProgressionKeys.NextDig, 3f,
                "the line: her last part is under the dust");
            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 stops by the buried part");
            yield return Until(() => _gameplay.Hints.TryGet(InteractionKind.Excavate, out _), 3f, "the Dig prompt");
            Press(_keyboard.eKey);
            yield return new WaitForSeconds(_gameplay.Friends.Tuning.PartDigSeconds + 0.5f);
            Release(_keyboard.eKey);
        }

        private IEnumerator RepairTilly()
        {
            Begin();
            Friend tilly = _gameplay.Friends.Find(TillyId);
            Vector3 site = tilly.Site.Position;
            Vector3 fromSite = _rover.Position - site;
            fromSite.y = 0f;
            yield return DriveTo(site + fromSite.normalized * RepairStandOff, 1.5f, 0.5f, 60f, "Tilly");
            yield return Until(() => _gameplay.Hints.TryGet(InteractionKind.Repair, out _), 3f,
                "the Repair prompt is offered next to her");
            Press(_keyboard.eKey);
            yield return Until(() => _events.FriendRepairStarted.Count > 0, 3f, "holding Interact starts the repair");
            Release(_keyboard.eKey);
            Assert.IsTrue(_rover.IsHeldStill, "07 holds still while its beam stitches her");
            yield return new WaitForSeconds(tilly.Definition.RepairDuration * 0.5f);
            Capture("09-tilly-stitching");
            Witness(tilly.Position, "09b-tilly-stitching-closeup");
            RepairSequence sequence = RepairSequence.For(tilly.Definition, _gameplay.Friends.Tuning);
            yield return Until(() => _events.FriendRepaired.Count > 0, sequence.Duration + 3f,
                "she boots up and looks at 07");
            Assert.IsFalse(_rover.IsHeldStill, "07 is free again");
            Assert.AreEqual(FriendState.Awake, tilly.Progress.State);
            yield return new WaitForSeconds(0.5f);
            Capture("10-tilly-awake");
            Witness(tilly.Position, "10b-tilly-awake-closeup");
            End("Repair Tilly", $"stitching {tilly.Definition.RepairDuration:F1} s, boot-up " +
                                $"{sequence.Duration - sequence.StitchEnd:F1} s");
        }

        private IEnumerator DriveHomeWithTilly()
        {
            Begin();
            Friend tilly = _gameplay.Friends.Find(TillyId);
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, home);
            float farthest = 0f;
            _pilot.GoTo(home, PadArrival, 0.8f);
            float deadline = Time.time + 60f;
            while (!_pilot.Arrived && Time.time < deadline)
            {
                farthest = Mathf.Max(farthest, Vector3.Distance(tilly.Position, _rover.Position));
                yield return null;
            }

            _pilot.Target = null;
            Assert.IsTrue(_pilot.Arrived, "07 reaches home");
            Assert.Less(farthest, _gameplay.Friends.Tuning.ReappearDistance,
                "she comes along all the way home (never lost, even when she stops to spot something)");
            yield return Until(() => _events.FriendGreeted.Count > 0, 10f, "she greets 07 coming home");
            Capture("11-tilly-greets");
            Witness(tilly.Position, "11b-tilly-greets-closeup");
            yield return Until(() => tilly.Activity == FriendActivity.Home || tilly.Activity == FriendActivity.Napping,
                20f, "she settles into her home life");
            End($"Drive home with Tilly ({distance:F0} m)", $"she stayed within {farthest:F0} m of 07, then greeted");
        }

        /// <summary>
        /// Out toward the garage, the route's next trip: Tilly spots it on the way, unless it already shows on the
        /// sonar (a ping, or Tilly on the way home, can find it first on the reshaped basin).
        /// </summary>
        private IEnumerator TillySpotsOnTheNextTrip(SalvageSite undiscovered)
        {
            Begin();
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            Assert.IsNotNull(undiscovered, "the garage stands out on the basin");
            Assert.IsTrue(undiscovered.AnswersSonar, "the garage answers the sonar");
            Assert.IsTrue(undiscovered.Gate.IsOpen(_context.Get<IRoverAbilities>()), "the garage is open to 07");
            if (undiscovered.Discovered)
            {
                End("Next trip: the garage is already on the sonar",
                    Spotted(undiscovered) ? "Tilly spotted it on the way home" : "a ping reached it");
                yield break;
            }

            Vector3 toSite = undiscovered.Position - home;
            toSite.y = 0f;
            Vector3 stand = undiscovered.Position - toSite.normalized * SpotStandOff;
            _pilot.GoTo(stand, 3f, 1f);
            float deadline = Time.time + 120f;
            while (!Spotted(undiscovered) && Time.time < deadline)
            {
                yield return null;
            }

            _pilot.Target = null;
            Assert.IsTrue(Spotted(undiscovered), $"Tilly spots '{undiscovered.Id}' on the next trip");
            Assert.IsTrue(undiscovered.Discovered, "it shows on 07's sonar without a ping");
            yield return new WaitForSeconds(1.5f);
            Capture("12-tilly-spots");
            End($"Next trip: Tilly spots '{undiscovered.Id}'",
                $"{_events.FriendSpotted.Count} spot(s) on the way");
        }

        private IEnumerator BuyHoverJumpAtTheBench()
        {
            Begin();
            Workshop bench = _gameplay.Workshop;
            var abilities = _context.Get<IRoverAbilities>();
            Assert.IsFalse(abilities.Has(RoverAbility.HoverJump), "07 cannot leap before the bay");
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 90f,
                "the base pad, clear of the lander");
            yield return DriveIntoTheBay(bench);
            yield return Until(() => _gameplay.Blueprints.IsDrawn(CargoCradle),
                _gameplay.Progression.Tuning.PlotterDrawSeconds + 3f,
                "the woken bay's plotter draws the Cradle's sheet that waited for it (M3-19)");
            Assert.AreSame(bench.Definition, _gameplay.Shop.StationUpgrade, "the bay offers Hover-Jump");
            Assert.AreEqual(UpgradeStationKind.Workshop, _gameplay.Shop.StationUpgrade.Station);

            int charges = _events.RoverJumpCharged.Count;
            _pilot.JumpHeld = true;
            yield return new WaitForSeconds(_rover.Tuning.HoverJump.ChargeTime + ChargeMargin);
            _pilot.JumpHeld = false;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(charges, _events.RoverJumpCharged.Count, "holding Jump does nothing before the bay");
            Assert.AreEqual(0, _events.RoverJumped.Count);

            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "the depot's materials cover Hover-Jump");
            int purchases = _events.UpgradePurchased.Count;
            int cues = _events.StationCued.Count;
            int fittings = _events.RoverBayFitting.Count;
            Press(_keyboard.eKey);
            yield return Until(() => _events.UpgradePurchased.Count > purchases, 15f, "holding confirm buys it");
            Release(_keyboard.eKey);
            UpgradePurchased purchase = _events.UpgradePurchased[_events.UpgradePurchased.Count - 1].Value;
            Assert.AreEqual(HoverJump, purchase.UpgradeId);
            Assert.AreEqual(1, purchase.Level);
            Assert.IsTrue(abilities.Has(RoverAbility.HoverJump), "07 can leap now");
            Assert.AreEqual(CargoCradle, bench.Definition.Id, "the bay offers the Cargo Cradle next");
            Assert.IsTrue(bench.Feeding, "07's beam feeds the bay's hopper");
            yield return new WaitForSeconds(0.45f);
            Assert.Greater(bench.BundlesInFlight, 0, "the recipe's bundles fly into the hopper");
            ReviewBay(bench, "13a-bay-hopper-feed");
            yield return Until(() => _events.RoverBayFitting.Count > fittings, 3f, "the bay starts fitting the kit");
            AssertCues(cues, HoverJump, StationCue.FeedStarted, StationCue.BundleDropped, StationCue.BundleDropped,
                StationCue.BundleDropped, StationCue.Fed);
            Assert.AreEqual(HoverJump, _events.RoverBayFitting[_events.RoverBayFitting.Count - 1].Value.UpgradeId);
            Assert.IsTrue(bench.Working, "the bay works: its lights are up");
            Assert.AreEqual(0, bench.SparkCount, "no sparks while the arms are folded");
            yield return Until(() => Fitted(HoverJump), BayFitTimeout, "the floor arm sets the coils on 07");
            Assert.Greater(bench.PitLevel, 0.5f, "the pit and the floor arm's tip light the coils under 07's belly");
            Vector3 bayFront = Flat(bench.BayForward).normalized;
            Review(bench.PadCentre + bayFront * PitViewDistance + Vector3.up * PitViewHeight,
                bench.PadCentre + Vector3.up * PitViewTarget, "13b0-bay-coils-pit-light");
            yield return new WaitForSeconds(SparkDelay);
            Assert.Greater(bench.FloorSparkCount, 0, "weld sparks fly from the floor arm's tip as the coils go on");
            Assert.Greater(bench.LightLevel, bench.Tuning.LightIdle * 2f, "the work lights light the bay");
            ReviewBay(bench, "13b-bay-sparks");
            yield return new WaitForSeconds(1.5f);
            Capture("13-bay-hover-jump");
            IMaterialStock stock = _gameplay.Materials;
            End("Park in Kenji's Rover Bay and craft Hover-Jump", $"recipe " +
                $"{_gameplay.Upgrades.Find(HoverJump).Levels[0].Recipe}, stock metal {stock.Metal}, wiring " +
                $"{stock.Wiring}, optics {stock.Optics}");
        }

        /// <summary>
        /// Still in the bay: craft the Cargo Cradle from the depot's metal, see the rack on 07 and back out.
        /// </summary>
        private IEnumerator CraftTheCargoCradle()
        {
            Begin();
            Workshop bench = _gameplay.Workshop;
            var seat = _context.Get<IRoverCargoSeat>();
            Assert.IsFalse(seat.IsFitted, "no rack on 07 yet");
            yield return Until(() => bench.Occupied, 3f, "07 is parked on the bay's turntable");
            Assert.AreSame(bench.Definition, _gameplay.Shop.StationUpgrade, "the bay offers the Cargo Cradle");
            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "the depot's metal covers the Cargo Cradle");
            int purchases = _events.UpgradePurchased.Count;
            Press(_keyboard.eKey);
            yield return Until(() => _events.UpgradePurchased.Count > purchases, 15f, "holding confirm crafts it");
            Release(_keyboard.eKey);
            Assert.AreEqual(CargoCradle, _events.UpgradePurchased[_events.UpgradePurchased.Count - 1].Value.UpgradeId);
            Assert.IsTrue(_context.Get<IRoverAbilities>().Has(RoverAbility.CargoCradle));
            yield return Until(() => Fitted(CargoCradle), BayFitTimeout, "an arm sets the rack on 07");
            yield return new WaitForSeconds(SparkDelay);
            Assert.Greater(bench.SparkCount - bench.FloorSparkCount, 0, "weld sparks fly from the fitting arm's tip");
            yield return Until(() => seat.IsFitted, 6f, "the rack is fitted on 07");
            yield return new WaitForSeconds(1.5f);
            Vector3 back = -Flat(_rover.Rotation * Vector3.forward).normalized;
            Review(_rover.Position + back * 5f + Vector3.up * 2.5f, seat.Position, "13c-cargo-cradle-fitted");
            yield return Until(() => !bench.Feeding, 3f, "the bay has fitted the rack");
            yield return BackOutOfTheBay(bench);
            IMaterialStock stock = _gameplay.Materials;
            End("Craft the Cargo Cradle in the bay", $"recipe " +
                $"{_gameplay.Upgrades.Find(CargoCradle).Levels[0].Recipe}, stock metal {stock.Metal}, wiring " +
                $"{stock.Wiring}, optics {stock.Optics}");
        }

        /// <summary>
        /// The gnome hopped out in front of 07: back out, turn and creep up on it with the tether button held, which
        /// lifts it gently into the rack instead of towing it.
        /// </summary>
        private IEnumerator StowInTheCradle(Relic relic)
        {
            Begin();
            CargoCradle cradle = _gameplay.Cradle;
            int tethers = _events.TetherAttached.Count;
            Vector3 back = Flat(_rover.Position - relic.transform.position).normalized;
            yield return DriveTo(Flat(relic.transform.position + back * RetreatDistance), 3f, 0.6f, 40f,
                "a spot to turn back and aim from");
            Press(_mouse.rightButton);
            float deadline = Time.time + LatchTimeout;
            while (cradle.Carried != relic && Time.time < deadline)
            {
                _pilot.GoTo(relic.transform.position, LatchReach, 0.45f);
                yield return null;
            }

            _pilot.Target = null;
            Release(_mouse.rightButton);
            Assert.AreSame(relic, cradle.Carried, $"the press lifts the relic into the rack ({relic.State}, " +
                $"{Vector3.Distance(relic.transform.position, _rover.Position):F1} m from 07)");
            Assert.AreEqual(tethers, _events.TetherAttached.Count, "instead of towing it");
            yield return new WaitForSeconds(0.3f);
            Capture("13d-cradle-lifting");
            yield return Until(() => cradle.IsSettled, 6f, "it settles into the rack");
            Assert.AreEqual(relic.Definition.Id, _events.RelicStowed[_events.RelicStowed.Count - 1].Value.RelicId);
            Witness(relic.transform.position, "13e-relic-in-the-cradle");
            End("Lift it into the Cargo Cradle", "a press of the tether button, a gentle float, a soft settle");
        }

        /// <summary>
        /// Home with the relic riding in the rack: out round the wreck, across the basin, up to the shelf, and a press
        /// sets it down onto it.
        /// </summary>
        private IEnumerator CarryHomeInTheCradle(SalvageSite site, Relic relic)
        {
            Begin();
            CargoCradle cradle = _gameplay.Cradle;
            var seat = _context.Get<IRoverCargoSeat>();
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            Vector3 toHome = Flat(home - site.Position).normalized;
            yield return CircleTo(site, Flat(site.Position + toHome * RetreatDistance), 0f);
            LiftStation lift = _gameplay.Lift;
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, lift.RideSpot);
            _pilot.GoTo(home, PadArrival, 0.9f);
            float deadline = Time.time + 90f;
            bool captured = false;
            while (!_pilot.Arrived && Time.time < deadline)
            {
                Assert.AreSame(relic, cradle.Carried, "it rides home in the rack, over every bump");
                if (!captured && _pilot.Distance < distance * 0.5f)
                {
                    Capture("13f-carrying-home");
                    captured = true;
                }

                yield return null;
            }

            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 comes to rest on the base pad");
            yield return null;
            Assert.Less(Vector3.Distance(relic.transform.position,
                seat.Position + seat.Rotation * Vector3.up * relic.RestHeight), 0.01f, "seated in the rack");
            yield return DriveOntoTheLift("the belly lift's platform");
            yield return Until(() => lift.Phase == LiftPhase.Inside, LiftPhaseTimeout,
                "resting on the platform carries 07 up into the lab, the gnome in the rack");
            yield return Until(() => cradle.CanUnload, LabTurnTimeout,
                "the turntable turns to the keepsake shelf: the rack is in reach of it");
            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Deposit, out _), "the Deposit prompt is offered");
            int deposits = _events.RelicDeposited.Count;
            Press(_mouse.rightButton);
            yield return Until(() => _events.RelicDeposited.Count > deposits, 8f,
                "a press sets it down onto the shelf");
            Release(_mouse.rightButton);
            Assert.AreEqual(relic.Definition.Id, _events.RelicDeposited[deposits].Value.RelicId);
            Assert.AreEqual(RelicState.Displayed, relic.State);
            Assert.IsNull(cradle.Carried, "the rack is empty again");
            yield return new WaitForSeconds(1f);
            Capture("13g-gnome-on-the-shelf");
            yield return RideDown("13h-down-again");
            End($"Carry it home in the Cargo Cradle and up the lift ({distance:F0} m)",
                $"{_events.RelicDeposited[deposits].Value.DisplayedCount} memories on display");
        }

        /// <summary>
        /// The park spot <paramref name="spot"/> is nearer <paramref name="relic"/> than any other relic still in the
        /// same heart, so the beam takes the one meant.
        /// </summary>
        private static bool NearestOfItsHeart(SalvageSite site, Relic relic, Vector3 spot)
        {
            float mine = SurfaceRules.HorizontalDistance(spot, relic.Site.Position);
            foreach (Relic other in site.Relics)
            {
                if (other != relic && other.CanBeLifted &&
                    SurfaceRules.HorizontalDistance(spot, other.Site.Position) < mine + HeartMargin)
                {
                    return false;
                }
            }

            return true;
        }

        private IEnumerator FirstLeap()
        {
            Begin();
            ChargingDock dock = _gameplay.Home.Dock;
            Vector3 lane = Flat(dock.Rotation * Vector3.forward).normalized;
            yield return DriveTo(Flat(dock.Position) + lane * LeapSpotAhead, 1.5f, 0.6f, 60f,
                "open ground on the base pad, ahead of the dock");
            float ground = _rover.Position.y;
            int charges = _events.RoverJumpCharged.Count;
            _pilot.JumpHeld = true;
            yield return new WaitForSeconds(_rover.Tuning.HoverJump.ChargeTime + ChargeMargin);
            Assert.Greater(_events.RoverJumpCharged.Count, charges, "07 crouches and charges");
            Assert.AreEqual(0, _events.RoverJumped.Count, "nothing leaps while Jump is held");
            _pilot.JumpHeld = false;
            yield return Until(() => _events.RoverJumped.Count > 0, 1f, "releasing Jump leaps");
            float strength = _events.RoverJumped[0].Value.Strength;
            float takeOff = Time.time;
            yield return Until(() => !_rover.IsGrounded, 1f, "07 leaves the ground");
            float apex = 0f;
            bool captured = false;
            while (!_rover.IsGrounded && Time.time < takeOff + 15f)
            {
                apex = Mathf.Max(apex, _rover.Position.y - ground);
                if (!captured && _rover.Velocity.y < 0f)
                {
                    Capture("14-first-leap-apex");
                    captured = true;
                }

                yield return null;
            }

            float hang = Time.time - takeOff;
            Assert.IsTrue(_rover.IsGrounded, "07 lands softly");
            Assert.Greater(strength, 0.95f, "a full charge");
            Assert.Greater(apex, MinLeapApex, "a big, floaty leap");
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 settles after landing");
            yield return new WaitForSeconds(1f);
            Capture("15-after-the-leap");
            End("First Hover-Jump on the base pad", $"strength {strength:F2}, apex {apex:F1} m, hang {hang:F1} s");
        }

        private IEnumerator LeapTheChasm()
        {
            Begin();
            WorldAnchor lip = Anchor(WorldAnchorIds.CanyonLip);
            int leaps = _events.RoverJumped.Count;
            _pilot.GoTo(lip.Position + lip.Forward * (ChasmWidth * 2f), 1f, 1f);
            float fixedStep = Time.fixedDeltaTime;
            int holdSteps = Mathf.RoundToInt(ChasmCharge * _rover.Tuning.HoverJump.ChargeTime / fixedStep) + 1;
            float deadline = Time.time + 30f;
            while (Along(lip) < -(ReleaseLead + _rover.Speed * holdSteps * fixedStep) && Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
            }

            float speed = _rover.Speed;
            _pilot.JumpHeld = true;
            for (int i = 0; i < holdSteps; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            _pilot.JumpHeld = false;
            yield return Until(() => _events.RoverJumped.Count > leaps, 1f, "07 leaps off the lip");
            yield return Until(() => !_rover.IsGrounded, 1f, "07 is in the air");
            bool captured = false;
            float takeOff = Time.time;
            while (!_rover.IsGrounded && Time.time < takeOff + 15f)
            {
                if (!captured && _rover.Velocity.y < 0f)
                {
                    Capture("17-leaping-the-chasm");
                    captured = true;
                }

                yield return null;
            }

            _pilot.Target = null;
            float landed = Along(lip);
            Assert.Greater(landed, ChasmWidth, "07 comes down past the chasm's far face");
            Assert.Greater(_rover.Position.y, lip.Position.y + ApronRise, "up on the landing apron");
            yield return Until(() => _rover.Speed < StopSpeed, 8f, "07 settles on the apron");
            End("Leap the chasm", $"{speed:F1} m/s at the lip, strength " +
                                  $"{_events.RoverJumped[_events.RoverJumped.Count - 1].Value.Strength:F2}, " +
                                  $"touchdown {landed:F1} m out");
        }

        private IEnumerator FindBell()
        {
            Begin();
            Friend bell = _gameplay.Friends.Find(BellId);
            Assert.IsNotNull(bell, "Bell lies somewhere in the canyon");
            WorldAnchor terminus = Anchor(WorldAnchorIds.CanyonTerminus);
            Assert.Less(SurfaceRules.HorizontalDistance(bell.Site.Position, terminus.Position), terminus.Radius + 1f,
                "at the terminus");
            WorldAnchor ledge = Anchor(WorldAnchorIds.CanyonLedge);
            CassetteSite ledgeTape = _gameplay.Cassettes.Site(CassetteIndex(SlowOrbit));
            Review(ledge.Position - ledge.Forward * 8f + Vector3.up * 2.5f, ledgeTape.Position, "18-ledge-tape");
            Vector3 site = bell.Site.Position;
            Vector3 aside = Vector3.Cross(Vector3.up, bell.Site.Facing);
            Review(site + bell.Site.Facing * 5.5f + Vector3.up * 2.2f, site + Vector3.up * 0.5f,
                "19-bell-at-the-terminus");
            Review(site + aside * 4f + bell.Site.Facing * 1.5f + Vector3.up * 1.2f, site + Vector3.up * 0.5f,
                "19c-bell-against-the-wall");
            float travelled = 0f;
            yield return DriveAlong(terminus.Position, "the terminus", value => travelled = value);
            yield return Until(() => LogFound("ro_1"), 3f, "Ro's log cache opens beside her");
            yield return Until(() => Collected(AfterDark), 6f, "Lumen After Dark, Vol. 1 drifts into 07");
            yield return Until(() => bell.Progress.ItemsCollected == 1, 1f,
                "her tape lights her fourth lamp: it is the fourth thing she needs");
            Capture("19b-terminus");
            End($"Find Bell at the terminus ({travelled:F0} m in)", "Ro's first log and her tape picked up");
        }

        private IEnumerator GatherBellsParts()
        {
            Begin();
            Friend bell = _gameplay.Friends.Find(BellId);
            int total = bell.Progress.PartCount;
            float travelled = 0f;
            bool reviewed = false;
            while (bell.Progress.Collected < total)
            {
                int next = NearestMissingPart(bell);
                Vector3 part = bell.PartRest[next];
                if (!reviewed)
                {
                    WorldAnchor alcove = Anchor(WorldAnchorIds.CanyonAlcovePrefix + next);
                    Review(alcove.Position - alcove.Forward * 7f + Vector3.up * 2.5f, part, "20-bell-part-alcove");
                    reviewed = true;
                }

                int before = bell.Progress.Collected;
                yield return DriveAlong(part, "one of Bell's parts", value => travelled += value);
                yield return PickupPress.Take(this, _gameplay, 4f, "one of Bell's parts");
                yield return Until(() => bell.Progress.Collected > before, 6f, "the part drifts into 07");
            }

            Assert.IsTrue(bell.Progress.CanRepair, "three parts and the tape: she can be repaired");
            End("Gather Bell's parts from the alcoves", $"{total} parts over {travelled:F0} m of canyon");
        }

        private IEnumerator RepairBell()
        {
            Begin();
            Friend bell = _gameplay.Friends.Find(BellId);
            Vector3 site = bell.Site.Position;
            yield return DriveAlong(site + bell.Site.Facing * RepairStandOff, "Bell", null);
            yield return Until(() => _gameplay.Hints.TryGet(InteractionKind.Repair, out _), 3f,
                "the Repair prompt is offered in front of her");
            Press(_keyboard.eKey);
            yield return Until(() => _events.FriendRepairStarted.Count > 1, 3f, "holding Interact starts her repair");
            Release(_keyboard.eKey);
            Assert.IsTrue(_rover.IsHeldStill, "07 holds still while its beam stitches her");
            BellRepairSequence sequence = BellRepairSequence.For(bell.Definition, _gameplay.Friends.BellTuning);
            float began = _events.FriendRepairStarted[_events.FriendRepairStarted.Count - 1].Time;
            Vector3 eye = site + bell.Site.Facing * 4.5f + Vector3.Cross(Vector3.up, bell.Site.Facing) * 2.5f +
                          Vector3.up * 1.8f;
            yield return new WaitForSeconds(began + sequence.StitchEnd * 0.5f - Time.time);
            Review(eye, site + Vector3.up * 0.5f, "21-bell-stitching");
            yield return new WaitForSeconds(began + (sequence.StitchEnd + sequence.TapeIn) * 0.5f - Time.time);
            Review(eye, site + Vector3.up * 0.5f, "22-bell-tape-slides-in");
            yield return Until(() => _events.FriendRepaired.Count > 1, sequence.Duration + 3f, "she stands up");
            Assert.AreEqual(BellId, _events.FriendRepaired[_events.FriendRepaired.Count - 1].Value.FriendId);
            Assert.IsFalse(_rover.IsHeldStill, "07 is free again");
            Assert.IsTrue(_context.Get<IRadioProgram>().DialUnlocked, "her gift: the radio dial");
            Assert.IsTrue(HasCue(BellCue.TapeSlotted) && HasCue(BellCue.NeedleSwept),
                "the tape clicks, the needle sweeps");
            Review(eye, site + Vector3.up * 0.8f, "23-bell-standing");
            yield return new WaitForSeconds(_gameplay.Friends.BellTuning.DanceDuration * 0.5f);
            Review(eye, site + Vector3.up * 0.8f, "24-bell-two-step");
            End("Repair Bell", $"stitching {bell.Definition.RepairDuration:F1} s, tape, dial and standing up " +
                               $"{sequence.Duration - sequence.StitchEnd:F1} s");
        }

        private IEnumerator TurnBellsDial()
        {
            Begin();
            Friend bell = _gameplay.Friends.Find(BellId);
            var cabinet = (RadioCabinetBody)bell.Body;
            IRadioProgram radio = _context.Get<IRadioProgram>();
            yield return DriveTo(cabinet.DialFront, 1f, 0.5f, 60f, "the spot in front of Bell's dial");
            yield return Until(() => _gameplay.Hints.Primary.Kind == InteractionKind.Tune, 3f,
                "the Tune prompt is offered in front of her");
            var heard = new List<string>();
            for (int turn = 0; turn < 3; turn++)
            {
                int cues = _events.BellCued.Count;
                Press(_keyboard.eKey, queueEventOnly: true);
                yield return null;
                yield return null;
                Release(_keyboard.eKey);
                Assert.IsTrue(_gameplay.Friends.TappingDial, "07's beam taps her dial first");
                if (turn == 0)
                {
                    yield return new WaitForSeconds(0.15f);
                    Transform corner = bell.Home;
                    Review(corner.position + corner.forward * 2.2f + corner.right * 1.4f + Vector3.up * 1.6f,
                        corner.position + Vector3.up * 1.1f, "26a-dial-tap");
                }

                yield return new WaitForSeconds(_gameplay.Friends.BellTuning.DialTapTime + DialPause);
                Assert.AreEqual(BellCue.DialTapped, _events.BellCued[cues].Value.Cue, "the beam taps her knob");
                Assert.AreEqual(BellCue.DialTurned, _events.BellCued[cues + 1].Value.Cue, "then she turns it");
                heard.Add(radio.Channel == RadioChannel.TapeDeck ? radio.Channel + " (" + radio.SelectedTape + ")"
                    : radio.Channel.ToString());
                Assert.AreEqual(_gameplay.Friends.BellTuning.Detent(radio.Channel), cabinet.Life.Needle, 2f,
                    "the needle eases onto the station's detent");
                if (turn == 0)
                {
                    Transform corner = bell.Home;
                    Review(corner.position + corner.forward * 1.6f + Vector3.up * 1.4f,
                        corner.position + Vector3.up * 1.2f, "26-dial-tape-deck");
                }
            }

            CollectionAssert.AreEqual(new[] { "TapeDeck (" + AfterDark + ")", "QuietHours", "LumenAfterDark" }, heard,
                "one click per detent, round again to Ro's show");
            End("Turn Bell's dial through every station", string.Join(" -> ", heard));
        }

        private IEnumerator FollowBellsSignal()
        {
            Begin();
            Assert.Greater(_events.BellSignalPicked.Count, 0, "home and listening, Bell picks up a signal");
            BellSignalPicked picked = _events.BellSignalPicked[_events.BellSignalPicked.Count - 1].Value;
            Assert.AreEqual(BellSignalTarget.Cassette, picked.Target, "the nearest reachable tape");
            CassetteSite tape = _gameplay.Cassettes.Site(CassetteIndex(DustAndHoney));
            Assert.Less(SurfaceRules.HorizontalDistance(picked.Position, tape.Position), 0.01f, "Dust & Honey");
            Assert.IsTrue(Ticker("ticker.bell.signal"), "Bell's picking something up... bearing");
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            Review(home + Vector3.up * 3f, tape.Position + Vector3.up * 12f, "27-bell-signal-pillar");
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, tape.Position);
            Vector3 stand = tape.Position + tape.Facing * 6f;
            yield return DriveTo(new Vector3(stand.x, 0f, stand.z), 2f, 0.8f, 90f, "Bell's signal");
            Review(tape.Position + tape.Facing * 3f + Vector3.up * 1.4f, tape.Position, "28-basin-tape-tucked");
            yield return DriveTo(new Vector3(tape.Position.x, 0f, tape.Position.z), 1f, 0.5f, 30f, "the tape");
            yield return Until(() => Collected(DustAndHoney), 6f, "the tape pops out of the dust into 07");
            yield return Until(() => _events.BellSignalFound.Count > 0, 2f, "Bell says so");
            Assert.IsTrue(Ticker(BellSignals.FoundLine0), "Bell says: told you so");
            Assert.AreEqual(2, _gameplay.Shelf.Shown, "both tapes stand on her rack");
            Transform corner = _gameplay.Friends.Find(BellId).Home;
            Review(_gameplay.Shelf.transform.position + _gameplay.Shelf.transform.forward * 2.2f + Vector3.up * 1.3f,
                _gameplay.Shelf.transform.position + Vector3.up * 0.6f, "29-tape-rack");
            End($"Follow Bell's signal to the basin tape ({distance:F0} m)",
                $"Dust & Honey on the rack next to Vol. 1 by {corner.name}");
        }

        /// <summary>
        /// The mound relay (relay.0), the teaching mast in the spawn view: its part from the dust, the materials it
        /// takes (still in the stock from the depot), a hold of Interact at its foot, and the whole beat until it comes
        /// online.
        /// </summary>
        /// <summary>
        /// The basin's other salvage, not driven (the drill and the Kestrel): what the masts out to the
        /// canyon (M3-06's escalating recipes) and Cell Pack II take, on top of the depot and the garage, as a player
        /// gathers it between trips.
        /// </summary>
        private IEnumerator StandInSalvage()
        {
            Begin();
            RelayTuning tuning = _gameplay.Relays.Tuning;
            Recipe pack = _gameplay.Upgrades.Find(CellPack).Levels[0].Recipe;
            Recipe need = tuning.CostAfter(0).Plus(tuning.CostAfter(1)).Plus(tuning.CostAfter(2)).Plus(pack);
            _gameplay.Materials.Deliver(SalvageMaterial.Metal, need.Metal);
            _gameplay.Materials.Deliver(SalvageMaterial.Wiring, need.Wiring);
            _gameplay.Materials.Deliver(SalvageMaterial.Optics, need.Optics);
            yield return null;
            End("The drill's and the Kestrel's salvage (not driven)", $"{need} for three masts and Cell Pack II");
        }

        /// <summary>
        /// Onto a lit mast's 4 m charge ring at walking pace, and rest until the cells (and drums) are full
        /// (docs/features/M3-20 "Chargers").
        /// </summary>
        private IEnumerator ChargeOnTheRing(int index)
        {
            Begin();
            RelayMast mast = _gameplay.Relays.Masts[index];
            BatteryModel battery = _gameplay.Battery.Model;
            float before = battery.Charge01;
            yield return DriveTo(Flat(mast.Anchor.Position), PadArrive, 0.35f, 60f, mast.Id + "'s charge ring");
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 stops on the ring");
            yield return Until(() => battery.FullCells == battery.Cells && !battery.IsCharging,
                _gameplay.Battery.Tuning.ChargeStopHold
                + battery.Cells * _gameplay.Battery.Tuning.MastSecondsPerCell + 3f,
                "the ring fills the cells");
            End($"Charge on {mast.Id}'s ring", string.Format(CultureInfo.InvariantCulture, "{0:P0} -> {1:P0}",
                before, battery.Charge01));
        }

        /// <summary>From wherever 07 is (a mast's pad by the mouth) up the mouth lane to the chasm's run-up.</summary>
        private IEnumerator ToTheRunUp()
        {
            Begin();
            WorldAnchor lip = Anchor(WorldAnchorIds.CanyonLip);
            Vector3 runStart = lip.Position - lip.Forward * RunUp;
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, runStart);
            yield return DriveTo(runStart, 2f, 1f, 90f, "the run-up on the mouth lane");
            Capture("16b-at-the-run-up");
            End($"To the chasm's run-up ({distance:F0} m)", "up the mouth lane");
        }

        /// <summary>A radio-hop from node <paramref name="from"/>'s pad (home or a lit mast) to node <paramref name="to"/>.</summary>
        private IEnumerator HopFrom(string from, string to)
        {
            Begin();
            yield return HopBetween(from, to);
            End($"Radio-hop from {from} to {to}", "a free hop between lit pads");
        }

        private IEnumerator HopBetween(string from, string to)
        {
            if (from == StationReach.HomeId)
            {
                ChargingDock dock = _gameplay.Home.Dock;
                Vector3 facing = Flat(dock.Rotation * Vector3.forward).normalized;
                yield return DriveTo(Flat(dock.Position) - facing * DockApproach, 1.5f, 0.6f, 60f,
                    "the front of the charging dock, home's hop pad");
                yield return DriveTo(Flat(dock.Position), DockParkRadius, 0.35f, 30f, "the charging dock");
            }
            else
            {
                StationReach reach = _gameplay.Relays.Reach;
                yield return DriveTo(Flat(reach.Position(reach.IndexOf(from))), PadArrive, 0.5f, 90f,
                    from + "'s pad");
            }

            yield return Hop(_context.Get<IRadioHop>(), to);
        }

        /// <summary>
        /// relay.2 by the canyon's mouth, the second mast: its part, the stitch, and as it straightens its service
        /// cabinet swings open on Cell Pack II's blueprint tube (docs/features/M3-20 §6).
        /// </summary>
        private IEnumerator RestoreTheMouthRelay()
        {
            int found = _events.BlueprintFound.Count;
            yield return RestoreRelay(2, "the mouth relay");
            yield return Until(() => _events.BlueprintFound.Count > found, 4f,
                "the mast's cabinet gives Cell Pack II's tube");
            Assert.AreEqual(CellPack, _events.BlueprintFound[_events.BlueprintFound.Count - 1].Value.KitId);
        }

        /// <summary>relay.3 at the terminus, beside Bell's wall: a charger at the canyon's far end.</summary>
        private IEnumerator RestoreTheTerminusRelay()
        {
            yield return RestoreRelay(3, "the terminus relay");
        }

        /// <summary>A mast's part, then the stitch at its foot; done once it comes online, linked home.</summary>
        private IEnumerator RestoreRelay(int index, string name)
        {
            RelayField relays = _gameplay.Relays;
            RelayMast mast = relays.Masts[index];
            Recipe recipe = relays.NextCost;
            Assert.IsTrue(_gameplay.Materials.Has(recipe), $"the stock covers {name}");
            Begin();
            int online = _events.RelayRestored.Count;
            float partAway = SurfaceRules.HorizontalDistance(mast.PartRest, mast.Anchor.Position);
            yield return DriveTo(Flat(mast.PartRest), 1.5f, 0.7f, 120f, name + "'s part");
            yield return PickupPress.Take(this, _gameplay, 4f, "the relay part");
            yield return Until(() => mast.PartState == RelayPartState.Held, 8f, "the relay part flies into 07");
            yield return AroundToTheFront(mast, name);
            Vector3 foot = mast.Broken.PartSocket.position + mast.Anchor.Forward * RelayFoot;
            yield return DriveTo(Flat(foot), 1.2f, 0.5f, 60f, name + "'s foot");
            yield return Until(() => _gameplay.Hints.TryGet(InteractionKind.Restore, out InteractionHint hint) &&
                                     hint.Ready, 4f, "the restore prompt is ready");
            Press(_keyboard.eKey);
            yield return Until(() => mast.Restoring, 4f, "holding Interact begins the restoration");
            Release(_keyboard.eKey);
            RelayBeat beat = RelayBeat.For(relays.Tuning);
            yield return Until(() => _events.RelayRestored.Count > online, beat.Duration + 2f,
                name + " comes online, linked home");
            Assert.AreEqual(mast.Id, _events.RelayRestored[_events.RelayRestored.Count - 1].Value.RelayId);
            End($"Restore {name} ({mast.Id})", $"recipe {recipe}, its part {partAway:F0} m from the mast");
        }

        /// <summary>
        /// From behind a mast (its part lay there), 07 swings out to the mast's side, on its own side of it, and on round
        /// to the front of the pad, clear of the footing, the cabinet and the guy wires.
        /// </summary>
        private IEnumerator AroundToTheFront(RelayMast mast, string name)
        {
            Vector3 forward = Flat(mast.Anchor.Forward).normalized;
            Vector3 fromMast = Flat(_rover.Position - mast.Anchor.Position);
            if (Vector3.Dot(fromMast, forward) >= RelayFoot)
            {
                yield break;
            }

            Vector3 side = Vector3.Cross(Vector3.up, forward);
            side *= Vector3.Dot(fromMast, side) < 0f ? -1f : 1f;
            yield return DriveTo(Flat(mast.Anchor.Position + side * RelayAside), WaypointArrive, CanyonThrottle, 60f,
                "the side of " + name);
            yield return DriveTo(Flat(mast.Anchor.Position + (side + forward) * RelayAside * 0.5f), WaypointArrive,
                CanyonThrottle, 60f, "the front of " + name + "'s pad");
        }

        /// <summary>
        /// Back into the bay for Cell Pack II: the spine set on 07's back, two more lamps lit; the turntable shows it
        /// turned to the open front, and 07 drives straight out.
        /// </summary>
        private IEnumerator CraftTheCellPack()
        {
            Begin();
            Workshop bench = _gameplay.Workshop;
            BatteryModel battery = _gameplay.Battery.Model;
            yield return Until(() => _gameplay.Blueprints.IsDrawn(CellPack),
                2f * _gameplay.Progression.Tuning.PlotterDrawSeconds + 3f, "the plotter draws Cell Pack II's sheet");
            yield return DriveIntoTheBay(bench);
            yield return Until(() => bench.Occupied, 3f, "07 is parked on the bay's turntable");
            Assert.AreEqual(CellPack, _gameplay.Shop.StationUpgrade.Id, "the bay offers Cell Pack II");
            int purchases = _events.UpgradePurchased.Count;
            Press(_keyboard.eKey);
            yield return Until(() => _events.UpgradePurchased.Count > purchases, 15f, "holding confirm crafts it");
            Release(_keyboard.eKey);
            Assert.IsTrue(_context.Get<IRoverAbilities>().Has(RoverAbility.CellPack));
            yield return Until(() => Fitted(CellPack), BayFitTimeout, "an arm sets the battery spine on 07");
            Assert.AreEqual(_gameplay.Battery.Tuning.CellPackCells, battery.Cells, "two more cells, arriving full");
            yield return new WaitForSeconds(1.5f);
            Vector3 back = -Flat(_rover.Rotation * Vector3.forward).normalized;
            Review(_rover.Position + back * 5f + Vector3.up * 2.5f, _rover.Position + Vector3.up, "13h-battery-spine");
            yield return Until(() => !bench.Feeding, 3f, "the bay has fitted the spine");
            Assert.Greater(Vector3.Dot(Flat(_rover.Rotation * Vector3.forward).normalized,
                Flat(bench.BayForward).normalized), 0.9f, "the turntable leaves 07 facing out of the open front");
            yield return DriveTo(Flat(bench.PadCentre) + Flat(bench.BayForward).normalized * BayApproach, 1.5f,
                BayThrottle, 20f, "out of the bay's open front");
            End("Craft Cell Pack II in the bay", $"{battery.Cells} cells, {battery.Charge01:P0}");
        }

        /// <summary>
        /// Bell is stitched: back to the terminus relay's lit pad and hop home; 07 rolls out to look over the basin, its
        /// back to the tower and Bell's corner, and Bell, unwatched, makes her own way home and greets 07 there.
        /// </summary>
        private IEnumerator HomeFromTheTerminus()
        {
            Begin();
            Friend bell = _gameplay.Friends.Find(BellId);
            int greetings = CountGreetings(BellId);
            yield return HopBetween(WorldAnchorIds.RelayPrefix + 3, StationReach.HomeId);
            Vector3 dock = Flat(_rover.Position);
            Vector3 away = Flat(dock - _gameplay.Tower.PadCentre).normalized;
            yield return DriveTo(dock + away * LookOutDistance, WaypointArrive, 0.5f, 30f, "a look over the basin");
            yield return Until(() => bell.IsHome, _gameplay.Friends.BellTuning.WakeDuration + 60f,
                "Bell walks home by the exit on her own");
            yield return Until(() => CountGreetings(BellId) > greetings, _gameplay.Friends.BellTuning.WakeDuration + 6f,
                "she greets 07 home");
            End("Home from the terminus by radio-hop", "Bell walked home by herself and greeted 07");
        }

        /// <summary>Out to the mound relay's pad, where level 3's relay spare waits (it lit early on the way out).</summary>
        private IEnumerator ToTheMoundRelay()
        {
            Begin();
            RelayMast mast = _gameplay.Relays.Masts[0];
            yield return DriveTo(Flat(mast.Anchor.Position + mast.Anchor.Forward * RelayFoot), WaypointArrive,
                CanyonThrottle, 90f, "the front of the mound relay's pad");
            End("Out to the mound relay", "its spare waits on the pad");
        }

        private IEnumerator RestoreTheMoundRelay()
        {
            RelayField relays = _gameplay.Relays;
            RelayMast mast = relays.Masts[0];
            Assert.AreEqual(WorldAnchorIds.RelayPrefix + 0, mast.Id);
            Recipe recipe = relays.NextCost;
            Assert.IsTrue(_gameplay.Materials.Has(recipe), "the depot's materials cover the mound relay too");
            Begin();
            Vector3 pad = mast.Anchor.Position;
            Review(pad + mast.Anchor.Forward * 16f + Vector3.up * 2.5f, pad + Vector3.up * 5f, "30-relay-dark");
            float partAway = SurfaceRules.HorizontalDistance(mast.PartRest, pad);
            yield return DriveTo(Flat(mast.PartRest), 1.5f, 0.7f, 120f, "the mound relay's part");
            yield return PickupPress.Take(this, _gameplay, 4f, "the relay part");
            yield return Until(() => mast.PartState == RelayPartState.Held, 8f, "the relay part flies into 07");
            Vector3 foot = mast.Broken.PartSocket.position + mast.Anchor.Forward * RelayFoot;
            yield return DriveTo(Flat(foot), 1.2f, 0.5f, 60f, "the mound relay's foot");
            yield return Until(() => _gameplay.Hints.TryGet(InteractionKind.Restore, out InteractionHint hint) &&
                                     hint.Ready, 4f, "the restore prompt is ready");
            Press(_keyboard.eKey);
            yield return Until(() => mast.Restoring, 4f, "holding Interact begins the restoration");
            Release(_keyboard.eKey);
            RelayBeat beat = RelayBeat.For(relays.Tuning);
            yield return new WaitForSeconds(beat.StitchEnd * 0.6f);
            Review(pad + mast.Anchor.Forward * 9f + Vector3.up * 2.4f, mast.Broken.BeamPoint.position,
                "31-relay-stitching");
            yield return Until(() => _events.RelayRestored.Count > 0, beat.Duration + 2f,
                "the mound relay comes online");
            Assert.AreEqual(mast.Id, _events.RelayRestored[0].Value.RelayId);
            Assert.IsTrue(Ticker("ticker.relay.online"), "Relay 1 online. Lumen Station can hear a little farther.");
            Assert.AreEqual(2, _context.Get<IStationReach>().LitCount, "home and the mound relay");
            Review(pad + mast.Anchor.Forward * 14f + Vector3.up * 1.2f, mast.Restored.Lamp.position,
                "32-relay-online");
            yield return new WaitForSeconds(1.2f);
            Review(pad + mast.Anchor.Forward * 30f + Vector3.up * 14f, pad + mast.Anchor.Forward * 40f,
                "33-relay-link-pulse");
            End("Restore the mound relay (relay.0)",
                $"recipe {recipe} ({mast.Paid} units), its part {partAway:F0} m from the mast, beat " +
                $"{beat.Duration:F1} s");
        }

        /// <summary>Past the mound relay, beyond the tower's own reach: home still reaches 07 there.</summary>
        private IEnumerator DriveOutInReach()
        {
            Begin();
            RelayMast mast = _gameplay.Relays.Masts[0];
            var reach = _context.Get<IStationReach>();
            var terrain = _context.Get<ITerrainQuery>();
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            Vector3 outward = -mast.Anchor.Forward;
            Vector3 target = Vector3.zero;
            bool found = false;
            for (float distance = PastTheMast.x; distance <= PastTheMast.y && !found; distance += 5f)
            {
                Vector3 point = mast.Anchor.Position + outward * distance;
                found = SurfaceRules.InsideDrivable(terrain, point.x, point.z, 3f) &&
                        SurfaceRules.HorizontalDistance(point, home) > _gameplay.Upgrades.SignalRadius;
                target = point;
            }

            Assert.IsTrue(found, "drivable ground past the mound relay, beyond the tower's reach");
            Vector3 aside = mast.Anchor.Position + Vector3.Cross(Vector3.up, mast.Anchor.Forward) * RelayAside;
            yield return DriveTo(Flat(aside), WaypointArrive, CanyonThrottle, 60f, "the side of the mound relay");
            yield return DriveAlong(target, "the ground past the mound relay", null);
            float fromHome = SurfaceRules.HorizontalDistance(_rover.Position, home);
            Assert.Greater(fromHome, _gameplay.Upgrades.SignalRadius, "beyond the tower's own circle");
            Assert.IsTrue(reach.IsInReach(_rover.Position), "the radio stays clear: the mast reaches 07 here");
            float nearest = reach.DistanceToNearestNode(_rover.Position);
            Witness(mast.Restored.Lamp.position, "34-in-reach-out-there");
            End("Drive out past the tower's reach",
                $"{fromHome:F0} m from home (tower {_gameplay.Upgrades.SignalRadius:F0} m), {nearest:F0} m from the " +
                "nearest lit node: in reach");
        }

        /// <summary>
        /// From the mound relay's pad, the radio-hop home, which sets 07 down on its dock on the base pad: home with
        /// news (the lit mast), it rests, the dock's evening opens once (P6) and the tower's listening sweep raises a
        /// pillar. Then from home's pad, back to the mast.
        /// </summary>
        private IEnumerator HopHomeAndBack()
        {
            Begin();
            RelayMast mast = _gameplay.Relays.Masts[0];
            IRadioHop hop = _context.Get<IRadioHop>();
            Vector3 aside = mast.Anchor.Position + Vector3.Cross(Vector3.up, mast.Anchor.Forward) * RelayAside;
            yield return DriveTo(Flat(aside), WaypointArrive, CanyonThrottle, 60f, "the side of the mound relay");
            yield return DriveTo(Flat(mast.Anchor.Position + mast.Anchor.Forward * RelayFoot), WaypointArrive,
                CanyonThrottle, 60f, "the front of the mound relay's pad");
            yield return DriveTo(Flat(mast.Anchor.Position), PadArrive, 0.5f, 90f, "the mound relay's pad");
            int evenings = _events.DockEveningOpened.Count;
            yield return Hop(hop, StationReach.HomeId);
            ChargingDock dock = _gameplay.Home.Dock;
            Assert.Less(SurfaceRules.HorizontalDistance(_rover.Position, dock.Position), 3f, "on home's dock");
            yield return Until(() => dock.Docked, _gameplay.Home.Tuning.DockDelay + 3f,
                "07 rests on the dock it landed on");
            yield return Until(() => _events.DockEveningOpened.Count > evenings, 1f,
                "home with news: the dock's evening opens (P6)");
            int heard = Heard();
            int seen = 0;
            using IDisposable blinks = _context.Events.Subscribe<MapPlaceSeen>(_ => seen++);
            yield return new WaitForSeconds(_gameplay.Progression.Tuning.EveningSweepDelay + 0.5f);
            Assert.AreEqual(evenings + 1, _events.DockEveningOpened.Count, "the evening opens once");
            Assert.AreEqual(heard + (heard < Hearable() ? 1 : 0), Heard(),
                "the tower's listening sweep raises one new pillar on the horizon, while any is left unheard");
            Capture("35-hopped-home");
            yield return Hop(hop, mast.Id);
            Assert.Less(SurfaceRules.HorizontalDistance(_rover.Position, mast.Anchor.Position), 3f,
                "back on the mound relay's pad");
            Capture("36-hopped-back");
            End("Radio-hop home and back", $"{_events.RadioHopFinished.Count} hops, about " +
                                           $"{HopSequence.For(_gameplay.Relays.Tuning).Duration:F1} s each; home on " +
                                           $"the dock, {seen} places blinked seen and the evening's sweep raised a " +
                                           "pillar");
        }

        /// <summary>
        /// Home at last: hop home, drive up to the charging dock on the base pad, stop on it and let go of the wheel;
        /// 07 settles in to rest and the dock's glow warms, with no second evening (nothing new since the last). A
        /// touch of the throttle leaves. The hop home lands 07 on the dock's pad, so it may rest there first: the rest
        /// and the evening are counted from before the hop.
        /// </summary>
        private IEnumerator RestOnTheDock()
        {
            Begin();
            int rests = _events.RoverDockChanged.Count;
            int evenings = _events.DockEveningOpened.Count;
            yield return Hop(_context.Get<IRadioHop>(), StationReach.HomeId);
            ChargingDock dock = _gameplay.Home.Dock;
            BaseTuning tuning = _gameplay.Home.Tuning;
            Vector3 facing = Flat(dock.Rotation * Vector3.forward).normalized;
            yield return DriveTo(Flat(dock.Position) - facing * DockApproach, 1.5f, 0.6f, 60f,
                "the front of the charging dock");
            yield return DriveTo(Flat(dock.Position), DockParkRadius, 0.35f, 30f, "the charging dock");
            yield return Until(() => dock.Docked, tuning.DockDelay + 3f, "07 settles in to rest on the dock");
            RoverDockChanged rest = _events.RoverDockChanged[rests].Value;
            Assert.IsTrue(rest.Docked);
            float off = SurfaceRules.HorizontalDistance(_rover.Position, dock.Position);
            yield return new WaitForSeconds(tuning.DockGlowEase * 3f);
            Assert.Greater(dock.GlowLevel, tuning.DockIdleGlow * 2f, "the dock glows warm while 07 charges");
            Assert.AreEqual(evenings, _events.DockEveningOpened.Count,
                "nothing new since the hop home's evening: no second evening (P6)");
            Vector3 aside = Vector3.Cross(Vector3.up, facing);
            Review(Flat(dock.Position) - facing * 6f + aside * 3f + Vector3.up * 2.5f,
                dock.Position + Vector3.up * 0.6f, "37-resting-on-the-dock");
            _pilot.Reverse = BayThrottle;
            yield return Until(() => !dock.Docked, 2f, "a touch of the throttle leaves the dock");
            _pilot.Reverse = 0f;
            Assert.IsFalse(_events.RoverDockChanged[_events.RoverDockChanged.Count - 1].Value.Docked);
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 stops off the dock");
            End("Rest on the charging dock", $"settled {off:F2} m from its anchor, glow {dock.GlowLevel:F2}; " +
                                             "no second evening");
        }

        /// <summary>Salvage sites and friends there are to hear.</summary>
        private int Hearable()
        {
            int hearable = _gameplay.Friends.Friends.Count;
            foreach (SalvageSite site in _gameplay.Salvage.Sites)
            {
                // A site cut clean before anything heard it (the depot, now that 07 listens instead of pinging)
                // has nothing left to answer with.
                hearable += site.Discovered || site.AnswersSonar ? 1 : 0;
            }

            return hearable;
        }

        /// <summary>Salvage sites and friends heard so far (by ping, by a spotter or by the evening's sweep).</summary>
        private int Heard()
        {
            int heard = 0;
            foreach (SalvageSite site in _gameplay.Salvage.Sites)
            {
                heard += site.Discovered ? 1 : 0;
            }

            foreach (Friend friend in _gameplay.Friends.Friends)
            {
                heard += friend.Progress.Discovered ? 1 : 0;
            }

            return heard;
        }

        /// <summary>Opens the hop list on 07's pad, steps to <paramref name="to"/> and holds Interact to go.</summary>
        private IEnumerator Hop(IRadioHop hop, string to)
        {
            int hops = _events.RadioHopFinished.Count;
            yield return Until(() => hop.CanOpen, 6f, "07 is parked on a lit pad");
            Press(_keyboard.eKey);
            yield return Until(() => hop.Phase == RadioHopPhase.Choosing, 2f, "Interact opens the hop list");
            Release(_keyboard.eKey);
            yield return null;
            var reach = _context.Get<IStationReach>();
            for (int i = 0; i < hop.ChoiceCount && reach.GetNode(hop.ChoiceNode(hop.Selected)).Id != to; i++)
            {
                hop.Next();
            }

            Assert.AreEqual(to, reach.GetNode(hop.ChoiceNode(hop.Selected)).Id, $"{to} is in the list");
            Press(_keyboard.eKey);
            yield return Until(() => hop.Phase >= RadioHopPhase.Leaving, 3f, "holding Interact hops");
            Release(_keyboard.eKey);
            yield return Until(() => _events.RadioHopFinished.Count > hops && hop.Phase == RadioHopPhase.Closed, 6f,
                "the hop eases out, 07 lands on " + to + " and the view eases back in");
            Assert.AreEqual(to, _events.RadioHopFinished[hops].Value.ToId);
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }

        /// <summary>
        /// Drives a line over drivable ground to <paramref name="target"/> (the planner Bell walks by), easing
        /// through the waypoints and to a stop at the end; reports the metres travelled.
        /// </summary>
        private IEnumerator DriveAlong(Vector3 target, string what, Action<float> travelled)
        {
            Vector3[] path = WalkPathPlanner.Plan(_context.Get<ITerrainQuery>(), _rover.Position, target,
                _gameplay.Friends.Tuning);
            Assert.IsNotNull(path, $"a drivable line to {what}");
            float length = 0f;
            for (int i = 1; i < path.Length; i++)
            {
                length += SurfaceRules.HorizontalDistance(path[i - 1], path[i]);
                bool last = i == path.Length - 1;
                _pilot.GoTo(path[i], last ? 1.5f : WaypointArrive, CanyonThrottle);
                yield return Until(() => _pilot.Arrived, 60f, $"07 reaches waypoint {i} toward {what}");
            }

            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 comes to rest at " + what);
            travelled?.Invoke(length);
        }

        private float Along(WorldAnchor anchor)
        {
            Vector3 offset = _rover.Position - anchor.Position;
            return offset.x * anchor.Forward.x + offset.z * anchor.Forward.z;
        }

        private WorldAnchor Anchor(string id)
        {
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(id, out WorldAnchor anchor), $"the World has {id}");
            return anchor;
        }

        private int CassetteIndex(string id)
        {
            for (int i = 0; i < _gameplay.Cassettes.Count; i++)
            {
                if (_gameplay.Cassettes.Definition(i).Id == id)
                {
                    return i;
                }
            }

            Assert.Fail($"No cassette '{id}'.");
            return -1;
        }

        private bool Collected(string cassette)
        {
            foreach (EventRecorder.Timed<CassetteCollected> collected in _events.CassetteCollected)
            {
                if (collected.Value.CassetteId == cassette)
                {
                    return true;
                }
            }

            return false;
        }

        private int CountGreetings(string friend)
        {
            int count = 0;
            foreach (EventRecorder.Timed<FriendGreeted> greeted in _events.FriendGreeted)
            {
                count += greeted.Value.FriendId == friend ? 1 : 0;
            }

            return count;
        }

        private bool Ticker(string key)
        {
            foreach (EventRecorder.Timed<TickerLine> line in _events.TickerLine)
            {
                if (line.Value.Key == key)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasCue(BellCue cue)
        {
            foreach (EventRecorder.Timed<BellCued> cued in _events.BellCued)
            {
                if (cued.Value.Cue == cue)
                {
                    return true;
                }
            }

            return false;
        }

        private bool Spotted(SalvageSite site)
        {
            foreach (EventRecorder.Timed<FriendSpotted> spotted in _events.FriendSpotted)
            {
                if (SurfaceRules.HorizontalDistance(spotted.Value.Position, site.Position) < 0.1f)
                {
                    return true;
                }
            }

            return false;
        }

        private int NearestMissingPart(Friend friend)
        {
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < friend.Progress.PartCount; i++)
            {
                float distance = SurfaceRules.HorizontalDistance(friend.PartRest[i], _rover.Position);
                if (!friend.Progress.IsCollected(i) && distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        private IEnumerator DriveTo(Vector3 target, float arriveRadius, float maxThrottle, float timeout, string what)
        {
            if (TryDetourRoundTheLander(target, out Vector3 detour))
            {
                _pilot.GoTo(detour, WaypointArrive, maxThrottle);
                yield return Until(() => _pilot.Arrived, timeout, "07 drives round the lander toward " + what);
            }

            _pilot.GoTo(target, arriveRadius, maxThrottle);
            yield return Until(() => _pilot.Arrived, timeout, "07 reaches " + what);
            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 comes to rest at " + what);
        }

        /// <summary>
        /// A long drive whose straight line would cross the lander (its porch, dock and lift stand out from it) goes
        /// round it by a point beside it instead, on the side away from the Rover Bay, which stands beside the lander
        /// (a player rounds home on its open side, never through the bay). Drives that start or end at home, next to
        /// the lander, go straight.
        /// </summary>
        private bool TryDetourRoundTheLander(Vector3 target, out Vector3 detour)
        {
            detour = Vector3.zero;
            Vector3 lander = Flat(_gameplay.Home.LanderPosition);
            Vector3 from = Flat(_rover.Position);
            Vector3 to = Flat(target);
            if (Vector3.Distance(from, lander) < LanderKeepOut || Vector3.Distance(to, lander) < LanderKeepOut)
            {
                return false;
            }

            Vector3 line = to - from;
            float length = line.magnitude;
            if (length < 1e-3f)
            {
                return false;
            }

            Vector3 direction = line / length;
            float along = Mathf.Clamp(Vector3.Dot(lander - from, direction), 0f, length);
            Vector3 closest = from + direction * along;
            Vector3 away = closest - lander;
            if (away.magnitude >= LanderKeepOut)
            {
                return false;
            }

            Vector3 side = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.Cross(Vector3.up, direction);
            Vector3 bay = Flat(_gameplay.Workshop.PadCentre);
            Vector3 near = lander + side * LanderDetour;
            Vector3 far = lander - side * LanderDetour;
            detour = Vector3.Distance(near, bay) >= Vector3.Distance(far, bay) ? near : far;
            return true;
        }

        /// <summary>
        /// Lines 07 up in front of the Rover Bay, drives in up its ramp and stops on the turntable, facing in.
        /// </summary>
        private IEnumerator DriveIntoTheBay(Workshop bay)
        {
            Vector3 entrance = Flat(bay.PadCentre) + Flat(bay.BayForward).normalized * BayApproach;
            yield return DriveTo(entrance, 1.5f, 0.6f, 60f, "the mouth of Kenji's Rover Bay");
            yield return DriveTo(Flat(bay.PadCentre), BayParkRadius, BayThrottle, 30f, "the bay's turntable");
            yield return Until(() => bay.Occupied, 3f, "07 is parked on the bay's turntable");
        }

        /// <summary>Backs 07 straight out of the Rover Bay the way it drove in, until it is clear of it.</summary>
        private IEnumerator BackOutOfTheBay(Workshop bay)
        {
            Vector3 front = Flat(bay.BayForward).normalized;
            _pilot.Target = null;
            _pilot.Reverse = BayThrottle;
            yield return Until(() => Vector3.Dot(Flat(_rover.Position - bay.PadCentre), front) >= BayApproach, 20f,
                "07 backs out of the bay");
            _pilot.Reverse = 0f;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 stops clear of the bay");
        }

        /// <summary>A review capture of the Rover Bay from out in front of it, a little aside.</summary>
        private void ReviewBay(Workshop bay, string name)
        {
            Vector3 front = Flat(bay.BayForward).normalized;
            Vector3 aside = Vector3.Cross(Vector3.up, front);
            Review(bay.BayPosition + front * BayViewDistance + aside * BayViewSide + Vector3.up * BayViewHeight,
                bay.BayPosition + Vector3.up, name);
        }

        /// <summary>True once the bay has set a piece bought as <paramref name="upgradeId"/> on 07.</summary>
        private bool Fitted(string upgradeId)
        {
            for (int i = 0; i < _events.RoverKitFitted.Count; i++)
            {
                RoverKitFitted fitted = _events.RoverKitFitted[i].Value;
                if (!fitted.Gift && fitted.UpgradeId == upgradeId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True once a station cued <paramref name="cue"/> after the first <paramref name="from"/>.</summary>
        private bool Cued(StationCue cue, int from)
        {
            for (int i = from; i < _events.StationCued.Count; i++)
            {
                if (_events.StationCued[i].Value.Cue == cue)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The station cues after the first <paramref name="from"/> are exactly these, all for it.</summary>
        private void AssertCues(int from, string upgradeId, params StationCue[] expected)
        {
            Assert.AreEqual(from + expected.Length, _events.StationCued.Count, "the station's beats, once each");
            for (int i = 0; i < expected.Length; i++)
            {
                StationCued cued = _events.StationCued[from + i].Value;
                Assert.AreEqual(expected[i], cued.Cue, $"beat {i}");
                Assert.AreEqual(upgradeId, cued.UpgradeId, $"beat {i} is for {upgradeId}");
            }
        }

        private IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            float deadline = Time.time + timeout;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }

            if (!condition())
            {
                Capture("timed-out");
                Review(_rover.Position + Vector3.up * StuckReviewHeight + Vector3.back, _rover.Position,
                    "timed-out-above");
            }

            Assert.IsTrue(condition(), $"timed out after {timeout:F0} s waiting until {what} (07 at {_rover.Position}, " +
                $"heading {_rover.Rotation.eulerAngles.y:F0}, {_rover.Speed:F1} m/s, steering for {_pilot.Target})");
        }

        /// <summary>Holds the winch in (+1) or out (-1) for <paramref name="seconds"/>, like a held scroll.</summary>
        private IEnumerator Reel(float direction, float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                Set(_mouse.scroll, new Vector2(0f, direction));
                yield return null;
            }

            Set(_mouse.scroll, Vector2.zero);
        }

        private int CompletedCuts()
        {
            int completed = 0;
            foreach (EventRecorder.Timed<SalvageCutStopped> stopped in _events.SalvageCutStopped)
            {
                completed += stopped.Value.Completed ? 1 : 0;
            }

            return completed;
        }

        private bool Surfaced(string relicId)
        {
            foreach (EventRecorder.Timed<RelicSurfaced> surfaced in _events.RelicSurfaced)
            {
                if (surfaced.Value.RelicId == relicId)
                {
                    return true;
                }
            }

            return false;
        }

        private void Begin()
        {
            _stepStart = Time.time;
            _stepMetres = 0f;
            _stepBeam = 0f;
            _stepLowest = _gameplay != null ? _gameplay.Battery.Model.Charge01 : 1f;
        }

        private void End(string step, string notes)
        {
            float seconds = Time.time - _stepStart;
            float charge = _gameplay != null ? _gameplay.Battery.Model.Charge01 : 1f;
            _steps.Add((step, seconds, notes, new Vector3(charge, _stepMetres, _stepBeam)));
            Debug.Log(string.Format(CultureInfo.InvariantCulture,
                "[playthrough] {0}: {1:F1} s ({2}); charge {3:P0} (lowest {6:P0}), {4:F0} m on the ground, {5:F1} s " +
                "of beam", step, seconds, notes, charge, _stepMetres, _stepBeam, Mathf.Min(_stepLowest, charge)));
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, 1280, 720,
                Path.Combine(GameplayFixture.CaptureFolder, "playthrough-" + name + ".png"));
        }

        /// <summary>
        /// A review capture of <paramref name="subject"/> from a second camera (same lens as the game's) set a few
        /// metres beyond it, looking back toward 07, so moments the follow camera faces away from still get a frame.
        /// </summary>
        private void Witness(Vector3 subject, string name)
        {
            Vector3 toRover = _rover.Position - subject;
            toRover.y = 0f;
            Review(subject - toRover.normalized * WitnessDistance + Vector3.up * WitnessHeight, subject, name);
        }

        /// <summary>A review capture from a second camera (the game's lens) at <paramref name="eye"/>.</summary>
        private void Review(Vector3 eye, Vector3 target, string name)
        {
            Camera view = _context.Get<IViewCamera>().Camera;
            var host = new GameObject("ReviewCamera");
            try
            {
                var review = host.AddComponent<Camera>();
                review.CopyFrom(view);
                review.enabled = false;
                host.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye));
                FrameCapture.SavePng(review, 1280, 720,
                    Path.Combine(GameplayFixture.CaptureFolder, "playthrough-" + name + ".png"));
            }
            finally
            {
                Object.Destroy(host);
            }
        }

        private void WriteReport(float total)
        {
            var report = new StringBuilder();
            report.AppendLine("# Golden-path playthrough (real Main scene)");
            report.AppendLine();
            report.AppendLine("| Step | Seconds | Notes | Charge | Ground m | Beam s |");
            report.AppendLine("|---|---:|---|---:|---:|---:|");
            foreach ((string step, float seconds, string notes, Vector3 battery) in _steps)
            {
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1:F1} | {2} | {3:P0} | {4:F0} | {5:F1} |", step, seconds, notes, battery.x, battery.y,
                    battery.z));
            }

            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| **Total** | **{0:F1}** | budget {1:F0} s; {2} error(s), {3} warning(s) in the log; " +
                "{4} crawl(s) | | | |",
                total, TotalBudget, _problems.Count, _warnings, _crawls));
            report.AppendLine();
            report.AppendLine("Range lessons (docs/features/M3-20 §6):");
            foreach ((string where, float charge, Vector2 target) in _lessons)
            {
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "- {0}: {1:P0} (target {2:P0}-{3:P0})",
                    where, charge, target.x, target.y));
            }
            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[playthrough] report: " + ReportPath + "\n" + report);
        }
    }
}

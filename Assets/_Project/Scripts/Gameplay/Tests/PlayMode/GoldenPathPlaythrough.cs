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
using MoonProject.Rover;
using MoonProject.Routing;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The opening v2 through the real game (docs/design/opening-v2.md §2, B0–B11), as a regression and a pacing
    /// measurement: the built Main scene (on its own save slot) boots, 07 wakes at half charge, and a calm autopilot
    /// holds the wheel while every verb goes through the keys a player presses: held E scans and cuts; B opens the
    /// build menu, the arrows pick, E takes the structure up, E places it where 07 looks and a held E welds it; Tab
    /// opens 07's face screen, the arrows reach the cutter's socket and Enter fits the igniter. Each beat starts at
    /// its step of the opening with no line on screen (the world leads, opening v3 1.5), and on each verb's first use
    /// its prompt chip must show
    /// (a missing one is a guidance gap, listed at the end). 07 drives to the landed lander, scans its cargo bay's two
    /// fragments (the charging pad), collects the lander's open crate (the cutter is FAULT: no igniter), builds the pad
    /// on the apron, charges on it from the lander's reserve until the reserve gives out, drives to the Lumen ruins
    /// and scans the fallen array (solar), reads the habitat's airlock panel, tries to cut the array's crates (the
    /// cutter sputters), takes the spare igniter from the rack beside the array and fits it where 07 stands, cuts
    /// the array's panels and junction box, builds the array
    /// (Power on), reads the 1971 seismic station and docks for the first dusk (R5–R6), scans the habitat (battery
    /// bank), cuts its cable reel and power module and builds it (night-ready),
    /// waits out the first sunset, scans the radio tower (antenna) and cuts its mast sections, feed cable and dish
    /// panels, and builds the mast: the west signal ends the opening. Every step is checked as
    /// the opening reaches it; the milestones FirstCharge, PowerOn, NightReady and SignalWest each come once. Every step
    /// is timed
    /// (Logs/gameplay-captures/playthrough.md) and captured; any error or exception in the log fails it, and so does a
    /// crawl or a run over the budget. Slow: run on demand with --category Playthrough.
    /// </summary>
    [Explicit("Slow end-to-end playthrough of the real Main scene; run on demand with --category Playthrough.")]
    [Category("Playthrough")]
    public sealed class GoldenPathPlaythrough : InputTestFixture
    {
        private const int TestTimeoutMs = 900000;

        /// <summary>
        /// Seconds the opening may take end to end through the real keys, standing still for each line (the Director's
        /// 12 min, up from opening-v2 §6's 10 for the scripted run).
        /// </summary>
        private const float TotalBudget = 720f;

        private const float StopSpeed = 0.3f;

        // 07's cheek lens scans what lies within this many degrees of its heading; off by more after arriving, it turns
        // on the spot until it faces the target within the tighter angle.
        private const float LensFacing = 25f;
        private const float FacedWithin = 8f;
        private const float SealStandOff = 1.6f;

        /// <summary>The lander's open charger kit (B4) and the CUTTER SPARES rack by the fallen array (B7).</summary>
        private const string ChargerKit = "supply.lander.open";
        private const string SparesRack = "supply.array.spares";

        /// <summary>The habitat's towable power module (opening v3 R6), and how far from the habitat it stands.</summary>
        private const string PowerModule = TowLoads.PowerModule;

        private const float PowerModuleReach = 15f;

        /// <summary>Seconds the chase view takes to settle on a new bearing before a capture.</summary>
        private const float ViewSettleSeconds = 1.5f;

        /// <summary>Metres out from the module's hitch where 07 turns its back to it before reversing.</summary>
        private const float HookStandOff = 4f;

        /// <summary>Metres beyond that stand 07 lines up from, so it comes in facing the hitch.</summary>
        private const float HookLineUp = 6f;

        /// <summary>Metres (flat) from the hitch the winch backs up to: inside the hook's reach.</summary>
        private const float HookBackTo = 2f;

        /// <summary>Seconds 07 may take to reverse its winch up to the hitch.</summary>
        private const float HookBackSeconds = 8f;

        /// <summary>Metres short of the apron's centre 07 stops towing: the module trails onto the apron behind it.</summary>
        private const float HomeStop = 1f;

        /// <summary>Metres from that stop point 07 counts as there: close, so the module trails onto the apron.</summary>
        private const float HomeArrive = 1f;

        /// <summary>Seconds after 07 stops for the trailing module to come onto the apron and be let go.</summary>
        private const float HomeLetGoSeconds = 8f;

        /// <summary>The 1971 seismic station's story scan (R5's curiosity scan).</summary>
        private const string SeismicStory = "seismic";

        /// <summary>Seconds the pad may take to take 07 in once it rests there (a full 07 waits out the rest time).</summary>
        private const float DuskTimeout = 15f;

        /// <summary>
        /// Throttle (0..1) 07 creeps the last metres to a seal or a scan target at, steering at it: it stops short and
        /// facing it instead of past it.
        /// </summary>
        private const float ApproachThrottle = 0.3f;

        /// <summary>Metres from the spot in front of a seal that count as there: close, so 07 faces the seal.</summary>
        private const float SealArrive = 1.5f;

        /// <summary>
        /// Metres from the seal at which 07, steering at it, stops nosing in even if the crate has not claimed E: a
        /// solid crate's side (07's nose touches a small one about here).
        /// </summary>
        private const float SealNose = 1.2f;

        /// <summary>Seconds 07 may creep at a seal, steering at it, before the crate claims E.</summary>
        private const float SealNoseSeconds = 6f;

        /// <summary>Seconds 07 backs straight off a seal it could not swing onto, for room to turn.</summary>
        private const float SealBackOffSeconds = 2f;

        /// <summary>Approaches to a seal before the run gives up on its claim: a small crate can end up beside
        /// 07.</summary>
        private const int SealTries = 3;


        /// <summary>Sides round a crate tried for open ground to come in from, its front first.</summary>
        private const int ApproachSides = 8;

        /// <summary>Metres above the ground, and the radius, of the space 07's body needs to stand in.</summary>
        private const float ClearHeight = 0.9f;

        private const float ClearRadius = 0.9f;

        /// <summary>Metres south of the fallen solar array the way into and out of the ruins lies.</summary>
        private const float RuinsGateOffset = 10f;

        /// <summary>Metres out from a seal's stand-off 07 lines up first, so it reaches the seal facing it.</summary>
        private const float SealLineUp = 3f;

        // The open ground 07 comes in from, this far beyond a seal's line-up (CrateApproachSessions keeps it clear).
        private const float SealOpenGround = 5f;

        /// <summary>Metres back from a build spot 07 parks, facing it: inside the weld's 6 m reach.</summary>
        private const float BuildStandOff = 4f;
        private const float SealCutTimeout = 25f;
        private const float DriveTimeout = 90f;

        /// <summary>Metres 07 must close on its target in <see cref="StuckSeconds"/>, or it counts as stuck.</summary>
        private const float StuckProgress = 0.3f;

        /// <summary>Seconds over which 07's progress is measured before it backs off and comes round.</summary>
        private const float StuckSeconds = 1.5f;

        private const float UnstickReverseThrottle = 0.6f;
        private const float UnstickReverseSeconds = 1.2f;

        /// <summary>Metres to the side (and ahead) 07 swings out to when it comes round an obstacle.</summary>
        private const float UnstickSidestep = 3f;

        /// <summary>Attempts after which the sidestep stops growing: a wide structure takes a wider berth.</summary>
        private const int UnstickMaxGrowth = 4;

        private const float UnstickSidestepSeconds = 6f;

        /// <summary>07 stops this far (m) from a fragment, facing it, to scan it.</summary>
        private const float ScanStandOff = 3.5f;

        /// <summary>Metres from a panel on a wall 07 scans it from, square to it: the panel fills the view.</summary>
        private const float PanelStandOff = 2.5f;

        /// <summary>Metres from a line-up spot that count as there: it only turns 07 roughly to its target.</summary>
        private const float LineUpArrive = 3f;

        /// <summary>Metres beyond a scan's stand-off 07 lines up first, where that ground is clear.</summary>
        private const float ScanLineUp = 3f;

        /// <summary>Seconds a scan may take beyond its size's time (the press, 07 easing to a stop).</summary>
        private const float ScanAllowance = 3f;

        /// <summary>Times 07 lets go and holds E again when the scan does not begin or is lost half way.</summary>
        private const int HoldAttempts = 3;

        private const float HoldReleaseSeconds = 0.3f;

        /// <summary>Seconds a held E may take to begin a scan before the player lets go and looks again.</summary>
        private const float HoldBeginSeconds = 1.5f;

        private const float RouteCellMetres = 2f;
        private const float RouteMarginMetres = 40f;
        private const float RouteClearance = 1f;
        private const float RouteRelaxMetres = 4f;
        private const float RouteArriveRadius = 2.5f;
        private readonly List<Vector2> _routePoints = new List<Vector2>();

        /// <summary>Seconds 07's view may take to settle on a fragment once it is parked facing it.</summary>
        private const float ScanLookTimeout = 3f;

        /// <summary>Metres from the lander's centre, on its port side, where 07 pulls up (B2).</summary>
        private const float LanderStandOff = 12f;

        /// <summary>Metres the solar array, the battery bank and the mast stand from the pad (inside 14 m
        /// reach).</summary>
        private const float BaseSpacing = 7f;

        /// <summary>Seconds the first sunset may take once the base is night-ready (opening-v2 §9 ruling 2).</summary>
        private const float SunsetTimeout = 240f;

        /// <summary>Seconds a line or a milestone takes to follow the game's state (the flow ticks every
        /// frame).</summary>
        private const float FollowTimeout = 3f;

        /// <summary>
        /// Seconds a verb's first-use chip may take to show once 07 is in place for it, if it has not shown already
        /// (opening v3 1.5: the world shows it when the verb first becomes possible; the bottom hint fades first).
        /// </summary>
        private const float FirstUseTimeout = 15f;

        /// <summary>Taps of an arrow a menu or the face screen may take to reach what is wanted.</summary>
        private const int MaxMenuSteps = 12;

        /// <summary>Seconds after a tap before the next, so each reads as its own press.</summary>
        private const float TapSeconds = 0.15f;

        /// <summary>Metres apart the rings of spots a structure is tried at round its build spot.</summary>
        private const float PlaceRingStep = 1.5f;

        private const int PlaceRings = 4;
        private const int PlaceRingSpots = 8;

        /// <summary>UI's localization keys of the verb words its chips show (UiKeys.Verb*).</summary>
        private const string VerbScanKey = "ui.verb.scan";

        private const string VerbCutKey = "ui.verb.cut";
        private const string VerbBuildKey = "ui.verb.build";
        private const string VerbFitKey = "ui.verb.fit";

        /// <summary>The face screen's device bubble of 07's cutter, where the spare igniter fits.</summary>
        private const int CutterDevice = 1;

        private static readonly string ReportPath = Path.Combine(GameplayFixture.CaptureFolder, "playthrough.md");

        private readonly List<(string Step, float Seconds, string Notes, float Charge)> _steps =
            new List<(string, float, string, float)>();

        private readonly List<string> _problems = new List<string>();
        private readonly List<OpeningMilestoneReached> _milestones = new List<OpeningMilestoneReached>();
        private readonly List<string> _nextLines = new List<string>();
        private readonly HashSet<string> _verbsUsed = new HashSet<string>();
        private readonly List<string> _chips = new List<string>();
        private readonly List<string> _guidanceGaps = new List<string>();
        private HudReader _hud;
        private ChipWatch _chipWatch;
        private ILocalization _localization;
        private int _warnings;
        private int _crawls;
        private int _towLetGo;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private Autopilot _pilot;
        private IDisposable _awoke;
        private IDisposable _crawled;
        private IDisposable _towReleased;
        private IDisposable _reached;
        private IDisposable _lines;
        private float _awokeAt = -1f;
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
            if (_chipWatch != null)
            {
                Object.DestroyImmediate(_chipWatch.gameObject);
            }

            _rover?.SetDriveSource(null);
            _awoke?.Dispose();
            _crawled?.Dispose();
            _towReleased?.Dispose();
            _reached?.Dispose();
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
        public IEnumerator FreshGame_TheOpening_FromTheEclipseWakeToTheWestSignal()
        {
            float started = Time.time;
            yield return Boot();
            yield return WakeUp();
            yield return ExpectStep(OpeningKeys.DriveToLander, "B1: woken half empty, the lander's beacon ahead");
            yield return DriveToTheLander();
            yield return ExpectStep(OpeningKeys.ScanLander, "B3: at the lander");
            yield return ScanTheSet("ruin." + ScanTuning.LanderCargoId, "the lander's cargo bay");
            Assert.IsTrue(Known(StructureIds.ChargingPad), "the cargo bay's two fragments teach the pad");
            yield return TakeFromTheOpenCrate(ChargerKit, "B4: collect the lander's open crate");
            yield return ExpectStep(OpeningKeys.BuildPad, "B5: the hold covers the pad");
            Vector3 pad = Anchor("place.apron").Position;
            yield return Build(StructureIds.ChargingPad, pad, "B5: the charging pad on the apron");
            yield return ExpectStep(OpeningKeys.Charge, "B6");
            yield return ChargeOnThePad(pad);
            yield return LookAtTheModuleFromTheApron();
            yield return ExpectStep(OpeningKeys.FindSolar, "B7: LANDER RESERVE · 0%");
            yield return DriveTo(RuinsGate(), 3f, "the Lumen ruins");
            yield return ScanTheSet("ruin.FallenSolarArray", "the fallen solar array");
            yield return ReadTheAirlockPanel();
            yield return ExpectStep(OpeningKeys.CutOptics, "B7: the array's own panels and junction box cover it");
            yield return TryTheFaultedCutter("FallenSolarArray");
            yield return ExpectStep(OpeningKeys.TakeIgniter, "B7: the cutter sputtered, the rack is pointed at");
            yield return TakeFromTheOpenCrate(SparesRack, "B7: take the spare igniter from the rack");
            yield return ExpectStep(OpeningKeys.FitIgniter, "B7: the spare igniter in the hold");
            yield return FitTheIgniter();
            yield return ExpectStep(OpeningKeys.CutOptics, "B7: the cutter works");
            yield return CutTheCrates("FallenSolarArray", "the fallen array's panels and junction box");
            yield return ExpectStep(OpeningKeys.BuildSolar, "B8: the array's cost is covered");
            yield return DriveTo(RuinsGate(), 3f, "out of the Lumen ruins");
            // West of the pad: the ruins side stays open for the power module towed home at R6.
            yield return Build(StructureIds.SolarArray, pad + Vector3.left * BaseSpacing, "B8: the solar array");
            yield return UntilReached(OpeningMilestone.PowerOn, "Power on: the array powers the pad");
            yield return ExpectStep(OpeningKeys.VisitSite, "R5: the first free choice names the near sites");
            yield return VisitTheSeismicStation();
            yield return DockForTheDusk(pad);
            yield return ExpectStep(OpeningKeys.ScanModule, "B9: night is coming, the power module can store it");
            yield return ScanTheSet("ruin.HabitatModules", "the habitat's power modules");
            yield return ExpectStep(OpeningKeys.TowModuleHome, "B9: the module is the bank's frame, tow it home");
            yield return CutTheCrates("HabitatModules", "the habitat's cable reel");
            yield return TowTheModuleHome(pad);
            yield return ExpectStep(OpeningKeys.BuildBatteryBank, "B9: the module is home, build the bank on it");
            yield return Build(StructureIds.BatteryBank, ModulePosition(), "B9: the battery bank on the module");
            yield return UntilReached(OpeningMilestone.NightReady, "night-ready: the bank is built and wired to the pad");
            yield return TheFirstNightfall();
            yield return ExpectStep(OpeningKeys.ScanTower, "B11: after nightfall");
            yield return CutTheCrates("RadioTower", "the tower's mast sections, feed cable and dish panels");
            yield return ScanTheSet("ruin.RadioTower", "the radio tower ruin");
            yield return ExpectStep(OpeningKeys.BuildMast, "B11");
            yield return DriveTo(RuinsGate(), 3f, "out of the Lumen ruins");
            yield return Build(StructureIds.AntennaMast, pad + Vector3.back * BaseSpacing, "B11: the antenna mast");
            yield return UntilReached(OpeningMilestone.SignalWest, "the west signal answers");
            yield return ExpectStep(OpeningKeys.Transmit, "the mast stands: transmit from it");

            float total = Time.time - started;
            WriteReport(total);
            Assert.AreEqual(4, _milestones.Count, "FirstCharge, PowerOn, NightReady and SignalWest, once each");
            Assert.IsFalse(_milestones.Exists(m => m.Restored), "all reached in play");
            Assert.AreEqual(0, _crawls, "the opening never runs the battery down to a crawl");
            Assert.Less(total, TotalBudget, "the opening fits its budget");
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
            Assert.IsEmpty(_guidanceGaps, "guidance gaps:\n" + string.Join("\n", _guidanceGaps));
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
            Assert.IsNotNull(_gameplay.Opening, "the opening is wired");
            _context = bootstrap.Context;
            Assert.IsTrue(_gameplay.Tow.TryFindLoad(PowerModule, out Transform module),
                "the habitat's power module stands, towable (R6)");
            Assert.Less(SurfaceRules.HorizontalDistance(module.position, Ruin("HabitatModules").position),
                PowerModuleReach, "beside the habitat it came out of");
            _hud = new HudReader(scene);
            _chipWatch = new GameObject(nameof(ChipWatch)).AddComponent<ChipWatch>();
            _chipWatch.Watch(_hud);
            _localization = _context.Get<ILocalization>();
            _rover = (RoverController)_context.Get<IRoverRig>();
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awokeAt = Time.time);
            _crawled = _context.Events.Subscribe<LowPowerChanged>(low => _crawls += low.LowPower ? 1 : 0);
            _towReleased = _context.Events.Subscribe<TowReleased>(
                released => _towLetGo += released.Reason == TowRelease.Snapped ? 1 : 0);
            _reached = _context.Events.Subscribe<OpeningMilestoneReached>(_milestones.Add);
            _lines = _context.Events.Subscribe<NextStepChanged>(changed =>
            {
                if (changed.Reason == NextLineReason.Next && changed.LineKey.Length > 0)
                {
                    _nextLines.Add(changed.LineKey);
                }
            });
            _pilot = new Autopilot(_rover);
            _rover.SetDriveSource(_pilot);
            Assert.AreEqual(0.5f, _gameplay.Battery.Model.Charge01, 0.02f, "a new game wakes at half charge");
            End("Boot the real Main scene", $"{_gameplay.Opening.Caches.Count} supply crates; hold {Hold()}");
        }

        private IEnumerator WakeUp()
        {
            Begin();
            yield return Until(() => _awokeAt >= 0f, 30f, "07 wakes up on its own in the eclipse");
            yield return new WaitForSeconds(2f);
            Capture("01-eclipse-wake");
            End("B0: 07 wakes", "woke on its own (not by player input)");
        }

        private IEnumerator DriveToTheLander()
        {
            Begin();
            WorldAnchor lander = Anchor("place.lander");
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, lander.Position);
            yield return DriveTo(Flat(lander.Position + lander.Forward * LanderStandOff), 4f, "the lander's port side");
            Assert.IsTrue(_gameplay.Opening.Port.Reached, "07 has reached the lander");
            Capture("02-the-lander");
            End($"B2: drive to the lander ({distance:F0} m)", "along the old cable");
        }

        /// <summary>
        /// Every fragment of <paramref name="blueprintId"/>'s set, one by one: to a spot facing it, its brackets show
        /// and the Scan chip with them; hold E until it is drawn whole, let go before the next.
        /// </summary>
        private IEnumerator ScanTheSet(string blueprintId, string what)
        {
            Begin();
            ScanField scans = _gameplay.Scans;
            int scanned = 0;
            for (ScanFragment next = FirstUnscanned(scans, blueprintId); next != null;
                 next = FirstUnscanned(scans, blueprintId))
            {
                yield return Scan(next, what);
                scanned++;
            }

            Assert.Greater(scanned, 0, $"{what} has fragments to scan");
            Capture("scan-" + blueprintId);
            End($"Scan {what}", $"{scanned} fragment(s)");
        }

        private static ScanFragment FirstUnscanned(ScanField scans, string blueprintId)
        {
            for (int i = 0; scans.Find(FragmentId(blueprintId, i)) != null; i++)
            {
                ScanFragment fragment = scans.Find(FragmentId(blueprintId, i));
                if (!fragment.Scanned)
                {
                    return fragment;
                }
            }

            return null;
        }

        /// <summary>B7: the habitat's airlock panel, read as 07 passes it; its readout shows on the CRT.</summary>
        /// <summary>
        /// R5, the first free choice: out to the 1971 seismic station, one of the two near sites, to read its dead
        /// station box (its curiosity scan); the sky holds in the afternoon the whole way.
        /// </summary>
        private IEnumerator VisitTheSeismicStation()
        {
            Begin();
            ScanFragment station = _gameplay.Scans.Find(StoryScan.IdPrefix + SeismicStory);
            Assert.IsNotNull(station, "the seismic station has its curiosity scan");
            float held = _context.Get<ISunlight>().TimeSeconds;
            Vector3 home = Flat(Anchor("place.apron").Position - station.Point).normalized;
            yield return Scan(station, "the seismic station", home, PanelStandOff);
            Assert.IsTrue(_gameplay.Opening.Dusk.Visited, "R5: 07 visited a near site");
            Assert.IsFalse(_gameplay.Opening.DuskBegun, "the afternoon holds until 07 docks");
            Assert.AreEqual(held, _context.Get<ISunlight>().TimeSeconds, 1f, "the Sun held while 07 looked round");
            Capture("08-the-seismic-station");
            End("R5: visit the seismic station", "SEISMIC STATION · DEPLOYED 1971");
        }

        /// <summary>
        /// R6: the habitat's power module home on the winch cable, as a player tows it: out beyond its hitch, 07 turns
        /// its back to it and reverses until the winch is in reach, the tether key (RMB) hooks it, and 07 tows it out
        /// of the ruins and home; the winch lets it go on the apron.
        /// </summary>
        private IEnumerator TowTheModuleHome(Vector3 pad)
        {
            Begin();
            Assert.IsTrue(_gameplay.Tow.TryFindLoad(PowerModule, out Transform module), "the module stands to be towed");
            Vector3 hitch = HitchOf(module);
            Vector3 outward = Flat(hitch - module.position).normalized;
            Vector3 stand = Flat(hitch) + outward * HookStandOff;
            yield return DriveTo(stand + outward * HookLineUp, LineUpArrive, "a line-up beyond the module's hitch");
            yield return DriveTo(stand, 1f, "the module's hitch", ApproachThrottle);
            yield return FaceToward(stand + outward * HookLineUp, "away from the module, the winch to its hitch");
            Transform winch = _context.Get<IRoverRig>().WinchOrigin;
            _pilot.Reverse = UnstickReverseThrottle;
            yield return Until(() => SurfaceRules.HorizontalDistance(winch.position, hitch) <= HookBackTo,
                HookBackSeconds, "07 backs its winch up to the module's hitch");
            _pilot.Reverse = 0f;
            yield return Until(() => _rover.Speed < StopSpeed, FollowTimeout, "07 comes to rest at the hitch");
            yield return Tap(_mouse.rightButton);
            yield return Until(() => _gameplay.Tow.IsTowing, FollowTimeout, "the tether key hooks the module",
                () => $"the winch {SurfaceRules.HorizontalDistance(winch.position, hitch):F1} m from the hitch");
            Capture("09-the-module-hooked");
            yield return DriveTo(RuinsGate(), 3f, "out of the Lumen ruins, towing");
            Vector3 fromHome = Flat(module.position - pad).normalized;
            yield return DriveTo(Flat(pad) + fromHome * HomeStop, HomeArrive, "home to the apron, towing");
            yield return Until(() => !_gameplay.Tow.IsTowing, HomeLetGoSeconds,
                "the winch lets the module go on the apron",
                () => $"the module {SurfaceRules.HorizontalDistance(module.position, pad):F1} m from the apron");
            Assert.AreEqual(0, _towLetGo, "the cable never let the module go on the way");
            Capture("09-the-module-home");
            End("R6: tow the power module home", $"{_gameplay.Tow.Lifts} lift(s) free on the way");
        }

        /// <summary>
        /// Sunrise over the apron (B6, the Sun just out from behind Earth): from the pad, the view turns to where the
        /// habitat's power module stands waiting to be towed home, and the capture keeps it (R6's spot, read from home).
        /// </summary>
        private IEnumerator LookAtTheModuleFromTheApron()
        {
            Look(ModulePosition());
            yield return new WaitForSeconds(ViewSettleSeconds);
            Capture("06-the-module-from-the-apron");
        }

        private Vector3 ModulePosition()
        {
            Assert.IsTrue(_gameplay.Tow.TryFindLoad(PowerModule, out Transform module));
            return module.position;
        }

        private static Vector3 HitchOf(Transform load)
        {
            foreach (Transform node in load.GetComponentsInChildren<Transform>())
            {
                if (node.name == TowLoads.HitchNode)
                {
                    return node.position;
                }
            }

            Assert.Fail($"{load.name} has its {TowLoads.HitchNode}");
            return load.position;
        }

        /// <summary>R6: back onto the pad; the dock after the visit begins the first dusk.</summary>
        private IEnumerator DockForTheDusk(Vector3 pad)
        {
            Begin();
            yield return DriveTo(Flat(pad) + Vector3.back * 6f, 2f, "a line-up for the pad");
            yield return DriveTo(Flat(pad), 0.5f, "the pad", ApproachThrottle);
            yield return Until(() => _gameplay.Opening.DuskBegun, DuskTimeout,
                "the dock after the visit begins the first dusk",
                () => $"on the pad {_gameplay.PadLock.OnPad}, lock raised {_gameplay.PadLock.Raised}");
            End("R6: dock, and the first dusk begins", $"{_gameplay.Battery.Model.Charge01:P0}");
        }

        private IEnumerator ReadTheAirlockPanel()
        {
            Begin();
            var read = new List<StoryScanned>();
            using (_context.Events.Subscribe<StoryScanned>(read.Add))
            {
                ScanFragment panel = _gameplay.Scans.Find(StoryScan.IdPrefix + "airlock");
                Assert.IsNotNull(panel, "the habitat has its airlock panel to read");
                Vector3 outward = Flat(panel.Point - Ruin("HabitatModules").position).normalized;
                yield return Scan(panel, "the airlock panel", outward, PanelStandOff);
                Assert.AreEqual(1, read.Count, "the panel's readout shows once");
                Assert.AreEqual(panel.FragmentId, read[0].StoryId, "it is the airlock panel that was read");
                Assert.IsTrue(panel.Scanned, "the airlock panel is scanned");
            }

            Capture("07-the-airlock-panel");
            End("B7: read the airlock panel", "HABITAT SEAL · LOCKED · 28 OCT 1988");
        }

        /// <summary>
        /// To a spot facing <paramref name="fragment"/>: its brackets show and the Scan chip with them; hold E until
        /// it is drawn whole, let go before the next.
        /// </summary>
        private IEnumerator Scan(ScanFragment fragment, string what)
        {
            Vector3 away = Flat(_rover.Position - fragment.Point);
            yield return Scan(fragment, what, away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back,
                ScanStandOff);
        }

        /// <summary>
        /// As the plain scan, coming in along <paramref name="preferred"/> (where the ground is clear) to
        /// <paramref name="standOff"/> metres: square to a panel on a wall, close enough that it fills the view.
        /// </summary>
        private IEnumerator Scan(ScanFragment fragment, string what, Vector3 preferred, float standOff)
        {
            ScanField scans = _gameplay.Scans;
            Vector3 away = ClearSide(fragment.Point, preferred, standOff, fragment.FragmentId, null);
            if (Clear(fragment.Point + away * (standOff + ScanLineUp), null))
            {
                yield return DriveTo(Flat(fragment.Point + away * (standOff + ScanLineUp)), LineUpArrive,
                    "a line-up for a scan");
            }

            yield return DriveTo(Flat(fragment.Point), standOff, $"the spot facing {what}", ApproachThrottle);
            Look(fragment.Point);
            yield return Until(() => OnSetCandidate(scans, fragment), ScanLookTimeout,
                $"07's view rests on {fragment.FragmentId}: its brackets and the Scan chip show",
                () => $"scan candidate {scans.Candidate?.FragmentId ?? "none"} (scanned " +
                      $"{scans.Candidate?.Scanned}, airlock scanned {scans.Find("story.airlock")?.Scanned}), primary hint " +
                      $"{_gameplay.Hints.Primary.Kind} at {_gameplay.Hints.Primary.Position} (piece " +
                      $"{_gameplay.Salvage.Candidate?.Site.Id}/{_gameplay.Salvage.Candidate?.Number}, crate " +
                      $"{_gameplay.Containers.Candidate?.Id}); the target at {fragment.Point}, the view " +
                      $"{ViewOff(fragment.Point):F0} deg off it ({View(fragment.Point)}), " +
                      $"{Vector3.Distance(Flat(fragment.Point), Flat(_rover.Position)):F2} m flat, " +
                      $"{HeadingOff(fragment.Point):F0} deg off");
            fragment = scans.Candidate;
            if (HeadingOff(fragment.Point) > LensFacing)
            {
                // 07 came to rest side-on (the stand sidesteps round the ruins): its cheek lens only scans what lies
                // ahead of it, so it turns on the spot to face the target, as a player steers.
                yield return FaceToward(fragment.Point, fragment.FragmentId);
            }

            yield return FirstUse("scan: hold E", VerbScanKey);
            for (int attempt = 0; attempt < HoldAttempts && !fragment.Scanned; attempt++)
            {
                // The framing camera can swing a neighbour into the brackets as the key goes down, or take them off
                // the target half way: the player lets go, looks at the target again and holds again (progress stays).
                Look(fragment.Point);
                Press(_keyboard.eKey);
                float begin = Time.time + HoldBeginSeconds;
                while (scans.Scanning == null && !fragment.Scanned && Time.time < begin)
                {
                    yield return null;
                }

                float limit = Time.time + scans.Tuning.Seconds(fragment.Size) + ScanAllowance;
                while (scans.Scanning != null && !fragment.Scanned && Time.time < limit)
                {
                    yield return null;
                }

                Release(_keyboard.eKey);
                yield return new WaitForSeconds(HoldReleaseSeconds);
            }

            yield return Until(() => fragment.Scanned, 0f, $"held E scans {fragment.FragmentId}");
            yield return null;
        }

        /// <summary>The brackets are on this part or another unscanned part of its set: a player scans what they frame.</summary>
        private static bool OnSetCandidate(ScanField scans, ScanFragment fragment)
        {
            ScanFragment candidate = scans.Candidate;
            if (candidate == null)
            {
                return false;
            }

            int split = fragment.FragmentId.LastIndexOf('.');
            bool story = fragment.FragmentId.StartsWith(StoryScan.IdPrefix, StringComparison.Ordinal);
            return candidate == fragment || (!story && !candidate.Scanned && split > 0 &&
                   candidate.FragmentId.StartsWith(fragment.FragmentId.Substring(0, split + 1),
                       StringComparison.Ordinal));
        }

        /// <summary>
        /// An open crate (B4: the lander's charger kit, once the line says to collect the parts; B7: the spares rack by
        /// the fallen array): up to it, where a held E takes everything it holds.
        /// </summary>
        private IEnumerator TakeFromTheOpenCrate(string cacheId, string what)
        {
            Begin();
            SealedContainer crate = _gameplay.Containers.Find(cacheId);
            Assert.IsTrue(crate.StandsOpen, $"{cacheId} stands open from the start: no cutting before the igniter");
            Assert.IsFalse(Taken(cacheId), $"{cacheId} gives nothing until 07 takes it");
            if (cacheId == ChargerKit)
            {
                yield return ExpectStep(OpeningKeys.CollectParts, "B4: the cutter is FAULT, the crate holds parts");
            }

            if (!_gameplay.Hints.TryGet(InteractionKind.PickUp, out _))
            {
                // Out of the crate's reach: drive up to it, as a player would.
                Vector3 side = crate.SealSide(_context.Get<IRoverState>().Position);
                Vector3 front = Flat(crate.CutPoint.position) + side * SealStandOff;
                yield return DriveTo(front + side * SealLineUp, LineUpArrive, "a line-up for " + cacheId);
                yield return DriveTo(front, 1f, cacheId);
            }

            yield return Until(() => _gameplay.Hints.TryGet(InteractionKind.PickUp, out _), FollowTimeout,
                cacheId + " offers what it holds (Pick up)");
            Press(_keyboard.eKey);
            yield return Until(() => Taken(cacheId), FollowTimeout, "held E takes what " + cacheId + " holds");
            Release(_keyboard.eKey);
            Capture(cacheId == ChargerKit ? "04-the-open-crate" : "07-the-spares-rack");
            End(what, $"igniter in the hold {_gameplay.StartKit.CarriesIgniter}; hold {Hold()}");
        }

        /// <summary>Open crate <paramref name="cacheId"/> has given everything it held to 07's hold.</summary>
        private bool Taken(string cacheId)
        {
            SupplyCaches caches = _gameplay.Opening.Caches;
            for (int i = 0; i < caches.Count; i++)
            {
                if (caches.Get(i).Id == cacheId)
                {
                    return caches.Left(i).IsFree && !caches.PartLeft(i);
                }
            }

            Assert.Fail($"the opening has a supply crate {cacheId}");
            return true;
        }

        /// <summary>
        /// B7: the first try at <paramref name="frame"/>'s sealed crates before the igniter is fitted: a held E at the
        /// first seal, and the faulted cutter sputters (CUTTER · NO IGNITER), cutting nothing.
        /// </summary>
        private IEnumerator TryTheFaultedCutter(string frame)
        {
            Begin();
            SupplyCaches caches = _gameplay.Opening.Caches;
            for (int i = 0; i < caches.Count; i++)
            {
                SupplyCache cache = caches.Get(i);
                SealedContainer crate = _gameplay.Containers.Find(cache.Id);
                if (cache.AnchorId != frame || crate.Opened)
                {
                    continue;
                }

                // The faulted try teaches nothing: the Cut chip comes once the cutter works.
                yield return ApproachTheSeal(cache, crate);
                Press(_keyboard.eKey);
                yield return Until(() => _gameplay.StartKit.Sputtered, FollowTimeout,
                    $"held E at the seal of {cache.Id} sputters the faulted cutter",
                    () => $"crate candidate {_gameplay.Containers.Candidate?.Id ?? "none"}, cutter " +
                          $"{_gameplay.StartKit.StateOf(RoverSystem.Cutter)}");
                Release(_keyboard.eKey);
                Assert.AreEqual(0f, crate.Progress, "the faulted cutter cuts nothing");
                Capture("07-the-cutter-sputters");
                End("B7: the faulted cutter sputters", $"at {cache.Id}: CUTTER · NO IGNITER");
                yield break;
            }

            Assert.Fail($"{frame} has a sealed crate for the cutter to try");
        }

        /// <summary>Cuts every sealed crate standing by <paramref name="frame"/> (an anchor or a ruin piece).</summary>
        private IEnumerator CutTheCrates(string frame, string what)
        {
            Begin();
            int cut = 0;
            SupplyCaches caches = _gameplay.Opening.Caches;
            for (int i = 0; i < caches.Count; i++)
            {
                SupplyCache cache = caches.Get(i);
                SealedContainer crate = _gameplay.Containers.Find(cache.Id);
                if (cache.AnchorId != frame || crate.Opened)
                {
                    continue;
                }

                yield return ApproachTheSeal(cache, crate);
                yield return FirstUse("cut: hold E", VerbCutKey);
                Press(_keyboard.eKey);
                yield return Until(() => crate.Opened, SealCutTimeout, $"held E cuts the seal of {cache.Id}",
                    () => $"crate candidate {_gameplay.Containers.Candidate?.Id ?? "none"}, cutter ready " +
                          $"{_gameplay.Cutter.Ready}, salvage candidate {_gameplay.Salvage.Candidate?.Number}, " +
                          $"scan candidate {_gameplay.Scans.Candidate?.FragmentId ?? "none"}, " +
                          $"primary hint {_gameplay.Hints.Primary.Kind}, low power " +
                          $"{_gameplay.Battery.Model.IsLowPower}; its cut point {crate.CutPoint.position}, " +
                          $"{Vector3.Distance(Flat(crate.CutPoint.position), Flat(_rover.Position)):F2} m flat " +
                          $"(reach {_gameplay.Salvage.Tuning.ReachRadius:F1}), facing " +
                          $"{HeadingOff(crate.CutPoint.position):F0} deg " +
                          $"(claim {_gameplay.Cargo.ContainerClaimAngle:F0}); the crate {Shape(crate.Root)}");
                Release(_keyboard.eKey);
                yield return Until(() => crate.StandsOpen, _gameplay.Cargo.ContainerLidSeconds + 2f,
                    $"{cache.Id} stands open");
                cut++;
            }

            Assert.Greater(cut, 0, $"{what}: crates to cut");
            End($"Cut {what}", $"{cut} crate(s); hold {Hold()}");
        }

        /// <summary>
        /// Out to the open ground before <paramref name="crate"/>'s seal, in to it and nosing at it until the crate
        /// claims E, backing off for room to turn when too close, as a player would.
        /// </summary>
        private IEnumerator ApproachTheSeal(SupplyCache cache, SealedContainer crate)
        {
            Vector3 side = ClearSide(crate.CutPoint.position, crate.SealSide(_context.Get<IRoverState>().Position),
                SealStandOff,
                $"{crate.Id} (its seal at {crate.CutPoint.position}; its frame {Frame(cache.AnchorId)})",
                crate.Root);
            Vector3 front = Flat(crate.CutPoint.position) + side * SealStandOff;
            if (Clear(crate.CutPoint.position + side * (SealStandOff + SealLineUp), crate.Root))
            {
                // Out to the open ground before the seal first (CrateApproachSessions keeps it clear): a
                // straight run between crates may cross a ruin.
                yield return DriveTo(front + side * (SealLineUp + SealOpenGround), LineUpArrive,
                    "the open ground before " + cache.Id);
                yield return DriveTo(front + side * SealLineUp, LineUpArrive, "a line-up for " + cache.Id);
            }

            yield return DriveTo(front, SealArrive, "the seal of " + cache.Id, ApproachThrottle,
                () => $"the crate at {crate.Root.position} facing {crate.Root.forward}; its frame " +
                      $"{cache.AnchorId}: {Frame(cache.AnchorId)}");
            yield return NoseIn(crate);
            for (int attempt = 1; attempt < SealTries && _gameplay.Containers.Candidate != crate; attempt++)
            {
                // Too close to swing onto a small crate's seal: back straight off for room to turn, and nose in
                // again from there, as a player would.
                _pilot.Reverse = UnstickReverseThrottle;
                yield return new WaitForSeconds(SealBackOffSeconds);
                _pilot.Reverse = 0f;
                yield return NoseIn(crate);
            }
        }

        /// <summary>
        /// 07 creeps at <paramref name="crate"/>'s seal, steering at it, until the crate claims E (07 faces it within
        /// reach) or 07's nose is at it; then it stops.
        /// </summary>
        private IEnumerator NoseIn(SealedContainer crate)
        {
            _pilot.GoTo(Flat(crate.CutPoint.position), SealNose, ApproachThrottle);
            float deadline = Time.time + SealNoseSeconds;
            while (_gameplay.Containers.Candidate != crate && !_pilot.Arrived && Time.time < deadline)
            {
                yield return null;
            }

            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 comes to rest at the seal of " + crate.Id);
        }

        /// <summary>
        /// Builds <paramref name="structureId"/> near <paramref name="at"/> as a player does: B opens the build menu,
        /// the arrows pick it, E takes it up, 07's look rests on a spot the placement rules accept (the aim is the
        /// run's, like the wheel), E places and pays it from the hold, and a held E welds it, so the weld's battery
        /// cost is in the run.
        /// </summary>
        private IEnumerator Build(string structureId, Vector3 at, string what)
        {
            Begin();
            BuildField build = _gameplay.Building;
            StructureDefinition definition = build.Catalog.Find(structureId);
            Assert.IsTrue(Known(structureId), $"{structureId}'s blueprint is learned");
            yield return DriveTo(Flat(at) + Vector3.back * BuildStandOff, 1f, "beside the build spot");
            yield return FirstUse("build: B", VerbBuildKey);
            yield return Tap(_keyboard.bKey);
            yield return Until(() => build.State == BuildModeState.Menu, FollowTimeout, "B opens the build menu");
            yield return Until(() => _hud.BuildStripShown, FollowTimeout, "the build strip shows the menu");
            for (int step = 0; step < MaxMenuSteps && build.Controller.SelectedStructure?.Id != structureId; step++)
            {
                yield return Tap(_keyboard.rightArrowKey);
            }

            Assert.AreEqual(structureId, build.Controller.SelectedStructure?.Id, $"the arrows pick {structureId}");
            yield return Tap(_keyboard.eKey);
            yield return Until(() => build.State == BuildModeState.Placing, FollowTimeout,
                $"E takes {structureId} up to place");
            yield return LookForASpot(build, at, structureId);
            Recipe before = new Recipe(_gameplay.Materials.Metal, _gameplay.Materials.Wiring,
                _gameplay.Materials.Optics);
            yield return Tap(_keyboard.eKey);
            yield return Until(() => build.State == BuildModeState.Welding, FollowTimeout,
                $"E places {structureId}", () => $"state {build.State}, reason {build.Controller.CandidateReason}");
            Assert.AreEqual(before.Metal - definition.BuildCost.Metal, _gameplay.Materials.Metal,
                $"placing {structureId} pays its metal from the hold");
            int instance = build.Controller.ActiveWeldInstanceId;
            Press(_keyboard.eKey);
            yield return Until(() => _gameplay.PlacedBase.TryGetStructure(instance, out PlacedStructure built) &&
                                     built.IsBuilt, definition.BuildSeconds + FollowTimeout,
                $"a held E welds {structureId}",
                () => $"state {build.State}, welding {build.IsWelding}, active " +
                      $"{build.Controller.ActiveWeldInstanceId} (placed {instance})");
            Release(_keyboard.eKey);
            build.SetAimPointOverride(null);
            yield return Until(() => build.State == BuildModeState.Closed, FollowTimeout,
                "a finished weld closes build mode");
            Capture("build-" + structureId);
            End(what, "placed, paid and welded");
        }

        /// <summary>
        /// B6: onto the pad, which locks 07 in and charges it from the lander's reserve until 07 is full and the
        /// reserve has given out.
        /// </summary>
        private IEnumerator ChargeOnThePad(Vector3 pad)
        {
            Begin();
            BatteryModel battery = _gameplay.Battery.Model;
            yield return DriveTo(Flat(pad) + Vector3.back * 6f, 2f, "a line-up for the pad");
            yield return DriveTo(Flat(pad), 0.5f, "the pad", ApproachThrottle);
            yield return Until(() => battery.IsCharging && battery.Charger == ChargerKind.Pad, 6f,
                "the built pad charges 07");
            yield return UntilReached(OpeningMilestone.FirstCharge, "FirstCharge: the eclipse ends");
            yield return Until(() => _gameplay.PadLock.Raised, FollowTimeout, "the pad's dock lock rises round 07");
            Capture("06-the-pad-locks");
            yield return Until(() => !battery.IsCharging, battery.Cells * 6f, "07 charges full");
            Assert.IsTrue(_gameplay.Opening.Port.Spent, "the lander's old cells gave out after one charge");
            End("B6: charge on the pad", $"{battery.Charge01:P0}, LANDER RESERVE · 0%");
        }

        /// <summary>B10: the night-ready base lets the first sunset run; the first night falls on a lit base.</summary>
        private IEnumerator TheFirstNightfall()
        {
            Begin();
            var sunlight = _context.Get<ISunlight>();
            yield return Until(() => sunlight.Phase == SkyPhase.Earthlit, SunsetTimeout, "the first night falls");
            Capture("10-first-nightfall");
            End("B10: the first nightfall", "the base's lights over a dark basin");
        }

        /// <summary>
        /// The opening stands at the step whose line is <paramref name="key"/>, and the success that led there named
        /// it as the next line (guidance audit G2); the first step's line is the session's start, no success.
        /// </summary>
        private IEnumerator ExpectStep(string key, string why)
        {
            yield return Until(() => _gameplay.Opening.Flow.Current == key, FollowTimeout,
                $"the opening stands at '{key}' ({why}), not '{_gameplay.Opening.Flow.Current}'");
            if (key.Length > 0 && key != OpeningKeys.DriveToLander)
            {
                Assert.Contains(key, _nextLines,
                    $"the success before '{key}' ({why}) named it as the next line (guidance audit G2)");
            }
        }

        /// <summary>
        /// On <paramref name="verb"/>'s first use a chip with its word (<paramref name="wordKey"/>, UI's) has shown:
        /// the world shows it once, when the verb first becomes possible (opening v3 1.5), so it may have come and gone
        /// before 07 is in place, or it shows within a little while. A missing chip is a guidance gap the run lists at
        /// the end rather than stopping on.
        /// </summary>
        private IEnumerator FirstUse(string verb, string wordKey)
        {
            if (!_verbsUsed.Add(verb))
            {
                yield break;
            }

            string word = _localization.Get(wordKey);
            float deadline = Time.time + FirstUseTimeout;
            string chip = _chipWatch.SeenWith(word);
            while (chip.Length == 0 && Time.time < deadline)
            {
                yield return null;
                chip = _chipWatch.SeenWith(word);
            }

            if (chip.Length == 0)
            {
                InteractionHint primary = _gameplay.Hints.Primary;
                _guidanceGaps.Add($"{verb}: no '{word}' chip by its first use (chips seen: " +
                                  $"{_chipWatch.Describe()}; on screen: chip '{_hud.Chip}', key chip " +
                                  $"'{_hud.KeyChip}'; primary hint {primary.Kind}, ready {primary.Ready}; step " +
                                  $"{_gameplay.Opening.Flow.Current})");
                Capture("gap-" + _verbsUsed.Count.ToString(CultureInfo.InvariantCulture));
            }

            _chips.Add($"{verb}: {(chip.Length > 0 ? chip : "none")}");
        }

        private IEnumerator Tap(ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return new WaitForSeconds(TapSeconds);
        }

        /// <summary>
        /// 07's look rests on a spot near <paramref name="at"/> the placement rules accept, in rings round it.
        /// </summary>
        private IEnumerator LookForASpot(BuildField build, Vector3 at, string structureId)
        {
            var terrain = _context.Get<ITerrainQuery>();
            for (int ring = 0; ring < PlaceRings; ring++)
            {
                for (int step = 0; step < PlaceRingSpots; step++)
                {
                    Vector3 spot = at + Quaternion.Euler(0f, step * 360f / PlaceRingSpots, 0f) * Vector3.forward *
                        (ring * PlaceRingStep);
                    build.SetAimPointOverride(SurfaceRules.OnSurface(terrain, spot.x, spot.z));
                    yield return null;
                    yield return null;
                    if (build.Controller.CanPlaceNow)
                    {
                        yield break;
                    }

                    if (ring == 0)
                    {
                        break;
                    }
                }
            }

            Assert.Fail($"{structureId} finds a spot near {at} the placement rules accept " +
                        $"(last reason {build.Controller.CandidateReason})");
        }

        /// <summary>
        /// B7's fit where 07 stands (a field swap), as a player does it: Tab opens 07's face screen, the arrows walk
        /// the focus to the cutter's socket, Enter fits the spare igniter there, Tab closes the screen.
        /// </summary>
        private IEnumerator FitTheIgniter()
        {
            RoverStartKit kit = _gameplay.StartKit;
            Assert.IsTrue(kit.CanFitIgniter, "off the pad, the cutter socket takes the field-swap igniter");
            yield return FirstUse("fit the igniter: Tab", VerbFitKey);
            yield return Tap(_keyboard.tabKey);
            yield return Until(() => _hud.FaceFocus.Length > 0, FollowTimeout, "the face screen opens with a focus");
            for (int step = 0; step < MaxMenuSteps && _hud.FaceFocusedDevice != CutterDevice; step++)
            {
                int device = _hud.FaceFocusedDevice;
                yield return Tap(device < 0 ? _keyboard.rightArrowKey
                    : device < CutterDevice ? _keyboard.downArrowKey : _keyboard.upArrowKey);
            }

            Assert.AreEqual(CutterDevice, _hud.FaceFocusedDevice,
                $"the arrows reach the cutter's socket on the face screen (focus '{_hud.FaceFocus}')");
            yield return Tap(_keyboard.enterKey);
            yield return Until(() => !kit.CarriesIgniter, FollowTimeout, "Enter fits the spare igniter",
                () => $"focus '{_hud.FaceFocus}', can fit {kit.CanFitIgniter}");
            yield return Tap(_keyboard.tabKey);
            var face = _context.Get<IRoverFaceScreen>();
            yield return Until(() => !face.IsOpen, FollowTimeout, "Tab closes the face screen");
        }

        private IEnumerator UntilReached(OpeningMilestone milestone, string what)
        {
            yield return Until(() => _milestones.Exists(reached => reached.Milestone == milestone), FollowTimeout,
                what);
        }

        private IEnumerator DriveTo(Vector3 target, float arriveRadius, string what, float maxThrottle = 1f,
            Func<string> detail = null)
        {
            var terrain = _context.Get<ITerrainQuery>();
            Vector3 position = _rover.Position;
            bool Blocked(Vector2 point)
            {
                float ground = terrain.SampleHeight(point.x, point.y);
                return Physics.CheckSphere(new Vector3(point.x, ground + RouteClearance, point.y), RouteClearance,
                    Layers.PropMask, QueryTriggerInteraction.Ignore);
            }

            // Round the props that stand between, as a player drives (a straight line stays one drive).
            RoutePlanner.Plan(new Vector2(position.x, position.z), new Vector2(target.x, target.z), Blocked,
                RouteCellMetres, RouteMarginMetres, RouteRelaxMetres, _routePoints);
            var route = new List<Vector3>();
            foreach (Vector2 point in _routePoints)
            {
                route.Add(new Vector3(point.x, target.y, point.y));
            }

            for (int i = 0; i < route.Count - 1; i++)
            {
                yield return DriveDirect(route[i], RouteArriveRadius, what + " (round the props)", maxThrottle, detail,
                    false);
            }

            yield return DriveDirect(target, arriveRadius, what, maxThrottle, detail, true);
        }

        private IEnumerator DriveDirect(Vector3 target, float arriveRadius, string what, float maxThrottle,
            Func<string> detail, bool settle)
        {
            _pilot.GoTo(target, arriveRadius, maxThrottle);
            float deadline = Time.time + DriveTimeout;
            int unsticks = 0;
            float windowStart = _pilot.Distance;
            float windowEnd = Time.time + StuckSeconds;
            while (!_pilot.Arrived && Time.time < deadline)
            {
                yield return null;
                if (Time.time < windowEnd)
                {
                    continue;
                }

                if (windowStart - _pilot.Distance < StuckProgress)
                {
                    // Blocked by a ruin or a crate, or circling the spot: back off and come round, as a player would.
                    unsticks++;
                    yield return Unstick(target, arriveRadius, maxThrottle, unsticks);
                }

                windowStart = _pilot.Distance;
                windowEnd = Time.time + StuckSeconds;
            }

            yield return Until(() => _pilot.Arrived, 0f, "07 reaches " + what,
                () => $"{unsticks} unstick(s); " + (detail != null ? detail() : string.Empty));
            if (!settle)
            {
                yield break;
            }

            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 comes to rest at " + what);
        }

        /// <summary>
        /// Backs 07 off whatever blocks it, swings round to one side (alternating), then heads for
        /// <paramref name="target"/> again.
        /// </summary>
        private IEnumerator Unstick(Vector3 target, float arriveRadius, float maxThrottle, int attempt)
        {
            _pilot.Reverse = UnstickReverseThrottle;
            yield return new WaitForSeconds(UnstickReverseSeconds);
            _pilot.Reverse = 0f;
            Vector3 toward = Flat(target - _rover.Position);
            toward = toward.sqrMagnitude > 1e-4f ? toward.normalized : Flat(_rover.Rotation * Vector3.forward);
            Vector3 side = Vector3.Cross(Vector3.up, toward) * (attempt % 2 == 0 ? -1f : 1f);
            float step = UnstickSidestep * Mathf.Min(attempt, UnstickMaxGrowth);
            _pilot.GoTo(Flat(_rover.Position) + side * step + toward * step, 1f, maxThrottle);
            float until = Time.time + UnstickSidestepSeconds;
            while (!_pilot.Arrived && Time.time < until)
            {
                yield return null;
            }

            _pilot.GoTo(target, arriveRadius, maxThrottle);
        }

        private IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            yield return Until(condition, timeout, what, () => string.Empty);
        }

        /// <summary>As the plain wait; <paramref name="detail"/> says, on a timeout, what the game was doing.</summary>
        private IEnumerator Until(Func<bool> condition, float timeout, string what, Func<string> detail)
        {
            float deadline = Time.time + timeout;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }

            if (!condition())
            {
                Capture("timed-out");
            }

            Assert.IsTrue(condition(), $"timed out after {timeout:F0} s waiting until {what} (07 at {_rover.Position}, " +
                $"heading {_rover.Rotation.eulerAngles.y:F0}, {_rover.Speed:F1} m/s, steering for {_pilot.Target}) " +
                (condition() ? string.Empty : detail()));
        }

        /// <summary>Degrees between 07's heading and <paramref name="point"/>, on the ground.</summary>
        private float HeadingOff(Vector3 point)
        {
            return Vector3.Angle(Flat(_rover.Rotation * Vector3.forward), Flat(point - _rover.Position));
        }

        /// <summary>Degrees between the view and <paramref name="point"/>.</summary>
        private float ViewOff(Vector3 point)
        {
            Transform view = _context.Get<IViewCamera>().Camera.transform;
            return Vector3.Angle(view.forward, point - view.position);
        }

        /// <summary>The view's turn and tilt against <paramref name="point"/>, and the orbit's own.</summary>
        private string View(Vector3 point)
        {
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            Transform view = rig.Camera.transform;
            Vector3 toward = point - view.position;
            float turn = Vector3.SignedAngle(Flat(view.forward), Flat(toward), Vector3.up);
            float tilt = Vector3.SignedAngle(Flat(view.forward), view.forward, view.right);
            float tiltTo = Vector3.SignedAngle(Flat(toward), toward, view.right);
            return $"turn {turn:F0} deg, view tilt {tilt:F0} vs {tiltTo:F0} deg to it, orbit bearing " +
                   $"{rig.Orbit.Bearing:F0} pitch {rig.Orbit.Pitch:F0}, camera at {view.position}";
        }

        /// <summary>Turns the view to <paramref name="point"/>, as the player's mouse would.</summary>
        /// <summary>07 turns on the spot until it faces <paramref name="point"/> within the lens's reach.</summary>
        private IEnumerator FaceToward(Vector3 point, string what)
        {
            _pilot.Target = null;
            _pilot.Face = point;
            yield return Until(() => HeadingOff(point) <= FacedWithin, FollowTimeout, $"07 turns to face {what}");
            _pilot.Face = null;
            Look(point);
            yield return Until(() => _rover.Speed < StopSpeed, FollowTimeout, $"07 comes to rest facing {what}");
        }

        private void Look(Vector3 point)
        {
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            // From 07, not the camera: the orbit may hang anywhere round 07 (in front of it after a turn), and a
            // bearing taken from there points the view away.
            Vector3 toward = Flat(point - _rover.Position);
            rig.Orbit.AdoptBearing(Quaternion.LookRotation(toward, Vector3.up).eulerAngles.y);
        }

        /// <summary>
        /// The way out from <paramref name="point"/> (a seal or a scan target) 07 comes in along:
        /// <paramref name="preferred"/> if 07 can stand there, else the first side round it where it can (they stand
        /// among the ruins and by the lander; a seal claims E, and a scan shows, whichever side 07 faces it from). The
        /// line-up further out is driven only where that ground is clear too. <paramref name="own"/> (the crate whose
        /// seal it is, or null) never blocks: 07 noses up to it.
        /// </summary>
        private static Vector3 ClearSide(Vector3 point, Vector3 preferred, float standOff, string what, Transform own)
        {
            for (int step = 0; step < ApproachSides; step++)
            {
                float turn = (step + 1) / 2 * (360f / ApproachSides) * (step % 2 == 0 ? 1f : -1f);
                Vector3 side = Quaternion.Euler(0f, turn, 0f) * preferred;
                if (Clear(point + side * standOff, own))
                {
                    return side;
                }
            }

            var blockers = new List<string>();
            for (int step = 0; step < ApproachSides; step++)
            {
                Vector3 side = Quaternion.Euler(0f, step * (360f / ApproachSides), 0f) * preferred;
                Collider blocker = Blocker(point + side * standOff, own);
                blockers.Add(blocker != null ? $"{blocker.name} {blocker.bounds.center} size {blocker.bounds.size}"
                    : "?");
            }

            Assert.Fail($"{what} has open ground on some side to come in from (blocked by " +
                        $"{string.Join(", ", blockers)})");
            return preferred;
        }

        /// <summary>No ruin or prop but <paramref name="own"/> where 07 would stand.</summary>
        private static bool Clear(Vector3 spot, Transform own)
        {
            return Blocker(spot, own) == null;
        }

        /// <summary>The first ruin or prop but <paramref name="own"/> where 07 would stand, or null.</summary>
        private static Collider Blocker(Vector3 spot, Transform own)
        {
            foreach (Collider hit in Physics.OverlapSphere(spot + Vector3.up * ClearHeight, ClearRadius,
                         Layers.PropMask, QueryTriggerInteraction.Ignore))
            {
                if (own == null || !hit.transform.IsChildOf(own))
                {
                    return hit;
                }
            }

            return null;
        }

        /// <summary>A crate's frame: a ruin's pose and bounds, or the anchor's pose.</summary>
        private string Frame(string id)
        {
            if (_context.Get<IWorldAnchors>().TryGet(id, out WorldAnchor anchor))
            {
                return $"anchor at {anchor.Position} facing {anchor.Forward}";
            }

            Transform ruin = Ruin(id);
            return $"ruin at {ruin.position} facing {ruin.forward}, {Shape(ruin)}";
        }

        /// <summary>Where a thing renders: its active state, renderer count and their bounds.</summary>
        private static string Shape(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return $"{(root.gameObject.activeInHierarchy ? "active" : "inactive")}, no renderers";
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return $"{(root.gameObject.activeInHierarchy ? "active" : "inactive")}, {renderers.Length} renderers " +
                   $"round {bounds.center} size {bounds.size}";
        }

        private bool Known(string structureId)
        {
            return _gameplay.KnownStructures.IsKnown(structureId);
        }

        private Recipe Hold()
        {
            CargoHold hold = _gameplay.Materials.Hold;
            return new Recipe(hold.Units(SalvageMaterial.Metal), hold.Units(SalvageMaterial.Wiring),
                hold.Units(SalvageMaterial.Optics));
        }

        private WorldAnchor Anchor(string id)
        {
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet(id, out WorldAnchor anchor), $"the World has {id}");
            return anchor;
        }

        /// <summary>Open ground off the ruins' south side: the way in and out among them.</summary>
        private static Vector3 RuinsGate()
        {
            return Flat(Ruin("FallenSolarArray").position) + Vector3.back * RuinsGateOffset;
        }

        private static Transform Ruin(string id)
        {
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                Transform ruins = root.transform.Find("Ruins");
                Transform piece = ruins != null ? ruins.Find(id) : null;
                if (piece != null)
                {
                    return piece;
                }
            }

            Assert.Fail($"the World stands the ruin piece {id} under [World]/Ruins");
            return null;
        }

        private static string FragmentId(string blueprintId, int index)
        {
            return blueprintId + "." + index.ToString(CultureInfo.InvariantCulture);
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }

        private void Begin()
        {
            _stepStart = Time.time;
        }

        private void End(string step, string notes)
        {
            float seconds = Time.time - _stepStart;
            float charge = _gameplay.Battery.Model.Charge01;
            _steps.Add((step, seconds, notes, charge));
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "[playthrough] {0}: {1:F1} s ({2}); charge {3:P0}",
                step, seconds, notes, charge));
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, 1280, 720,
                Path.Combine(GameplayFixture.CaptureFolder, "playthrough-" + name + ".png"));
        }

        private void WriteReport(float total)
        {
            var report = new StringBuilder();
            report.AppendLine("# Opening v2 playthrough (real Main scene)");
            report.AppendLine();
            report.AppendLine("| Step | Seconds | Notes | Charge |");
            report.AppendLine("|---|---:|---|---:|");
            foreach ((string step, float seconds, string notes, float charge) in _steps)
            {
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1:F1} | {2} | {3:P0} |", step,
                    seconds, notes, charge));
            }

            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| **Total** | **{0:F1}** | budget {1:F0} s; {2} error(s), {3} warning(s); {4} crawl(s) | |", total,
                TotalBudget, _problems.Count, _warnings, _crawls));
            report.AppendLine();
            report.AppendLine("Prompt chips on each verb's first use:");
            foreach (string chip in _chips)
            {
                report.AppendLine("- " + chip);
            }

            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[playthrough] report: " + ReportPath + "\n" + report);
        }

        /// <summary>
        /// Every chip the HUD shows over the run, prompt chip or key chip, as it read: a first-use chip shows once, at
        /// the moment its verb becomes possible, so the run notes it then and checks it at the verb's first use.
        /// </summary>
        private sealed class ChipWatch : MonoBehaviour
        {
            private readonly HashSet<string> _seen = new HashSet<string>();
            private HudReader _hud;

            public void Watch(HudReader hud)
            {
                _hud = hud;
            }

            /// <summary>The first chip seen whose word is <paramref name="word"/>, or empty.</summary>
            public string SeenWith(string word)
            {
                foreach (string chip in _seen)
                {
                    if (chip.Contains(" " + word, StringComparison.Ordinal))
                    {
                        return chip;
                    }
                }

                return string.Empty;
            }

            public string Describe()
            {
                return _seen.Count == 0 ? "none" : string.Join("; ", _seen);
            }

            private void Update()
            {
                if (_hud == null)
                {
                    return;
                }

                Note(_hud.Chip);
                Note(_hud.KeyChip);
            }

            private void Note(string chip)
            {
                if (chip.Length > 0)
                {
                    _seen.Add(chip);
                }
            }
        }
    }
}

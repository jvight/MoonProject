using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;
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
    /// holds the wheel while the real input actions scan and cut. 07 drives to the landed lander, scans its cargo bay's
    /// two fragments (the charging pad), collects the lander's open crate (the cutter is FAULT: no igniter), builds the
    /// pad on the apron (placed through the base's placement rules and paid from the hold: the build menu is UI's and
    /// not what this run measures), charges on it from the lander's reserve until the reserve gives out and fits the
    /// spare igniter while docked, cuts the lander's sealed crates, drives to the Lumen ruins and scans the fallen
    /// array (solar), reads the habitat's airlock panel, cuts the workshop's lockers, builds the array (Power on),
    /// scans the habitat (battery bank) and builds it (night-ready), waits out the first sunset, scans the radio tower
    /// (antenna) and cuts its panels, and builds the mast: the west signal ends the opening. Every objective line is
    /// checked as it shows; the milestones FirstCharge, PowerOn, NightReady and SignalWest each come once. Every step
    /// is timed
    /// (Logs/gameplay-captures/playthrough.md) and captured; any error or exception in the log fails it, and so does a
    /// crawl or a run over the budget. Slow: run on demand with --category Playthrough.
    /// </summary>
    [Explicit("Slow end-to-end playthrough of the real Main scene; run on demand with --category Playthrough.")]
    [Category("Playthrough")]
    public sealed class GoldenPathPlaythrough : InputTestFixture
    {
        private const int TestTimeoutMs = 900000;

        /// <summary>Seconds the opening may take end to end (opening-v2 §6: the Playthrough runs in ≤ 10
        /// min).</summary>
        private const float TotalBudget = 600f;

        private const float StopSpeed = 0.3f;
        private const float SealStandOff = 1.6f;

        /// <summary>
        /// Throttle (0..1) 07 creeps the last metres to a seal or a scan target at, steering at it: it stops short and
        /// facing it instead of past it.
        /// </summary>
        private const float ApproachThrottle = 0.3f;

        /// <summary>Metres from the spot in front of a seal that count as there: close, so 07 faces the seal.</summary>
        private const float SealArrive = 1f;

        /// <summary>Metres from the seal 07 noses in to, steering at it, so it ends square to the crate.</summary>
        private const float SealNose = 1.2f;

        /// <summary>Sides round a crate tried for open ground to come in from, its front first.</summary>
        private const int ApproachSides = 8;

        /// <summary>Metres above the ground, and the radius, of the space 07's body needs to stand in.</summary>
        private const float ClearHeight = 0.9f;

        private const float ClearRadius = 1.1f;

        /// <summary>Metres south of the fallen solar array the way into and out of the ruins lies.</summary>
        private const float RuinsGateOffset = 10f;

        /// <summary>Metres out from a seal's stand-off 07 lines up first, so it reaches the seal facing it.</summary>
        private const float SealLineUp = 3f;

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

        private const float UnstickSidestepSeconds = 6f;

        /// <summary>07 stops this far (m) from a fragment, facing it, to scan it.</summary>
        private const float ScanStandOff = 3.5f;

        /// <summary>Metres from a line-up spot that count as there: it only turns 07 roughly toward its target.</summary>
        private const float LineUpArrive = 3f;

        /// <summary>Metres beyond a scan's stand-off 07 lines up first, where that ground is clear.</summary>
        private const float ScanLineUp = 3f;

        /// <summary>Seconds a scan may take beyond its size's time (the press, 07 easing to a stop).</summary>
        private const float ScanAllowance = 3f;

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

        private static readonly string ReportPath = Path.Combine(GameplayFixture.CaptureFolder, "playthrough.md");

        private readonly List<(string Step, float Seconds, string Notes, float Charge)> _steps =
            new List<(string, float, string, float)>();

        private readonly List<string> _problems = new List<string>();
        private readonly List<OpeningMilestoneReached> _milestones = new List<OpeningMilestoneReached>();
        private int _warnings;
        private int _crawls;
        private Keyboard _keyboard;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private Autopilot _pilot;
        private IDisposable _awoke;
        private IDisposable _crawled;
        private IDisposable _reached;
        private float _awokeAt = -1f;
        private float _stepStart;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Application.logMessageReceived += OnLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _rover?.SetDriveSource(null);
            _awoke?.Dispose();
            _crawled?.Dispose();
            _reached?.Dispose();
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
            yield return ExpectLine(OpeningKeys.DriveToLander, "B1: woken half empty, the lander's beacon ahead");
            yield return DriveToTheLander();
            yield return ExpectLine(OpeningKeys.ScanLander, "B3: at the lander");
            yield return ScanTheSet("ruin." + ScanTuning.LanderCargoId, "the lander's cargo bay");
            Assert.IsTrue(Known(StructureIds.ChargingPad), "the cargo bay's two fragments teach the pad");
            yield return ExpectLine(OpeningKeys.CollectParts, "B4: the cutter is FAULT, the open crate holds parts");
            yield return CollectTheOpenCrate();
            yield return ExpectLine(OpeningKeys.BuildPad, "B5: the hold covers the pad");
            Vector3 pad = Anchor("place.apron").Position;
            yield return Build(StructureIds.ChargingPad, pad, "B5: the charging pad on the apron");
            yield return ExpectLine(OpeningKeys.Charge, "B6");
            yield return ChargeOnThePad(pad);
            yield return ExpectLine(OpeningKeys.FindSolar, "B7: LANDER RESERVE · 0%");
            yield return DriveTo(RuinsGate(), 3f, "the Lumen ruins");
            yield return ScanTheSet("ruin.FallenSolarArray", "the fallen solar array");
            yield return ReadTheAirlockPanel();
            yield return CutTheCrates("KenjiWorkshop", "Kenji's workshop lockers");
            yield return ExpectLine(OpeningKeys.BuildSolar, "B8: the array's cost is covered");
            yield return DriveTo(RuinsGate(), 3f, "out of the Lumen ruins");
            yield return Build(StructureIds.SolarArray, pad + Vector3.right * BaseSpacing, "B8: the solar array");
            yield return UntilReached(OpeningMilestone.PowerOn, "Power on: the array powers the pad");
            yield return ExpectLine(OpeningKeys.BuildBatteryBank, "B9: night is coming");
            yield return ScanTheSet("ruin.HabitatModules", "the habitat's power modules");
            yield return CutTheCrates("HabitatModules", "the habitat's cable reel");
            yield return DriveTo(RuinsGate(), 3f, "out of the Lumen ruins");
            yield return Build(StructureIds.BatteryBank, pad + Vector3.left * BaseSpacing, "B9: the battery bank");
            yield return UntilReached(OpeningMilestone.NightReady, "night-ready: the bank holds charge");
            yield return TheFirstNightfall();
            yield return ExpectLine(OpeningKeys.ScanTower, "B11: after nightfall");
            yield return CutTheCrates("RadioTower", "the tower's dish-frame panels");
            yield return ScanTheSet("ruin.RadioTower", "the radio tower ruin");
            yield return ExpectLine(OpeningKeys.BuildMast, "B11");
            yield return DriveTo(RuinsGate(), 3f, "out of the Lumen ruins");
            yield return Build(StructureIds.AntennaMast, pad + Vector3.back * BaseSpacing, "B11: the antenna mast");
            yield return UntilReached(OpeningMilestone.SignalWest, "the west signal answers");
            yield return ExpectLine(string.Empty, "the pillar in the west is the message: the opening ends");

            float total = Time.time - started;
            WriteReport(total);
            Assert.AreEqual(4, _milestones.Count, "FirstCharge, PowerOn, NightReady and SignalWest, once each");
            Assert.IsFalse(_milestones.Exists(m => m.Restored), "all reached in play");
            Assert.AreEqual(0, _crawls, "the opening never runs the battery down to a crawl");
            Assert.Less(total, TotalBudget, "the opening fits its budget");
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
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
            _rover = (RoverController)_context.Get<IRoverRig>();
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awokeAt = Time.time);
            _crawled = _context.Events.Subscribe<LowPowerChanged>(low => _crawls += low.LowPower ? 1 : 0);
            _reached = _context.Events.Subscribe<OpeningMilestoneReached>(_milestones.Add);
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
            for (int i = 0; scans.Find(FragmentId(blueprintId, i)) != null; i++)
            {
                ScanFragment fragment = scans.Find(FragmentId(blueprintId, i));
                if (!fragment.Scanned)
                {
                    yield return Scan(fragment, what);
                    scanned++;
                }
            }

            Assert.Greater(scanned, 0, $"{what} has fragments to scan");
            Capture("scan-" + blueprintId);
            End($"Scan {what}", $"{scanned} fragment(s)");
        }

        /// <summary>B7: the habitat's airlock panel, read as 07 passes it; its readout shows on the CRT.</summary>
        private IEnumerator ReadTheAirlockPanel()
        {
            Begin();
            var read = new List<StoryScanned>();
            using (_context.Events.Subscribe<StoryScanned>(read.Add))
            {
                ScanFragment panel = _gameplay.Scans.Find(StoryScan.IdPrefix + "airlock");
                Assert.IsNotNull(panel, "the habitat has its airlock panel to read");
                yield return Scan(panel, "the airlock panel");
                Assert.AreEqual(1, read.Count, "the panel's readout shows once");
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
            ScanField scans = _gameplay.Scans;
            Vector3 away = Flat(_rover.Position - fragment.Point);
            away = ClearSide(fragment.Point, away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back, ScanStandOff,
                fragment.FragmentId);
            if (Clear(fragment.Point + away * (ScanStandOff + ScanLineUp)))
            {
                yield return DriveTo(Flat(fragment.Point + away * (ScanStandOff + ScanLineUp)), LineUpArrive,
                    "a line-up for a scan");
            }

            yield return DriveTo(Flat(fragment.Point), ScanStandOff, $"the spot facing {what}", ApproachThrottle);
            Look(fragment.Point);
            yield return Until(() => scans.Candidate == fragment, ScanLookTimeout,
                $"07's view rests on {fragment.FragmentId}: its brackets and the Scan chip show",
                () => $"scan candidate {scans.Candidate?.FragmentId ?? "none"}, primary hint " +
                      $"{_gameplay.Hints.Primary.Kind} at {_gameplay.Hints.Primary.Position} (piece " +
                      $"{_gameplay.Salvage.Candidate?.Site.Id}/{_gameplay.Salvage.Candidate?.Number}, crate " +
                      $"{_gameplay.Containers.Candidate?.Id}), the view " +
                      $"{ViewOff(fragment.Point):F0} deg off it, the target " +
                      $"{Vector3.Distance(Flat(fragment.Point), Flat(_rover.Position)):F2} m flat, " +
                      $"{HeadingOff(fragment.Point):F0} deg off");
            Press(_keyboard.eKey);
            yield return Until(() => fragment.Scanned, scans.Tuning.Seconds(fragment.Size) + ScanAllowance,
                $"held E scans {fragment.FragmentId}");
            Release(_keyboard.eKey);
            yield return null;
        }

        /// <summary>B4: up to the lander's open crate, which gives Metal 2, Wiring 1 and the spare igniter.</summary>
        private IEnumerator CollectTheOpenCrate()
        {
            Begin();
            SealedContainer crate = _gameplay.Containers.Find("supply.lander.open");
            Assert.IsTrue(crate.StandsOpen, "the lander's crate stands open from the start: no cutting before B6");
            Vector3 front = Flat(crate.CutPoint.position + crate.Root.forward * SealStandOff);
            yield return DriveTo(front + Flat(crate.Root.forward) * 3f, 2f, "a line-up for the open crate");
            yield return DriveTo(front, 1f, "the open crate");
            yield return Until(() => _gameplay.StartKit.CarriesIgniter, FollowTimeout, "07 takes the spare igniter");
            Capture("04-the-open-crate");
            End("B4: collect the lander's open crate", $"the igniter; hold {Hold()}");
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

                Vector3 side = ClearSide(crate.CutPoint.position, Flat(crate.Root.forward).normalized, SealStandOff,
                    crate.Id);
                Vector3 front = Flat(crate.CutPoint.position) + side * SealStandOff;
                if (Clear(crate.CutPoint.position + side * (SealStandOff + SealLineUp)))
                {
                    yield return DriveTo(front + side * SealLineUp, LineUpArrive, "a line-up for " + cache.Id);
                }

                yield return DriveTo(front, SealArrive, "the seal of " + cache.Id, ApproachThrottle,
                    () => $"the crate at {crate.Root.position} facing {crate.Root.forward}; its frame " +
                          $"{cache.AnchorId}: {Frame(cache.AnchorId)}");
                yield return DriveTo(Flat(crate.CutPoint.position), SealNose, "nose to the seal of " + cache.Id,
                    ApproachThrottle);
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
        /// Builds <paramref name="structureId"/> near <paramref name="at"/> through the base's placement rules, pays it
        /// from the hold and welds it with a held E, as a player resumes a weld (the build menu is UI's and the feel
        /// captures', not this run's), so the weld's battery cost is in the run.
        /// </summary>
        private IEnumerator Build(string structureId, Vector3 at, string what)
        {
            Begin();
            StructureDefinition definition = _gameplay.Building.Catalog.Find(structureId);
            Assert.IsTrue(Known(structureId), $"{structureId}'s blueprint is learned");
            Assert.IsTrue(_gameplay.Materials.TrySpend(definition.BuildCost), $"the hold covers {structureId}");
            yield return DriveTo(Flat(at) + Vector3.back * BuildStandOff, 1f, "beside the build spot");
            var terrain = _context.Get<ITerrainQuery>();
            bool placed = false;
            int instance = 0;
            for (int ring = 0; ring < 4 && !placed; ring++)
            {
                for (int step = 0; step < 8 && !placed; step++)
                {
                    Vector3 spot = at + Quaternion.Euler(0f, step * 45f, 0f) * Vector3.forward * (ring * 1.5f);
                    spot = SurfaceRules.OnSurface(terrain, spot.x, spot.z);
                    placed = _gameplay.PlacedBase.TryPlace(definition, new StructurePose(spot, 0f), terrain,
                        _rover.Position, out instance, out _);
                }
            }

            Assert.IsTrue(placed, $"{structureId} finds a spot near {at} the placement rules accept");
            yield return null;
            Press(_keyboard.eKey);
            yield return Until(() => _gameplay.PlacedBase.TryGetStructure(instance, out PlacedStructure built) &&
                                     built.IsBuilt, definition.BuildSeconds + FollowTimeout,
                $"a held E welds {structureId}",
                () => $"weld hint {_gameplay.Building.TryGetWeldCandidate(out _)}, hovered " +
                      $"{_gameplay.Building.HoveredInstanceId} (placed {instance}), state {_gameplay.Building.State}");
            Release(_keyboard.eKey);
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
            yield return DriveTo(Flat(pad), 0.5f, "the pad");
            yield return Until(() => battery.IsCharging && battery.Charger == ChargerKind.Pad, 6f,
                "the built pad charges 07");
            yield return UntilReached(OpeningMilestone.FirstCharge, "FirstCharge: the eclipse ends");
            yield return Until(() => _gameplay.PadLock.Raised, FollowTimeout, "the pad's dock lock rises round 07");
            Capture("06-the-pad-locks");
            yield return ExpectLine(OpeningKeys.FitIgniter, "B6: charging, the igniter in the hold");
            RoverStartKit kit = _gameplay.StartKit;
            Assert.IsTrue(kit.CanFitIgniter, "docked on its pad, the cutter socket takes the igniter");
            Assert.IsTrue(kit.TryFitIgniter(), "the socket screen fits it (UI's; this run fits it directly)");
            Assert.AreEqual(RoverSystemState.Ok, kit.StateOf(RoverSystem.Cutter), "the cutter's pilot light is on");
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

        private IEnumerator ExpectLine(string key, string why)
        {
            yield return Until(() => _gameplay.Opening.Flow.Current == key, FollowTimeout,
                $"the objective line is '{key}' ({why}), not '{_gameplay.Opening.Flow.Current}'");
        }

        private IEnumerator UntilReached(OpeningMilestone milestone, string what)
        {
            yield return Until(() => _milestones.Exists(reached => reached.Milestone == milestone), FollowTimeout,
                what);
        }

        private IEnumerator DriveTo(Vector3 target, float arriveRadius, string what, float maxThrottle = 1f,
            Func<string> detail = null)
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
            _pilot.GoTo(Flat(_rover.Position) + side * UnstickSidestep + toward * UnstickSidestep, 1f, maxThrottle);
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

        /// <summary>Turns the view to <paramref name="point"/>, as the player's mouse would.</summary>
        private void Look(Vector3 point)
        {
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            Vector3 toward = Flat(point - rig.Camera.transform.position);
            rig.Orbit.AdoptBearing(Quaternion.LookRotation(toward, Vector3.up).eulerAngles.y);
        }

        /// <summary>
        /// The way out from <paramref name="point"/> (a seal or a scan target) 07 comes in along:
        /// <paramref name="preferred"/> if 07 can stand there, else the first side round it where it can (they stand
        /// among the ruins and by the lander; a seal claims E, and a scan shows, whichever side 07 faces it from). The
        /// line-up further out is driven only where that ground is clear too.
        /// </summary>
        private static Vector3 ClearSide(Vector3 point, Vector3 preferred, float standOff, string what)
        {
            for (int step = 0; step < ApproachSides; step++)
            {
                float turn = (step + 1) / 2 * (360f / ApproachSides) * (step % 2 == 0 ? 1f : -1f);
                Vector3 side = Quaternion.Euler(0f, turn, 0f) * preferred;
                if (Clear(point + side * standOff))
                {
                    return side;
                }
            }

            Assert.Fail($"{what} has open ground on some side to come in from");
            return preferred;
        }

        /// <summary>No ruin or prop where 07 would stand.</summary>
        private static bool Clear(Vector3 spot)
        {
            return !Physics.CheckSphere(spot + Vector3.up * ClearHeight, ClearRadius, Layers.PropMask,
                QueryTriggerInteraction.Ignore);
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
            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[playthrough] report: " + ReportPath + "\n" + report);
        }
    }
}

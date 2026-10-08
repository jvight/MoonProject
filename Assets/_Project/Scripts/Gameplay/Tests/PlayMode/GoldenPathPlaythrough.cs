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
    /// The golden path through the real game, as a regression and a pacing measurement: the built Main scene (on its
    /// own save slot) boots, 07 wakes, and a calm autopilot holds the wheel while the real input actions do the rest.
    /// Salvage (M3-13): drive to the supply depot, ping (the site answers), cut it piece by piece with the beam until
    /// it is picked clean, dig the walkman out of its heart, tow it home (round by the base pad, since the lander is
    /// solid), put it on the shelf, park on the tower pad and craft level 1 from the depot's materials through the UI's
    /// hold. Then Tilly (M3-02): find her with a ping, gather her three parts, repair her, drive home with her, be
    /// greeted, and let her spot a salvage site on the next trip. Then the workshop (M3-03): find that holding Jump
    /// does nothing yet, craft Hover-Jump at Kenji's workbench through the same hold, and take the first full-charge
    /// leap on the base pad. Then the Cargo Cradle (M3-11): craft it at the bench, dig Kenji's duck out of the garage's
    /// heart, lift it into the rack with the tether button, drive home and set it down on the shelf from the rack.
    /// Then Bell (M3-04/05): drive up the mouth lane, leap the chasm, find her lying at the
    /// terminus with Ro's log cache and her tape, gather her three parts from the alcoves, repair her (the tape slides
    /// in, her dial wakes, she stands), drive home by the one-way exit while she makes her own way, be greeted by her,
    /// turn her dial through every station, and follow her first signal to the basin tape onto her rack. Then the
    /// relay network (M3-06): pick up the mound relay's part, restore it at its foot with materials and watch it come
    /// online, drive out past the tower's reach and find home still reaching 07 there, then radio-hop home from the
    /// mast's pad and back. Inside the canyon 07 drives a line found over drivable ground (the same planner Bell walks
    /// by). Every step is timed (Logs/gameplay-captures/playthrough.md) and captured, with review frames of every
    /// M3-05 placement; any error or exception in the log fails it. Slow: run on demand with --category Playthrough.
    /// </summary>
    [Explicit("Slow end-to-end playthrough of the real Main scene; run on demand with --category Playthrough.")]
    [Category("Playthrough")]
    public sealed class GoldenPathPlaythrough : InputTestFixture
    {
        private const string Tower = "radio_tower";
        private const string HoverJump = "rover.hover_jump";
        private const string CargoCradle = "rover.cargo_cradle";
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

        /// <summary>Metres past the exit anchor, down its step, onto the basin floor.</summary>
        private const float BelowExit = 14f;

        /// <summary>Seconds between two turns of Bell's dial (the needle settles in between).</summary>
        private const float DialPause = 1.5f;

        /// <summary>Hold Jump this much longer than the rover's full charge time (s), as a player would.</summary>
        private const float ChargeMargin = 0.25f;

        /// <summary>A full-charge leap rises at least this high (m); the rover spec asks for ~6-8 m.</summary>
        private const float MinLeapApex = 5f;

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
        private const float ApproachOffset = 3f;
        private const float RetreatDistance = 16f;
        private const float ShelfStandOff = 4f;
        private const float ReelFrom = 12f;

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

        /// <summary>Close enough (m) to a hop pad's centre to park on it.</summary>
        private const float PadArrive = 1f;

        /// <summary>
        /// A drive whose line passes this close (m) to the lander's centre goes round it, by a point this far out.
        /// </summary>
        private const float LanderKeepOut = 9f;
        private const float LanderDetour = 13f;

        /// <summary>Close enough (m) to the base pad centre to turn toward the shelf or the tower.</summary>
        private const float PadArrival = 5f;
        private const float TotalBudget = 1100f;

        private static readonly string ReportPath = Path.Combine(GameplayFixture.CaptureFolder, "playthrough.md");

        private readonly List<(string Step, float Seconds, string Notes)> _steps =
            new List<(string, float, string)>();

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
            _rover?.SetDriveSource(null);
            _events?.Dispose();
            _awoke?.Dispose();
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
        [Timeout(1200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator FreshGame_FirstRelicHome_AndTheTowerAwake()
        {
            float started = Time.time;
            yield return Boot();
            yield return WakeUp();

            SalvageSite depot = _gameplay.Salvage.Find(WorldAnchorIds.SitePrefix + "depot");
            Assert.IsNotNull(depot, "the supply depot stands near home");
            Relic relic = depot.Relics[0];
            yield return DriveToSite(depot);
            yield return Ping(depot);
            yield return SalvageTheDepot(depot);
            yield return Excavate(depot, relic);
            Assert.IsFalse(depot.AnswersSonar, "picked clean and its relic lifted: the depot falls silent");
            yield return LatchTether(relic);
            yield return TowHome(depot, relic);
            yield return Deposit(relic);
            yield return BuyTowerLevel();
            yield return FindTilly();
            yield return GatherTillyParts();
            yield return RepairTilly();
            yield return DriveHomeWithTilly();
            yield return TillySpotsOnTheNextTrip();
            yield return BuyHoverJumpAtTheBench();
            yield return CraftTheCargoCradle();
            SalvageSite garage = _gameplay.Salvage.Find(WorldAnchorIds.SitePrefix + "garage");
            Relic duck = garage.Relics[0];
            yield return Excavate(garage, duck);
            yield return StowInTheCradle(duck);
            yield return CarryHomeInTheCradle(garage, duck);
            yield return FirstLeap();
            yield return DriveToTheCanyon();
            yield return LeapTheChasm();
            yield return FindBell();
            yield return GatherBellsParts();
            yield return RepairBell();
            yield return HomeByTheExit();
            yield return TurnBellsDial();
            yield return FollowBellsSignal();
            yield return RestoreTheMoundRelay();
            yield return DriveOutInReach();
            yield return HopHomeAndBack();

            float total = Time.time - started;
            WriteReport(total);
            Assert.Less(total, TotalBudget, "the whole golden path fits a generous time budget");
            Assert.IsEmpty(_problems, "no errors or exceptions anywhere in the log during the playthrough");
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
            _pilot = new Autopilot(_rover);
            _rover.SetDriveSource(_pilot);
            Assert.AreEqual(0, _gameplay.Materials.Total, "a fresh game (its own save slot)");
            SalvageField salvage = _gameplay.Salvage;
            int units = salvage.TrailCount * salvage.Tuning.TrailYield;
            foreach (SalvagePiece piece in salvage.Pieces)
            {
                units += piece.Units;
            }

            End("Boot the real Main scene", $"{salvage.Sites.Count} salvage sites with {salvage.Pieces.Count} " +
                                            $"pieces and {salvage.TrailCount} trail bits: {units} units of salvage");
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

        private IEnumerator Ping(SalvageSite site)
        {
            Begin();
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.spaceKey);
            yield return Until(() => _events.SonarPinged.Count > 0, 2f, "space pings");
            yield return Until(() => SiteHeard(site.Id), _gameplay.Sonar.Tuning.RingDuration + 2f,
                "the depot answers the ping with its own tone");
            Assert.IsEmpty(_events.RelicAnswered, "the relic in its heart lets the site answer for it");
            Assert.IsTrue(site.Discovered);
            Capture("02-ping-answered");
            End("Ping", $"{_events.SiteAnswered.Count} site(s) answered");
        }

        /// <summary>
        /// Every piece of the depot in turn: circle the wreck to it, line up in front of its cut, ease in facing it and
        /// hold Excavate until it folds into 07. A piece the aim cannot pick from there is retried on a second pass.
        /// </summary>
        private IEnumerator SalvageTheDepot(SalvageSite site)
        {
            Begin();
            SalvageField salvage = _gameplay.Salvage;
            int cuts = 0;
            bool captured = false;
            for (int pass = 0; pass < 2 && !site.IsPickedClean; pass++)
            {
                foreach (SalvagePiece piece in site.Pieces)
                {
                    if (!piece.IsCuttable)
                    {
                        continue;
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
                    Press(_keyboard.eKey);
                    yield return Until(() => _events.SalvageCutStarted.Count > cuts, 6f, "the beam starts cutting");
                    Assert.IsTrue(_rover.IsHeldStill, "07 is asked to hold still while it cuts");
                    if (!captured)
                    {
                        yield return new WaitForSeconds(cutSeconds * 0.5f);
                        Capture("02b-salvage-cut");
                        Witness(salvage.Cutting != null ? salvage.Cutting.CutPosition : site.Position,
                            "02c-salvage-cut-closeup");
                        captured = true;
                    }

                    yield return Until(() => CompletedCuts() > completed, cutSeconds + 4f, "the beam cuts it free");
                    Release(_keyboard.eKey);
                    yield return Until(() => _events.MaterialSalvaged.Count > salvaged, 6f,
                        "the piece comes away and folds into 07");
                    cuts = _events.SalvageCutStarted.Count;
                    yield return null;
                }
            }

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
                                     $"stock metal {stock.Metal}, wiring {stock.Wiring}, optics {stock.Optics}");
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
        /// the target.
        /// </summary>
        private IEnumerator CircleTo(SalvageSite site, Vector3 target, float extra)
        {
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

        private static float Bearing(Vector3 direction)
        {
            return SurfaceRules.Bearing(Flat(direction));
        }

        /// <summary>
        /// Dig the relic out of the heart: park within the beam's reach on open ground round the heart, facing it, and
        /// hold Excavate until it is free.
        /// </summary>
        private IEnumerator Excavate(SalvageSite site, Relic relic)
        {
            Begin();
            Vector3 heart = relic.Site.Position;
            Vector3 park = Vector3.zero;
            Vector3 outward = Vector3.zero;
            float best = float.MaxValue;
            for (float bearing = 0f; bearing < 360f; bearing += 30f)
            {
                Vector3 direction = SurfaceRules.BearingDirection(bearing);
                Vector3 spot = Flat(heart + direction * DigStandOff);
                float ground = _context.Get<ITerrainQuery>().SampleHeight(spot.x, spot.z);
                bool open = !Physics.CheckSphere(new Vector3(spot.x, ground + ParkClearance + 0.2f, spot.z),
                    ParkClearance, Layers.PropMask, QueryTriggerInteraction.Ignore);
                float distance = SurfaceRules.HorizontalDistance(spot, _rover.Position);
                if (open && NearestOfItsHeart(site, relic, spot) && distance < best)
                {
                    best = distance;
                    park = spot;
                    outward = direction;
                }
            }

            Assert.Less(best, float.MaxValue, $"open ground to dig from round the heart of {site.Id}");
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
        /// The walkman hopped out in front of 07, against the wreck: back out the way 07 came in, turn and creep up on
        /// it with the tether held until it latches.
        /// </summary>
        private IEnumerator LatchTether(Relic relic)
        {
            Begin();
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
            Assert.AreEqual(1, _events.TetherAttached.Count);
            End("Turn back and latch the tether", $"latched at {_gameplay.Tether.Length:F1} m");
        }

        /// <summary>
        /// Reel the relic in close, pull it straight out from the wreck and round it, wide, to its home side; then home
        /// and up to the shelf.
        /// </summary>
        private IEnumerator TowHome(SalvageSite site, Relic relic)
        {
            Begin();
            yield return Reel(1f, ReelSeconds);
            Assert.Less(_gameplay.Tether.Length, _gameplay.Tether.Tuning.MinLength + 0.5f, "reeled in close");
            Vector3 centre = Flat(site.Position);
            float ring = Anchor(site.Id).Radius + RingMargin + TowRingExtra;
            yield return DriveTo(centre + Flat(_rover.Position - centre).normalized * ring, 3f, 0.6f, 40f,
                "open ground out from the wreck, the relic in tow");
            Vector3 toHome = Flat(_context.Get<IWorldLayout>().BasePosition - site.Position).normalized;
            yield return CircleTo(site, centre + toHome * ring, TowRingExtra);
            Assert.AreEqual(0, _events.TetherReleased.Count, "the relic follows 07 out round the wreck");
            yield return Reel(-1f, ReelSeconds);
            HomeBase homeBase = _gameplay.Home;
            Vector3 shelf = homeBase.ShelfPosition;
            Vector3 front = _context.Get<IWorldLayout>().BasePosition - shelf;
            front.y = 0f;
            Vector3 stand = shelf + front.normalized * ShelfStandOff;
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, stand);
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 60f,
                "the base pad, clear of the lander");
            _pilot.GoTo(stand, 2.5f, 0.6f);
            float deadline = Time.time + 90f;
            bool captured = false;
            while ((!_pilot.Arrived || !homeBase.InDepositZone(relic.transform.position)) && Time.time < deadline)
            {
                Assert.AreEqual(0, _events.TetherReleased.Count, "the tether holds all the way home");
                if (_pilot.Distance < ReelFrom)
                {
                    Set(_mouse.scroll, new Vector2(0f, 1f));
                }

                if (!captured && _pilot.Distance < distance * 0.5f)
                {
                    Capture("04-towing-home");
                    captured = true;
                }

                yield return null;
            }

            _pilot.Target = null;
            Assert.IsTrue(homeBase.InDepositZone(relic.transform.position),
                "the relic is towed into the shelf's reach");
            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Deposit, out _), "the Deposit prompt is offered");
            yield return new WaitForSeconds(0.5f);
            Capture("05-at-the-shelf");
            End($"Tow it home ({distance:F0} m)", $"tether at {_gameplay.Tether.Length:F1} m on arrival");
        }

        private IEnumerator Deposit(Relic relic)
        {
            Begin();
            int materials = _events.MaterialsChanged.Count;
            Release(_mouse.rightButton);
            yield return Until(() => _events.RelicDeposited.Count > 0, 8f, "the relic floats onto the shelf");
            RelicDeposited deposited = _events.RelicDeposited[0].Value;
            Assert.AreEqual(relic.Definition.Id, deposited.RelicId);
            Assert.AreEqual(1, deposited.DisplayedCount);
            Assert.AreEqual(RelicState.Displayed, relic.State);
            yield return null;
            Assert.AreEqual(materials, _events.MaterialsChanged.Count, "memories are not paid for");
            yield return new WaitForSeconds(1f);
            Capture("06-on-the-shelf");
            End("Deposit on the shelf", $"{deposited.DisplayedCount} memory on display");
        }

        private IEnumerator BuyTowerLevel()
        {
            Begin();
            RadioTower tower = _gameplay.Tower;
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.6f, 60f,
                "the base pad, clear of the lander");
            yield return DriveTo(tower.PadCentre, 1f, 0.6f, 60f, "the tower pad");
            yield return Until(() => _gameplay.Shop.IsAtStation, 3f, "07 is parked on the pad");
            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "the depot's materials cover the first level");
            int purchasesBefore = _events.SignalRadiusChanged.Count;
            int cues = _events.StationCued.Count;
            Press(_keyboard.eKey);
            yield return Until(() => _events.UpgradePurchased.Count > 0, 15f, "holding confirm buys the level");
            Release(_keyboard.eKey);
            UpgradePurchased purchase = _events.UpgradePurchased[0].Value;
            Assert.AreEqual(Tower, purchase.UpgradeId);
            Assert.AreEqual(1, purchase.Level);
            Assert.Greater(_events.SignalRadiusChanged.Count, purchasesBefore);
            float radius = _events.SignalRadiusChanged[_events.SignalRadiusChanged.Count - 1].Value.Radius;
            Assert.AreEqual(_gameplay.Upgrades.Find(Tower).SignalRadiusAt(1), radius, 1e-3f);
            Assert.IsTrue(tower.Feeding, "07's beam feeds the service port's hopper");
            yield return new WaitForSeconds(0.4f);
            Vector3 port = tower.HopperMouth;
            Vector3 front = Flat(tower.PadCentre - port).normalized;
            Vector3 aside = Vector3.Cross(Vector3.up, front);
            Review(port + front * PortViewDistance + aside * PortViewSide + Vector3.up * PortViewHeight, port,
                "07a-tower-port-feed");
            yield return Until(() => Cued(StationCue.StitchStarted, cues), 6f, "the hatch opens and the beam stitches");
            yield return new WaitForSeconds(tower.Tuning.FlareDuration);
            Review(port + front * PortViewDistance * 1.6f + aside * PortViewSide + Vector3.up * PortViewHeight,
                Vector3.Lerp(port, tower.BeaconPosition, 0.5f), "07b-tower-port-stitch");
            yield return Until(() => !tower.Crafting, 6f, "the hatch shuts and the moment ends");
            AssertCues(cues, Tower, StationCue.FeedStarted, StationCue.Fed, StationCue.HatchOpened,
                StationCue.StitchStarted, StationCue.HatchClosed);
            yield return new WaitForSeconds(1f);
            Capture("07-tower-awake");
            End("Park on the pad and craft tower level 1", $"signal radius {radius:F0} m, recipe " +
                                                          $"{_gameplay.Upgrades.Find(Tower).Levels[0].Recipe}");
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
            yield return Until(() => _gameplay.Sonar.IsReady, 6f, "the sonar is ready");
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.spaceKey);
            yield return Until(() => _events.FriendAnswered.Count > answersBefore, 6f,
                "a broken chirp answers from the crater");
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
                yield return Until(() => tilly.Progress.Collected > before, 6f, "the part drifts into 07");
            }

            Assert.AreEqual(total, _events.FriendPartCollected.Count);
            FriendPartCollected last = _events.FriendPartCollected[total - 1].Value;
            Assert.AreEqual(total, last.Collected);
            Assert.AreEqual(total, last.Total);
            Assert.IsTrue(tilly.Progress.CanRepair);
            End("Gather Tilly's parts", $"{total} parts over {travelled:F0} m of driving");
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

        private IEnumerator TillySpotsOnTheNextTrip()
        {
            Begin();
            SalvageSite undiscovered = null;
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            var abilities = _context.Get<IRoverAbilities>();
            foreach (SalvageSite site in _gameplay.Salvage.Sites)
            {
                bool hidden = site.AnswersSonar && !site.Discovered && site.Gate.IsOpen(abilities);
                if (hidden && (undiscovered == null ||
                               SurfaceRules.HorizontalDistance(site.Position, home) <
                               SurfaceRules.HorizontalDistance(undiscovered.Position, home)))
                {
                    undiscovered = site;
                }
            }

            Assert.IsNotNull(undiscovered, "a salvage site is still waiting to be found");
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
            AssertCues(cues, HoverJump, StationCue.FeedStarted, StationCue.Fed);
            Assert.AreEqual(HoverJump, _events.RoverBayFitting[_events.RoverBayFitting.Count - 1].Value.UpgradeId);
            yield return new WaitForSeconds(SparkDelay);
            Assert.Greater(bench.SparkCount, 0, "weld sparks fly from the gantry arms' tips");
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
        /// The duck hopped out in front of 07: back out, turn and creep up on it with the tether button held, which now
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
            HomeBase homeBase = _gameplay.Home;
            Vector3 shelf = homeBase.ShelfPosition;
            Vector3 front = Flat(home - shelf).normalized;
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, shelf);
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
            yield return DriveTo(shelf + front * ShelfStandOff, 1.5f, 0.5f, 60f, "the museum shelf");
            yield return Until(() => cradle.CanUnload, 3f, "the rack is in reach of the shelf");
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
            Capture("13g-duck-on-the-shelf");
            End($"Carry it home in the Cargo Cradle ({distance:F0} m)",
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
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.6f, 60f,
                "the open base pad");
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

        private IEnumerator DriveToTheCanyon()
        {
            Begin();
            WorldAnchor lip = Anchor(WorldAnchorIds.CanyonLip);
            Vector3 runStart = lip.Position - lip.Forward * RunUp;
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 60f, "the base pad");
            Review(_context.Get<IWorldLayout>().BasePosition + Vector3.up * 3f, lip.Position + Vector3.up * 2f,
                "16-toward-the-canyon");
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, runStart);
            yield return DriveTo(runStart, 2f, 1f, 90f, "the run-up on the mouth lane");
            Capture("16b-at-the-run-up");
            End($"Drive to the canyon ({distance:F0} m)", "up the mouth lane to the run-up");
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
            yield return Until(() => _events.CrewLogFound.Count > 0, 3f, "Ro's log cache opens beside her");
            Assert.AreEqual("ro_1", _events.CrewLogFound[0].Value.LogId);
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

        private IEnumerator HomeByTheExit()
        {
            Begin();
            Friend bell = _gameplay.Friends.Find(BellId);
            WorldAnchor exit = Anchor(WorldAnchorIds.CanyonExit);
            float travelled = 0f;
            yield return DriveAlong(exit.Position, "the top of the way out", value => travelled = value);
            Vector3 below = exit.Position + exit.Forward * BelowExit;
            yield return DriveTo(new Vector3(below.x, 0f, below.z), 2f, 0.6f, 30f, "the basin, down the exit step");
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            travelled += SurfaceRules.HorizontalDistance(_rover.Position, home);
            int greetings = CountGreetings(BellId);
            yield return DriveTo(home, PadArrival, 1f, 120f, "home");
            Assert.IsTrue(bell.IsHome, "Bell got home before 07, unseen");
            Assert.Less(Vector3.Distance(bell.Position, _gameplay.Friends.Find(BellId).Home.position), 0.01f,
                "in her corner by the radio tower");
            yield return Until(() => CountGreetings(BellId) > greetings, _gameplay.Friends.BellTuning.WakeDuration + 6f,
                "she wakes and greets 07 home");
            Assert.IsTrue(Ticker("ticker.bell.home"), "Bell got home before you");
            Transform corner = bell.Home;
            Review(corner.position + corner.forward * 4.5f + corner.right * 1.5f + Vector3.up * 1.8f,
                corner.position + Vector3.up * 0.8f, "25-bell-home");
            End($"Drive home by the exit ({travelled:F0} m)", "Bell was already home, and greeted 07");
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
                Assert.AreEqual(BellCue.DialTurned, _events.BellCued[cues].Value.Cue, "then she turns it");
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

        /// <summary>From the mound relay's pad, the radio-hop home; then from home's pad, back to the mast.</summary>
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
            yield return Hop(hop, StationReach.HomeId);
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            Assert.Less(SurfaceRules.HorizontalDistance(_rover.Position, home), 3f, "on home's pad");
            Capture("35-hopped-home");
            yield return Hop(hop, mast.Id);
            Assert.Less(SurfaceRules.HorizontalDistance(_rover.Position, mast.Anchor.Position), 3f,
                "back on the mound relay's pad");
            Capture("36-hopped-back");
            End("Radio-hop home and back", $"{_events.RadioHopFinished.Count} hops, about " +
                                           $"{HopSequence.For(_gameplay.Relays.Tuning).Duration:F1} s each");
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
        /// round it by a point beside it instead. Drives that start or end at home, next to the lander, go straight.
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
            detour = lander + side * LanderDetour;
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

            Assert.IsTrue(condition(), $"timed out after {timeout:F0} s waiting until {what}");
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

        private bool SiteHeard(string siteId)
        {
            foreach (EventRecorder.Timed<SiteAnswered> answer in _events.SiteAnswered)
            {
                if (answer.Value.SiteId == siteId)
                {
                    return true;
                }
            }

            return false;
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
        }

        private void End(string step, string notes)
        {
            float seconds = Time.time - _stepStart;
            _steps.Add((step, seconds, notes));
            Debug.Log($"[playthrough] {step}: {seconds:F1} s ({notes})");
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
            report.AppendLine("| Step | Seconds | Notes |");
            report.AppendLine("|---|---:|---|");
            foreach ((string step, float seconds, string notes) in _steps)
            {
                report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1:F1} | {2} |", step,
                    seconds, notes));
            }

            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "| **Total** | **{0:F1}** | budget {1:F0} s; {2} error(s), {3} warning(s) in the log |", total,
                TotalBudget, _problems.Count, _warnings));
            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(ReportPath, report.ToString());
            Debug.Log("[playthrough] report: " + ReportPath + "\n" + report);
        }
    }
}

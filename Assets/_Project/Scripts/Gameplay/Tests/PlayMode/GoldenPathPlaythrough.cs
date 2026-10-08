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
    /// Drive to the nearest onboarding relic picking up scrap on the way, ping, dig it up, tow it home (round by the
    /// base pad, since the lander is solid), put it on the shelf, pick up scrap until the tower is affordable, park on
    /// the tower pad and buy level 1 through the UI's hold. Then Tilly (M3-02): find her with a ping, gather her three
    /// parts, repair her, drive home with her, be greeted, and let her spot a relic on the next trip. Then the
    /// workshop (M3-03): gather scrap for Hover-Jump, find that holding Jump does nothing yet, buy it at Kenji's
    /// workbench through the same hold, and take the first full-charge leap on the base pad. Then Bell (M3-04/05):
    /// follow the scrap trail up the mouth lane, leap the chasm, find her lying at the terminus with Ro's log cache
    /// and her tape, gather her three parts from the alcoves, repair her (the tape slides in, her dial wakes, she
    /// stands), drive home by the one-way exit while she makes her own way, be greeted by her, turn her dial through
    /// every station, and follow her first signal to the basin tape onto her rack. Then the relay network (M3-06):
    /// gather scrap for the mound relay, pick up its part, restore it at its foot and watch it come online, drive out
    /// past the tower's reach and find home still reaching 07 there, then radio-hop home from the mast's pad and back.
    /// Inside the canyon 07 drives a line found over drivable ground (the same planner Bell walks by). Every step is
    /// timed
    /// (Logs/gameplay-captures/playthrough.md) and captured, with review frames of every M3-05 placement; any error or
    /// exception in the log fails it. Slow: run on demand with --category Playthrough.
    /// </summary>
    [Explicit("Slow end-to-end playthrough of the real Main scene; run on demand with --category Playthrough.")]
    [Category("Playthrough")]
    public sealed class GoldenPathPlaythrough : InputTestFixture
    {
        private const string Tower = "radio_tower";
        private const string HoverJump = "rover.hover_jump";
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

        /// <summary>07 waits this far (m) short of an undiscovered relic for Tilly to spot it.</summary>
        private const float SpotStandOff = 25f;

        /// <summary>Where the review camera stands relative to Tilly (m away from 07, m up).</summary>
        private const float WitnessDistance = 4f;
        private const float WitnessHeight = 1.5f;

        /// <summary>Where the review camera stands to watch the bench's sparks (m out in front of it, m up).</summary>
        private const float BenchViewDistance = 6.5f;
        private const float BenchViewHeight = 2.2f;
        private const float BenchViewSide = 2.5f;
        private const float SparkDelay = 0.3f;
        private const float StopSpeed = 0.4f;
        private const float ApproachOffset = 3f;
        private const float RetreatDistance = 16f;
        private const float ShelfStandOff = 4f;
        private const float ReelFrom = 12f;

        /// <summary>07 parks this far (m) in front of a relay mast's junction box to restore it.</summary>
        private const float RelayFoot = 3f;

        /// <summary>07 first swings this far (m) to the side of the mast, clear of it and its guy wires.</summary>
        private const float RelayAside = 12f;

        /// <summary>How far past the mound relay (m, away from home) 07 drives to find home still in reach.</summary>
        private static readonly Vector2 PastTheMast = new Vector2(55f, 95f);

        /// <summary>Close enough (m) to a hop pad's centre to park on it.</summary>
        private const float PadArrive = 1f;

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

            Relic relic = NearestOnboardingRelic();
            yield return DriveToRelic(relic);
            yield return Ping(relic);
            yield return Excavate(relic);
            yield return LatchTether(relic);
            yield return TowHome(relic);
            yield return Deposit(relic);
            yield return GatherScrapFor(Tower, "the tower", 120f);
            yield return BuyTowerLevel();
            yield return FindTilly();
            yield return GatherTillyParts();
            yield return RepairTilly();
            yield return DriveHomeWithTilly();
            yield return TillySpotsOnTheNextTrip();
            yield return GatherScrapFor(HoverJump, "Hover-Jump", 300f);
            yield return BuyHoverJumpAtTheBench();
            yield return FirstLeap();
            yield return FollowTheTrailToTheCanyon();
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
            Assert.AreEqual(0, _gameplay.Wallet.Balance, "a fresh game (its own save slot)");
            End("Boot the real Main scene", $"{_gameplay.Scrap.Count} scrap pieces, " +
                                            $"{_gameplay.Scrap.RemainingValue} scrap value in the basin");
        }

        private IEnumerator WakeUp()
        {
            Begin();
            yield return Until(() => _awokeAt >= 0f, 20f, "07 wakes up on its own");
            yield return new WaitForSeconds(2f);
            Capture("01-awake");
            End("07 wakes", "woke on its own (not by player input)");
        }

        private Relic NearestOnboardingRelic()
        {
            Relic nearest = null;
            float best = float.MaxValue;
            foreach (Relic relic in _gameplay.Relics.Relics)
            {
                float distance = SurfaceRules.HorizontalDistance(relic.Site.Position, _rover.Position);
                if (relic.Definition.Placement == RelicPlacementBand.Onboarding && distance < best)
                {
                    best = distance;
                    nearest = relic;
                }
            }

            Assert.IsNotNull(nearest, "an onboarding relic exists");
            return nearest;
        }

        private IEnumerator DriveToRelic(Relic relic)
        {
            Begin();
            Vector3 site = relic.Site.Position;
            Vector3 toSite = site - _rover.Position;
            toSite.y = 0f;
            float distance = toSite.magnitude;
            Vector3 stop = site - toSite.normalized * ApproachOffset;
            yield return DriveTo(stop, 1.5f, 1f, 60f, "the relic site");
            Assert.Greater(_events.ScrapCollected.Count, 0, "scrap is collected on the way");
            End($"Drive to '{relic.Definition.Id}' ({distance:F0} m)",
                $"{_events.ScrapCollected.Count} scrap pieces picked up, wallet {_gameplay.Wallet.Balance}");
        }

        private IEnumerator Ping(Relic relic)
        {
            Begin();
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.spaceKey);
            yield return Until(() => _events.SonarPinged.Count > 0, 2f, "space pings");
            yield return Until(() => Answered(relic.Definition.Id), _gameplay.Sonar.Tuning.RingDuration + 2f,
                "the relic answers the ping with its id");
            Capture("02-ping-answered");
            End("Ping", $"{_events.RelicAnswered.Count} relic(s) answered");
        }

        private IEnumerator Excavate(Relic relic)
        {
            Begin();
            Press(_keyboard.eKey);
            yield return Until(() => _events.ExcavationStarted.Count > 0, 6f, "the tractor beam takes hold");
            Assert.IsTrue(_rover.IsHeldStill, "07 is asked to hold still while the beam is held");
            yield return new WaitForSeconds(1.5f);
            Capture("03-excavating");
            float duration = _gameplay.Excavation.Tuning.DurationFor(relic.Definition.Mass);
            yield return Until(() => Surfaced(relic.Definition.Id), duration + 8f, "the relic surfaces");
            Release(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.IsFalse(_rover.IsHeldStill, "07 is free again once the relic is up");
            Assert.AreEqual(RelicState.Loose, relic.State);
            yield return new WaitForSeconds(1.5f);
            End("Excavate", $"lift took {duration:F1} s of beam for {relic.Definition.Mass:F0} kg");
        }

        private IEnumerator LatchTether(Relic relic)
        {
            Begin();
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            Vector3 toHome = home - relic.transform.position;
            toHome.y = 0f;
            Vector3 retreat = relic.transform.position + toHome.normalized * RetreatDistance;
            yield return DriveTo(retreat, 3f, 0.7f, 40f, "a spot to turn back and aim from");

            Press(_mouse.rightButton);
            float deadline = Time.time + 40f;
            while (_gameplay.Tether.State != TetherAimState.Towing && Time.time < deadline)
            {
                _pilot.GoTo(relic.transform.position, 5f, 0.45f);
                yield return null;
            }

            _pilot.Target = null;
            Assert.AreEqual(TetherAimState.Towing, _gameplay.Tether.State, "the tether latches onto the relic");
            Assert.AreSame(relic, _gameplay.Tether.Towed);
            Assert.AreEqual(1, _events.TetherAttached.Count);
            End("Turn back and latch the tether", $"latched at {_gameplay.Tether.Length:F1} m");
        }

        private IEnumerator TowHome(Relic relic)
        {
            Begin();
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
            int balance = _gameplay.Wallet.Balance;
            Release(_mouse.rightButton);
            yield return Until(() => _events.RelicDeposited.Count > 0, 8f, "the relic floats onto the shelf");
            RelicDeposited deposited = _events.RelicDeposited[0].Value;
            Assert.AreEqual(relic.Definition.Id, deposited.RelicId);
            Assert.AreEqual(1, deposited.DisplayedCount);
            Assert.AreEqual(RelicState.Displayed, relic.State);
            int gift = _gameplay.Home.Tuning.DepositGift;
            yield return null;
            Assert.AreEqual(balance + gift, _gameplay.Wallet.Balance, "the scrap gift for bringing a memory home");
            yield return new WaitForSeconds(1f);
            Capture("06-on-the-shelf");
            End("Deposit on the shelf", $"+{gift} scrap gift, wallet {_gameplay.Wallet.Balance}");
        }

        private IEnumerator GatherScrapFor(string upgradeId, string what, float timeout)
        {
            yield return GatherScrap(_gameplay.Upgrades.Find(upgradeId).Levels[0].Cost, what, timeout);
        }

        private IEnumerator GatherScrap(int cost, string what, float timeout)
        {
            Begin();
            int start = _gameplay.Wallet.Balance;
            int detours = 0;
            float deadline = Time.time + timeout;
            while (_gameplay.Wallet.Balance < cost && Time.time < deadline)
            {
                Vector3 piece = NearestRestingScrap();
                yield return DriveTo(new Vector3(piece.x, 0f, piece.z), 1.5f, 0.8f, 40f, "a scrap cluster");
                yield return new WaitForSeconds(1.5f);
                detours++;
            }

            Assert.GreaterOrEqual(_gameplay.Wallet.Balance, cost, "enough scrap for " + what);
            End("Gather scrap for " + what, $"{detours} detour(s), wallet {start} -> {_gameplay.Wallet.Balance} / " +
                                            $"{cost}, {_gameplay.Scrap.RemainingValue} left in the basin");
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
            Assert.IsTrue(hint.Ready, "the first level is affordable");
            int purchasesBefore = _events.SignalRadiusChanged.Count;
            Press(_keyboard.eKey);
            yield return Until(() => _events.UpgradePurchased.Count > 0, 15f, "holding confirm buys the level");
            Release(_keyboard.eKey);
            UpgradePurchased purchase = _events.UpgradePurchased[0].Value;
            Assert.AreEqual(Tower, purchase.UpgradeId);
            Assert.AreEqual(1, purchase.Level);
            Assert.Greater(_events.SignalRadiusChanged.Count, purchasesBefore);
            float radius = _events.SignalRadiusChanged[_events.SignalRadiusChanged.Count - 1].Value.Radius;
            Assert.AreEqual(_gameplay.Upgrades.Find(Tower).SignalRadiusAt(1), radius, 1e-3f);
            yield return new WaitForSeconds(2.5f);
            Capture("07-tower-awake");
            End("Park on the pad and buy tower level 1", $"signal radius {radius:F0} m, wallet " +
                                                        $"{_gameplay.Wallet.Balance}");
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
            Relic undiscovered = null;
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            foreach (Relic relic in _gameplay.Relics.Relics)
            {
                bool hidden = relic.State == RelicState.Buried && !relic.Discovered;
                if (hidden && (undiscovered == null ||
                               SurfaceRules.HorizontalDistance(relic.Site.Position, home) <
                               SurfaceRules.HorizontalDistance(undiscovered.Site.Position, home)))
                {
                    undiscovered = relic;
                }
            }

            Assert.IsNotNull(undiscovered, "a relic is still waiting to be found");
            Vector3 toRelic = undiscovered.Site.Position - home;
            toRelic.y = 0f;
            Vector3 stand = undiscovered.Site.Position - toRelic.normalized * SpotStandOff;
            _pilot.GoTo(stand, 3f, 1f);
            float deadline = Time.time + 120f;
            while (!Spotted(undiscovered) && Time.time < deadline)
            {
                yield return null;
            }

            _pilot.Target = null;
            Assert.IsTrue(Spotted(undiscovered), $"Tilly spots '{undiscovered.Definition.Id}' on the next trip");
            Assert.IsTrue(undiscovered.Discovered, "it shows on 07's sonar without a ping");
            yield return new WaitForSeconds(1.5f);
            Capture("12-tilly-spots");
            End($"Next trip: Tilly spots '{undiscovered.Definition.Id}'",
                $"{_events.FriendSpotted.Count} spot(s) on the way");
        }

        private IEnumerator BuyHoverJumpAtTheBench()
        {
            Begin();
            Workshop bench = _gameplay.Workshop;
            var abilities = _context.Get<IRoverAbilities>();
            Assert.IsFalse(abilities.Has(RoverAbility.HoverJump), "07 cannot leap before the workbench");
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 90f,
                "the base pad, clear of the lander");
            yield return DriveTo(bench.PadCentre, 1f, 0.6f, 60f, "the workbench pad");
            yield return Until(() => bench.Occupied, 3f, "07 is parked on the bench's pad");
            Assert.AreSame(bench.Definition, _gameplay.Shop.StationUpgrade, "the bench offers Hover-Jump");
            Assert.AreEqual(UpgradeStationKind.Workshop, _gameplay.Shop.StationUpgrade.Station);

            int charges = _events.RoverJumpCharged.Count;
            _pilot.JumpHeld = true;
            yield return new WaitForSeconds(_rover.Tuning.HoverJump.ChargeTime + ChargeMargin);
            _pilot.JumpHeld = false;
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(charges, _events.RoverJumpCharged.Count, "holding Jump does nothing before the bench");
            Assert.AreEqual(0, _events.RoverJumped.Count);

            Assert.IsTrue(_gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "Hover-Jump is affordable");
            int purchases = _events.UpgradePurchased.Count;
            Press(_keyboard.eKey);
            yield return Until(() => _events.UpgradePurchased.Count > purchases, 15f, "holding confirm buys it");
            Release(_keyboard.eKey);
            UpgradePurchased purchase = _events.UpgradePurchased[_events.UpgradePurchased.Count - 1].Value;
            Assert.AreEqual(HoverJump, purchase.UpgradeId);
            Assert.AreEqual(1, purchase.Level);
            Assert.IsTrue(abilities.Has(RoverAbility.HoverJump), "07 can leap now");
            Assert.IsFalse(_gameplay.Hints.TryGet(InteractionKind.Upgrade, out _), "the bench has nothing left");
            yield return new WaitForSeconds(SparkDelay);
            Assert.Greater(bench.SparkCount, 0, "sparks fly from between the vice jaws");
            Vector3 front = bench.PadCentre - bench.BenchPosition;
            front.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, front.normalized);
            Review(bench.BenchPosition + front.normalized * BenchViewDistance + side * BenchViewSide +
                   Vector3.up * BenchViewHeight, bench.BenchPosition + Vector3.up, "13b-workbench-sparks");
            yield return new WaitForSeconds(1.5f);
            Capture("13-workbench-hover-jump");
            End("Park at the workbench and buy Hover-Jump", $"wallet {_gameplay.Wallet.Balance} after " +
                                                            $"{_gameplay.Upgrades.Find(HoverJump).Levels[0].Cost}");
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

        private IEnumerator FollowTheTrailToTheCanyon()
        {
            Begin();
            WorldAnchor lip = Anchor(WorldAnchorIds.CanyonLip);
            Vector3 runStart = lip.Position - lip.Forward * RunUp;
            List<int> trail = TrailPieces();
            Assert.AreEqual(_gameplay.Scrap.Tuning.CanyonTrailPieces, trail.Count, "the whole trail lies on the lane");
            int earlier = CollectedOf(trail);
            yield return DriveTo(_context.Get<IWorldLayout>().BasePosition, PadArrival, 0.8f, 60f, "the base pad");
            Review(_context.Get<IWorldLayout>().BasePosition + Vector3.up * 3f, lip.Position + Vector3.up * 2f,
                "16-trail-to-the-canyon");
            float distance = SurfaceRules.HorizontalDistance(_rover.Position, runStart);
            yield return DriveTo(runStart, 2f, 1f, 90f, "the run-up on the mouth lane");
            Capture("16b-at-the-run-up");
            End($"Follow the scrap trail to the canyon ({distance:F0} m)",
                $"{earlier} trail piece(s) already gathered for the workshop, {CollectedOf(trail) - earlier} on the " +
                $"way, {trail.Count - CollectedOf(trail)} left up to the lip");
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
            List<int> trail = TrailPieces();
            yield return Until(() => CollectedOf(trail) == trail.Count, 3f,
                "the scrap trail is gathered all the way up to the lip");
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
                Press(_keyboard.eKey, queueEventOnly: true);
                yield return null;
                yield return null;
                Release(_keyboard.eKey);
                yield return new WaitForSeconds(DialPause);
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
        /// The mound relay (relay.0), the teaching mast in the spawn view: its part from the dust, the scrap it costs,
        /// a hold of Interact at its foot, and the whole beat until it comes online.
        /// </summary>
        private IEnumerator RestoreTheMoundRelay()
        {
            RelayField relays = _gameplay.Relays;
            RelayMast mast = relays.Masts[0];
            Assert.AreEqual(WorldAnchorIds.RelayPrefix + 0, mast.Id);
            int cost = relays.NextCost;
            yield return GatherScrap(cost, "the mound relay", 300f);
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
                $"{mast.Paid} scrap, its part {partAway:F0} m from the mast, beat {beat.Duration:F1} s");
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

        /// <summary>The scrap pieces of the trail to the canyon (planned again from the same world, tuning).</summary>
        private List<int> TrailPieces()
        {
            ScrapField scrap = _gameplay.Scrap;
            List<ScrapSpawn> planned = ScrapTrailPlanner.Plan(_context.Get<ITerrainQuery>(),
                _context.Get<IWorldLayout>(), _context.Get<IWorldAnchors>(), scrap.Tuning, scrap.Catalog.Variants,
                out string problem);
            Assert.IsNotNull(planned, problem);
            var pieces = new List<int>();
            foreach (ScrapSpawn spawn in planned)
            {
                for (int i = 0; i < scrap.Count; i++)
                {
                    if (Vector3.Distance(scrap.RestPosition(i), spawn.Position) < 0.01f)
                    {
                        pieces.Add(i);
                    }
                }
            }

            return pieces;
        }

        private int CollectedOf(List<int> pieces)
        {
            int collected = 0;
            foreach (int piece in pieces)
            {
                collected += _gameplay.Scrap.IsCollected(piece) ? 1 : 0;
            }

            return collected;
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

        private bool Spotted(Relic relic)
        {
            foreach (EventRecorder.Timed<FriendSpotted> spotted in _events.FriendSpotted)
            {
                if (SurfaceRules.HorizontalDistance(spotted.Value.Position, relic.Site.Position) < 0.1f)
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
            _pilot.GoTo(target, arriveRadius, maxThrottle);
            yield return Until(() => _pilot.Arrived, timeout, "07 reaches " + what);
            _pilot.Target = null;
            yield return Until(() => _rover.Speed < StopSpeed, 6f, "07 comes to rest at " + what);
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

        private bool Answered(string relicId)
        {
            foreach (EventRecorder.Timed<RelicAnswered> answer in _events.RelicAnswered)
            {
                if (answer.Value.RelicId == relicId)
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

        private Vector3 NearestRestingScrap()
        {
            ScrapField scrap = _gameplay.Scrap;
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < scrap.Count; i++)
            {
                float distance = SurfaceRules.HorizontalDistance(scrap.RestPosition(i), _rover.Position);
                if (!scrap.IsCollected(i) && distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            Assert.GreaterOrEqual(nearest, 0, "scrap is left in the basin");
            return scrap.RestPosition(nearest);
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

        /// <summary>A review capture from a second camera (same lens as the game's) at <paramref name="eye"/>.</summary>
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

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
    /// workbench through the same hold, and take the first full-charge leap on the base pad.
    /// Every step is timed (Logs/gameplay-captures/playthrough.md) and captured; any error or exception in the log
    /// fails it. Slow: run on demand with --category Playthrough.
    /// </summary>
    [Explicit("Slow end-to-end playthrough of the real Main scene; run on demand with --category Playthrough.")]
    [Category("Playthrough")]
    public sealed class GoldenPathPlaythrough : InputTestFixture
    {
        private const string Tower = "radio_tower";
        private const string HoverJump = "rover.hover_jump";
        private const string TillyId = "tilly";

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
        private const float StopSpeed = 0.4f;
        private const float ApproachOffset = 3f;
        private const float RetreatDistance = 16f;
        private const float ShelfStandOff = 4f;
        private const float ReelFrom = 12f;

        /// <summary>Close enough (m) to the base pad centre to turn toward the shelf or the tower.</summary>
        private const float PadArrival = 5f;
        private const float TotalBudget = 600f;

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
        [Timeout(600000)]
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
            Begin();
            int cost = _gameplay.Upgrades.Find(upgradeId).Levels[0].Cost;
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
            Camera view = _context.Get<IViewCamera>().Camera;
            var host = new GameObject("WitnessCamera");
            try
            {
                var witness = host.AddComponent<Camera>();
                witness.CopyFrom(view);
                witness.enabled = false;
                Vector3 toRover = _rover.Position - subject;
                toRover.y = 0f;
                Vector3 eye = subject - toRover.normalized * WitnessDistance + Vector3.up * WitnessHeight;
                host.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(subject - eye));
                FrameCapture.SavePng(witness, 1280, 720,
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

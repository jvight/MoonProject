using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
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
    /// The owner saw 07's head vanish while it stood on the charging pad (2026-10-19) and wanted it to turn to the pad's
    /// heading as it parks. In the real Main scene, with a built pad: 07 drives onto it <see cref="ArrivalDegrees"/>
    /// off the pad's heading, docks, and rests on the pad's centre line at its heading, within
    /// <see cref="PoseDegrees"/> and <see cref="PoseMetres"/>, with its head renderers on, seen from the chase camera,
    /// the whole time it is docked. The docked view is captured (Logs/gameplay-captures/dock-docked.png).
    /// </summary>
    [Category("Dock")]
    public sealed class DockSessions : InputTestFixture
    {
        /// <summary>Degrees off the pad's heading 07 rolls onto it.</summary>
        private const float ArrivalDegrees = 30f;

        /// <summary>Degrees from the pad's forward axis 07 docks at: nose in toward the cooling tower at its rear.
        /// </summary>
        private const float NoseInDegrees = 180f;

        /// <summary>The heading the game gives the pad on the apron (BuildingTuning.ApronPadYaw): open front south.
        /// </summary>
        private const float PadYaw = 180f;

        /// <summary>Metres south of the pad the autoplay bot lines up before it drives in.</summary>
        private const float BotLineUpMetres = -6f;

        /// <summary>Metres past the plate's edge that count as on the pad (inside the 0.3 m margin), and not.</summary>
        private const float InsideMargin = 0.2f;

        private const float OutsideMargin = 0.45f;

        private const float CloseShotSide = 2.6f;
        private const float CloseShotRise = 1.7f;
        private const float CloseShotBack = -3.4f;
        private const float CloseShotLookHeight = 0.6f;
        private const float PartRise = 0.12f;
        private const float ChockLockDegrees = 65f;
        private const float ArmLockDegrees = 80f;
        private const float AngleSlack = 2f;
        private const float NearlyFullCells = 1.2f;
        private const float HalfCharge = 0.5f;
        private const float SteerWatchSeconds = 2.5f;
        private const float ReleaseWithin = 0.6f;
        private const float PoseTimeout = 12f;
        private const float LightThrottle = 0.15f;
        private const float HeldSteer = 0.8f;
        private const float PoseDegrees = 2f;
        private const float PoseMetres = 0.05f;

        /// <summary>Metres from the pad's centre 07 starts its approach.</summary>
        private const float ApproachMetres = 6f;

        private const float ArriveRadius = 0.4f;
        private const float WakeTimeout = 20f;
        private const float DriveTimeout = 30f;
        private const float DockTimeout = 6f;
        private const float WatchSeconds = 4f;
        private const float SettleSeconds = 0.6f;

        /// <summary>Seconds after the dock takes 07 the chase view is captured: walls up, 07 settled.</summary>
        private const float ShotSeconds = 1.5f;
        private const float TouchThrottle = 0.6f;
        private const float TouchTimeout = 2f;
        private const float TouchDriveSeconds = 3f;
        private const float DrivenOffMetres = 0.5f;

        private Keyboard _keyboard;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private bool _awake;
        private ITerrainQuery _terrain;
        private Autopilot _pilot;
        private StructurePose _pose;
        private int _padId;
        private readonly List<PadLockClosed> _closed = new List<PadLockClosed>();
        private readonly List<PadLockOpened> _opened = new List<PadLockOpened>();
        private Transform[] _chocks;
        private Transform[] _arms;
        private Quaternion[] _chockRest;
        private Quaternion[] _armRest;
        private Vector3 _centre;
        private Vector3 _dockHeading;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
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
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator Docked_07TurnsToThePadsHeading_AndItsHeadStaysOn()
        {
            yield return Stage(ArrivalDegrees);
            yield return RollOn();
            Vector3 centre = _centre;
            Vector3 dockHeading = _dockHeading;
            Autopilot pilot = _pilot;

            yield return new WaitForSeconds(ShotSeconds);
            Capture("dock-docked");
            Assert.Greater(_gameplay.Building.HoveredInstanceId, 0, "07 looks at the pad it rests on");
            Assert.IsFalse(_gameplay.Hints.TryGet(InteractionKind.Deconstruct, out _),
                "no Deconstruct prompt while 07 rests docked on its pad outside build mode");
            Renderer[] head = HeadRenderers();
            float until = Time.time + WatchSeconds;
            float closest = float.MaxValue;
            int hidden = 0;
            while (Time.time < until)
            {
                yield return null;
                closest = Mathf.Min(closest, Vector3.Distance(_context.Get<IViewCamera>().Camera.transform.position,
                    _rover.Position));
                if (!HeadOn(head))
                {
                    hidden++;
                }
            }

            Assert.AreEqual(0, hidden, "07's head is drawn every frame it is docked, not hidden for a close camera");
            Vector3 error = _rover.Position - centre;
            error.y = 0f;
            float turned = Mathf.Abs(Mathf.DeltaAngle(_rover.Rotation.eulerAngles.y, Quaternion.LookRotation(dockHeading).eulerAngles.y));
            Debug.Log($"[dock] docked {error.magnitude * 100f:F1} cm and {turned:F2} deg off the pad's pose; the " +
                      $"camera came within {closest:F2} m of 07");
            Assert.LessOrEqual(error.magnitude, PoseMetres, "07 rests on the pad's centre line");
            Assert.LessOrEqual(turned, PoseDegrees, "07 rests at the pad's heading");

            pilot.Reverse = TouchThrottle;
            yield return Until(() => !_rover.IsDocked, TouchTimeout, "a touch of the wheel lets 07 go from the dock");
            yield return Until(() => !_gameplay.PadLock.Raised, TouchTimeout, "the walls drop when 07 is let go");
            yield return new WaitForSeconds(TouchDriveSeconds);
            pilot.Reverse = 0f;
            Vector3 driven = _rover.Position - centre;
            driven.y = 0f;
            Assert.Greater(driven.magnitude, DrivenOffMetres,
                $"07 drives off the pad, not back onto the dock (docked {_rover.IsDocked}, speed {_rover.Speed:F2})");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ComingInFromTheBotsSide_07RollsOntoThePad_AndChargesAtOnce()
        {
            yield return Stage(0f);
            Vector3 start = SurfaceRules.OnSurface(_terrain, _centre.x, _centre.z + BotLineUpMetres);
            _context.Get<IRoverPlacement>().PlaceAt(start, Quaternion.LookRotation(Vector3.back, Vector3.up));
            yield return new WaitForSeconds(SettleSeconds);
            _pilot.Target = _centre;
            float began = Time.time;
            yield return Until(() => _gameplay.Battery.Model.IsCharging, DriveTimeout,
                () => $"07 rolls onto the pad from the bot's side and charges (07 at {_rover.Position}, the pad at " +
                      $"{_centre}, speed {_rover.Speed:F2})");
            _pilot.Target = null;
            Debug.Log($"[dock] from the bot's side, 07 charged {Time.time - began:F1} s after it set off");
            yield return Until(() => _rover.IsDocked && _gameplay.PadLock.Raised, DockTimeout, "07 docks on the pad");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ArrivingWithAFingerOnTheThrottleAndTheWheelHeld_ChargingStartsAndTheDockHolds()
        {
            yield return Stage(ArrivalDegrees);
            _rover.SetDriveSource(new LightHold(_pilot, LightThrottle, HeldSteer));
            _pilot.Target = _centre;
            yield return Until(() => _gameplay.Battery.Model.IsCharging, DriveTimeout,
                () => $"07 charges on the pad with a light throttle held (07 at {_rover.Position}, the pad at " +
                      $"{_centre}, speed {_rover.Speed:F2})");
            _pilot.Target = null;
            float until = Time.time + WatchSeconds;
            bool docked = false;
            while (Time.time < until)
            {
                yield return null;
                docked |= _rover.IsDocked;
                Assert.IsTrue(_gameplay.Battery.Model.IsCharging, "a light touch of the wheel never ends the charge");
            }

            Assert.IsTrue(docked, "07 docks on the pad though a light throttle is held");
            Assert.IsTrue(_rover.IsDocked, "a light touch of the wheel does not let 07 go from the dock");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator OnThePlate_TheChocksStandStaggered_ThenTheArmsClose_AndTheLockClosesOnce()
        {
            yield return Stage(0f);
            FindLockParts();
            CaptureClose("pad-lock-open-close");
            Bounds[] chocksOpen = PartBounds(_chocks);
            Bounds[] armsOpen = PartBounds(_arms);
            _pilot.Target = _centre;
            var seen = new List<float>();
            float until = Time.time + DriveTimeout;
            while (Time.time < until && _closed.Count == 0)
            {
                yield return null;
                seen.Add(ChockAngle(0));
            }

            Assert.AreEqual(1, _closed.Count, "the lock closes once 07 is held");
            for (int i = 0; i < _chocks.Length; i++)
            {
                Assert.AreEqual(ChockLockDegrees, ChockAngle(i), AngleSlack, $"chock {i} stands, hinged up to cradle the wheel");
            }

            for (int i = 0; i < _arms.Length; i++)
            {
                Assert.AreEqual(ArmLockDegrees, ArmAngle(i), AngleSlack, $"clamp arm {i} has swung in against the flank");
            }

            Bounds[] chocksShut = PartBounds(_chocks);
            Bounds[] armsShut = PartBounds(_arms);
            for (int i = 0; i < _chocks.Length; i++)
            {
                Debug.Log($"[padlock] chock {i}: top {chocksOpen[i].max.y:F2} -> {chocksShut[i].max.y:F2}, centre " +
                          $"{chocksOpen[i].center} -> {chocksShut[i].center}");
                Assert.Greater(chocksShut[i].max.y - chocksOpen[i].max.y, PartRise, $"chock {i} stands up from its slot");
            }

            for (int i = 0; i < _arms.Length; i++)
            {
                Debug.Log($"[padlock] arm {i}: top {armsOpen[i].max.y:F2} -> {armsShut[i].max.y:F2}, centre " +
                          $"{armsOpen[i].center} -> {armsShut[i].center}");
                Assert.Greater(armsShut[i].max.y - armsOpen[i].max.y, PartRise, $"clamp arm {i} swings up");
                Assert.Less(Mathf.Abs(armsShut[i].center.x - _centre.x), Mathf.Abs(armsOpen[i].center.x - _centre.x),
                    $"clamp arm {i} swings in toward 07");
            }

            Assert.IsTrue(seen.Exists(angle => angle > 5f && angle < ChockLockDegrees - 5f), "the chocks rise: they were seen part way up");
            yield return new WaitForSeconds(ShotSeconds);
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, 1600, 900,
                Path.Combine(GameplayFixture.CaptureFolder, "pad-lock-locked.png"));
            CaptureClose("pad-lock-locked-close");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator WhenTheBatteryIsFull_TheArmsOpenFirst_AndTheChocksLowerLast()
        {
            yield return Stage(0f);
            FindLockParts();
            BatteryModel battery = _gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells - NearlyFullCells });
            _pilot.Target = _centre;
            yield return Until(() => _closed.Count == 1, DriveTimeout, "the lock closes");
            float armsOpenAt = -1f;
            float chocksDownAt = -1f;
            float deadline = Time.time + DriveTimeout;
            while (Time.time < deadline && chocksDownAt < 0f)
            {
                yield return null;
                if (armsOpenAt < 0f && ArmAngle(0) < ArmLockDegrees * 0.5f)
                {
                    armsOpenAt = Time.time;
                }

                if (chocksDownAt < 0f && ChockAngle(0) < ChockLockDegrees * 0.5f && ChockAngle(3) < ChockLockDegrees * 0.5f)
                {
                    chocksDownAt = Time.time;
                }
            }

            Assert.IsTrue(armsOpenAt >= 0f && chocksDownAt >= 0f, "a full battery opens the lock");
            Assert.LessOrEqual(armsOpenAt, chocksDownAt, "the arms open first, the chocks last");
            Assert.AreEqual(1, _opened.Count, "the lock opens once");
            yield return new WaitForSeconds(SettleSeconds);
            Assert.IsFalse(_rover.IsDocked, "and 07 is free once the lock has let go");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ThrottleOpensTheLockAtOnce_ButSteeringAloneNeverDoes()
        {
            yield return Stage(0f);
            FindLockParts();
            BatteryModel battery = _gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells * HalfCharge });
            _pilot.Target = _centre;
            yield return Until(() => _closed.Count == 1, DriveTimeout, "the lock closes");

            _rover.SetDriveSource(new SteerOnly());
            yield return new WaitForSeconds(SteerWatchSeconds);
            Assert.AreEqual(0, _opened.Count, "steering alone never lets 07 go");
            Assert.AreEqual(ArmLockDegrees, ArmAngle(0), AngleSlack, "the arms stay closed");

            _pilot.Target = null;
            _pilot.Reverse = TouchThrottle;
            _rover.SetDriveSource(_pilot);
            float pushed = Time.time;
            yield return Until(() => _opened.Count == 1, ReleaseWithin, "a push on the throttle opens the lock");
            Assert.LessOrEqual(Time.time - pushed, ReleaseWithin, "and at once");
            yield return Until(() => ArmAngle(0) < 1f, PoseTimeout, "the arms are open");
            yield return Until(() => ChockAngle(0) < 1f, PoseTimeout,
                "the chocks are down: an early drive-off loses nothing");
            _pilot.Reverse = 0f;
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ArrivingFull_ThePadStillTakes07In_PlaysTheFullBeat_AndLetsGoOnce()
        {
            yield return Stage(0f);
            FindLockParts();
            BatteryModel battery = _gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells });
            float closedAt = -1f;
            float fullAt = -1f;
            float openedAt = -1f;
            using (_context.Events.Subscribe<PadLockClosed>(_ => closedAt = closedAt < 0f ? Time.time : closedAt))
            using (_context.Events.Subscribe<PadLockOpened>(_ => openedAt = openedAt < 0f ? Time.time : openedAt))
            using (_context.Events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Dock && beat.Phase == FeelPhase.Reward && fullAt < 0f)
                {
                    fullAt = Time.time;
                }
            }))
            {
                // Set down on the plate with no drive to spend a cell: 07 comes to rest full.
                _context.Get<IRoverPlacement>().PlaceAt(_centre,
                    Quaternion.LookRotation(_dockHeading, Vector3.up));
                float until = Time.time + DriveTimeout;
                while (Time.time < until && openedAt < 0f)
                {
                    yield return null;
                }

                Assert.GreaterOrEqual(closedAt, 0f, "a full 07 resting on the pad is locked in");
                Assert.GreaterOrEqual(fullAt, 0f, $"the full beat plays (closed {closedAt:F2}, opened {openedAt:F2}, " +
                                                  $"charge {battery.Charge:F3}/{battery.Cells}, full {battery.CellsFull}, " +
                                                  $"charging {battery.IsCharging})");
                Assert.GreaterOrEqual(openedAt, 0f, "and the lock lets go");
                Assert.Less(closedAt, fullAt, "the lock closes before the full beat");
                Assert.Less(fullAt, openedAt, "and opens after it");
                Assert.IsTrue(_gameplay.Opening.FirstCharge, "the old cells gave out: the first charge happened");
                yield return Until(() => !_rover.IsDocked, PoseTimeout, "07 is free once the lock has let go");
                Assert.AreEqual(1, _closed.Count, "the lock closed once");
                Assert.AreEqual(1, _opened.Count, "and opened once");
                yield return new WaitForSeconds(SteerWatchSeconds);
                Assert.AreEqual(1, _closed.Count, "07 resting there, full, is not locked in again");
            }
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator OffThePlate_NothingLocks()
        {
            yield return Stage(0f);
            FindLockParts();
            StructureDefinition definition = _gameplay.Building.Catalog.Find(StructureIds.ChargingPad);
            Vector3 at = SurfaceRules.OnSurface(_terrain, _centre.x + definition.Width * 0.5f + OutsideMargin + 0.5f,
                _centre.z);
            _context.Get<IRoverPlacement>().PlaceAt(at, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return new WaitForSeconds(DockTimeout);
            Assert.IsFalse(_gameplay.PadLock.OnPad, "07 is off the plate");
            Assert.AreEqual(0, _closed.Count, "no lock closes");
            for (int i = 0; i < _chocks.Length; i++)
            {
                Assert.Less(ChockAngle(i), 0.5f, $"chock {i} lies flush");
            }

            Assert.Less(ArmAngle(0), 0.5f, "the arms lie flat");
        }

        private sealed class SteerOnly : IRoverDriveSource
        {
            public Vector2 Drive => new Vector2(1f, 0f);
        }

        private void FindLockParts()
        {
            Assert.IsTrue(_gameplay.Building.TryGetView(_padId, out StructureView view), "the pad has its view");
            string[] chockNames = { "Chock_FL", "Chock_FR", "Chock_RL", "Chock_RR" };
            string[] armNames = { "ClampArm_L", "ClampArm_R" };
            _chocks = new Transform[chockNames.Length];
            _chockRest = new Quaternion[chockNames.Length];
            _arms = new Transform[armNames.Length];
            _armRest = new Quaternion[armNames.Length];
            for (int i = 0; i < chockNames.Length; i++)
            {
                _chocks[i] = FindIn(view.transform, chockNames[i]);
                if (_chocks[i] == null)
                {
                    Assert.Ignore("the pad model has no wheel chocks and clamp arms yet (Argon's box/agy-pad-lock)");
                }

                _chockRest[i] = _chocks[i].localRotation;
            }

            for (int i = 0; i < armNames.Length; i++)
            {
                _arms[i] = FindIn(view.transform, armNames[i]);
                Assert.IsNotNull(_arms[i], armNames[i] + " is on the pad");
                _armRest[i] = _arms[i].localRotation;
            }

            _closed.Clear();
            _opened.Clear();
            _context.Events.Subscribe<PadLockClosed>(_closed.Add);
            _context.Events.Subscribe<PadLockOpened>(_opened.Add);
        }

        /// <summary>The game's camera, for one frame, close by the pad from its front corner, so the lock shows.</summary>
        private void CaptureClose(string name)
        {
            Transform lens = _context.Get<IViewCamera>().Camera.transform;
            Vector3 position = lens.position;
            Quaternion rotation = lens.rotation;
            Vector3 target = _centre + Vector3.up * CloseShotLookHeight;
            Vector3 eye = _centre + new Vector3(CloseShotSide, CloseShotRise, CloseShotBack);
            lens.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
            FrameCapture.SavePng(lens.GetComponent<Camera>(), 1600, 900,
                Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
            lens.SetPositionAndRotation(position, rotation);
        }

        private static Transform FindIn(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindIn(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Bounds[] PartBounds(Transform[] parts)
        {
            var bounds = new Bounds[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                Renderer[] renderers = parts[i].GetComponentsInChildren<Renderer>();
                bounds[i] = renderers[0].bounds;
                for (int r = 1; r < renderers.Length; r++)
                {
                    bounds[i].Encapsulate(renderers[r].bounds);
                }
            }

            return bounds;
        }

        private float ChockAngle(int index)
        {
            return Quaternion.Angle(_chockRest[index], _chocks[index].localRotation);
        }

        private float ArmAngle(int index)
        {
            return Quaternion.Angle(_armRest[index], _arms[index].localRotation);
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator OnThePadMeansOnItsPlate_EdgeIncluded_NotAMetreAndAHalfFromItsCentre()
        {
            yield return Stage(0f);
            StructureDefinition definition = _gameplay.Building.Catalog.Find(StructureIds.ChargingPad);
            var terrain = _context.Get<ITerrainQuery>();
            float edge = definition.Width * 0.5f;
            Vector3 along = Vector3.right;

            yield return PlaceAt(terrain, along * (edge + InsideMargin));
            Assert.IsTrue(_gameplay.PadLock.OnPad, "just past the plate's edge, within the margin, is on the pad");
            Assert.IsTrue(_gameplay.StartKit.OnPad, "and the cutter socket is reached there");

            yield return PlaceAt(terrain, along * (edge + OutsideMargin));
            Assert.IsFalse(_gameplay.PadLock.OnPad, "beyond the margin 07 is off the pad");
            Assert.IsFalse(_gameplay.StartKit.OnPad);

            yield return PlaceAt(terrain, Vector3.forward * (definition.Depth * 0.5f + InsideMargin));
            Assert.IsTrue(_gameplay.PadLock.OnPad, "the plate's front and back edges count as well");

            yield return PlaceAt(terrain, Vector3.zero);
            Assert.IsTrue(_gameplay.PadLock.OnPad, "the centre is on the pad");
        }

        private IEnumerator PlaceAt(ITerrainQuery terrain, Vector3 offset)
        {
            Vector3 at = SurfaceRules.OnSurface(terrain, _centre.x + offset.x, _centre.z + offset.z);
            _context.Get<IRoverPlacement>().PlaceAt(at, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return null;
            yield return null;
        }

        /// <summary>
        /// The Main scene with a built pad where the game puts it (the apron, open front to the south), 07 on its way:
        /// <paramref name="degrees"/> off the dock heading, <see cref="ApproachMetres"/> out on the open side.
        /// </summary>
        private IEnumerator Stage(float degrees)
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();

            _terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron),
                "the World has the apron, where the lander's cable ends");
            _centre = SurfaceRules.OnSurface(_terrain, apron.Position.x, apron.Position.z);
            _pose = new StructurePose(_centre, PadYaw);
            _padId = _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.ChargingPad), _pose,
                1f);
            yield return new WaitForSeconds(SettleSeconds);

            _dockHeading = Quaternion.Euler(0f, NoseInDegrees, 0f) * _pose.Forward;
            Vector3 arrival = Quaternion.Euler(0f, degrees, 0f) * _dockHeading;
            Vector3 start = SurfaceRules.OnSurface(_terrain, _centre.x - arrival.x * ApproachMetres,
                _centre.z - arrival.z * ApproachMetres);
            _context.Get<IRoverPlacement>().PlaceAt(start, Quaternion.LookRotation(arrival, Vector3.up));
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            rig.Orbit.AdoptBearing(Quaternion.LookRotation(arrival, Vector3.up).eulerAngles.y);
            yield return new WaitForSeconds(SettleSeconds);

            _pilot = new Autopilot(_rover) { ArriveRadius = ArriveRadius };
            _rover.SetDriveSource(_pilot);
        }

        /// <summary>07 drives onto the pad's centre and the dock takes it.</summary>
        private IEnumerator RollOn()
        {
            _pilot.Target = _centre;
            yield return Until(() => _pilot.Arrived, DriveTimeout,
                () => $"07 rolls onto the pad (07 at {_rover.Position}, the pad at {_centre}, {_pilot.Distance:F2} m " +
                      $"off, docked {_rover.IsDocked}, charging {_gameplay.Battery.Model.IsCharging}, speed " +
                      $"{_rover.Speed:F2})");
            _pilot.Target = null;
            yield return Until(() => _rover.IsDocked && _gameplay.PadLock.Raised, DockTimeout,
                () => $"07 docks on the pad and its walls rise (docked {_rover.IsDocked}, on pad " +
                      $"{_gameplay.PadLock.OnPad}, charging {_gameplay.Battery.Model.IsCharging})");
        }

        /// <summary>
        /// A finger kept lightly on the throttle and the wheel held over, on top of whatever the pilot does.
        /// </summary>
        private sealed class LightHold : IRoverDriveSource
        {
            private readonly Autopilot _pilot;
            private readonly float _throttle;
            private readonly float _steer;

            public LightHold(Autopilot pilot, float throttle, float steer)
            {
                _pilot = pilot;
                _throttle = throttle;
                _steer = steer;
            }

            public Vector2 Drive
            {
                get
                {
                    Vector2 drive = _pilot.Drive;
                    return new Vector2(Mathf.Abs(drive.x) > _steer ? drive.x : _steer, Mathf.Max(drive.y, _throttle));
                }
            }
        }

        private static Renderer[] HeadRenderers()
        {
            var body = Object.FindFirstObjectByType<RoverBodyLanguage>();
            Assert.IsNotNull(body, "07 has a body language rig");
            Assert.IsNotNull(body.NeckTransform, "07 has a neck");
            Renderer[] renderers = body.NeckTransform.GetComponentsInChildren<Renderer>(true);
            Assert.IsNotEmpty(renderers, "07's head has renderers");
            return renderers;
        }

        private static bool HeadOn(Renderer[] head)
        {
            for (int i = 0; i < head.Length; i++)
            {
                if (head[i].enabled && head[i].shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                {
                    return false;
                }
            }

            return true;
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, 1280, 720,
                Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
        }

        private IEnumerator Boot()
        {
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The dock sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            _rover = (RoverController)_context.Get<IRoverRig>();
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                yield return Until(() => _awake, WakeTimeout, "07 wakes");
            }
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            yield return Until(condition, timeout, () => what);
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, Func<string> what)
        {
            float deadline = Time.time + timeout;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), "timed out waiting until " + (condition() ? string.Empty : what()));
        }
    }
}

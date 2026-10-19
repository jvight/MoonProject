using System;
using System.Collections;
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
    /// The opening's first charge (B6) and the eclipse it releases, in the real Main scene (found by the autoplay bot
    /// 2026-10-19: a player who takes their time must never stall on the pad). However full 07's battery is when it
    /// rolls onto the built pad on the lander's cable, FirstCharge fires: 98 % within the dock's settle, already full
    /// at once, 30 % as the pad charges it; and the eclipse holds until it does, however long the opening runs first.
    /// </summary>
    [Category("FirstCharge")]
    public sealed class FirstChargeSessions : InputTestFixture
    {
        /// <summary>The heading the game gives the pad on the apron (BuildingTuning.ApronPadYaw).</summary>
        private const float PadYaw = 180f;

        /// <summary>07 rolls in from the south, onto the pad's open front, heading north toward its tower.</summary>

        private const float ApproachMetres = 6f;
        private const float ArriveRadius = 0.4f;
        private const float WakeTimeout = 20f;
        private const float DriveTimeout = 30f;
        private const float SettleSeconds = 0.6f;

        /// <summary>Seconds after 07 rests on the pad a first charge must have fired for a nearly full battery.</summary>
        private const float FiresWithin = 4f;

        private const float LongWait = 240f;
        private const float WaitTimeScale = 24f;
        private const float WaitStep = 0.5f;

        private Keyboard _keyboard;
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private bool _awake;
        private int _milestones;
        private IDisposable _reached;
        private Vector3 _centre;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _milestones = 0;
            _awake = false;
            _gameplay = null;
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
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
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ArrivingAt98Percent_TheFirstChargeFiresWithinTheSettle()
        {
            yield return StageOnThePad(0.98f);
            yield return RollOn();
            float arrived = Time.time;
            yield return Until(() => _gameplay.Opening.FirstCharge, FiresWithin,
                () => $"the first charge fires for a battery at 98 % (charge {_gameplay.Battery.Model.Charge01:P0}, " +
                      $"charging {_gameplay.Battery.Model.IsCharging}, on the pad {_gameplay.PadLock.OnPad})");
            Debug.Log($"[firstcharge] at 98 %, fired {Time.time - arrived:F1} s after 07 rolled on");
            Assert.AreEqual(1, _milestones, "the FirstCharge milestone is reached once");
            Assert.IsFalse(_context.Get<ISunlight>().IsHoldingEclipse, "the first charge releases the eclipse");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ArrivingAlreadyFull_TheFirstChargeStillFires()
        {
            yield return StageOnThePad(1f);
            // Set down on the pad with no drive to spend a cell: a battery that is full when 07 rests there.
            _context.Get<IRoverPlacement>().PlaceAt(_centre, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return Until(() => _gameplay.Opening.FirstCharge, FiresWithin,
                () => $"the first charge fires for a full battery (charge {_gameplay.Battery.Model.Charge01:P0}, " +
                      $"resting {_gameplay.Battery.Model.RestsOnCharger}, on the pad {_gameplay.PadLock.OnPad})");
            Assert.AreEqual(1, _milestones, "the FirstCharge milestone is reached once");
            Assert.IsFalse(_context.Get<ISunlight>().IsHoldingEclipse, "the first charge releases the eclipse");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ArrivingAt30Percent_TheEclipseHoldsUntilThePadChargesIt()
        {
            yield return StageOnThePad(0.3f);
            var sunlight = _context.Get<ISunlight>();
            Assert.IsTrue(sunlight.IsHoldingEclipse, "the eclipse holds while 07 is on its way to the pad");
            yield return RollOn();
            yield return Until(() => _gameplay.Opening.FirstCharge, DriveTimeout,
                () => $"the pad charging 07 from 30 % is its first charge (charging " +
                      $"{_gameplay.Battery.Model.IsCharging}, charge {_gameplay.Battery.Model.Charge01:P0})");
            Assert.IsTrue(_gameplay.Battery.Model.IsCharging || _gameplay.Battery.Model.Charge01 > 0.3f,
                "the first charge is a real charge here");
            Assert.AreEqual(1, _milestones, "the FirstCharge milestone is reached once");
            Assert.IsFalse(sunlight.IsHoldingEclipse, "the first charge releases the eclipse");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator HoweverLongTheOpeningRuns_NoSunriseBeforeTheFirstCharge()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();
            var sunlight = _context.Get<ISunlight>();
            Assert.IsTrue(sunlight.IsHoldingEclipse, "a new game starts in the eclipse");
            Time.timeScale = WaitTimeScale;
            float simulated = 0f;
            while (simulated < LongWait)
            {
                yield return new WaitForSeconds(WaitStep);
                simulated += WaitStep * WaitTimeScale;
                Assert.IsTrue(sunlight.IsHoldingEclipse, $"the eclipse still holds {simulated:F0} s into the opening");
                Assert.AreEqual(SkyPhase.Eclipse, sunlight.Phase, $"no sunrise {simulated:F0} s into the opening");
            }

            Assert.IsFalse(_gameplay.Opening.FirstCharge, "no first charge happened in all that time");
        }

        /// <summary>The Main scene with a built pad on the apron, 07's battery at <paramref name="charge01"/>.</summary>
        private IEnumerator StageOnThePad(float charge01)
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();

            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron),
                "the World has the apron, where the lander's cable ends");
            Vector3 centre = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.ChargingPad),
                new StructurePose(centre, PadYaw), 1f);
            BatteryModel battery = _gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells * charge01 });
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 arrival = Vector3.forward;
            Vector3 start = SurfaceRules.OnSurface(terrain, centre.x - arrival.x * ApproachMetres,
                centre.z - arrival.z * ApproachMetres);
            _context.Get<IRoverPlacement>().PlaceAt(start, Quaternion.LookRotation(arrival, Vector3.up));
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            rig.Orbit.AdoptBearing(Quaternion.LookRotation(arrival, Vector3.up).eulerAngles.y);
            yield return new WaitForSeconds(SettleSeconds);
            _centre = centre;
        }

        /// <summary>07 drives onto the pad's centre and comes to rest there.</summary>
        private IEnumerator RollOn()
        {
            var pilot = new Autopilot(_rover) { ArriveRadius = ArriveRadius };
            _rover.SetDriveSource(pilot);
            pilot.Target = _centre;
            yield return Until(() => pilot.Arrived, DriveTimeout,
                () => $"07 rolls onto the pad (07 at {_rover.Position}, the pad at {_centre})");
            pilot.Target = null;
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
            throw new NotSupportedException("The first-charge sessions load the scene through the editor.");
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
            _reached = _context.Events.Subscribe<OpeningMilestoneReached>(reached =>
                _milestones += reached.Milestone == OpeningMilestone.FirstCharge ? 1 : 0);
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

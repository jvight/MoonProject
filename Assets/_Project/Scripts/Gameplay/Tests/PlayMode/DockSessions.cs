using System;
using System.Collections;
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
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            yield return Boot();

            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron),
                "the World has the apron, where the lander's cable ends");
            Vector3 centre = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            var pose = new StructurePose(centre, 0f);
            _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.ChargingPad), pose, 1f);
            yield return new WaitForSeconds(SettleSeconds);

            Vector3 dockHeading = Quaternion.Euler(0f, NoseInDegrees, 0f) * pose.Forward;
            Vector3 arrival = Quaternion.Euler(0f, ArrivalDegrees, 0f) * dockHeading;
            Vector3 start = SurfaceRules.OnSurface(terrain, centre.x - arrival.x * ApproachMetres,
                centre.z - arrival.z * ApproachMetres);
            _context.Get<IRoverPlacement>().PlaceAt(start, Quaternion.LookRotation(arrival, Vector3.up));
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            rig.Orbit.AdoptBearing(Quaternion.LookRotation(arrival, Vector3.up).eulerAngles.y);
            yield return new WaitForSeconds(SettleSeconds);

            var pilot = new Autopilot(_rover) { ArriveRadius = ArriveRadius };
            _rover.SetDriveSource(pilot);
            pilot.Target = centre;
            yield return Until(() => pilot.Arrived, DriveTimeout,
                () => $"07 rolls onto the pad (07 at {_rover.Position}, the pad at {centre}, {pilot.Distance:F2} m " +
                      $"off, docked {_rover.IsDocked}, charging {_gameplay.Battery.Model.IsCharging}, speed " +
                      $"{_rover.Speed:F2})");
            pilot.Target = null;
            yield return Until(() => _rover.IsDocked && _gameplay.PadLock.Raised, DockTimeout,
                () => $"07 docks on the pad and its walls rise (docked {_rover.IsDocked}, on pad " +
                      $"{_gameplay.PadLock.OnPad}, charging {_gameplay.Battery.Model.IsCharging})");

            yield return new WaitForSeconds(ShotSeconds);
            Capture("dock-docked");
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

            Capture("dock-docked");
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

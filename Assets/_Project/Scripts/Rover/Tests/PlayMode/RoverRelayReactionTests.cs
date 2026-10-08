using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07 and its camera around the relay network (M3-06, real wiring): a restored mast's lamp gets a long look up, a
    /// perk-up and a slow camera tilt that frames it, then everything returns (07 held parked meanwhile, as the
    /// repair does, so the wide shot does not open over it); a new Bell signal pillar within range
    /// gets a glance (beyond it, none); landing from a radio-hop 07 looks around, left then right, then settles; the
    /// open hop list keeps the wide shot closed.
    /// </summary>
    public sealed class RoverRelayReactionTests : InputTestFixture
    {
        /// <summary>The lamp of a mast just ahead and to the right of 07 (heading frame, m).</summary>
        private static readonly Vector3 LampOffset = new Vector3(2.5f, 8.2f, 5f);

        private const float NearSignal = 120f;
        private const float FarSignal = 420f;

        /// <summary>A shortened rest before the wide shot opens (s).</summary>
        private const float WideShotDelay = 1.5f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -150f), 0f, wideShotDelay: WideShotDelay);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        /// <summary>Angle (deg) between 07's gaze (the lens's +Z) and <paramref name="point"/>.</summary>
        private float GazeOff(Vector3 point)
        {
            Transform eye = _rover.Context.Get<IRoverRig>().TetherOrigin;
            return Vector3.Angle(eye.forward, point - eye.position);
        }

        private float NeckYaw => Mathf.DeltaAngle(0f, _rover.Neck.localEulerAngles.y);

        [UnityTest]
        public IEnumerator RestoredMast_LooksUpAtTheLamp_TiltsTheCameraUp_ThenReturns()
        {
            var repair = new object();
            _rover.Context.Get<IRoverRig>().SetHoldStill(repair, true);
            yield return Wait(1.5f);
            RoverController controller = _rover.Controller;
            Transform camera = _rover.Camera.transform;
            Vector3 lamp = controller.Position + controller.Rotation * LampOffset;
            float gazeBefore = GazeOff(lamp);
            float cameraBefore = Vector3.Angle(camera.forward, lamp - camera.position);
            Vector3 restingCamera = camera.position;

            _rover.Context.Events.Publish(new RelayRestored("relay.0", lamp, 1, 4, "home", 0f));
            var trace = new CameraTrace(camera);
            float closestGaze = gazeBefore;
            float closestCamera = cameraBefore;
            float perk = 0f;
            float until = Time.time + 4f;
            while (Time.time < until)
            {
                yield return null;
                trace.Step(_rover.CameraRig.MomentWeight);
                closestGaze = Mathf.Min(closestGaze, GazeOff(lamp));
                closestCamera = Mathf.Min(closestCamera, Vector3.Angle(camera.forward, lamp - camera.position));
                perk = Mathf.Max(perk, _rover.BodyLanguage.Mood.Perk);
                Vector3 onScreen = _rover.Camera.WorldToViewportPoint(controller.Position);
                Assert.That(onScreen.x, Is.InRange(0f, 1f), "07 stays in frame.");
                Assert.That(onScreen.y, Is.InRange(0f, 1f), "07 stays in frame.");
            }

            Debug.Log($"[rover-relay] gaze {gazeBefore:0.0} -> {closestGaze:0.0} deg off the lamp, camera "
                + $"{cameraBefore:0.0} -> {closestCamera:0.0} deg, perk {perk:0.00}; {trace}");
            Assert.Less(closestGaze, 15f, "07 looks up at the lamp.");
            Assert.Less(closestCamera, cameraBefore - 12f, "The camera tilts up toward the mast and its lamp.");
            Assert.Greater(perk, 0.4f, "A perk-up.");
            Assert.Less(trace.Fastest, 6f, "A slow tilt, never a jolt.");

            yield return Wait(6f);
            Assert.Less(Vector3.Distance(restingCamera, camera.position), 0.5f, "Back to the chase view.");
            Assert.Greater(GazeOff(lamp), 25f, "07 looks away again once the look is over.");
            _rover.Context.Get<IRoverRig>().SetHoldStill(repair, false);
        }

        [UnityTest]
        public IEnumerator BellSignal_WithinRange_GetsAGlance_FarAway_None()
        {
            yield return Wait(1f);
            RoverController controller = _rover.Controller;
            Vector3 right = controller.Rotation * Vector3.right;

            Vector3 far = controller.Position + right * FarSignal;
            _rover.Context.Events.Publish(new BellSignalPicked(BellSignalTarget.Relic, far));
            yield return Wait(1f);
            Assert.AreEqual(0, controller.Gaze.Count, "A pillar beyond range gets no glance.");

            Vector3 near = controller.Position + right * NearSignal;
            float before = GazeOff(near);
            _rover.Context.Events.Publish(new BellSignalPicked(BellSignalTarget.Relic, near));
            float closest = before;
            float until = Time.time + 1.5f;
            while (Time.time < until)
            {
                yield return null;
                closest = Mathf.Min(closest, GazeOff(near));
            }

            Debug.Log($"[rover-relay] signal glance {before:0.0} -> {closest:0.0} deg");
            Assert.Less(closest, before - 40f, "07 glances toward the pillar.");
            yield return Wait(_rover.CharacterTuning.SignalGlanceSeconds + 1f);
            Assert.AreEqual(0, controller.Gaze.Count, "Only a glance.");
        }

        [UnityTest]
        public IEnumerator RadioHopLanding_LooksLeft_ThenRight_ThenSettles()
        {
            yield return Wait(1f);
            _rover.Context.Events.Publish(new RadioHopFinished("relay.0"));
            float left = 0f;
            float right = 0f;
            float leftAt = -1f;
            float rightAt = -1f;
            float started = Time.time;
            RoverCharacterTuning tuning = _rover.CharacterTuning;
            float until = started + tuning.LookAroundDelay + tuning.LookAroundSeconds;
            while (Time.time < until)
            {
                yield return null;
                float yaw = NeckYaw;
                if (yaw < left)
                {
                    left = yaw;
                    leftAt = Time.time - started;
                }

                if (yaw > right)
                {
                    right = yaw;
                    rightAt = Time.time - started;
                }
            }

            yield return Wait(2f);
            Debug.Log($"[rover-relay] look around: left {left:0.0} deg at {leftAt:0.00} s, right {right:0.0} deg at "
                + $"{rightAt:0.00} s, settled {NeckYaw:0.0} deg");
            Assert.Less(left, -25f, "Looks left.");
            Assert.Greater(right, 25f, "Then right.");
            Assert.Less(leftAt, rightAt, "Left first.");
            Assert.Less(Mathf.Abs(NeckYaw), 5f, "Then settles ahead.");
        }

        [UnityTest]
        public IEnumerator HopListOpen_KeepsTheWideShotClosed()
        {
            _rover.Context.Events.Publish(new RadioHopListChanged(true));
            yield return Wait(WideShotDelay + 2f);
            Assert.IsFalse(_rover.CameraRig.WideShot.IsOpen, "Choosing where to hop: the camera stays close.");

            _rover.Context.Events.Publish(new RadioHopListChanged(false));
            yield return Wait(WideShotDelay + 0.5f);
            Assert.IsTrue(_rover.CameraRig.WideShot.IsOpen, "List closed and left alone: the frame opens.");

            _rover.Context.Events.Publish(new RadioHopListChanged(true));
            yield return null;
            Assert.IsFalse(_rover.CameraRig.WideShot.IsOpen, "Opening the list hands the view back.");
        }
    }
}

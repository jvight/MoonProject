using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The camera nudge toward the next action (G5, docs/design/opening-v3-guidance-audit.md), in the real Main scene:
    /// when Story's next line has a place that is off screen, the camera drifts that way once (a slow, eased turn); any
    /// input ends it; a target nudged once is never nudged toward again; a place already in view, or a reminder,
    /// changes nothing.
    /// </summary>
    public sealed class NextActionNudgePlayModeTests : InputTestFixture
    {
        private const string RealLine = "next.v2.build_pad";
        private const float FrameTime = 1f / 60f;
        private const float TargetDistance = 120f;
        private const float MaxTurnDegreesPerSecond = 30f;
        private const float SettleSeconds = 1.2f;
        private const float NudgeSeconds = 6f;
        private const float EaseBackSeconds = 2.5f;

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;
        private readonly object _holdOwner = new object();

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator AnOffScreenTarget_TurnsTheHead_AndNudgesTheCameraOnce_SlowlyAndEased()
        {
            yield return Boot();
            Vector3 at = _scene.Rover.Position;
            Vector3 target = at + Bearing(Facing() + 100f) * TargetDistance;
            _scene.Context.Events.Publish(Step("relay.1", target));

            float lastYaw = _scene.View.transform.eulerAngles.y;
            float fastest = 0f;
            float peak = 0f;
            float until = Time.time + NudgeSeconds;
            while (Time.time < until)
            {
                yield return null;
                float yaw = _scene.View.transform.eulerAngles.y;
                fastest = Mathf.Max(fastest, Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) / Time.deltaTime);
                lastYaw = yaw;
                peak = Mathf.Max(peak, _scene.CameraRig.MomentWeight);
            }

            Assert.Greater(peak, 0.9f, "the camera was nudged toward it");
            Assert.LessOrEqual(fastest, MaxTurnDegreesPerSecond, "a soft drift, never a whip");
            yield return RealRoverScene.WaitSeconds(EaseBackSeconds);
            Assert.Less(_scene.CameraRig.MomentWeight, 0.05f, "and it eased back");

            _scene.Context.Events.Publish(Step("relay.1", target));
            float again = 0f;
            until = Time.time + NudgeSeconds * 0.5f;
            while (Time.time < until)
            {
                yield return null;
                again = Mathf.Max(again, _scene.CameraRig.MomentWeight);
            }

            Assert.AreEqual(0f, again, 1e-3f, "never twice for one target");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator AnyDriveInput_EndsTheNudgeAtOnce_WithASoftEaseBack()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Boot();
            Vector3 target = _scene.Rover.Position + Bearing(Facing() + 100f) * TargetDistance;
            _scene.Context.Events.Publish(Step("apron", target));
            yield return RealRoverScene.WaitSeconds(1.6f);
            float before = _scene.CameraRig.MomentWeight;
            Assert.Greater(before, 0.4f, "the nudge was under way");

            Press(_keyboard.wKey);
            float last = before;
            float biggestDrop = 0f;
            float until = Time.time + EaseBackSeconds;
            while (Time.time < until)
            {
                yield return null;
                float weight = _scene.CameraRig.MomentWeight;
                biggestDrop = Mathf.Max(biggestDrop, last - weight);
                last = weight;
            }

            Release(_keyboard.wKey);
            Assert.Less(last, 0.1f, "driving ended it");
            Assert.Less(biggestDrop, 0.1f, "and it eased back, it did not cut");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ATargetAlreadyInView_ChangesNothing()
        {
            yield return Boot();
            Vector3 target = _scene.Rover.Position + Bearing(Facing()) * TargetDistance;
            _scene.Context.Events.Publish(Step("lamp", target));

            float strongest = 0f;
            float until = Time.time + NudgeSeconds * 0.5f;
            while (Time.time < until)
            {
                yield return null;
                strongest = Mathf.Max(strongest, _scene.CameraRig.MomentWeight);
            }

            Assert.AreEqual(0f, strongest, 1e-3f, "seen already: no camera move");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator AReminder_NeverNudgesTheCamera()
        {
            yield return Boot();
            Vector3 target = _scene.Rover.Position + Bearing(Facing() + 100f) * TargetDistance;
            _scene.Context.Events.Publish(
                new NextStepChanged(RealLine, NextLineReason.Reminder, default, false, true, "relay.1", target));

            float strongest = 0f;
            float until = Time.time + NudgeSeconds * 0.5f;
            while (Time.time < until)
            {
                yield return null;
                strongest = Mathf.Max(strongest, _scene.CameraRig.MomentWeight);
            }

            Assert.AreEqual(0f, strongest, 1e-3f, "a stall reminder is a line, not a pointing finger");
        }

        private static NextStepChanged Step(string id, Vector3 position)
        {
            return new NextStepChanged(RealLine, NextLineReason.Next, default, false, true, id, position);
        }

        private IEnumerator Boot()
        {
            yield return _scene.Boot();
            _scene.Context.Get<IRoverRig>().SetHoldStill(_holdOwner, true);
            _scene.Context.Get<IRoverPlacement>().PlaceAt(_scene.Rover.Position, Quaternion.identity);
            _scene.CameraRig.Snap();
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
        }

        /// <summary>The bearing (deg) 07 and the camera behind it face.</summary>
        private float Facing()
        {
            Vector3 forward = _scene.View.transform.forward;
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        private static Vector3 Bearing(float degrees)
        {
            return new Vector3(Mathf.Sin(degrees * Mathf.Deg2Rad), 0f, Mathf.Cos(degrees * Mathf.Deg2Rad));
        }

    }
}

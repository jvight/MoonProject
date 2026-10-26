using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07's wheels turn with the ground (owner playtest f3d1ff265: "wheels spin much faster than 07 travels"): at any
    /// speed the wheel's rim speed (spin rate times its radius) equals the distance 07 actually covers (up to the soft
    /// ceiling on a fast wheel's drawn turn), so nothing reads fake. Driven straight at three speeds in the real rig; the numbers are logged.
    /// </summary>
    public sealed class RoverWheelSpinTests : InputTestFixture
    {
        private const float SettleSeconds = 3f;
        private const float MeasureSeconds = 2f;

        /// <summary>The rim speed may differ from the ground speed by at most this share (no slip allowance when
        /// driving straight on flat ground).</summary>
        private const float Tolerance = 0.03f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -300f), 0f);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private Transform Wheel(int index)
        {
            foreach (Transform node in _rover.Root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == RoverModelNodes.Wheel(index))
                {
                    return node;
                }
            }

            Assert.Fail($"No {RoverModelNodes.Wheel(index)} node.");
            return null;
        }

        private static float SignedSpinDegrees(Quaternion previous, Quaternion current)
        {
            Quaternion delta = Quaternion.Inverse(previous) * current;
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
            {
                angle -= 360f;
            }

            return axis.x >= 0f ? angle : -angle;
        }

        [UnityTest]
        public IEnumerator RimSpeed_MatchesGroundSpeed_AtThreeSpeeds()
        {
            Transform wheel = Wheel(0);
            float radius = WheelKinematics.Radius(wheel);
            foreach (float stick in new[] { 0.3f, 0.6f, 1f })
            {
                _rover.Drive.Drive = new Vector2(0f, stick);
                float settled = Time.time + SettleSeconds;
                while (Time.time < settled)
                {
                    yield return null;
                }

                Vector3 start = _rover.Controller.Position;
                Quaternion last = wheel.localRotation;
                float spin = 0f;
                float elapsed = 0f;
                while (elapsed < MeasureSeconds)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    spin += SignedSpinDegrees(last, wheel.localRotation);
                    last = wheel.localRotation;
                }

                Vector3 travelled = _rover.Controller.Position - start;
                travelled.y = 0f;
                float ground = travelled.magnitude / elapsed;
                float rim = spin * Mathf.Deg2Rad * radius / elapsed;
                Debug.Log($"[rover-wheelspin] stick {stick:0.0}: ground {ground:0.000} m/s, rim {rim:0.000} m/s, "
                    + $"ratio {rim / ground:0.000}");
                Assert.Greater(ground, 0.5f, "07 is moving.");
                RoverRigTuning rigTuning = _rover.Rig.Tuning;
                float expected = WheelKinematics.SoftCap(ground / radius * Mathf.Rad2Deg, rigTuning.WheelSpinKnee,
                    rigTuning.WheelSpinCeiling) * Mathf.Deg2Rad * radius;
                Assert.AreEqual(expected, rim, expected * Tolerance,
                    $"At stick {stick}: the rim keeps pace with the ground, under the soft ceiling.");
            }

            _rover.Drive.Drive = Vector2.zero;
        }
    }
}

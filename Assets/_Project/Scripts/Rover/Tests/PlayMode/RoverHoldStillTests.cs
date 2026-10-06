using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>IRoverRig.SetHoldStill (real wiring): a soft stop, no rebound, parked while any owner holds.</summary>
    public sealed class RoverHoldStillTests : InputTestFixture
    {
        private const float StoppedSpeed = 0.05f;

        private readonly object _beam = new object();
        private readonly object _cinematic = new object();
        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -250f), 0f);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private static IEnumerator Physics(float seconds)
        {
            float until = Time.fixedTime + seconds;
            while (Time.fixedTime < until)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator HoldStill_SettlesSoftly_StaysParked_AndReleasesWithEasing()
        {
            IRoverRig rig = _rover.Context.Get<IRoverRig>();
            RoverController controller = _rover.Controller;
            DriveSettings drive = _rover.Tuning.Drive;
            _rover.Drive.Drive = new Vector2(0f, 1f);
            yield return Physics(6f);
            Assert.Greater(controller.ForwardSpeed, 0.95f * drive.TopSpeed);

            rig.SetHoldStill(_beam, true);
            rig.SetHoldStill(_cinematic, true);
            float start = Time.fixedTime;
            float previous = controller.ForwardSpeed;
            float lowest = previous;
            float stoppedAfter = float.NaN;
            float maxPitch = 0f;
            while (Time.fixedTime - start < 4f)
            {
                yield return new WaitForFixedUpdate();
                float speed = controller.ForwardSpeed;
                Assert.LessOrEqual(speed, previous + 0.02f, "Holding still only ever slows 07 down.");
                previous = speed;
                lowest = Mathf.Min(lowest, speed);
                maxPitch = Mathf.Max(maxPitch, -Mathf.Asin(Mathf.Clamp(_rover.Chassis.forward.y, -1f, 1f)));
                if (float.IsNaN(stoppedAfter) && speed < StoppedSpeed)
                {
                    stoppedAfter = Time.fixedTime - start;
                }
            }

            Debug.Log($"[rover-hold] stopped after {stoppedAfter:0.00} s, lowest {lowest:0.000} m/s, "
                + $"nose dip {maxPitch * Mathf.Rad2Deg:0.0} deg");
            Assert.That(stoppedAfter, Is.InRange(0.6f * drive.HoldStopTime, drive.HoldStopTime + 0.4f),
                "An eased stop, neither abrupt nor lazy.");
            Assert.GreaterOrEqual(lowest, -0.02f, "No rebound backwards (no overshoot).");
            Assert.Greater(maxPitch * Mathf.Rad2Deg, 0.5f, "The jelly lean shows the gentle brake.");
            Assert.Less(controller.DriveInput.y, 0.01f, "Throttle is ignored while held, not queued.");

            rig.SetHoldStill(_beam, false);
            Vector3 parked = controller.Position;
            yield return Physics(1f);
            Assert.Less(Vector3.Distance(parked, controller.Position), 0.05f, "Still parked while any owner holds.");

            rig.SetHoldStill(_cinematic, false);
            yield return Physics(0.1f);
            Assert.Less(controller.Speed, 0.5f, "Release eases back in from rest.");
            yield return Physics(2f);
            Assert.Greater(controller.Speed, 3f, "Control is handed back.");
            _rover.Drive.Drive = Vector2.zero;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// IRoverPlacement (real wiring), as the radio-hop uses it while the view is dark: 07 is set down at rest on the
    /// terrain (height and slope) facing the asked yaw, nothing carries over from where it was, and from the very next
    /// frame the camera sits behind 07 at the new spot instead of easing across from the old one.
    /// </summary>
    public sealed class RoverPlacementTests : InputTestFixture
    {
        /// <summary>A shortened rest before the wide shot opens (s).</summary>
        private const float WideShotDelay = 1.5f;

        /// <summary>On the test slope, well up from its foot (local x).</summary>
        private const float SlopeSpotX = -75f;

        private const float SlopeSpotZ = 20f;

        /// <summary>A far spot on the plain, so any camera lerp from the old place would show.</summary>
        private const float FarSpotX = 150f;

        private const float FarSpotZ = 150f;

        private const float ArrivalYaw = 230f;

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

        private IRoverPlacement Placement => _rover.Context.Get<IRoverPlacement>();

        [UnityTest]
        public IEnumerator PlacedOnASlope_RestsOnTheTerrainFacingTheYaw()
        {
            yield return Wait(1f);
            Vector3 spot = TestWorld.Point(SlopeSpotX, SlopeSpotZ);
            Placement.PlaceAt(spot + Vector3.up * 40f, Quaternion.Euler(10f, ArrivalYaw, 5f));
            yield return null;

            RoverController controller = _rover.Controller;
            float ground = TestWorld.Height(spot.x, spot.z);
            Vector3 normal = _world.Terrain.SampleNormal(spot.x, spot.z);
            Vector3 sphere = controller.SpherePosition;
            Vector3 expected = new Vector3(spot.x, ground, spot.z) + normal * controller.SphereRadius;
            float tilt = Vector3.Angle(controller.Rotation * Vector3.up, normal);
            Debug.Log($"[rover-place] slope: sphere {Vector3.Distance(sphere, expected):0.000} m from its resting "
                + $"centre, heading {controller.Heading:0.0}, body {tilt:0.0} deg off the slope");
            Assert.Less(Vector3.Distance(sphere, expected), 0.05f, "At rest on the slope, lifted along its normal.");
            Assert.AreEqual(ArrivalYaw, controller.Heading, 0.5f, "Facing the asked yaw, pitch and roll ignored.");
            Assert.Less(tilt, 3f, "The body sits on the slope at once, not easing down from level.");
            Assert.Less(controller.Velocity.magnitude, 0.2f, "At rest.");
        }

        [UnityTest]
        public IEnumerator Placed_WhileDrivingAndCharging_CarriesNothingOver()
        {
            _rover.Context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            var landings = new List<RoverLanded>();
            var lifts = new List<RoverRecovering>();
            using (_rover.Context.Events.Subscribe<RoverLanded>(landings.Add))
            using (_rover.Context.Events.Subscribe<RoverRecovering>(lifts.Add))
            {
                _rover.Drive.Drive = new Vector2(0.6f, 1f);
                yield return Wait(3f);
                _rover.Drive.JumpHeld = true;
                yield return Wait(0.3f);
                RoverController controller = _rover.Controller;
                Assert.Greater(controller.Speed, 2f, "Driving fast.");
                Assert.Greater(controller.JumpCharge, 0f, "Charging a Hover-Jump.");

                _rover.Drive.Drive = Vector2.zero;
                _rover.Drive.JumpHeld = false;
                Placement.PlaceAt(TestWorld.Point(FarSpotX, FarSpotZ), Quaternion.Euler(0f, ArrivalYaw, 0f));
                Assert.AreEqual(0f, controller.Speed, 1e-3f, "Velocity zeroed.");
                Assert.AreEqual(Vector2.zero, controller.DriveInput, "Eased input zeroed.");
                Assert.AreEqual(0f, controller.JumpCharge, "The charge is gone, no leap follows.");
                Assert.AreEqual(0f, _rover.Context.Get<IRoverStillness>().StillSeconds, "Stillness starts over.");

                yield return Wait(2f);
                Assert.Less(controller.Speed, 0.2f, "Still at rest two seconds later.");
                Assert.IsTrue(controller.IsGrounded);
                Assert.IsFalse(controller.IsLeaping);
                Assert.IsEmpty(landings, "No landing thump from the placement.");
                Assert.IsEmpty(lifts, "No stuck lift either.");
                Assert.Greater(_rover.Context.Get<IRoverStillness>().StillSeconds, 1f, "Resting again.");
            }
        }

        [UnityTest]
        public IEnumerator Placed_TheCameraIsBehind07AtOnce_AndTheWideShotIsGone()
        {
            var changes = new List<bool>();
            using (_rover.Context.Events.Subscribe<RoverWideShotChanged>(changed => changes.Add(changed.Wide)))
            {
                yield return Wait(WideShotDelay + _rover.CameraTuning.WideShot.OpenSeconds);
                Assert.IsTrue(_rover.CameraRig.WideShot.IsOpen, "The wide shot was open.");

                Placement.PlaceAt(TestWorld.Point(FarSpotX, FarSpotZ), Quaternion.Euler(0f, ArrivalYaw, 0f));
                Assert.AreEqual(0f, _rover.CameraRig.WideShot.Weight, "Gone at once, no 0.65 s ease.");
                CollectionAssert.AreEqual(new[] { true, false }, changes, "Its hand-back was announced.");

                Transform camera = _rover.Camera.transform;
                float chase = _rover.CameraTuning.Distance;
                float farthest = 0f;
                float until = Time.time + 1.5f;
                while (Time.time < until)
                {
                    yield return null;
                    Vector3 body = _rover.Controller.Position + Vector3.up * _rover.CameraTuning.TargetHeight;
                    farthest = Mathf.Max(farthest, Vector3.Distance(camera.position, body));
                }

                Vector3 behind = Quaternion.Euler(0f, ArrivalYaw, 0f) * Vector3.back;
                Vector3 toCamera = Vector3.ProjectOnPlane(camera.position - _rover.Controller.Position, Vector3.up);
                float off = Vector3.Angle(behind, toCamera);
                Debug.Log($"[rover-place] camera at most {farthest:0.00} m from 07 after the placement (chase "
                    + $"{chase} m), {off:0.0} deg off straight behind");
                Assert.Less(farthest, 1.2f * chase, "No lerp across from the old spot, ever.");
                Assert.Less(off, 10f, "Straight behind 07, facing out.");
            }
        }
    }
}

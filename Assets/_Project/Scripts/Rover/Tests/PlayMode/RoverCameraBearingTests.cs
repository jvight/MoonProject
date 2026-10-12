using System.Collections;
using System.Globalization;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The camera rule of docs/design/first-hour-v2.md §9 with the real wiring (stick look, Cinemachine): with the view
    /// held 120 deg to the side, a 90 deg turn by 07 leaves the view's bearing alone. Logs the orbit's bearing and the
    /// rendered camera's heading every tenth of a second.
    /// </summary>
    public sealed class RoverCameraBearingTests : InputTestFixture
    {
        private const float LookStick = 0.7f;
        private const float HeldOffset = 120f;
        private const float Turn = 90f;
        private const float TurnTimeout = 6f;
        private const float LogEvery = 0.1f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -150f), 0f);
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

        private static float Heading(Vector3 forward)
        {
            return Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        }

        [UnityTest]
        public IEnumerator ViewHeld120ToTheSide_A90DegreeTurnBy07_KeepsTheBearing()
        {
            yield return Wait(2f);
            var gamepad = InputSystem.AddDevice<Gamepad>();
            CameraOrbit orbit = _rover.CameraRig.Orbit;
            Set(gamepad.rightStick, new Vector2(LookStick, 0f));
            float until = Time.time + 5f;
            while (Mathf.Abs(orbit.YawOffset) < HeldOffset - 4f && Time.time < until)
            {
                yield return null;
            }

            Set(gamepad.rightStick, Vector2.zero);
            yield return Wait(0.5f);
            Assert.AreEqual(HeldOffset, Mathf.Abs(orbit.YawOffset), 8f, "The view is held to the side.");

            Transform camera = _rover.Camera.transform;
            RoverController controller = _rover.Controller;
            float bearing = orbit.Bearing;
            float view = Heading(camera.forward);
            float startYaw = Heading(controller.Rotation * Vector3.forward);
            float turned = 0f;
            float worstBearing = 0f;
            float worstView = 0f;
            float strongestPull = 0f;
            float start = Time.time;
            float nextLog = start;
            var log = new StringBuilder("[camera-bearing] t, 07 turned, orbit bearing change, camera heading change\n");
            _rover.Drive.Drive = new Vector2(1f, 1f);
            until = Time.time + TurnTimeout;
            while (Mathf.Abs(turned) < Turn && Time.time < until)
            {
                yield return null;
                turned = Mathf.DeltaAngle(startYaw, Heading(controller.Rotation * Vector3.forward));
                float bearingChange = Mathf.DeltaAngle(bearing, orbit.Bearing);
                float viewChange = Mathf.DeltaAngle(view, Heading(camera.forward));
                worstBearing = Mathf.Max(worstBearing, Mathf.Abs(bearingChange));
                worstView = Mathf.Max(worstView, Mathf.Abs(viewChange));
                strongestPull = Mathf.Max(strongestPull, orbit.RecenterWeight);
                if (Time.time >= nextLog)
                {
                    log.AppendFormat(CultureInfo.InvariantCulture, "{0:0.00}, {1:0.0}, {2:0.00}, {3:0.00}\n",
                        Time.time - start, turned, bearingChange, viewChange);
                    nextLog = Time.time + LogEvery;
                }
            }

            _rover.Drive.Drive = Vector2.zero;
            log.AppendFormat(CultureInfo.InvariantCulture,
                "worst orbit bearing change {0:0.00} deg, worst camera heading change {1:0.00} deg, over {2:0.0} s",
                worstBearing, worstView, Time.time - start);
            Debug.Log(log.ToString());
            Assert.GreaterOrEqual(Mathf.Abs(turned), Turn, "07 made its turn.");
            Assert.AreEqual(0f, strongestPull, "No recentering while the view is fresh from the player's hand.");
            Assert.Less(worstBearing, 2f, "07 turned under the view: its bearing held.");
        }
    }
}

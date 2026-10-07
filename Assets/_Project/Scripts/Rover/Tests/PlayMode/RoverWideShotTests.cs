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
    /// The lonely wide shot and IRoverStillness (real wiring): 07 resting drifts the camera out slowly to a wide frame
    /// with 07 small in the lower third; drive or look input hands the view back quickly but smoothly; the tether and
    /// camera moments keep it closed; a Hover-Jump charge or a hold is never stillness; 07 sighs as the frame opens.
    /// </summary>
    public sealed class RoverWideShotTests : InputTestFixture
    {
        /// <summary>A shortened rest, so the session need not wait the shipped delay.</summary>
        private const float Delay = 2f;

        /// <summary>Opening is a slow drift: the camera never travels faster than this (m/s).</summary>
        private const float MaxOpeningSpeed = 8f;

        /// <summary>Opening never swings the view faster than this (deg/s).</summary>
        private const float MaxOpeningTurn = 15f;

        /// <summary>The hand-back is quick (~0.8 s over ~20 m) but eased: never faster than this (m/s).</summary>
        private const float MaxHandBackSpeed = 70f;

        /// <summary>Height (m) of the middle of 07's body above its ground contact.</summary>
        private const float BodyHeight = 0.8f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private readonly List<bool> _changes = new List<bool>();

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -150f), 0f, wideShotDelay: Delay);
            _changes.Clear();
            _rover.Context.Events.Subscribe<RoverWideShotChanged>(changed => _changes.Add(changed.Wide));
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

        private WideShotSettings Settings => _rover.CameraTuning.WideShot;

        private Vector3 Body => _rover.Controller.Position + Vector3.up * BodyHeight;

        /// <summary>Waits until the frame has opened and settled.</summary>
        private IEnumerator OpenFully()
        {
            yield return Wait(Delay + Settings.OpenSeconds + 2f);
            Assert.IsTrue(_rover.CameraRig.WideShot.IsOpen, "The wide shot opened.");
            Assert.Greater(_rover.CameraRig.WideShot.Weight, 0.95f, "And settled.");
        }

        [UnityTest]
        public IEnumerator Resting_DriftsOutSlowlyToASmallLonely07_AndKeepsBreathing()
        {
            yield return Wait(0.5f);
            Transform camera = _rover.Camera.transform;
            float chaseDistance = Vector3.Distance(camera.position, Body);
            var trace = new CameraTrace(camera);
            float until = Time.time + Delay + Settings.OpenSeconds + 2f;
            while (Time.time < until)
            {
                yield return null;
                trace.Step(_rover.CameraRig.WideShot.Weight);
            }

            float wideDistance = Vector3.Distance(camera.position, Body);
            Vector3 onScreen = _rover.Camera.WorldToViewportPoint(Body);
            Vector3 level = camera.position + Vector3.ProjectOnPlane(camera.forward, Vector3.up).normalized * 1000f;
            float horizon = _rover.Camera.WorldToViewportPoint(level).y;
            Debug.Log($"[rover-wide] chase {chaseDistance:0.0} m -> wide {wideDistance:0.0} m; 07 at screen y "
                + $"{onScreen.y:0.00}, horizon {horizon:0.00}; {trace}");

            CollectionAssert.AreEqual(new[] { true }, _changes, "One RoverWideShotChanged(true) as it opened.");
            Assert.Greater(wideDistance, 2.5f * chaseDistance, "07 reads small: the camera is far out.");
            Assert.That(onScreen.x, Is.InRange(0.4f, 0.6f), "07 stays centred left to right.");
            Assert.That(onScreen.y, Is.InRange(0.2f, 0.42f), "07 sits in the lower third.");
            Assert.Less(horizon, 0.5f, "The horizon sits low: a big sky.");
            Assert.Less(trace.Fastest, MaxOpeningSpeed, "A slow drift, never a jolt.");
            Assert.Less(trace.FastestTurn, MaxOpeningTurn, "The view turns gently.");

            Vector3 settled = camera.position;
            yield return Wait(4f);
            Assert.Greater(Vector3.Distance(settled, camera.position), 0.05f, "The frame breathes: never dead still.");
        }

        [UnityTest]
        public IEnumerator DriveInput_HandsBackQuicklyButEased()
        {
            yield return OpenFully();
            Transform camera = _rover.Camera.transform;
            var trace = new CameraTrace(camera);
            _rover.Drive.Drive = new Vector2(0f, 0.6f);
            float started = Time.time;
            float home = -1f;
            while (Time.time < started + 2f)
            {
                yield return null;
                trace.Step(_rover.CameraRig.WideShot.Weight);
                if (home < 0f && _rover.CameraRig.WideShot.Weight < 0.1f)
                {
                    home = Time.time - started;
                }
            }

            float distance = Vector3.Distance(camera.position, Body);
            Debug.Log($"[rover-wide] hand-back: weight under 0.1 after {home:0.00} s, {distance:0.0} m from 07 after "
                + $"2 s; {trace}");
            CollectionAssert.AreEqual(new[] { true, false }, _changes, "Opened, then handed back.");
            Assert.That(home, Is.InRange(0.3f, 1.1f), "Quick, but an ease, not a cut.");
            Assert.Less(trace.Fastest, MaxHandBackSpeed, "No frame jumps.");
            Assert.Less(distance, 1.6f * _rover.CameraTuning.Distance, "Back to the chase camera.");
        }

        [UnityTest]
        public IEnumerator LookInput_HandsBack()
        {
            yield return OpenFully();
            var gamepad = InputSystem.AddDevice<Gamepad>();
            Set(gamepad.rightStick, new Vector2(0.7f, 0f));
            yield return Wait(1f);
            Set(gamepad.rightStick, Vector2.zero);
            Assert.IsFalse(_rover.CameraRig.WideShot.IsOpen);
            Assert.Less(_rover.CameraRig.WideShot.Weight, 0.15f, "Handed back within about a second.");
        }

        [UnityTest]
        public IEnumerator TheTetherAndCameraMoments_ComeFirst()
        {
            EventBus events = _rover.Context.Events;
            events.Publish(new TetherAttached(_rover.Controller.Position + Vector3.forward * 4f, 1f));
            yield return Wait(Delay + 2f);
            Assert.IsFalse(_rover.CameraRig.WideShot.IsOpen, "Towing: the camera stays with the work.");

            events.Publish(new TetherReleased(_rover.Controller.Position, false));
            yield return Wait(Delay + 1f);
            Assert.IsTrue(_rover.CameraRig.WideShot.IsOpen, "Let go and left alone: the frame opens.");

            Vector3 site = _rover.Controller.Position + _rover.Controller.Rotation * new Vector3(0f, 0f, 3f);
            events.Publish(new ExcavationStarted(site));
            yield return null;
            Assert.IsFalse(_rover.CameraRig.WideShot.IsOpen, "A camera moment takes over at once.");
            events.Publish(new ExcavationStopped(site, false));
        }

        [UnityTest]
        public IEnumerator AJumpChargeOrAHold_IsNeverStillness()
        {
            IRoverStillness stillness = _rover.Context.Get<IRoverStillness>();
            _rover.Context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            yield return Wait(1f);
            Assert.Greater(stillness.StillSeconds, 0.5f, "Parked and left alone: resting.");

            RoverController controller = _rover.Controller;
            _rover.Drive.JumpHeld = true;
            yield return Wait(0.2f);
            Assert.Greater(controller.JumpCharge, 0f, "07 is charging a Hover-Jump.");
            yield return AssertNotStill(stillness, 0.4f, "Charging a Hover-Jump is not resting.");

            var owner = new object();
            IRoverRig rig = _rover.Context.Get<IRoverRig>();
            rig.SetHoldStill(owner, true);
            yield return Wait(0.2f);
            Assert.AreEqual(0f, controller.JumpCharge, "A hold cancels the charge quietly.");
            _rover.Drive.JumpHeld = false;
            yield return AssertNotStill(stillness, 0.5f, "Held parked by an interaction is not resting.");

            rig.SetHoldStill(owner, false);
            yield return Wait(1f);
            Assert.IsTrue(controller.IsGrounded, "No leap came of the cancelled charge.");
            Assert.Greater(stillness.StillSeconds, 0.5f, "Released and left alone: resting again.");
        }

        private static IEnumerator AssertNotStill(IRoverStillness stillness, float seconds, string message)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
                Assert.AreEqual(0f, stillness.StillSeconds, message);
            }
        }

        [UnityTest]
        public IEnumerator TheRoverSighs_AsTheFrameOpens()
        {
            RoverMood mood = _rover.BodyLanguage.Mood;
            float deepestBefore = 0f;
            while (_changes.Count == 0)
            {
                yield return null;
                deepestBefore = Mathf.Max(deepestBefore, mood.Sighing);
            }

            float deepest = 0f;
            float until = Time.time + _rover.CharacterTuning.IdleDelay + 2f;
            while (Time.time < until)
            {
                yield return null;
                deepest = Mathf.Max(deepest, mood.Sighing);
            }

            Assert.Less(deepestBefore, 0.01f, "No sigh before the frame opens.");
            Assert.Greater(deepest, 0.5f, "07 sighs with the opening frame.");
        }
    }
}

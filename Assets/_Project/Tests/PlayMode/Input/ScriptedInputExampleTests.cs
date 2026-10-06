using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core.Input;
using MoonProject.Testing;

namespace MoonProject.Tests.PlayMode.Input
{
    /// <summary>
    /// Template for feel tests that drive the game with scripted devices. InputTestFixture swaps in an isolated input
    /// system per test (no real devices, deterministic state): add virtual devices, build the bootstrap, then
    /// Press/Release/Set controls and read the game's own InputReader after a frame. For one-frame signals
    /// (WasPressedThisFrame), queue the event (queueEventOnly: true) so the next frame's input update delivers it
    /// exactly like a real press.
    /// </summary>
    public sealed class ScriptedInputExampleTests : InputTestFixture
    {
        private InputActionAsset _controls;
        private GameBootstrap _bootstrap;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            if (_bootstrap != null)
            {
                Object.Destroy(_bootstrap.gameObject);
            }

            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Keyboard_WAndD_SteerAndThrottle()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputReader input = Boot();
            yield return null;

            Press(keyboard.wKey);
            yield return null;
            Assert.AreEqual(1f, input.Drive.y, 1e-3f, "W is full throttle");

            Press(keyboard.dKey);
            yield return null;
            Assert.Greater(input.Drive.x, 0.5f, "D steers right");
            Assert.LessOrEqual(input.Drive.magnitude, 1f + 1e-3f, "diagonals are clamped to unit length");

            Release(keyboard.wKey);
            Release(keyboard.dKey);
            yield return null;
            Assert.AreEqual(Vector2.zero, input.Drive);
        }

        [UnityTest]
        public IEnumerator Gamepad_StickAndButtons_ReachTheReader()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            InputReader input = Boot();
            yield return null;

            Set(gamepad.leftStick, new Vector2(0f, 1f));
            yield return null;
            Assert.AreEqual(1f, input.Drive.y, 1e-3f, "full stick is full throttle after the dead zone");

            Press(gamepad.buttonSouth, queueEventOnly: true);
            yield return null;
            Assert.IsTrue(input.PingPressed, "south button pings in the frame it is pressed");
            yield return null;
            Assert.IsFalse(input.PingPressed, "a press is reported for one frame only");
            Release(gamepad.buttonSouth);

            Press(gamepad.leftTrigger);
            yield return null;
            Assert.IsTrue(input.TetherHeld);
            Release(gamepad.leftTrigger);
            yield return null;
            Assert.IsFalse(input.TetherHeld);
        }

        private InputReader Boot()
        {
            _bootstrap = BootstrapHarness.Create(_controls);
            return _bootstrap.Context.Input;
        }
    }
}

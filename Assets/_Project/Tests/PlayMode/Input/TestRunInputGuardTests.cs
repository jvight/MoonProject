using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Testing;

namespace MoonProject.Tests.PlayMode.Input
{
    /// <summary>
    /// A scripted event stamped inside the editor's "exit edit mode .. enter play mode" window still reaches the game
    /// during a test run (MoonProject.Testing.TestRunInputGuard); without the guard the Input System drops it silently.
    /// </summary>
    public sealed class TestRunInputGuardTests : InputTestFixture
    {
        private const double WindowMargin = 0.001;

        private static readonly Vector2 Drive = new Vector2(0f, 1f);

        [UnityTest]
        public IEnumerator AnEventStampedInTheEditModeWindow_StillArrives()
        {
            Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
            yield return null;
            double entered = TestRunInputGuard.EnterPlayModeTime;
            Assume.That(entered, Is.GreaterThan(0d), "the Input System saw play mode entered");

            // The fixture's own clock reaches the old window, as it does after enough frames of a long session.
            currentTime = entered - WindowMargin;
            Set(gamepad.leftStick, Drive);
            yield return null;
            Assert.AreEqual(Drive.y, gamepad.leftStick.ReadValue().y, 0.05f, "the event reaches the device");
            Assert.AreEqual(0d, TestRunInputGuard.ExitEditModeTime, "the guard cleared the edit-mode window");
        }
    }
}

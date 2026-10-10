using System.Collections;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace MoonProject.Tests.PlayMode.Input
{
    /// <summary>
    /// The test-run input hook (MoonProject.Testing.TestRunInputFocus) reaches every InputTestFixture's own settings
    /// within a frame, so a focus change in the unfocused batch editor cannot reset or disable the virtual devices.
    /// </summary>
    public sealed class TestRunInputFocusTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator AFixturesFreshSettings_IgnoreFocus_AndInputStillArrives()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
            Assert.AreEqual(InputSettings.BackgroundBehavior.IgnoreFocus, InputSystem.settings.backgroundBehavior,
                "input ignores the window's focus during a test run");
            Assert.AreEqual(InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView,
                InputSystem.settings.editorInputBehaviorInPlayMode, "all device input goes to the game view");

            Press(keyboard.escapeKey);
            yield return null;
            Assert.IsTrue(keyboard.enabled, "the virtual keyboard stays enabled");
            Assert.IsTrue(keyboard.escapeKey.isPressed, "a scripted Esc arrives");
            Release(keyboard.escapeKey);
        }
    }
}

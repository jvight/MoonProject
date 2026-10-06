using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core.Input;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The prompt glyphs read their text from the real Controls bindings, so a rebinding can never leave a prompt
    /// naming the wrong key.
    /// </summary>
    public sealed class InputGlyphLabelTests
    {
        private const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";

        private InputActionAsset _asset;
        private InputReader _reader;

        [SetUp]
        public void SetUp()
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            Assert.IsNotNull(asset, $"Controls asset missing at {ControlsPath}");
            _asset = Object.Instantiate(asset);
            _reader = new InputReader(_asset);
        }

        [TearDown]
        public void TearDown()
        {
            _reader.Dispose();
            Object.DestroyImmediate(_asset);
        }

        [TestCase(RoverAction.Ping, InputDeviceKind.KeyboardMouse, "Space")]
        [TestCase(RoverAction.Excavate, InputDeviceKind.KeyboardMouse, "E")]
        [TestCase(RoverAction.Tether, InputDeviceKind.KeyboardMouse, "RMB")]
        [TestCase(RoverAction.Ping, InputDeviceKind.Gamepad, "A")]
        [TestCase(RoverAction.Excavate, InputDeviceKind.Gamepad, "X")]
        [TestCase(RoverAction.Tether, InputDeviceKind.Gamepad, "LT")]
        public void SingleBindings_GiveShortKeyNames(RoverAction action, InputDeviceKind device, string expected)
        {
            Assert.AreEqual(expected, _reader.GetBindingLabel(action, device));
        }

        [TestCase(InputDeviceKind.KeyboardMouse, "Scroll")]
        [TestCase(InputDeviceKind.Gamepad, "D-Pad")]
        public void Winch_IsNamedByItsPhysicalControl(InputDeviceKind device, string expected)
        {
            Assert.AreEqual(expected, _reader.GetBindingLabel(RoverAction.Winch, device));
        }

        [TestCase(InputDeviceKind.KeyboardMouse, "Escape")]
        [TestCase(InputDeviceKind.Gamepad, "B")]
        public void Cancel_IsNamedForTheCardGlyph(InputDeviceKind device, string expected)
        {
            Assert.AreEqual(expected, _reader.Menu.GetCancelLabel(device));
        }

        [Test]
        public void MenuMap_IsSeparateFromTheRoverControls()
        {
            _reader.Enable();
            _reader.Menu.Enable();
            _reader.Disable();

            Assert.IsFalse(_reader.Enabled, "pausing turns the rover controls off");
            Assert.IsTrue(_reader.Menu.Enabled, "while the menu map stays live, so Pause can be undone");
            Assert.AreEqual(InputDeviceKind.KeyboardMouse, _reader.ActiveDevice,
                "keyboard glyphs until told otherwise");
        }
    }
}

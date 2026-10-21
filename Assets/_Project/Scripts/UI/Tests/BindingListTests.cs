using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core.Input;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Every way to do one thing is named from the real Controls bindings, so a prompt can read "LMB / E Place" and
    /// "Scroll / Z / C Rotate" and never lie after a rebinding: the mouse's left button is Excavate's second binding and
    /// Z and C are Winch's keys (they rotate the placing ghost).
    /// </summary>
    public sealed class BindingListTests
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

        [Test]
        public void Excavate_IsTheKeyAndTheLeftButton_OnKeyboardAndMouse_AndOneButtonOnThePad()
        {
            var labels = new List<string>();
            _reader.GetBindingLabels(RoverAction.Excavate, InputDeviceKind.KeyboardMouse, labels);
            CollectionAssert.AreEqual(new[] { "E", "LMB" }, labels);

            var ids = new List<string>();
            _reader.GetBindingControls(RoverAction.Excavate, InputDeviceKind.KeyboardMouse, ids);
            CollectionAssert.AreEqual(new[] { "keyboard.e", "mouse.leftbutton" }, ids);

            labels.Clear();
            _reader.GetBindingLabels(RoverAction.Excavate, InputDeviceKind.Gamepad, labels);
            CollectionAssert.AreEqual(new[] { "X" }, labels);
        }

        [Test]
        public void Winch_IsTheWheelAndTheZAndCKeys_OnKeyboardAndMouse_AndTheDPadOnThePad()
        {
            var ids = new List<string>();
            _reader.GetBindingControls(RoverAction.Winch, InputDeviceKind.KeyboardMouse, ids);
            CollectionAssert.AreEqual(new[] { "mouse.scroll", "keyboard.z", "keyboard.c" }, ids);

            ids.Clear();
            _reader.GetBindingControls(RoverAction.Winch, InputDeviceKind.Gamepad, ids);
            CollectionAssert.AreEqual(new[] { "gamepad.dpad" }, ids, "the d-pad's two directions are one control");
        }

        [Test]
        public void TheFirstBinding_StaysTheOneTheSingleLabelNames()
        {
            Assert.AreEqual("E", _reader.GetBindingLabel(RoverAction.Excavate, InputDeviceKind.KeyboardMouse));
            Assert.AreEqual("Scroll", _reader.GetBindingLabel(RoverAction.Winch, InputDeviceKind.KeyboardMouse));
        }
    }
}

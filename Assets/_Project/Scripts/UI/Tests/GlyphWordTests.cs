using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.UI.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Keycap words: short ("Esc", "LMB", face names), localized where words are needed, and every glyph the game can
    /// show fits a keycap in every language.
    /// </summary>
    public sealed class GlyphWordTests
    {
        private const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";
        private const int MaxKeycapCharacters = 10;

        private InputActionAsset _asset;
        private InputReader _reader;
        private LocalizationService _localization;
        private GlyphLabels _glyphs;

        [SetUp]
        public void SetUp()
        {
            _asset = Object.Instantiate(AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath));
            _reader = new InputReader(_asset);
            TextAsset[] assets = StringTableAssets.Load();
            var tables = new StringTable[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                tables[i] = StringTable.Parse(assets[i].text, assets[i].name);
            }

            _localization = new LocalizationService(new EventBus(), tables);
            _glyphs = new GlyphLabels(_reader, _localization);
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
        [TestCase(RoverAction.Winch, InputDeviceKind.KeyboardMouse, "Scroll")]
        [TestCase(RoverAction.Ping, InputDeviceKind.Gamepad, "A")]
        [TestCase(RoverAction.Excavate, InputDeviceKind.Gamepad, "X")]
        [TestCase(RoverAction.Tether, InputDeviceKind.Gamepad, "LT")]
        [TestCase(RoverAction.Winch, InputDeviceKind.Gamepad, "D-Pad")]
        [TestCase(RoverAction.Jump, InputDeviceKind.KeyboardMouse, "Shift")]
        [TestCase(RoverAction.Jump, InputDeviceKind.Gamepad, "RB")]
        public void English_Keycaps(RoverAction action, InputDeviceKind device, string expected)
        {
            Assert.AreEqual(expected, _glyphs.For(action, device));
        }

        [Test]
        public void Cancel_ReadsEscAndB()
        {
            Assert.AreEqual("Esc", _glyphs.Cancel(InputDeviceKind.KeyboardMouse));
            Assert.AreEqual("B", _glyphs.Cancel(InputDeviceKind.Gamepad));
        }

        [Test]
        public void Words_FollowTheLanguage_AfterAClear()
        {
            Assert.AreEqual("RMB", _glyphs.For(RoverAction.Tether, InputDeviceKind.KeyboardMouse));
            _localization.SetLanguage("vi");
            _glyphs.Clear();
            Assert.AreEqual("Chuột phải", _glyphs.For(RoverAction.Tether, InputDeviceKind.KeyboardMouse));
            Assert.AreEqual("Phím cách", _glyphs.For(RoverAction.Ping, InputDeviceKind.KeyboardMouse));
            Assert.AreEqual("E", _glyphs.For(RoverAction.Excavate, InputDeviceKind.KeyboardMouse), "letters stay");
            Assert.AreEqual("A", _glyphs.For(RoverAction.Ping, InputDeviceKind.Gamepad), "face names stay");
        }

        [Test]
        public void EveryGlyph_IsShort_InEveryLanguage()
        {
            foreach (string language in _localization.Languages)
            {
                _localization.SetLanguage(language);
                _glyphs.Clear();
                foreach (InputDeviceKind device in Enum.GetValues(typeof(InputDeviceKind)))
                {
                    foreach (RoverAction action in Enum.GetValues(typeof(RoverAction)))
                    {
                        AssertKeycap(_glyphs.For(action, device), $"{language} {action} {device}");
                    }

                    AssertKeycap(_glyphs.Cancel(device), $"{language} cancel {device}");
                }
            }
        }

        private static void AssertKeycap(string label, string what)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(label), what);
            Assert.LessOrEqual(label.Length, MaxKeycapCharacters, $"{what}: '{label}' does not fit a keycap");
        }
    }
}

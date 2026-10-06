using System;
using UnityEngine.UIElements;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// A button glyph: a key cap on keyboard and mouse, a round face button on a gamepad. Writes to the element only
    /// when the text or the device changes.
    /// </summary>
    internal sealed class GlyphView
    {
        public const string PadClass = "glyph--pad";

        private readonly VisualElement _glyph;
        private readonly Label _label;
        private string _text;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;

        public GlyphView(VisualElement glyph, Label label)
        {
            _glyph = glyph ?? throw new ArgumentNullException(nameof(glyph));
            _label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public void Set(string text, InputDeviceKind device)
        {
            if (!ReferenceEquals(text, _text))
            {
                _label.text = text;
                _text = text;
            }

            if (device != _device)
            {
                _glyph.EnableInClassList(PadClass, device == InputDeviceKind.Gamepad);
                _device = device;
            }
        }
    }
}

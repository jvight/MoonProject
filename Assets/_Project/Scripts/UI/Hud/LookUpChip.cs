using System;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The "Look up" hint in the prompt chip's style, low and centred on screen: the camera control's glyph (the right
    /// stick, or the mouse) and the word. <see cref="LookUpHint"/> decides when it shows.
    /// </summary>
    internal sealed class LookUpChip
    {
        private const string PadGlyphKey = "glyph.gamepad.rightstick";
        private const string MouseGlyphKey = "glyph.mouse.move";

        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly GlyphView _glyph;

        public LookUpChip(UiLayout layout, StargazeSettings settings, ILocalization localization)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _reveal = new Reveal(layout.LookHint, settings.Hint);
            _reveal.Snap(false);
            _glyph = new GlyphView(layout.LookHintGlyph, layout.LookHintGlyphLabel);
            new ShadowPainter(layout.LookHintShadow);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="shown">True while the hint is wanted (and the game is not paused).</param>
        /// <param name="device">The active device: its camera control is the glyph.</param>
        public void Tick(float deltaTime, bool shown, InputDeviceKind device)
        {
            if (shown)
            {
                _glyph.Set(_localization.Get(device == InputDeviceKind.Gamepad ? PadGlyphKey : MouseGlyphKey), device);
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _layout.LookHintWord.text = _localization.Get(UiKeys.LookUp);
        }
    }
}

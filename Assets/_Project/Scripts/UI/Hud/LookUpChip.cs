using System;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The low look-hint chip in the prompt chip's style, centred on screen: a glyph and one word (<see cref="HintWord"/>).
    /// "Look up" shows the camera control (the right stick, or the mouse), "Drive" the drive control (the left stick,
    /// or WASD), "Hold" the cargo control (Tab, or View; "Fit" shows the same control). <see cref="LookUpHint"/>,
    /// <see cref="FirstPrompt"/>, <see cref="HoldHint"/> and <see cref="FitHint"/> decide when it shows; a new word waits until the
    /// chip has faded out.
    /// </summary>
    internal sealed class LookUpChip
    {
        private const string PadGlyphKey = "glyph.gamepad.rightstick";
        private const string MouseGlyphKey = "glyph.mouse.move";
        private const string DrivePadGlyphKey = "glyph.gamepad.leftstick";
        private const string DriveKeysGlyphKey = "glyph.keyboard.wasd";
        private const string HoldPadGlyphKey = "glyph.gamepad.select";
        private const string HoldKeysGlyphKey = "glyph.keyboard.tab";

        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly GlyphView _glyph;
        private HintWord _word = HintWord.LookUp;

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
        /// <param name="word">The word wanted (None to hide, and while the game is paused).</param>
        /// <param name="device">The active device: its control for the word is the glyph.</param>
        public void Tick(float deltaTime, HintWord word, InputDeviceKind device)
        {
            if (word != HintWord.None && word != _word && _reveal.IsHidden)
            {
                _word = word;
                Relocalize();
            }

            bool shown = word != HintWord.None && word == _word;
            if (shown)
            {
                _glyph.Set(_localization.Get(GlyphKey(device)), device);
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _layout.LookHintWord.text = _localization.Get(WordKey());
        }

        private string WordKey()
        {
            switch (_word)
            {
                case HintWord.Drive:
                    return UiKeys.Drive;
                case HintWord.Hold:
                    return UiKeys.HintHold;
                case HintWord.Fit:
                    return UiKeys.HintFit;
                default:
                    return UiKeys.LookUp;
            }
        }

        private string GlyphKey(InputDeviceKind device)
        {
            bool pad = device == InputDeviceKind.Gamepad;
            if (_word == HintWord.Hold || _word == HintWord.Fit)
            {
                return pad ? HoldPadGlyphKey : HoldKeysGlyphKey;
            }

            if (_word == HintWord.Drive)
            {
                return pad ? DrivePadGlyphKey : DriveKeysGlyphKey;
            }

            return pad ? PadGlyphKey : MouseGlyphKey;
        }
    }
}

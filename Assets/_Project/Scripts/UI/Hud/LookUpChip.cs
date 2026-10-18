using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The low look-hint chip in the prompt chip's style, centred on screen: a glyph and one word (<see cref="HintWord"/>).
    /// "Look up" shows the camera control (the right stick, or the mouse), "Drive" the drive control (the left stick,
    /// or WASD), "Hold" the cargo control (Tab, or View). <see cref="FirstPrompt"/> and
    /// <see cref="HoldHint"/> decide when it shows; a new word waits until the
    /// chip has faded out. The verb words (Scan, Cut, Build, Fit) belong to the objective line
    /// (<see cref="ObjectiveChip"/>): they show with it and are not a prompt of their own.
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
        private readonly GlyphLabels _labels;
        private readonly Reveal _reveal;
        private readonly GlyphView _glyph;
        private HintWord _word = HintWord.LookUp;
        private float _nudgeOffset;
        private float _nudgeOpacity = 1f;

        public LookUpChip(UiLayout layout, StargazeSettings settings, ILocalization localization,
            GlyphLabels labels)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            _reveal = new Reveal(layout.LookHint, settings.Hint);
            _reveal.Snap(false);
            _glyph = new GlyphView(layout.LookHintGlyph, layout.LookHintGlyphLabel);
            new ShadowPainter(layout.LookHintShadow);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while the chip on screen is a prompt (look, drive, hold), not the objective's verb chip.</summary>
        public bool ShowsPrompt => !_reveal.IsHidden && !IsVerb(_word);

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
                _glyph.Set(GlyphText(device), device);
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        public void Relocalize()
        {
            _layout.LookHintWord.text = _localization.Get(WordKey());
        }

        /// <summary>The blocked cue's dim and shake on the chip (0 and 1 leave it as it is).</summary>
        public void Nudge(float offsetPixels, float opacity)
        {
            if (offsetPixels == _nudgeOffset && opacity == _nudgeOpacity)
            {
                return;
            }

            _nudgeOffset = offsetPixels;
            _nudgeOpacity = opacity;
            _layout.LookHint.style.translate = new Translate(offsetPixels, 0f);
            _layout.LookHintWord.style.opacity = opacity;
            _layout.LookHintGlyph.style.opacity = opacity;
        }

        private string WordKey()
        {
            switch (_word)
            {
                case HintWord.Drive:
                    return UiKeys.Drive;
                case HintWord.Hold:
                    return UiKeys.HintHold;
                case HintWord.Scan:
                    return UiKeys.VerbScan;
                case HintWord.Cut:
                    return UiKeys.VerbCut;
                case HintWord.Build:
                    return UiKeys.VerbBuild;
                case HintWord.Fit:
                    return UiKeys.VerbFit;
                default:
                    return UiKeys.LookUp;
            }
        }

        private static bool IsVerb(HintWord word)
        {
            return word >= HintWord.Scan;
        }

        /// <summary>The key to press: the real binding for a verb, the control's cap for the look and hold words.</summary>
        private string GlyphText(InputDeviceKind device)
        {
            switch (_word)
            {
                case HintWord.Scan:
                case HintWord.Cut:
                    return _labels.For(RoverAction.Excavate, device);
                case HintWord.Build:
                    return _labels.For(RoverAction.Build, device);
                default:
                    return _localization.Get(GlyphKey(device));
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

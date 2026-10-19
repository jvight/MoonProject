using System;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The first controls card on screen: one small glass row at the bottom left, "WASD Drive · Mouse Look · Tab 07 ·
    /// L Lights", with the gamepad's caps (left stick, right stick, View, the lights button) while a pad was the last
    /// input. <see cref="ControlsCard"/> decides when it shows; this eases it in and out and keeps its caps on the
    /// active device. The lights cap is the real binding (<see cref="GlyphLabels"/>).
    /// </summary>
    internal sealed class ControlsCardView
    {
        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly GlyphLabels _labels;
        private readonly Reveal _reveal;
        private readonly GlyphView _drive;
        private readonly GlyphView _look;
        private readonly GlyphView _face;
        private readonly GlyphView _lights;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;

        public ControlsCardView(UiLayout layout, ControlsCardSettings settings, ILocalization localization,
            GlyphLabels labels)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            _reveal = new Reveal(layout.ControlsCard, settings.Reveal);
            _reveal.Snap(false);
            _drive = new GlyphView(layout.ControlsDriveGlyph, layout.ControlsDriveGlyphLabel);
            _look = new GlyphView(layout.ControlsLookGlyph, layout.ControlsLookGlyphLabel);
            _face = new GlyphView(layout.ControlsFaceGlyph, layout.ControlsFaceGlyphLabel);
            _lights = new GlyphView(layout.ControlsLightsGlyph, layout.ControlsLightsGlyphLabel);
            new ShadowPainter(layout.ControlsCardShadow);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="shown">The card holds the screen now.</param>
        /// <param name="device">The last input's device, for the caps.</param>
        public void Tick(float deltaTime, bool shown, InputDeviceKind device)
        {
            if (device != _device)
            {
                _device = device;
                WriteGlyphs();
            }

            _reveal.Set(shown);
            _reveal.Tick(deltaTime);
        }

        /// <summary>Rewrites the words and caps in the current language.</summary>
        public void Relocalize()
        {
            _layout.ControlsDriveWord.text = _localization.Get(UiKeys.Drive);
            _layout.ControlsLookWord.text = _localization.Get(UiKeys.ControlsLook);
            _layout.ControlsFaceWord.text = _localization.Get(UiKeys.ControlsFace);
            _layout.ControlsLightsWord.text = _localization.Get(UiKeys.ControlsLights);
            WriteGlyphs();
        }

        private void WriteGlyphs()
        {
            bool pad = _device == InputDeviceKind.Gamepad;
            _drive.Set(_localization.Get(pad ? UiKeys.PadDrive : UiKeys.KeyDrive), _device);
            _look.Set(_localization.Get(pad ? UiKeys.PadLook : UiKeys.MouseLook), _device);
            _face.Set(_localization.Get(pad ? UiKeys.PadView : UiKeys.KeyTab), _device);
            _lights.Set(_labels.For(RoverAction.Lights, _device), _device);
        }
    }
}

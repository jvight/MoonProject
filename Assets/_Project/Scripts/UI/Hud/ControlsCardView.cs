using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The first controls card on screen: one small glass row at the bottom left, "WASD Drive · Mouse Look · Tab 07 ·
    /// L Lights", with the gamepad's caps (left stick, right stick, View, the lights button) while a pad was the last
    /// input. <see cref="ControlsCard"/> decides when it shows; this eases it in and out and keeps its caps on the
    /// active device. The lights cap is the real binding (<see cref="GlyphLabels"/>). The build and hook-cable keys join
    /// the row once added (<see cref="ControlsKeys"/>), the newest with a soft amber wash.
    /// </summary>
    internal sealed class ControlsCardView
    {
        private const string NewClass = "controls-card__item--new";

        private readonly UiLayout _layout;
        private readonly ILocalization _localization;
        private readonly GlyphLabels _labels;
        private readonly Reveal _reveal;
        private readonly GlyphView _drive;
        private readonly GlyphView _look;
        private readonly GlyphView _face;
        private readonly GlyphView _lights;
        private readonly GlyphView _build;
        private readonly GlyphView _hook;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;
        private ControlsKeys _added;
        private ControlsKeys _fresh;

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
            _build = new GlyphView(layout.ControlsBuildGlyph, layout.ControlsBuildGlyphLabel);
            _hook = new GlyphView(layout.ControlsHookGlyph, layout.ControlsHookGlyphLabel);
            ShowKeys(ControlsKeys.None, ControlsKeys.None);
            new ShadowPainter(layout.ControlsCardShadow);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The added keys on the row now (tests and captures).</summary>
        public ControlsKeys Added => _added;

        /// <summary>The added key marked new now (tests and captures).</summary>
        public ControlsKeys Fresh => _fresh;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="shown">The card holds the screen now.</param>
        /// <param name="device">The last input's device, for the caps.</param>
        /// <param name="added">The keys the card has added so far (in this save).</param>
        /// <param name="fresh">The key it has just added, marked new.</param>
        public void Tick(float deltaTime, bool shown, InputDeviceKind device, ControlsKeys added, ControlsKeys fresh)
        {
            if (device != _device)
            {
                _device = device;
                WriteGlyphs();
            }

            // The row changes only while the card is out of sight, so a key never pops in under the player's eyes,
            // and the wash stays until the card has gone.
            if (_reveal.IsHidden && (added != _added || fresh != _fresh))
            {
                ShowKeys(added, fresh);
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
            _layout.ControlsBuildWord.text = _localization.Get(UiKeys.Hint(InteractionKind.OpenBuild));
            _layout.ControlsHookWord.text = _localization.Get(UiKeys.Hint(InteractionKind.Tether));
            WriteGlyphs();
        }

        private void WriteGlyphs()
        {
            bool pad = _device == InputDeviceKind.Gamepad;
            _drive.Set(_localization.Get(pad ? UiKeys.PadDrive : UiKeys.KeyDrive), _device);
            _look.Set(_localization.Get(pad ? UiKeys.PadLook : UiKeys.MouseLook), _device);
            _face.Set(_localization.Get(pad ? UiKeys.PadView : UiKeys.KeyTab), _device);
            _lights.Set(_labels.For(RoverAction.Lights, _device), _device);
            _build.Set(_labels.For(RoverAction.Build, _device), _device);
            _hook.Set(_labels.For(RoverAction.Tether, _device), _device);
        }

        private void ShowKeys(ControlsKeys added, ControlsKeys fresh)
        {
            _added = added;
            _fresh = fresh;
            ShowKey(_layout.ControlsBuild, ControlsKeys.Build);
            ShowKey(_layout.ControlsHook, ControlsKeys.Hook);
        }

        private void ShowKey(VisualElement item, ControlsKeys key)
        {
            item.style.display = (_added & key) != 0 ? DisplayStyle.Flex : DisplayStyle.None;
            item.EnableInClassList(NewClass, (_fresh & key) != 0);
        }
    }
}

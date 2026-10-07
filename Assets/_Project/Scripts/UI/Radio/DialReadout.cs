using System;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The dial readout: when the player turns Bell's dial (while the game runs, never at load, see
    /// <see cref="DialWatch"/>), a small label eases in near the bottom centre, just above the ticker's band: one
    /// detent pip per station with the current one lit, the station's name ("radio.channel.&lt;station&gt;") and, on
    /// the Tape Deck, the chosen tape's title. It rests a moment after the last turn and fades; turning again while it
    /// is up rewrites it in place. A turn made while a context prompt is still on screen waits for the prompt to fade
    /// (the UI closes the prompts' gate while the readout is busy), so the two never share the screen.
    /// </summary>
    internal sealed class DialReadout
    {
        public const string DetentClass = "dial-readout__detent";
        public const string DetentLitClass = "dial-readout__detent--lit";

        private readonly DialReadoutSettings _settings;
        private readonly ILocalization _localization;
        private readonly IRadioProgram _program;
        private readonly DialWatch _watch = new DialWatch();
        private readonly Reveal _reveal;
        private readonly Label _station;
        private readonly Label _tape;
        private readonly VisualElement[] _detents;
        private float _rest;
        private bool _pending;

        public DialReadout(UiLayout layout, DialReadoutSettings settings, ILocalization localization,
            IRadioProgram program)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _program = program ?? throw new ArgumentNullException(nameof(program));
            _reveal = new Reveal(layout.DialReadout, settings.Reveal);
            _reveal.Snap(false);
            _station = layout.DialStation;
            _tape = layout.DialTape;
            new ShadowPainter(layout.DialReadoutShadow);

            int stations = 0;
            foreach (RadioChannel channel in Enum.GetValues(typeof(RadioChannel)))
            {
                stations = Math.Max(stations, (int)channel + 1);
            }

            _detents = new VisualElement[stations];
            for (int i = 0; i < stations; i++)
            {
                var detent = new VisualElement { pickingMode = PickingMode.Ignore };
                detent.AddToClassList(DetentClass);
                layout.DialDetents.Add(detent);
                _detents[i] = detent;
            }
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while a turn waits to be shown or the readout is on screen.</summary>
        public bool IsBusy => _pending || IsVisible;

        /// <summary>
        /// The radio program changed: true (and the readout will show) when that was a turn of the dial.
        /// </summary>
        public bool OnProgramChanged()
        {
            if (!_watch.Changed(_program))
            {
                return false;
            }

            _pending = true;
            return true;
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="stageClear">False while a context prompt is still on screen: a new turn waits for it.</param>
        public void Tick(float deltaTime, bool stageClear)
        {
            if (!_watch.IsArmed)
            {
                _watch.Arm(_program);
            }

            if (_pending && stageClear)
            {
                _pending = false;
                Write();
                _rest = _settings.HoldSeconds;
                _reveal.Show();
            }

            if (_reveal.IsShown)
            {
                _rest -= deltaTime;
                if (_rest <= 0f)
                {
                    _reveal.Hide();
                }
            }

            _reveal.Tick(deltaTime);
        }

        /// <summary>Re-reads the readout on screen in the new language.</summary>
        public void Relocalize()
        {
            if (IsVisible)
            {
                Write();
            }
        }

        private void Write()
        {
            RadioChannel channel = _program.Channel;
            _station.text = _localization.Get(UiKeys.RadioChannelName(channel));
            string tape = _program.SelectedTape;
            bool showTape = channel == RadioChannel.TapeDeck && !string.IsNullOrEmpty(tape);
            _tape.style.display = showTape ? DisplayStyle.Flex : DisplayStyle.None;
            if (showTape)
            {
                _tape.text = _localization.Get(UiKeys.CassetteTitle(tape));
            }

            for (int i = 0; i < _detents.Length; i++)
            {
                _detents[i].EnableInClassList(DetentLitClass, i == (int)channel);
            }
        }
    }
}

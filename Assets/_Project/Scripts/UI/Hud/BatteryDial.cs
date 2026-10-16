using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's battery as the big CRT dial at the bottom left (Variant C amber CRT dial): one ring
    /// segment per cell, the cell in use filled by its share and breathing, the charge as a percentage in the middle
    /// over the word "power". On the sealed reserve (Low) the last lit cell breathes slowly in amber. Never red. When it
    /// shows follows <see cref="CellRowRules"/> (first-hour-v2 6.4). A frame writes styles and repaints only when they
    /// move; the percentages are formatted once.
    /// </summary>
    internal sealed class BatteryDial
    {
        private const float OpacityEpsilon = 0.01f;
        private const float DimSnap = 0.002f;
        private const int Percents = 100;

        private readonly IRoverBattery _battery;
        private readonly IBeamStatus _beam;
        private readonly CellRowSettings _settings;
        private readonly CellRowRules _rules;
        private readonly Reveal _reveal;
        private readonly VisualElement _face;
        private readonly Label _value;
        private readonly CellRingPainter _ring;
        private readonly string[] _percentTexts = new string[Percents + 1];
        private float _dim = 1f;
        private float _writtenDim = -1f;
        private float _clock;
        private int _writtenPercent = -1;

        public BatteryDial(UiLayout layout, CellRowSettings settings, IRoverBattery battery, IBeamStatus beam)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
            _beam = beam ?? throw new ArgumentNullException(nameof(beam));
            _rules = new CellRowRules(settings);
            _reveal = new Reveal(layout.BatteryDial, settings.Reveal);
            _reveal.Snap(false);
            _face = layout.BatteryDialFace;
            _value = layout.BatteryDialValue;
            _ring = new CellRingPainter(layout.BatteryDialRing);
            for (int i = 0; i <= Percents; i++)
            {
                _percentTexts[i] = i + "%";
            }
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The percentage shown (tests and captures).</summary>
        public string Shown => _value.text;

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="revealed">The dial has revealed itself in this game.</param>
        /// <param name="open">False while something else needs the screen (pause, the map, stargazing).</param>
        public void Tick(float deltaTime, bool revealed, bool open)
        {
            int cells = _battery.Cells;
            float charge = _battery.Charge;
            _rules.Tick(deltaTime, revealed, open, charge >= cells, _battery.IsCharging, _beam.Working);
            _reveal.Set(_rules.Shown);
            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden)
            {
                return;
            }

            _clock += deltaTime;
            Ease(deltaTime);
            int percent = Mathf.Clamp(Mathf.RoundToInt(_battery.Charge01 * Percents), 0, Percents);
            if (percent != _writtenPercent)
            {
                _value.text = _percentTexts[percent];
                _writtenPercent = percent;
            }

            bool low = _battery.IsLowPower;
            int breathing = CellRowRules.InUse(cells, charge);
            if (low && charge > 0f)
            {
                breathing = Mathf.Min(cells, Mathf.CeilToInt(charge)) - 1;
            }

            float period = low ? _settings.LowBreathSeconds : _settings.BreathSeconds;
            float wave = 0.5f + 0.5f * Mathf.Sin(_clock * Mathf.PI * 2f / period);
            _ring.Set(cells, charge, breathing, 1f - _settings.BreathDepth * wave, low);
        }

        private void Ease(float deltaTime)
        {
            float target = _rules.Dim;
            _dim = Mathf.Abs(_dim - target) < DimSnap
                ? target
                : Mathf.Lerp(_dim, target, 1f - Mathf.Exp(-_settings.DimRate * deltaTime));
            if (Mathf.Abs(_dim - _writtenDim) > OpacityEpsilon || _dim == target && _dim != _writtenDim)
            {
                _face.style.opacity = _dim;
                _writtenDim = _dim;
            }
        }
    }
}

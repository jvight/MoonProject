using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's cells as a small row of pills at the top left of the HUD (docs/design/first-hour-v2 6.4), one pill per
    /// cell, mirroring the flank lamps: a full cell is amber, the cell in use breathes, an empty cell is a faint
    /// outline, and on the sealed reserve (Low) the last lit pill breathes slowly. Never red. When it shows follows
    /// <see cref="CellRowRules"/>. Pills are built when the number of cells changes; a frame writes styles only when
    /// they move.
    /// </summary>
    internal sealed class CellRow
    {
        private const string PillClass = "cell-row__pill";
        private const string FullClass = "cell-row__pill--full";
        private const string EmptyClass = "cell-row__pill--empty";
        private const float OpacityEpsilon = 0.01f;
        private const float DimSnap = 0.002f;

        private readonly IRoverBattery _battery;
        private readonly IBeamStatus _beam;
        private readonly CellRowSettings _settings;
        private readonly CellRowRules _rules;
        private readonly Reveal _reveal;
        private readonly VisualElement _plate;
        private VisualElement[] _pills = Array.Empty<VisualElement>();
        private bool[] _lit = Array.Empty<bool>();
        private float _dim = 1f;
        private float _writtenDim = -1f;
        private float _clock;
        private float _writtenBreath = -1f;
        private int _writtenBreathing = -1;

        public CellRow(UiLayout layout, CellRowSettings settings, IRoverBattery battery, IBeamStatus beam)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
            _beam = beam ?? throw new ArgumentNullException(nameof(beam));
            _rules = new CellRowRules(settings);
            _reveal = new Reveal(layout.CellRow, settings.Reveal);
            _reveal.Snap(false);
            _plate = layout.CellRowPlate;
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The number of pills now built (tests and captures).</summary>
        public int PillCount => _pills.Length;

        /// <summary>The pill of cell <paramref name="index"/> (tests and captures).</summary>
        public VisualElement Pill(int index)
        {
            return _pills[index];
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="revealed">The row has revealed itself in this game.</param>
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

            EnsurePills(cells);
            _clock += deltaTime;
            Ease(deltaTime);
            int inUse = CellRowRules.InUse(cells, charge);
            for (int i = 0; i < cells; i++)
            {
                bool lit = charge >= i + 1 || i == inUse;
                if (lit != _lit[i])
                {
                    _pills[i].EnableInClassList(FullClass, lit);
                    _pills[i].EnableInClassList(EmptyClass, !lit);
                    _lit[i] = lit;
                }
            }

            Breathe(inUse, cells, charge);
        }

        private void Ease(float deltaTime)
        {
            float target = _rules.Dim;
            _dim = Mathf.Abs(_dim - target) < DimSnap
                ? target
                : Mathf.Lerp(_dim, target, 1f - Mathf.Exp(-_settings.DimRate * deltaTime));
            if (Mathf.Abs(_dim - _writtenDim) > OpacityEpsilon || _dim == target && _dim != _writtenDim)
            {
                _plate.style.opacity = _dim;
                _writtenDim = _dim;
            }
        }

        /// <summary>The cell in use breathes; on the sealed reserve the last lit pill breathes slowly instead.</summary>
        private void Breathe(int inUse, int cells, float charge)
        {
            bool low = _battery.IsLowPower;
            int breathing = inUse;
            if (low && charge > 0f)
            {
                breathing = Mathf.Min(cells, Mathf.CeilToInt(charge)) - 1;
            }

            if (breathing != _writtenBreathing && _writtenBreathing >= 0 && _writtenBreathing < _pills.Length)
            {
                _pills[_writtenBreathing].style.opacity = 1f;
                _writtenBreath = -1f;
            }

            _writtenBreathing = breathing;
            if (breathing < 0 || breathing >= _pills.Length)
            {
                return;
            }

            float period = low ? _settings.LowBreathSeconds : _settings.BreathSeconds;
            float wave = 0.5f + 0.5f * Mathf.Sin(_clock * Mathf.PI * 2f / period);
            float opacity = 1f - _settings.BreathDepth * wave;
            if (Mathf.Abs(opacity - _writtenBreath) > OpacityEpsilon)
            {
                _pills[breathing].style.opacity = opacity;
                _writtenBreath = opacity;
            }
        }

        private void EnsurePills(int cells)
        {
            if (cells == _pills.Length)
            {
                return;
            }

            _plate.Clear();
            _pills = new VisualElement[cells];
            _lit = new bool[cells];
            for (int i = 0; i < cells; i++)
            {
                var pill = new VisualElement { pickingMode = PickingMode.Ignore, name = PillClass + "-" + i };
                pill.AddToClassList(PillClass);
                pill.AddToClassList(EmptyClass);
                _plate.Add(pill);
                _pills[i] = pill;
            }

            _writtenBreathing = -1;
            _writtenBreath = -1f;
        }
    }
}

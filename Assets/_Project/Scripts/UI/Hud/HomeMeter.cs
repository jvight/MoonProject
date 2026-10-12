using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The distance home at the top centre (Moonlight Glass, Subnautica's depth gauge): metres to the base over the
    /// compass point it lies in ("home · NE"), under a shallow arc that fills as 07 roams farther. The number eases and
    /// steps in whole <see cref="HomeMeterSettings.StepMetres"/>; it fades away near home. Every string is made once
    /// (the bearings again on a language change), so a frame never allocates.
    /// </summary>
    internal sealed class HomeMeter
    {
        private const string LabelKey = "ui.meter.home";

        private readonly HomeMeterSettings _settings;
        private readonly IRoverState _rover;
        private readonly IWorldLayout _world;
        private readonly ILocalization _localization;
        private readonly UiLayout _layout;
        private readonly Reveal _reveal;
        private readonly MeterArcPainter _arc;
        private readonly IntText _metres;
        private readonly string[] _labels = new string[CompassPoint.Count];
        private float _shown = -1f;
        private int _writtenMetres = -1;
        private int _writtenPoint = -1;

        public HomeMeter(UiLayout layout, HomeMeterSettings settings, IRoverState rover, IWorldLayout world,
            ILocalization localization)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _world = world ?? throw new ArgumentNullException(nameof(world));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _reveal = new Reveal(layout.HomeMeter, settings.Reveal);
            _reveal.Snap(false);
            _arc = new MeterArcPainter(layout.HomeMeterArc);
            _metres = new IntText(settings.MaxShownMetres + 1);
            Relocalize();
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The number shown (tests and captures).</summary>
        public string Shown => _layout.HomeMeterValue.text;

        /// <summary>The bearing line shown (tests and captures).</summary>
        public string Bearing => _layout.HomeMeterKey.text;

        /// <summary>Writes the bearing lines in the language now in use (allocates; a language change only).</summary>
        public void Relocalize()
        {
            string label = _localization.Get(LabelKey);
            for (int i = 0; i < _labels.Length; i++)
            {
                _labels[i] = string.Format(label, _localization.Get(CompassPoint.Key(i)));
            }

            if (_writtenPoint >= 0)
            {
                _layout.HomeMeterKey.text = _labels[_writtenPoint];
            }
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="open">False while the HUD gives way (pause, the title, a card, the map).</param>
        public void Tick(float deltaTime, bool open)
        {
            Vector3 rover = _rover.Position;
            Vector3 home = _world.BasePosition;
            float distance = new Vector2(home.x - rover.x, home.z - rover.z).magnitude;
            _shown = _shown < 0f
                ? distance
                : Mathf.Lerp(_shown, distance, 1f - Mathf.Exp(-_settings.EaseRate * deltaTime));
            _reveal.Set(open && _shown >= _settings.NearMetres);
            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden)
            {
                return;
            }

            int step = _settings.StepMetres;
            int metres = Mathf.Min(_settings.MaxShownMetres, Mathf.RoundToInt(_shown / step) * step);
            if (metres != _writtenMetres)
            {
                _layout.HomeMeterValue.text = _metres.Get(metres);
                _writtenMetres = metres;
            }

            int point = CompassPoint.Of(rover, home);
            if (point != _writtenPoint)
            {
                _layout.HomeMeterKey.text = _labels[point];
                _writtenPoint = point;
            }

            _arc.Fill = _shown / _settings.FullArcMetres;
        }
    }
}

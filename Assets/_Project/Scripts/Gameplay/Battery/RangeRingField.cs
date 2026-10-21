using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The range ring in play (<see cref="IRangeRing"/>): shown faint for a few seconds at the first charge, and in
    /// full when asked (<see cref="Show"/>; the release's staggered order calls it). Each showing works the ring out
    /// from 07's place and charge with <see cref="RangeRingModel"/>, holds that polygon for the showing (a ring that
    /// follows 07 about would read as a radius, not as the world's edge), publishes <see cref="RangeRingShown"/> and
    /// fades in and out. The charge points come from the built pads, and from whatever else adds them later.
    /// </summary>
    public sealed class RangeRingField : IRangeRing, IDisposable
    {
        private readonly EventBus _events;
        private readonly RangeRingSettings _settings;
        private readonly RangeRingModel _model;
        private readonly BatteryModel _battery;
        private readonly IChargePointSource _chargePointSource;
        private readonly IRoverState _rover;
        private readonly Vector3[] _polygon;
        private readonly List<Vector3> _chargePoints = new List<Vector3>();
        private readonly IDisposable _milestones;
        private float _seconds;
        private float _left;
        private Vector3 _centre;

        public RangeRingField(EventBus events, RangeRingSettings settings, RangeRingModel model, BatteryModel battery,
            IChargePointSource chargePointSource, IRoverState rover)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _battery = battery ?? throw new ArgumentNullException(nameof(battery));
            _chargePointSource = chargePointSource ?? throw new ArgumentNullException(nameof(chargePointSource));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _polygon = new Vector3[settings.Points];
            _milestones = events.Subscribe<OpeningMilestoneReached>(OnMilestone);
        }

        public bool IsShown => _left > 0f;

        public RangeRingMode Mode { get; private set; }

        public float Visibility01
        {
            get
            {
                if (_left <= 0f)
                {
                    return 0f;
                }

                float in01 = Mathf.Clamp01((_seconds - _left) / Mathf.Max(0.01f, _settings.FadeInSeconds));
                float out01 = Mathf.Clamp01(_left / Mathf.Max(0.01f, _settings.FadeOutSeconds));
                float t = Mathf.Min(in01, out01);
                return t * t * (3f - 2f * t);
            }
        }

        /// <summary>
        /// How far the ring has drawn out from 07 toward its edge, 0..1, eased: it grows outward, then holds.
        /// </summary>
        public float Reveal01
        {
            get
            {
                if (_left <= 0f)
                {
                    return 0f;
                }

                float t = Mathf.Clamp01((_seconds - _left) / Mathf.Max(0.01f, _settings.RevealSeconds));
                return t * t * (3f - 2f * t);
            }
        }

        /// <summary>Where the ring was worked out from: 07's place when it was shown. It draws out from here.</summary>
        public Vector3 Centre => _centre;

        /// <summary>The line's strength for the mode now (the mode's own strength times the fade).</summary>
        public float Strength => Visibility01 * (Mode == RangeRingMode.Full ? _settings.FullStrength
            : _settings.FaintStrength);

        public int Count => _polygon.Length;

        public Vector3 Point(int index)
        {
            return _polygon[index];
        }

        /// <summary>Shows the ring in <paramref name="mode"/>, worked out from 07's place and charge now.</summary>
        public void Show(RangeRingMode mode)
        {
            _chargePoints.Clear();
            _chargePointSource.CopyChargePoints(_chargePoints);
            _centre = _rover.Position;
            _model.Compute(_centre, _battery.Charge, _chargePoints, _polygon);
            Mode = mode;
            _seconds = mode == RangeRingMode.Full ? _settings.FullSeconds : _settings.FaintSeconds;
            _left = _seconds;
            _events.Publish(new RangeRingShown(mode, _seconds));
        }

        public void Step(float deltaTime)
        {
            if (_left > 0f)
            {
                _left = Mathf.Max(0f, _left - deltaTime);
            }
        }

        public void Dispose()
        {
            _milestones.Dispose();
        }

        private void OnMilestone(OpeningMilestoneReached reached)
        {
            if (reached.Milestone == OpeningMilestone.FirstCharge && !reached.Restored)
            {
                Show(RangeRingMode.Faint);
            }
        }
    }
}

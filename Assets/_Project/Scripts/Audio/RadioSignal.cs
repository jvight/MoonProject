using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio's signal state: an eased signal radius (upgrades bloom in slowly) and an eased clarity computed
    /// from the listener's distance to the base through <see cref="SignalField"/>.
    /// </summary>
    public sealed class RadioSignal
    {
        private readonly RadioTuning _tuning;
        private readonly EasedValue _radius;
        private readonly EasedValue _clarity;
        private float _targetRadius;

        public RadioSignal(RadioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _targetRadius = tuning.SignalRadius;
            _radius = new EasedValue(_targetRadius);
            _clarity = new EasedValue(1f);
        }

        /// <summary>The radius currently in effect (metres, eased towards <see cref="TargetRadius"/>).</summary>
        public float Radius => _radius.Value;

        public float TargetRadius => _targetRadius;

        /// <summary>Smoothed clarity, 0 (no signal) .. 1 (perfect).</summary>
        public float Clarity => _clarity.Value;

        public RadioMix Mix => RadioMix.Evaluate(_clarity.Value, _tuning);

        /// <summary>Changes the clear-signal radius; the change eases in over the tuning's radius ease time.</summary>
        public void SetTargetRadius(float radius)
        {
            _targetRadius = Mathf.Max(0f, radius);
        }

        /// <summary>Jumps straight to the clarity at <paramref name="distance"/> (first frame, no fade from
        /// 1).</summary>
        public void Snap(float distance)
        {
            _radius.Snap(_targetRadius);
            _clarity.Snap(SignalField.Clarity(distance, _radius.Value, _tuning.FalloffWidth));
        }

        /// <summary>Advances the easing for a listener <paramref name="distance"/> metres from the base.</summary>
        public float Step(float distance, float deltaTime)
        {
            _radius.Step(_targetRadius, deltaTime, _tuning.RadiusEaseTime);
            float raw = SignalField.Clarity(distance, _radius.Value, _tuning.FalloffWidth);
            return _clarity.Step(raw, deltaTime, _tuning.ClaritySmoothing);
        }
    }
}

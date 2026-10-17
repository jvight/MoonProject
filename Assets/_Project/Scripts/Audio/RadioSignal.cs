using System;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio's signal state: an eased clarity computed from the listener's distance to the base through
    /// <see cref="SignalField"/>, inside the tuning's clear radius.
    /// </summary>
    public sealed class RadioSignal
    {
        private readonly RadioTuning _tuning;
        private readonly EasedValue _clarity;

        public RadioSignal(RadioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _clarity = new EasedValue(1f);
        }

        /// <summary>The clear-signal radius (metres from the base).</summary>
        public float Radius => _tuning.SignalRadius;

        /// <summary>Smoothed clarity, 0 (no signal) .. 1 (perfect).</summary>
        public float Clarity => _clarity.Value;

        public RadioMix Mix => RadioMix.Evaluate(_clarity.Value, _tuning);

        /// <summary>Jumps straight to the clarity at <paramref name="distance"/> (first frame, no fade from
        /// 1).</summary>
        public void Snap(float distance)
        {
            _clarity.Snap(SignalField.Clarity(distance, Radius, _tuning.FalloffWidth));
        }

        /// <summary>Advances the easing for a listener <paramref name="distance"/> metres from the base.</summary>
        public float Step(float distance, float deltaTime)
        {
            float raw = SignalField.Clarity(distance, Radius, _tuning.FalloffWidth);
            return _clarity.Step(raw, deltaTime, _tuning.ClaritySmoothing);
        }
    }
}

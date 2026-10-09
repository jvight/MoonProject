using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Motion of the Hover-Jump coils, stepped once per frame: hidden until they are on 07 (carried in by the Rover
    /// Bay's floor arm or there from a loaded game, see <see cref="RoverKit"/>). The coil length squashes with the
    /// charge and springs out on the leap; the glow (0..1) rises with the charge and fades after it.
    /// </summary>
    public sealed class HoverCoilMotion
    {
        /// <summary>Below this the glow is invisible: it snaps to dark so the light can switch off.</summary>
        private const float Dark = 1e-3f;

        private readonly HoverCoilSettings _settings;
        private DampedSpring _length;

        public HoverCoilMotion(HoverCoilSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _length.Reset(1f);
        }

        public bool Visible { get; private set; }

        /// <summary>Coil length as a multiple of its rest length (local Y scale).</summary>
        public float CoilLength => _length.Value;

        /// <summary>Glow level 0..1 (exactly 0 when dark).</summary>
        public float Glow { get; private set; }

        /// <summary>
        /// Advances one frame. <paramref name="shown"/>: the coils are on 07 (once on, they stay).
        /// <paramref name="charge"/>: the Hover-Jump charge 0..1.
        /// </summary>
        public void Step(bool shown, float charge, float deltaTime)
        {
            if (shown && !Visible)
            {
                Visible = true;
                _length.Reset(1f);
            }

            if (!Visible || deltaTime <= 0f)
            {
                return;
            }

            float squashed = 1f - _settings.ChargeSquash;
            _length.Step(1f - _settings.ChargeSquash * Mathf.Clamp01(charge), _settings.SpringFrequency,
                _settings.SpringDamping, deltaTime);
            _length.Clamp(squashed, Mathf.Max(1f, _settings.MaxStretch));

            float halfLife = charge > Glow ? _settings.GlowRiseHalfLife : _settings.GlowFallHalfLife;
            Glow = Smoothing.Damp(Glow, Mathf.Clamp01(charge), halfLife, deltaTime);
            if (Glow < Dark)
            {
                Glow = 0f;
            }
        }

        /// <summary>The springs let go: a quick outward kick, bigger for a stronger leap (0 tap .. 1 full).</summary>
        public void Leap(float strength)
        {
            _length.AddVelocity(Mathf.Lerp(_settings.TapKick, _settings.LeapKick, Mathf.Clamp01(strength)));
        }
    }
}

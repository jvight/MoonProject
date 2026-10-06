using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Motion of the Hover-Jump coils, stepped once per frame: hidden until 07 owns the ability; owned from the start
    /// they are simply there, bought later they pop in (the mount scales up from nothing, overshoots once and settles).
    /// The coil length squashes with the charge and springs out on the leap; the glow (0..1) rises with the charge
    /// and fades after it.
    /// </summary>
    public sealed class HoverCoilMotion
    {
        /// <summary>Below this the glow is invisible: it snaps to dark so the light can switch off.</summary>
        private const float Dark = 1e-3f;

        private readonly HoverCoilSettings _settings;
        private DampedSpring _mount;
        private DampedSpring _length;
        private bool _started;

        public HoverCoilMotion(HoverCoilSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _mount.Reset(1f);
            _length.Reset(1f);
        }

        public bool Visible { get; private set; }

        /// <summary>Uniform scale of the coil mount (1 at rest; 0 to above 1 while popping in).</summary>
        public float MountScale => _mount.Value;

        /// <summary>Coil length as a multiple of its rest length (local Y scale).</summary>
        public float CoilLength => _length.Value;

        /// <summary>Glow level 0..1 (exactly 0 when dark).</summary>
        public float Glow { get; private set; }

        /// <summary>
        /// Advances one frame. <paramref name="charge"/>: the Hover-Jump charge 0..1. Returns true on the frame the
        /// coils pop in after a purchase.
        /// </summary>
        public bool Step(bool owned, float charge, float deltaTime)
        {
            bool appeared = false;
            if (owned && !Visible)
            {
                Visible = true;
                appeared = _started;
                _mount.Reset(_started ? 0f : 1f);
                _length.Reset(1f);
            }

            _started = true;
            if (!Visible || deltaTime <= 0f)
            {
                return appeared;
            }

            _mount.Step(1f, _settings.PopFrequency, _settings.PopDamping, deltaTime);
            _mount.Clamp(0f, float.MaxValue);

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

            return appeared;
        }

        /// <summary>The springs let go: a quick outward kick, bigger for a stronger leap (0 tap .. 1 full).</summary>
        public void Leap(float strength)
        {
            _length.AddVelocity(Mathf.Lerp(_settings.TapKick, _settings.LeapKick, Mathf.Clamp01(strength)));
        }
    }
}

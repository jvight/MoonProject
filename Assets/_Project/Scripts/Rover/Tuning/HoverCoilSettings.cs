using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Hover-Jump coils under 07's belly (art's HoverCoils on CoilSocket): they pop in when the ability is bought,
    /// squash and glow while the jump charges, spring out on the leap, and a soft cyan light under the belly lets the
    /// charge read from the chase camera, which never sees the coils themselves.
    /// </summary>
    [Serializable]
    public sealed class HoverCoilSettings
    {
        [Tooltip("Spring frequency (Hz) of the coils popping in when the Hover-Jump is bought.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _popFrequency = 2.8f;

        [Tooltip("Damping ratio of the pop-in: below 1 the mount overshoots once and settles, like a happy unfold.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _popDamping = 0.4f;

        [Tooltip("Upward chassis bob (m/s) as the coils unfold, so the pop reads from behind 07.")]
        [Range(0f, 2f)]
        [SerializeField] private float _popHeaveKick = 0.5f;

        [Tooltip("Antenna wiggle (deg/s) as the coils unfold.")]
        [Range(0f, 300f)]
        [SerializeField] private float _popAntennaKick = 80f;

        [Tooltip("Fraction of a coil's length squashed away at a full charge.")]
        [Range(0f, 0.8f)]
        [SerializeField] private float _chargeSquash = 0.35f;

        [Tooltip("Spring frequency (Hz) of the coil length (squash while charging, spring-out on the leap).")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _springFrequency = 4.5f;

        [Tooltip("Damping ratio of the coil length: low enough for a bouncy spring-out after the leap.")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _springDamping = 0.3f;

        [Tooltip("Outward kick (coil lengths per second) a tap gives the springs.")]
        [Range(0f, 30f)]
        [SerializeField] private float _tapKick = 5f;

        [Tooltip("Outward kick (coil lengths per second) a full leap gives the springs.")]
        [Range(0f, 30f)]
        [SerializeField] private float _leapKick = 15f;

        [Tooltip("Longest a coil may stretch (multiple of its rest length) when it springs out.")]
        [Range(1f, 3f)]
        [SerializeField] private float _maxStretch = 1.45f;

        [Tooltip("Glow ring emission at a full charge (linear HDR multiplier; 1 = the authored glow).")]
        [Range(0f, 4f)]
        [SerializeField] private float _glowPeak = 2.44f;

        [Tooltip("Half-life (s) of the glow rising with the charge.")]
        [Range(0.005f, 0.5f)]
        [SerializeField] private float _glowRiseHalfLife = 0.04f;

        [Tooltip("Half-life (s) of the glow fading after the leap (or a cancelled charge).")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _glowFallHalfLife = 0.18f;

        [Tooltip("Intensity of the cyan point light at CoilSocket at a full charge (no shadows). It sits ~0.3 m above "
            + "the ground, so a little goes a long way: a soft pool around the wheels, never a hot spot.")]
        [Range(0f, 10f)]
        [SerializeField] private float _lightIntensity = 1.2f;

        [Tooltip("Range (m) of the charge light.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lightRange = 3f;

        public float PopFrequency => _popFrequency;

        public float PopDamping => _popDamping;

        public float PopHeaveKick => _popHeaveKick;

        public float PopAntennaKick => _popAntennaKick;

        public float ChargeSquash => _chargeSquash;

        public float SpringFrequency => _springFrequency;

        public float SpringDamping => _springDamping;

        public float TapKick => _tapKick;

        public float LeapKick => _leapKick;

        public float MaxStretch => _maxStretch;

        public float GlowPeak => _glowPeak;

        public float GlowRiseHalfLife => _glowRiseHalfLife;

        public float GlowFallHalfLife => _glowFallHalfLife;

        public float LightIntensity => _lightIntensity;

        public float LightRange => _lightRange;
    }
}

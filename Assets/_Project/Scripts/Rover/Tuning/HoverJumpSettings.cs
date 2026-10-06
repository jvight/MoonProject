using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Hover-Jump workshop ability (M3-03): hold to charge, release to leap. Heights are the designer-facing
    /// numbers; the take-off speed is derived from them and the air gravity. Descents are cushioned so even a full leap
    /// lands softly: no fall damage, ever.
    /// </summary>
    [Serializable]
    public sealed class HoverJumpSettings
    {
        [Tooltip("Seconds of holding Jump to reach a full charge.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _chargeTime = 0.8f;

        [Tooltip("Apex height (m) of a tap: a small, happy hop.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _tapHeight = 0.8f;

        [Tooltip("Apex height (m) of a fully charged leap.")]
        [Range(1f, 20f)]
        [SerializeField] private float _fullHeight = 7f;

        [Tooltip("Seconds after landing before 07 can charge again (short enough to chain hops for fun).")]
        [Range(0f, 2f)]
        [SerializeField] private float _cooldown = 0.4f;

        [Tooltip("Seconds after take-off during which ground forces are off, so 07 leaves the ground cleanly.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _liftoffTime = 0.15f;

        [Tooltip("Fraction of ground steering available during a leap; it also bends the flight path gently.")]
        [Range(0f, 1f)]
        [SerializeField] private float _airSteer = 0.2f;

        [Tooltip("Height (m) above the ground at which the landing cushion can start to act.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _cushionProbe = 8f;

        [Tooltip("Deceleration (m/s^2) the cushion may use to soften a descent.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _cushionDeceleration = 6f;

        [Tooltip("Speed (m/s) 07 touches down at after a cushioned leap: soft enough for a happy perk, never an oof.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _cushionLandingSpeed = 1.6f;

        [Tooltip("RoverJumpCharged is published at the start of a charge and this many times more until it is full.")]
        [Range(1, 8)]
        [SerializeField] private int _chargeSteps = 4;

        public float ChargeTime => _chargeTime;

        public float TapHeight => _tapHeight;

        public float FullHeight => Mathf.Max(_tapHeight, _fullHeight);

        public float Cooldown => _cooldown;

        public float LiftoffTime => _liftoffTime;

        public float AirSteer => _airSteer;

        public float CushionProbe => _cushionProbe;

        public float CushionDeceleration => _cushionDeceleration;

        public float CushionLandingSpeed => _cushionLandingSpeed;

        public int ChargeSteps => _chargeSteps;
    }
}

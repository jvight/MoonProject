using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The hidden physics sphere and how it meets the ground: contact probe, downforce, grip, slope help and the
    /// extra gravity that shapes low-gravity hops. World gravity is lunar (ProjectSettings); everything here is added
    /// on top explicitly.
    /// </summary>
    [Serializable]
    public sealed class GroundSettings
    {
        [Tooltip("Mass of the physics sphere (kg). Heavy, so tethered relics never yank the rover around.")]
        [Min(1f)]
        [SerializeField] private float _mass = 250f;

        [Tooltip("Radius (m) of the hidden physics sphere. Smaller squeezes between rocks; larger rolls over bumps.")]
        [Range(0.2f, 1.2f)]
        [SerializeField] private float _sphereRadius = 0.5f;

        [Tooltip("Radius of the ground probe as a fraction of the sphere radius.")]
        [Range(0.5f, 0.99f)]
        [SerializeField] private float _probeRadiusFactor = 0.9f;

        [Tooltip("Gap (m) under the sphere that still counts as touching the ground. Larger hugs crests longer.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _groundSnapDistance = 0.12f;

        [Tooltip("Surfaces steeper than this (deg) are walls, not ground.")]
        [Range(20f, 89f)]
        [SerializeField] private float _maxGroundAngle = 60f;

        [Tooltip("Extra acceleration (m/s^2) pressing the rover into the ground along its normal while grounded. "
            + "Keeps it hugging gentle dunes; sharp crests at speed still launch joyful hops.")]
        [Range(0f, 20f)]
        [SerializeField] private float _downforce = 4f;

        [Tooltip("How quickly (1/s) the velocity swings back in line with the heading while grounded. Lower = more "
            + "lunar drift in turns; speed is preserved, never scrubbed.")]
        [Range(0.5f, 40f)]
        [SerializeField] private float _gripRate = 9f;

        [Tooltip("Slopes up to this angle (deg) are fully assisted: gravity does not slow you uphill or drag you down.")]
        [Range(0f, 60f)]
        [SerializeField] private float _slopeAssistFullAngle = 32f;

        [Tooltip("Slope angle (deg) where assistance has faded out completely and the rover gently slides back.")]
        [Range(0f, 80f)]
        [SerializeField] private float _slopeAssistZeroAngle = 45f;

        [Tooltip("Fraction of along-slope gravity still felt while rolling, so hills have a little character.")]
        [Range(0f, 1f)]
        [SerializeField] private float _slopeInfluence = 0.3f;

        [Tooltip("Speed (m/s) above which Slope Influence fully applies; below it the rover parks firmly on slopes.")]
        [Range(0.05f, 4f)]
        [SerializeField] private float _slopeRollSpeed = 0.8f;

        [Tooltip("Total downward acceleration (m/s^2) while airborne and rising. Lunar is 1.62; a little more keeps "
            + "hops floaty but short enough to stay in control.")]
        [Range(0.5f, 9.81f)]
        [SerializeField] private float _airRiseGravity = 2.6f;

        [Tooltip("Total downward acceleration (m/s^2) while airborne and falling.")]
        [Range(0.5f, 9.81f)]
        [SerializeField] private float _airFallGravity = 3.6f;

        [Tooltip("Fraction of drive acceleration available in the air (minimal air control).")]
        [Range(0f, 1f)]
        [SerializeField] private float _airThrottleFactor = 0.1f;

        [Tooltip("Half-life (s) smoothing the published ground normal across low-poly facet edges.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _groundNormalHalfLife = 0.06f;

        public float Mass => _mass;

        public float SphereRadius => _sphereRadius;

        public float ProbeRadiusFactor => _probeRadiusFactor;

        public float GroundSnapDistance => _groundSnapDistance;

        public float MaxGroundAngle => _maxGroundAngle;

        public float Downforce => _downforce;

        public float GripRate => _gripRate;

        public float SlopeAssistFullAngle => _slopeAssistFullAngle;

        public float SlopeAssistZeroAngle => Mathf.Max(_slopeAssistFullAngle, _slopeAssistZeroAngle);

        public float SlopeInfluence => _slopeInfluence;

        public float SlopeRollSpeed => _slopeRollSpeed;

        public float AirRiseGravity => _airRiseGravity;

        public float AirFallGravity => _airFallGravity;

        public float AirThrottleFactor => _airThrottleFactor;

        public float GroundNormalHalfLife => _groundNormalHalfLife;
    }
}

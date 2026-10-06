using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>When a loss of ground contact counts as a hop, and which touchdowns are worth announcing.</summary>
    [Serializable]
    public sealed class LandingSettings
    {
        [Tooltip("Ground contact may flicker off for this long (s) on small bumps without counting as airborne.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _coyoteTime = 0.1f;

        [Tooltip("Touchdowns after less air time than this (s) are silent (no RoverLanded event).")]
        [Range(0f, 2f)]
        [SerializeField] private float _minAirTime = 0.25f;

        [Tooltip("Touchdowns slower than this into the ground (m/s) are silent (no RoverLanded event).")]
        [Range(0f, 5f)]
        [SerializeField] private float _minImpactSpeed = 0.5f;

        public float CoyoteTime => _coyoteTime;

        public float MinAirTime => _minAirTime;

        public float MinImpactSpeed => _minImpactSpeed;
    }
}

using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Yaw feel. The turn rate depends on speed: a calm pivot at standstill (six-wheel lunar rovers can turn on the
    /// spot, so the player never needs a three-point turn), the most agile response at walking pace, and a wider,
    /// stable arc at top speed so steering never feels twitchy.
    /// </summary>
    [Serializable]
    public sealed class SteeringSettings
    {
        [Tooltip("Half-life (s) of the steering input easing toward a held direction.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _steerRiseHalfLife = 0.07f;

        [Tooltip("Half-life (s) of the steering input easing back to centre when released.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _steerReturnHalfLife = 0.06f;

        [Tooltip("Turn rate (deg/s) when standing still: a deliberate on-the-spot pivot.")]
        [Range(0f, 180f)]
        [SerializeField] private float _pivotTurnRate = 70f;

        [Tooltip("Highest turn rate (deg/s), reached at Peak Turn Speed.")]
        [Range(10f, 240f)]
        [SerializeField] private float _peakTurnRate = 105f;

        [Tooltip("Speed (m/s) at which the turn rate peaks.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _peakTurnSpeed = 2.5f;

        [Tooltip("Turn rate (deg/s) at top speed. Turn radius at top speed = topSpeed / rate (in radians).")]
        [Range(10f, 180f)]
        [SerializeField] private float _topSpeedTurnRate = 62f;

        [Tooltip("Rolling backwards faster than this (m/s), or holding reverse near standstill, steers like a car "
            + "backing up: the rear swings toward the pressed side.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _reverseSteerSpeed = 0.3f;

        [Tooltip("Half-life (s) of the blend between forward and backing-up steering, so the switch never snaps.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _steerDirectionHalfLife = 0.12f;

        [Tooltip("Fraction of the turn rate available in the air. Small: hops are about floating, not stunts.")]
        [Range(0f, 1f)]
        [SerializeField] private float _airTurnFactor = 0.3f;

        public float SteerRiseHalfLife => _steerRiseHalfLife;

        public float SteerReturnHalfLife => _steerReturnHalfLife;

        public float PivotTurnRate => _pivotTurnRate;

        public float PeakTurnRate => _peakTurnRate;

        public float PeakTurnSpeed => _peakTurnSpeed;

        public float TopSpeedTurnRate => _topSpeedTurnRate;

        public float ReverseSteerSpeed => _reverseSteerSpeed;

        public float SteerDirectionHalfLife => _steerDirectionHalfLife;

        public float AirTurnFactor => _airTurnFactor;
    }
}

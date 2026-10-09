using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The Boost Coils (docs/features/M3-11): holding Drive forward at cruise on open, flat-ish ground with the wheel
    /// near straight raises the top speed a little, eased in and out. Never a race: a gentle extra glide.
    /// </summary>
    [Serializable]
    public sealed class BoostSettings
    {
        [Tooltip("Extra top speed (m/s) at a full boost.")]
        [Range(0f, 5f)]
        [SerializeField] private float _extraSpeed = 1.6f;

        [Tooltip("Forward throttle (0..1) the player must hold for the boost.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _throttle = 0.9f;

        [Tooltip("The boost needs 07 already cruising: at least this fraction of the normal top speed.")]
        [Range(0f, 1f)]
        [SerializeField] private float _cruiseFraction = 0.85f;

        [Tooltip("Steering (0..1) above this is a turn, not open road: no boost.")]
        [Range(0f, 1f)]
        [SerializeField] private float _maxSteer = 0.35f;

        [Tooltip("Ground steeper than this (deg) is not flat-ish: no boost.")]
        [Range(0f, 30f)]
        [SerializeField] private float _maxSlope = 8f;

        [Tooltip("Seconds the conditions must fail before the boost lets go (no flicker over small bumps).")]
        [Range(0f, 2f)]
        [SerializeField] private float _releaseDelay = 0.35f;

        [Tooltip("Half-life (s) of the boost easing in.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _riseHalfLife = 0.9f;

        [Tooltip("Half-life (s) of the boost easing out.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _fallHalfLife = 0.45f;

        public float ExtraSpeed => _extraSpeed;

        public float Throttle => _throttle;

        public float CruiseFraction => _cruiseFraction;

        public float MaxSteer => _maxSteer;

        public float MaxSlope => _maxSlope;

        public float ReleaseDelay => _releaseDelay;

        public float RiseHalfLife => _riseHalfLife;

        public float FallHalfLife => _fallHalfLife;
    }
}

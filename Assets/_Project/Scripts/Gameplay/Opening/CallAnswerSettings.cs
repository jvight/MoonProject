using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The answer to the call from the sky (docs/design/opening-v3-purpose.md R8, <see cref="CallAnswer"/>): where 07
    /// may transmit, how long it holds, and how the pulse of the old network runs out to relay.1 and the Peak.
    /// </summary>
    [Serializable]
    public sealed class CallAnswerSettings
    {
        [Tooltip("Metres from the mast's foot within which 07 may hold to transmit.")]
        [Range(2f, 20f)]
        [SerializeField] private float _transmitRadius = 9f;

        [Tooltip("Ground speed (m/s) at or under which 07 counts as standing at the mast.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stillSpeed = 0.4f;

        [Tooltip("Seconds of holding E at the mast before the transmission goes out.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _holdSeconds = 2.5f;

        [Tooltip("How many times faster the hold's progress drains when E is let go than it fills.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _holdDrainRate = 2f;

        [Tooltip("Metres a second the network pulse runs along its way; its seconds are the way over this, within the " +
                 "bounds below.")]
        [Range(10f, 400f)]
        [SerializeField] private float _pulseSpeed = 90f;

        [Tooltip("Fewest seconds a leg of the pulse takes.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _minLegSeconds = 3f;

        [Tooltip("Most seconds a leg of the pulse takes.")]
        [Range(2f, 20f)]
        [SerializeField] private float _maxLegSeconds = 9f;

        [Tooltip("Seconds after relay.1 answers before the pulse runs on to the Peak's dish.")]
        [Range(0f, 5f)]
        [SerializeField] private float _routeDelaySeconds = 1.5f;

        [Tooltip("Metres above the summit's ground the dish's glint shows (the dish stands here).")]
        [Range(0f, 20f)]
        [SerializeField] private float _dishHeight = 3f;

        public float TransmitRadius => _transmitRadius;
        public float StillSpeed => _stillSpeed;
        public float HoldSeconds => _holdSeconds;
        public float HoldDrainRate => _holdDrainRate;
        public float PulseSpeed => _pulseSpeed;
        public float MinLegSeconds => _minLegSeconds;
        public float MaxLegSeconds => Mathf.Max(_minLegSeconds, _maxLegSeconds);
        public float RouteDelaySeconds => _routeDelaySeconds;
        public float DishHeight => _dishHeight;

        /// <summary>Seconds a pulse leg over <paramref name="metres"/> takes.</summary>
        public float LegSeconds(float metres)
        {
            return Mathf.Clamp(metres / _pulseSpeed, _minLegSeconds, MaxLegSeconds);
        }
    }
}

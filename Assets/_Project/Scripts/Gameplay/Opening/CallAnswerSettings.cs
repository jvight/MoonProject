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

        [Header("The pulse and the glint (what the player sees)")]
        [Tooltip("Metres above the mast's foot where the pulse starts (the top of the mast).")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _mastTopHeight = 2.8f;

        [Tooltip("Metres above relay.1's ground where the pulse ends and its lamp stands.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _relayLampHeight = 3.5f;

        [Tooltip("How high the pulse arcs, as a share of the way it runs.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _arcRise = 0.1f;

        [Tooltip("The most metres the pulse's arc rises.")]
        [Range(1f, 120f)]
        [SerializeField] private float _arcMaxRise = 35f;

        [Tooltip("Share of the way the pulse's bright tail trails behind its head.")]
        [Range(0.01f, 0.4f)]
        [SerializeField] private float _tailShare = 0.12f;

        [Tooltip("Width (m) of the pulse's streak.")]
        [Range(0.05f, 6f)]
        [SerializeField] private float _pulseWidth = 0.9f;

        [Tooltip("The pulse's colour: the cool blue-white of the old network (HDR).")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _pulseColor = new Color(0.5f, 0.95f, 1.6f, 1f);

        [Tooltip("Radii (m) the glint on the Peak's dish grows from and to.")]
        [SerializeField] private Vector2 _glintRadius = new Vector2(4f, 40f);

        [Tooltip("Brightness of the glint on the Peak's dish.")]
        [Range(0.2f, 8f)]
        [SerializeField] private float _glintIntensity = 3f;

        [Tooltip("Seconds the glint on the dish takes to flare and fade.")]
        [Range(0.3f, 5f)]
        [SerializeField] private float _glintSeconds = 1.8f;

        [Header("The release (R9)")]
        [Tooltip("Seconds 07 looks at each of the three lures from the rise: relay.1, the Peak's dish, Kestrel-3.")]
        [Range(1f, 10f)]
        [SerializeField] private float _viewSecondsEach = 3.5f;

        [Tooltip("Seconds between the range ring going and the view beginning.")]
        [Range(0f, 5f)]
        [SerializeField] private float _releaseGapSeconds = 1.2f;

        public float ViewSecondsEach => _viewSecondsEach;
        public float ReleaseGapSeconds => _releaseGapSeconds;
        public float TransmitRadius => _transmitRadius;
        public float MastTopHeight => _mastTopHeight;
        public float RelayLampHeight => _relayLampHeight;
        public float ArcRise => _arcRise;
        public float ArcMaxRise => _arcMaxRise;
        public float TailShare => _tailShare;
        public float PulseWidth => _pulseWidth;
        public Color PulseColor => _pulseColor;
        public Vector2 GlintRadius => _glintRadius;
        public float GlintIntensity => _glintIntensity;
        public float GlintSeconds => _glintSeconds;
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

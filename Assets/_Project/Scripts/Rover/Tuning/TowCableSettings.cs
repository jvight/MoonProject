using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's tow cable as seen (docs/design/opening-v3-plan.md 3.2b, <see cref="RoverTowCable"/>): a plain dark rope from
    /// the winch to the load's hitch with a small clamp at its end, sagging when slack and straight when taut, and the
    /// winch drum that turns as it pays out and takes in. Mechanical: no glow, no sparkle.
    /// </summary>
    [Serializable]
    public sealed class TowCableSettings
    {
        [Tooltip("Points along the rope: enough for a smooth sag.")]
        [Range(4, 48)]
        [SerializeField] private int _points = 24;

        [Tooltip("Width (m) of the rope.")]
        [Range(0.01f, 0.2f)]
        [SerializeField] private float _width = 0.04f;

        [Tooltip("Length (m) of the clamp at the load's hitch.")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _hookLength = 0.28f;

        [Tooltip("Width (m) of the clamp at the load's hitch.")]
        [Range(0.02f, 0.4f)]
        [SerializeField] private float _hookWidth = 0.11f;

        [Tooltip("Metres the lowest point of a slack rope may sag at most: a rope never trails further than this.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _maxSag = 2.5f;

        [Tooltip("Metres the rope keeps above the ground, so a slack rope lies on it and never sinks into it.")]
        [Range(0.005f, 0.3f)]
        [SerializeField] private float _groundGap = 0.04f;

        [Tooltip("Metres of rope that go onto or off the winch drum per full turn of it.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _metresPerTurn = 1.2f;

        [Tooltip("Seconds the rope takes to wind back in to the winch once the load is let go.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _retractSeconds = 0.9f;

        public int Points => _points;
        public float Width => _width;
        public float HookLength => _hookLength;
        public float HookWidth => _hookWidth;
        public float MaxSag => _maxSag;
        public float GroundGap => _groundGap;
        public float MetresPerTurn => _metresPerTurn;
        public float RetractSeconds => _retractSeconds;
    }
}

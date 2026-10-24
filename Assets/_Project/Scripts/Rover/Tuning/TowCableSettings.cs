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

        [Header("Seen from far")]
        [Tooltip("Fewest pixels (at a 1440p screen) the rope is drawn across at any distance: its real width up close, " +
                 "wider far off, so the load is seen to be towed by a line, not following by magic.")]
        [Range(1f, 8f)]
        [SerializeField] private float _minPixels = 4f;

        [Tooltip("Fewest pixels (at a 1440p screen) the clamp at the hitch is drawn across.")]
        [Range(2f, 14f)]
        [SerializeField] private float _hookMinPixels = 6f;

        [Tooltip("The rope's colour: weathered rubber, lifted well off the night regolith so it reads as a line.")]
        [SerializeField] private Color _ropeColor = new Color(0.78f, 0.72f, 0.62f, 1f);

        [Tooltip("How much the rubber's sheen catches the light (its top smoothness): a faint gleam along the rope, no glow.")]
        [Range(0.06f, 0.9f)]
        [SerializeField] private float _ropeSheen = 0.55f;

        [Tooltip("The pale thread laid along the rope (a chalky fibre, unlit): what keeps it a line against the night's " +
                 "regolith, where the dark rubber alone is lost in shadow. Its colour; no glow, nowhere near bloom.")]
        [SerializeField] private Color _threadColor = new Color(0.5f, 0.48f, 0.43f, 1f);

        [Tooltip("How wide the thread is, as a share of the rope's drawn width.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _threadShare = 0.55f;

        [Header("Skid dust under the load")]
        [Tooltip("Grains a second kicked up under a load dragging at full speed (ballistic: no smoke).")]
        [Range(0f, 200f)]
        [SerializeField] private float _skidMaxRate = 70f;

        [Tooltip("Speed (m/s) under which the load kicks up no dust.")]
        [Range(0f, 3f)]
        [SerializeField] private float _skidMinSpeed = 0.6f;

        [Tooltip("Speed (m/s) at which the load's dust is at its full rate.")]
        [Range(1f, 10f)]
        [SerializeField] private float _skidFullSpeed = 4f;

        [Tooltip("How far (m) past the hitch, along the rope, the dust leaves the ground: under the load's middle.")]
        [Range(0f, 4f)]
        [SerializeField] private float _skidPastHitch = 1.2f;

        [Tooltip("Seconds a grain of the load's dust flies.")]
        [Range(0.3f, 4f)]
        [SerializeField] private float _skidLifetime = 1.4f;

        [Tooltip("Size (m) of a grain.")]
        [Range(0.02f, 0.6f)]
        [SerializeField] private float _skidSize = 0.18f;

        [Tooltip("Speed (m/s) the grains leave at.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _skidSpeed = 1.6f;

        [Tooltip("Opacity of a grain.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _skidOpacity = 0.55f;

        [Tooltip("Gravity multiplier on the grains: the Moon's pull, so they fly in clean parabolas.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _skidGravity = 1f;

        public Color RopeColor => _ropeColor;
        public float RopeSheen => _ropeSheen;
        public Color ThreadColor => _threadColor;
        public float ThreadShare => _threadShare;
        public float MinPixels => _minPixels;
        public float HookMinPixels => _hookMinPixels;
        public float SkidMaxRate => _skidMaxRate;
        public float SkidMinSpeed => _skidMinSpeed;
        public float SkidFullSpeed => _skidFullSpeed;
        public float SkidPastHitch => _skidPastHitch;
        public float SkidLifetime => _skidLifetime;
        public float SkidSize => _skidSize;
        public float SkidSpeed => _skidSpeed;
        public float SkidOpacity => _skidOpacity;
        public float SkidGravity => _skidGravity;
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

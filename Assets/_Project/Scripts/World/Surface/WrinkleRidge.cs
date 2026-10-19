using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One mare wrinkle ridge on the basin floor: a long, low, sinuous rise, a broad arch with a narrower crest along
    /// it, tapering out at both ends. It catches raking light at golden hour.
    /// </summary>
    [Serializable]
    public struct WrinkleRidge
    {
        [Tooltip("Where the ridge starts (world XZ, metres).")]
        [SerializeField] private Vector2 _start;

        [Tooltip("Direction the ridge runs, degrees clockwise from +Z.")]
        [Range(0f, 360f)]
        [SerializeField] private float _bearing;

        [Tooltip("Length of the ridge, metres.")]
        [Range(50f, 600f)]
        [SerializeField] private float _length;

        [Tooltip("Height of the broad arch, metres.")]
        [Range(0f, 3f)]
        [SerializeField] private float _archHeight;

        [Tooltip("Half width of the broad arch, metres: wide enough that its flanks stay drivable.")]
        [Range(5f, 40f)]
        [SerializeField] private float _archHalfWidth;

        [Tooltip("Height of the narrower crest on the arch, metres.")]
        [Range(0f, 2f)]
        [SerializeField] private float _crestHeight;

        [Tooltip("Half width of the crest, metres.")]
        [Range(2f, 15f)]
        [SerializeField] private float _crestHalfWidth;

        [Tooltip("Half width of the crest's steep side, metres: the lee face away from the golden-hour Sun, steep enough " +
            "to fall into shade (still drivable) so the ridge reads in raking light.")]
        [Range(2f, 15f)]
        [SerializeField] private float _crestSteepHalfWidth;

        [Tooltip("The steep lee face lies to the left of the ridge's run (else to its right).")]
        [SerializeField] private bool _steepToLeft;

        [Tooltip("How far the ridge's line wanders to either side, metres.")]
        [Range(0f, 40f)]
        [SerializeField] private float _sinuosity;

        [Tooltip("Length of one wander of the ridge's line, metres.")]
        [Range(20f, 300f)]
        [SerializeField] private float _sinuosityWavelength;

        [Tooltip("Length over which the ridge rises from and sinks back into the plain at its ends, metres.")]
        [Range(5f, 150f)]
        [SerializeField] private float _taper;

        public WrinkleRidge(Vector2 start, float bearing, float length, float archHeight, float archHalfWidth,
            float crestHeight, float crestHalfWidth, float crestSteepHalfWidth, bool steepToLeft, float sinuosity,
            float sinuosityWavelength, float taper)
        {
            _crestSteepHalfWidth = crestSteepHalfWidth;
            _steepToLeft = steepToLeft;
            _start = start;
            _bearing = bearing;
            _length = length;
            _archHeight = archHeight;
            _archHalfWidth = archHalfWidth;
            _crestHeight = crestHeight;
            _crestHalfWidth = crestHalfWidth;
            _sinuosity = sinuosity;
            _sinuosityWavelength = sinuosityWavelength;
            _taper = taper;
        }

        public Vector2 Start => _start;
        public float Bearing => _bearing;
        public float Length => _length;
        public float ArchHeight => _archHeight;
        public float ArchHalfWidth => _archHalfWidth;
        public float CrestHeight => _crestHeight;
        public float CrestHalfWidth => _crestHalfWidth;
        public float CrestSteepHalfWidth => _crestSteepHalfWidth;
        public bool SteepToLeft => _steepToLeft;
        public float Sinuosity => _sinuosity;
        public float SinuosityWavelength => _sinuosityWavelength;
        public float Taper => _taper;
    }
}

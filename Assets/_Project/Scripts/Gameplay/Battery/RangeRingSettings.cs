using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The range ring (<see cref="RangeRingModel"/>, <see cref="RangeRingField"/>): how finely it is worked out, how far
    /// it looks, and how it shows itself on the ground.
    /// </summary>
    [Serializable]
    public sealed class RangeRingSettings
    {
        [Tooltip("Points in the ring's polygon, evenly round 07.")]
        [Range(16, 180)]
        [SerializeField] private int _points = 72;

        [Tooltip("Metres between the ring's samples going outward on each bearing.")]
        [Range(2f, 30f)]
        [SerializeField] private float _stepMetres = 6f;

        [Tooltip("Metres between the terrain samples when costing a way back to a charge point.")]
        [Range(4f, 40f)]
        [SerializeField] private float _pathStepMetres = 12f;

        [Tooltip("The furthest the ring looks from 07 (m), so a full battery on open ground ends somewhere.")]
        [Range(50f, 2000f)]
        [SerializeField] private float _maxRangeMetres = 700f;

        [Tooltip("Seconds the faint ring stays on show (R3), fading in and out.")]
        [Range(1f, 10f)]
        [SerializeField] private float _faintSeconds = 3f;

        [Tooltip("Seconds the full ring stays on show (R9), fading in and out.")]
        [Range(2f, 20f)]
        [SerializeField] private float _fullSeconds = 7f;

        [Tooltip("Seconds the ring takes to fade in and out.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _fadeSeconds = 0.9f;

        [Tooltip("Strength (0..1) of the faint ring's line.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _faintStrength = 0.3f;

        [Tooltip("Strength (0..1) of the full ring's line.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _fullStrength = 0.85f;

        [Tooltip("Width (m) of the ring's line on the ground: wide, because the edge lies hundreds of metres away.")]
        [Range(0.05f, 10f)]
        [SerializeField] private float _lineWidth = 4f;

        [Tooltip("Metres the line floats above the ground, so it never sinks into it.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _lineLift = 0.25f;

        [Tooltip("The ring's colour: the cool blue-white of the opening's guidance light.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _color = new Color(0.55f, 0.9f, 1.4f, 1f);

        public int Points => _points;
        public float StepMetres => _stepMetres;
        public float PathStepMetres => _pathStepMetres;
        public float MaxRangeMetres => _maxRangeMetres;
        public float FaintSeconds => _faintSeconds;
        public float FullSeconds => _fullSeconds;
        public float FadeSeconds => _fadeSeconds;
        public float FaintStrength => _faintStrength;
        public float FullStrength => _fullStrength;
        public float LineWidth => _lineWidth;
        public float LineLift => _lineLift;
        public Color Color => _color;
    }
}

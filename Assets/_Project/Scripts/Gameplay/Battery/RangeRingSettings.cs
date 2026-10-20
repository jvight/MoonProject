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

        [Tooltip("Seconds the faint ring stays on show (R3): drawn out, held, then faded.")]
        [Range(2f, 12f)]
        [SerializeField] private float _faintSeconds = 4.5f;

        [Tooltip("Seconds the full ring stays on show (R9): drawn out, held, then faded.")]
        [Range(3f, 20f)]
        [SerializeField] private float _fullSeconds = 8f;

        [Tooltip("Seconds the line takes to come up to its strength as it starts to draw.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _fadeInSeconds = 0.8f;

        [Tooltip("Seconds the line takes to fade away at the end: slow, so it is never switched off.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _fadeOutSeconds = 3f;

        [Tooltip("Seconds the ring takes to draw outward from 07 to its edge.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _revealSeconds = 2.2f;

        [Tooltip("Strength (0..1) of the faint ring's line.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _faintStrength = 0.1f;

        [Tooltip("Strength (0..1) of the full ring's line: low, so it stays under bloom and under relay.1's lamp.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _fullStrength = 0.3f;

        [Tooltip("Widest the line is on screen (px), near enough to see: a drawn line, never a band.")]
        [Range(1f, 8f)]
        [SerializeField] private float _maxPixels = 3f;

        [Tooltip("Thinnest the line gets on screen (px) with distance.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _minPixels = 1f;

        [Tooltip("Distance (m) out to which the line holds its widest; beyond it, it thins with distance.")]
        [Range(10f, 300f)]
        [SerializeField] private float _fullPixelDistance = 80f;

        [Tooltip("Vertices the line has between two of the ring's points, each set on the ground: a thin line cut straight " +
                 "between far-apart points would dip under the terrain between them and break into dashes.")]
        [Range(1, 8)]
        [SerializeField] private int _subdivisions = 4;

        [Tooltip("Metres the line floats above the ground, so it never sinks into it.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _lineLift = 0.25f;

        [Tooltip("Extra metres the line floats per metre it lies from 07: far out it is seen almost edge-on, where every " +
                 "rise of the ground would hide a thin line and break it into dashes.")]
        [Range(0f, 0.02f)]
        [SerializeField] private float _liftPerMetre = 0.004f;

        [Tooltip("The ring's colour: the opening's cool guidance light, desaturated toward moonlit white.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _color = new Color(0.8f, 0.9f, 1f, 1f);

        public int Points => _points;
        public float StepMetres => _stepMetres;
        public float PathStepMetres => _pathStepMetres;
        public float MaxRangeMetres => _maxRangeMetres;
        public float FaintSeconds => _faintSeconds;
        public float FullSeconds => _fullSeconds;
        public float FadeInSeconds => _fadeInSeconds;
        public float FadeOutSeconds => _fadeOutSeconds;
        public float RevealSeconds => _revealSeconds;
        public float FaintStrength => _faintStrength;
        public float FullStrength => _fullStrength;
        public float MaxPixels => _maxPixels;
        public float MinPixels => _minPixels;
        public float FullPixelDistance => _fullPixelDistance;
        public float LineLift => _lineLift;
        public int Subdivisions => _subdivisions;
        public float LiftPerMetre => _liftPerMetre;
        public Color Color => _color;
    }
}

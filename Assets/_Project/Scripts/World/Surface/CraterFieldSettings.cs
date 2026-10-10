using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The basin's field of small craters (M3-18 V4, the ground's middle scale): a few hundred crisp dents on the open
    /// plains, gathered in patchy fields with bare plains between (design ruling 9: patchy, never confetti).
    /// </summary>
    [Serializable]
    public sealed class CraterFieldSettings
    {
        [Tooltip("Grid cell of the field, metres: at most one small crater per cell.")]
        [Range(6f, 60f)]
        [SerializeField] private float _cellSize = 18f;

        [Tooltip("Share of cells that hold a crater inside the densest fields (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _density = 0.7f;

        [Tooltip("Size of the crater fields and the bare plains between them, metres.")]
        [Range(40f, 600f)]
        [SerializeField] private float _fieldWavelength = 170f;

        [Tooltip("Field noise above which craters gather (-1..1): higher leaves more bare plain.")]
        [Range(-1f, 1f)]
        [SerializeField] private float _fieldThreshold = -0.05f;

        [Tooltip("Crater radius range, metres (x = min, y = max).")]
        [SerializeField] private Vector2 _radius = new Vector2(2.5f, 6f);

        [Tooltip("Size distribution: radius = min + (max - min) * u^this, so higher gives many small and few large.")]
        [Range(1f, 5f)]
        [SerializeField] private float _sizePower = 2.2f;

        [Tooltip("Bowl depth as a fraction of the radius. Shallow: 07 rolls through them.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _depth = 0.045f;

        [Tooltip("Raised rim height as a fraction of the radius: one crisp lit facet.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float _rimHeight = 0.015f;

        [Tooltip("Width of the raised rim outside the bowl, as a fraction of the radius.")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _rimWidth = 0.7f;

        [Tooltip("Bowl paint weight at a crater's centre (0..1). Under the terrain painter's crater floor, so the whole " +
            "small bowl takes the shaded wall tone: a dark floor under a light rim.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bowlShade = 0.65f;

        [Tooltip("Clear ground (metres) kept beyond the base pad and around every world anchor's radius.")]
        [Range(0f, 40f)]
        [SerializeField] private float _clearance = 6f;

        public float CellSize => _cellSize;
        public float Density => _density;
        public float FieldWavelength => _fieldWavelength;
        public float FieldThreshold => _fieldThreshold;
        public Vector2 Radius => _radius;
        public float SizePower => _sizePower;
        public float Depth => _depth;
        public float RimHeight => _rimHeight;
        public float RimWidth => _rimWidth;
        public float BowlShade => _bowlShade;
        public float Clearance => _clearance;

        /// <summary>Null when the settings are usable, otherwise what is wrong.</summary>
        public string Validate()
        {
            if (_radius.x <= 0f || _radius.x > _radius.y)
            {
                return "Crater field radius range needs 0 < min <= max.";
            }

            return _radius.y * (1f + _rimWidth) * 2f > _cellSize * 2f
                ? "Crater field cells are too small for the largest crater and its rim (they would reach two cells out)."
                : null;
        }
    }
}

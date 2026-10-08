using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Kestrel-3, the fallen relay satellite (docs/features/M3-13): a shallow impact crater, the basin's big
    /// landmark, with a long furrow trailing back along its fall line toward the base, both dusted dark with scorch.
    /// Every slope stays a gentle drive (ruling 10: never a trap).
    /// </summary>
    [Serializable]
    public sealed class KestrelSettings
    {
        [Tooltip("Bearing of the crater from the base, degrees: in the spawn first frame, left of The Peak.")]
        [Range(0f, 360f)]
        [SerializeField] private float _bearing = 356f;

        [Tooltip("Distance of the crater from the base, metres.")]
        [Range(100f, 280f)]
        [SerializeField] private float _distance = 185f;

        [Tooltip("Bearing Kestrel-3 was travelling when it came down, degrees: the furrow trails back the other way.")]
        [Range(0f, 360f)]
        [SerializeField] private float _fallBearing = 20f;

        [Tooltip("Radius of the crater's flat floor, where the wreck lies, metres.")]
        [Range(4f, 14f)]
        [SerializeField] private float _floorRadius = 8f;

        [Tooltip("Radius of the crater's rim crest, metres.")]
        [Range(8f, 20f)]
        [SerializeField] private float _rimRadius = 13f;

        [Tooltip("Depth of the crater floor below the ground around it, metres.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _depth = 0.6f;

        [Tooltip("Height of the rim crest above the ground around it, metres.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rimHeight = 0.3f;

        [Tooltip("Rounded length at each end of the crater wall, metres: the rest is a straight, gentle slope.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _wallEase = 1.25f;

        [Tooltip("Width of the rim's outer flank, metres.")]
        [Range(2f, 15f)]
        [SerializeField] private float _rimFlank = 6f;

        [Header("Furrow")]
        [Tooltip("Length of the furrow trailing back from the rim crest, metres.")]
        [Range(40f, 140f)]
        [SerializeField] private float _furrowLength = 90f;

        [Tooltip("Half-width of the furrow where it meets the crater, metres.")]
        [Range(2f, 8f)]
        [SerializeField] private float _furrowHalfWidth = 6f;

        [Tooltip("Half-width of the furrow at its far, faint end, metres.")]
        [Range(1f, 6f)]
        [SerializeField] private float _furrowTailHalfWidth = 2f;

        [Tooltip("Depth of the furrow where it meets the crater, metres; it fades out toward its far end.")]
        [Range(0.1f, 1.2f)]
        [SerializeField] private float _furrowDepth = 0.3f;

        [Tooltip("Height of the low berms of pushed-up dust along the furrow's sides, metres.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _bermHeight = 0.15f;

        [Tooltip("Half-width of each berm, metres.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _bermHalfWidth = 1.5f;

        [Tooltip("Soft gouges along the furrow where pieces bounced, count.")]
        [Range(0, 8)]
        [SerializeField] private int _gougeCount = 4;

        [Tooltip("Depth of a gouge, metres.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _gougeDepth = 0.2f;

        [Tooltip("Half-length of a gouge along the furrow, metres.")]
        [Range(1f, 10f)]
        [SerializeField] private float _gougeHalfLength = 5f;

        [Tooltip("Half-width of a gouge, metres.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _gougeHalfWidth = 2f;

        [Header("Scorch")]
        [Tooltip("How far the scorched dust fades out past the rim crest and the furrow's edges, metres: a soft " +
            "edge several facets wide, never a paper-cut step (ruling 9).")]
        [Range(4f, 30f)]
        [SerializeField] private float _scorchReach = 8f;

        [Tooltip("Margin around the crater and the furrow inside which no older crater survives, metres.")]
        [Range(0f, 20f)]
        [SerializeField] private float _clearMargin = 4f;

        public float Bearing => _bearing;
        public float Distance => _distance;
        public float FallBearing => _fallBearing;
        public float FloorRadius => _floorRadius;
        public float RimRadius => _rimRadius;
        public float Depth => _depth;
        public float RimHeight => _rimHeight;
        public float WallEase => _wallEase;
        public float RimFlank => _rimFlank;
        public float FurrowLength => _furrowLength;
        public float FurrowHalfWidth => _furrowHalfWidth;
        public float FurrowTailHalfWidth => _furrowTailHalfWidth;
        public float FurrowDepth => _furrowDepth;
        public float BermHeight => _bermHeight;
        public float BermHalfWidth => _bermHalfWidth;
        public int GougeCount => _gougeCount;
        public float GougeDepth => _gougeDepth;
        public float GougeHalfLength => _gougeHalfLength;
        public float GougeHalfWidth => _gougeHalfWidth;
        public float ScorchReach => _scorchReach;
        public float ClearMargin => _clearMargin;

        /// <summary>Returns null when the settings describe a valid impact, otherwise the problem.</summary>
        public string Validate()
        {
            if (_rimRadius - _floorRadius <= 2f * _wallEase)
            {
                return "Kestrel: the crater wall must be longer than its two rounded ends.";
            }

            if (_furrowTailHalfWidth > _furrowHalfWidth)
            {
                return "Kestrel: the furrow narrows toward its tail; its tail half-width must not exceed its head's.";
            }

            if (_bermHalfWidth >= _furrowTailHalfWidth)
            {
                return "Kestrel: a berm must be narrower than the furrow's tail half-width, so it never reaches the " +
                    "furrow's centre line.";
            }

            return null;
        }
    }
}

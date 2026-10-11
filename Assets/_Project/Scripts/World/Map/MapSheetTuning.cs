using UnityEngine;

namespace MoonProject.World.Map
{
    /// <summary>
    /// How the crew map's sheet is traced from the generated surface (docs/features/M3-07): its extent, the contour
    /// interval and how finely each kind of line is followed. Read by the map sheet builder at edit time only.
    /// </summary>
    public sealed class MapSheetTuning : ScriptableObject
    {
        [Tooltip("Metres from the basin's centre to the paper's edge: the rim crest and The Peak's flanks fit inside.")]
        [Min(50f)]
        [SerializeField] private float _extent = 520f;

        [Tooltip("Metres between two height samples when tracing the contours.")]
        [Range(1f, 10f)]
        [SerializeField] private float _gridStep = 4f;

        [Tooltip("Metres of height between two contour lines.")]
        [Range(1f, 20f)]
        [SerializeField] private float _contourInterval = 5f;

        [Tooltip("Metres a thinned line may stray from the traced one (fewer points, same shape).")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _simplifyTolerance = 0.75f;

        [Tooltip("Closed contour loops shorter than this many metres (the floor's dune blobs) are left off the paper.")]
        [Min(0f)]
        [SerializeField] private float _minLoopLength = 40f;

        [Tooltip("Metres between two samples when tracing the canyon's walls and the chasm.")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _canyonGridStep = 1.5f;

        [Tooltip("Metres in from a corridor's basin end where the traced floor edge may cross the mouth.")]
        [Min(0f)]
        [SerializeField] private float _canyonMouthZone = 30f;

        [Tooltip("Within the mouth zone, points closer to the corridor's centre line than this fraction of its mouth " +
            "half-width are cut, so no wall line closes a mouth.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _canyonMouthCut = 0.75f;

        [Tooltip("Bearings the rim crest is searched along, all the way round.")]
        [Range(36, 1440)]
        [SerializeField] private int _crestBearings = 360;

        [Tooltip("Metres either side of the nominal crest radius searched for the crest's highest point.")]
        [Min(1f)]
        [SerializeField] private float _crestSearchBand = 60f;

        [Tooltip("Bearings either side averaged into each crest point: a pencil line along the ridge, not its crags.")]
        [Range(0, 16)]
        [SerializeField] private int _crestSmoothing = 4;

        [Tooltip("Metres between two samples along a crest search.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _crestSearchStep = 2f;

        [Tooltip("Craters smaller than this radius in metres get no rim on the paper.")]
        [Min(0f)]
        [SerializeField] private float _craterMinRadius = 8f;

        [Tooltip("Points around a crater's rim.")]
        [Range(8, 96)]
        [SerializeField] private int _craterSegments = 40;

        public float Extent => _extent;

        public float GridStep => _gridStep;

        public float ContourInterval => _contourInterval;

        public float SimplifyTolerance => _simplifyTolerance;

        public float MinLoopLength => _minLoopLength;

        public float CanyonGridStep => _canyonGridStep;

        public float CanyonMouthZone => _canyonMouthZone;

        public float CanyonMouthCut => _canyonMouthCut;

        public int CrestBearings => _crestBearings;

        public float CrestSearchBand => _crestSearchBand;

        public int CrestSmoothing => _crestSmoothing;

        public float CrestSearchStep => _crestSearchStep;

        public float CraterMinRadius => _craterMinRadius;

        public int CraterSegments => _craterSegments;
    }
}

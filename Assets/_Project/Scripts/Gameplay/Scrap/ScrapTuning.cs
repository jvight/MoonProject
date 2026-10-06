using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Scrap: where the clusters lie, how pieces idle, how they spiral into 07 and how the pickup melody chains.
    /// Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class ScrapTuning : ScriptableObject
    {
        [Header("Placement")]
        [Tooltip("Seed of the scrap field: the same seed and world always give the same field.")]
        [SerializeField] private int _seed = 7;

        [Tooltip("Steepest ground (degrees) scrap may lie on.")]
        [Range(2f, 30f)] [SerializeField] private float _maxSlopeDegrees = 14f;

        [Tooltip("Metres kept between scrap and the edge of the drivable floor.")]
        [Range(0f, 60f)] [SerializeField] private float _edgeMargin = 8f;

        [Tooltip("No scrap closer to the base centre than this (m): the lander's yard stays clean.")]
        [Range(0f, 40f)] [SerializeField] private float _baseClearRadius = 11f;

        [Tooltip("Ring (m from the base centre) of the welcome clusters around home.")]
        [SerializeField] private Vector2 _baseRing = new Vector2(15f, 24f);

        [Tooltip("Welcome clusters around home.")]
        [Range(0, 16)] [SerializeField] private int _baseRingClusters = 5;

        [Tooltip("Trails of clusters lead from home toward each relic and The Peak, starting this far out (m).")]
        [Range(10f, 120f)] [SerializeField] private float _trailStart = 34f;

        [Tooltip("Metres between clusters along a trail (short enough that driving it chains the melody).")]
        [Range(6f, 60f)] [SerializeField] private float _trailSpacing = 16f;

        [Tooltip("Sideways wander (m) of trail clusters, so trails read as paths, not lines.")]
        [Range(0f, 20f)] [SerializeField] private float _trailJitter = 4f;

        [Tooltip("Ring (m from a relic site) where clusters gather around points of interest.")]
        [SerializeField] private Vector2 _poiRing = new Vector2(7f, 15f);

        [Tooltip("Clusters around each relic site.")]
        [Range(0, 8)] [SerializeField] private int _poiClusters = 2;

        [Tooltip("Lone clusters scattered over the rest of the basin.")]
        [Range(0, 200)] [SerializeField] private int _fillClusters = 18;

        [Tooltip("Attempts per lone cluster before giving up on it.")]
        [Range(1, 200)] [SerializeField] private int _fillAttempts = 24;

        [Tooltip("Minimum metres between cluster centres.")]
        [Range(2f, 40f)] [SerializeField] private float _minClusterSpacing = 7f;

        [Tooltip("No scrap within this many metres of a relic's dig spot.")]
        [Range(0f, 15f)] [SerializeField] private float _siteClearRadius = 4f;

        [Tooltip("Radius range (m) of one cluster.")]
        [SerializeField] private Vector2 _clusterRadius = new Vector2(1.2f, 2.6f);

        [Tooltip("Pieces per cluster (min, max).")]
        [SerializeField] private Vector2Int _piecesPerCluster = new Vector2Int(3, 5);

        [Tooltip("The basin always holds at least this much scrap value (no grind: at least twice what the slice's " +
                 "upgrades cost); lone clusters are added until it does.")]
        [Range(0, 5000)] [SerializeField] private int _minTotalValue = 300;

        [Tooltip("Extra lone-cluster attempts allowed to reach the minimum total value.")]
        [Range(0, 5000)] [SerializeField] private int _topUpAttempts = 600;

        [Tooltip("Minimum metres between pieces of a cluster.")]
        [Range(0.2f, 3f)] [SerializeField] private float _minPieceSpacing = 0.75f;

        [Tooltip("Height (m) pieces float above the ground.")]
        [Range(0.1f, 2f)] [SerializeField] private float _hoverHeight = 0.45f;

        [Header("Idle")]
        [Tooltip("Bob amplitude (m) of a floating piece.")]
        [Range(0f, 0.5f)] [SerializeField] private float _bobAmplitude = 0.08f;

        [Tooltip("Bob frequency (Hz): slow, like breathing.")]
        [Range(0.05f, 2f)] [SerializeField] private float _bobFrequency = 0.35f;

        [Tooltip("Idle spin (degrees per second).")]
        [Range(0f, 180f)] [SerializeField] private float _spinSpeed = 25f;

        [Tooltip("Only pieces within this distance (m) of 07 animate; farther ones rest (the bob is invisible there).")]
        [Range(10f, 300f)] [SerializeField] private float _animateRadius = 70f;

        [Tooltip("07 glances at the nearest piece within this distance (m).")]
        [Range(0f, 40f)] [SerializeField] private float _glanceRadius = 9f;

        [Header("Magnet")]
        [Tooltip("Pieces within this distance (m) of 07 lift off and spiral to it.")]
        [Range(1f, 20f)] [SerializeField] private float _magnetRadius = 5.5f;

        [Tooltip("Flight time (s) of a piece right next to 07.")]
        [Range(0.1f, 3f)] [SerializeField] private float _flightBaseDuration = 0.6f;

        [Tooltip("Extra flight time (s) per metre of distance.")]
        [Range(0f, 0.5f)] [SerializeField] private float _flightSecondsPerMetre = 0.06f;

        [Tooltip("Height (m) a piece lifts during its flight.")]
        [Range(0f, 4f)] [SerializeField] private float _flightLift = 0.9f;

        [Tooltip("Radius (m) of the spiral around the flight path.")]
        [Range(0f, 3f)] [SerializeField] private float _spiralRadius = 0.6f;

        [Tooltip("Turns of the spiral during a flight.")]
        [Range(0f, 4f)] [SerializeField] private float _spiralTurns = 1.25f;

        [Tooltip("Scale of a piece as it reaches the cargo socket (it is drawn in).")]
        [Range(0.05f, 1f)] [SerializeField] private float _arrivalScale = 0.35f;

        [Header("Melody")]
        [Tooltip("Seconds within which the next pickup climbs one step of the pentatonic melody.")]
        [Range(0.2f, 6f)] [SerializeField] private float _comboWindow = 2f;

        [Tooltip("Minimum seconds between two pickups: a cluster plays as an arpeggio, never a chord.")]
        [Range(0f, 0.5f)] [SerializeField] private float _minPickupInterval = 0.11f;

        [Header("Pickup flash")]
        [Tooltip("Pooled flashes (enough for a quick arpeggio).")]
        [Range(1, 32)] [SerializeField] private int _flashPoolSize = 8;

        [Tooltip("Seconds a flash lasts.")]
        [Range(0.05f, 2f)] [SerializeField] private float _flashDuration = 0.4f;

        [Tooltip("Flash radius (m) at its start and end.")]
        [SerializeField] private Vector2 _flashRadius = new Vector2(0.12f, 0.55f);

        [Tooltip("Peak brightness of a flash (HDR, feeds bloom).")]
        [Range(0f, 10f)] [SerializeField] private float _flashIntensity = 2.5f;

        public int Seed => _seed;
        public float MaxSlopeDegrees => _maxSlopeDegrees;
        public float EdgeMargin => _edgeMargin;
        public float BaseClearRadius => _baseClearRadius;
        public Vector2 BaseRing => Ordered(_baseRing);
        public int BaseRingClusters => _baseRingClusters;
        public float TrailStart => _trailStart;
        public float TrailSpacing => _trailSpacing;
        public float TrailJitter => _trailJitter;
        public Vector2 PoiRing => Ordered(_poiRing);
        public int PoiClusters => _poiClusters;
        public int FillClusters => _fillClusters;
        public int FillAttempts => _fillAttempts;
        public float MinClusterSpacing => _minClusterSpacing;
        public float SiteClearRadius => _siteClearRadius;
        public Vector2 ClusterRadius => Ordered(_clusterRadius);

        public Vector2Int PiecesPerCluster => new Vector2Int(Mathf.Max(1, Mathf.Min(_piecesPerCluster.x,
            _piecesPerCluster.y)), Mathf.Max(1, Mathf.Max(_piecesPerCluster.x, _piecesPerCluster.y)));

        public int MinTotalValue => _minTotalValue;
        public int TopUpAttempts => _topUpAttempts;
        public float MinPieceSpacing => _minPieceSpacing;
        public float HoverHeight => _hoverHeight;
        public float BobAmplitude => _bobAmplitude;
        public float BobFrequency => _bobFrequency;
        public float SpinSpeed => _spinSpeed;
        public float AnimateRadius => _animateRadius;
        public float GlanceRadius => _glanceRadius;
        public float MagnetRadius => _magnetRadius;
        public float FlightBaseDuration => _flightBaseDuration;
        public float FlightSecondsPerMetre => _flightSecondsPerMetre;
        public float FlightLift => _flightLift;
        public float SpiralRadius => _spiralRadius;
        public float SpiralTurns => _spiralTurns;
        public float ArrivalScale => _arrivalScale;
        public float ComboWindow => _comboWindow;
        public float MinPickupInterval => _minPickupInterval;
        public int FlashPoolSize => _flashPoolSize;
        public float FlashDuration => _flashDuration;
        public Vector2 FlashRadius => _flashRadius;
        public float FlashIntensity => _flashIntensity;

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }
    }
}

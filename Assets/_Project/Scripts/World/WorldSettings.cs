using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Tuning root of the World domain: the world seed plus every number for the surface shape, the terrain mesh and
    /// its colours, the rock scatter, the sky, The Peak's beacon, the atmosphere and the post-processing look.
    /// Read-only at runtime.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/World Settings", fileName = "WorldSettings")]
    public sealed class WorldSettings : ScriptableObject
    {
        /// <summary>Seed of the shipped world; tests pin their expectations to it.</summary>
        public const int DefaultSeed = 1337;

        [Tooltip("Master seed: craters, noise, jitter and scatter all derive from it. Same seed = same world.")]
        [SerializeField] private int _seed = DefaultSeed;

        [Tooltip("Shape of the moon surface: basin, rim, The Peak, dunes, craters and play features.")]
        [SerializeField] private SurfaceSettings _surface = new SurfaceSettings();

        [Tooltip("How the surface is cut into chunk meshes and the backdrop ring.")]
        [SerializeField] private TerrainMeshSettings _mesh = new TerrainMeshSettings();

        [Tooltip("Which palette swatch each terrain face gets.")]
        [SerializeField] private TerrainPaintSettings _paint = new TerrainPaintSettings();

        [Tooltip("Where pebbles and boulders lie, and which lanes stay clear.")]
        [SerializeField] private ScatterSettings _scatter = new ScatterSettings();

        [Tooltip("The night sky: gradient, stars, milky way, shooting stars and Earth.")]
        [SerializeField] private SkySettings _sky = new SkySettings();

        [Tooltip("The slow red beacon on The Peak.")]
        [SerializeField] private BeaconSettings _beacon = new BeaconSettings();

        [Tooltip("Where the relay masts of the station-reach network stand (M3-06).")]
        [SerializeField] private RelaySettings _relays = new RelaySettings();

        [Tooltip("Earthlight, ambient and fog.")]
        [SerializeField] private AtmosphereSettings _atmosphere = new AtmosphereSettings();

        [Tooltip("How home's warm points wake with the base's power stages (M3-16).")]
        [SerializeField] private HomeLightingSettings _homeLighting = new HomeLightingSettings();

        [Tooltip("How the habitat lander's belly lift and the crew's lab glow (M3-19).")]
        [SerializeField] private LabLightingSettings _labLighting = new LabLightingSettings();

        [Tooltip("Camera look, written into the URP Volume profile by the World builder.")]
        [SerializeField] private PostProcessSettings _postProcessing = new PostProcessSettings();

        [Tooltip("Marks pressed into the dust: 07's worn tracks, the landing-blast scar, bootprints (M3-18 V4).")]
        [SerializeField] private GroundMarkSettings _groundMarks = new GroundMarkSettings();

        [Tooltip("Where the crew's remnants from Art's landmark kit stand (M3-18 L2, L4, L7).")]
        [SerializeField] private LandmarkSettings _landmarks = new LandmarkSettings();

        [Tooltip("The warm ring that rolls out over the ground when a reach grows (polish wave 1).")]
        [SerializeField] private ReachRingSettings _reachRing = new ReachRingSettings();

        [Tooltip("Kestrel-3's trail as a field of half-buried hull plates (0.5.5).")]
        [SerializeField] private ScrapFieldSettings _scrapField = new ScrapFieldSettings();

        [Tooltip("The picture through 07's own eye in the opening (docs/design/cutscenes.md §1).")]
        [SerializeField] private LensViewSettings _lensView = new LensViewSettings();

        [Tooltip("The warm pools on lit relay masts' charge rings (docs/features/M3-20).")]
        [SerializeField] private ChargerPoolSettings _chargerPools = new ChargerPoolSettings();

        [Tooltip("The handshake that runs lamp to lamp when a mast lights (north-star beat 7).")]
        [SerializeField] private NetworkPulseSettings _networkPulse = new NetworkPulseSettings();

        [Tooltip("The low circling Sun, the opening eclipse and the look C sunlit atmosphere (0.5.7 SKY-CYCLE).")]
        [SerializeField] private SkyCycleSettings _skyCycle = new SkyCycleSettings();

        public int Seed => _seed;

        public SurfaceSettings Surface => _surface;

        public TerrainMeshSettings Mesh => _mesh;

        public TerrainPaintSettings Paint => _paint;

        public ScatterSettings Scatter => _scatter;

        public SkySettings Sky => _sky;

        public BeaconSettings Beacon => _beacon;

        public RelaySettings Relays => _relays;

        public AtmosphereSettings Atmosphere => _atmosphere;

        public PostProcessSettings PostProcessing => _postProcessing;

        public HomeLightingSettings HomeLighting => _homeLighting;

        public LabLightingSettings LabLighting => _labLighting;

        public GroundMarkSettings GroundMarks => _groundMarks;

        public LandmarkSettings Landmarks => _landmarks;

        public ReachRingSettings ReachRing => _reachRing;

        public ScrapFieldSettings ScrapField => _scrapField;

        public LensViewSettings LensView => _lensView;

        public ChargerPoolSettings ChargerPools => _chargerPools;

        public NetworkPulseSettings NetworkPulse => _networkPulse;

        public SkyCycleSettings SkyCycle => _skyCycle;

        /// <summary>Builds the analytic surface for these settings (throws on invalid settings).</summary>
        public MoonSurface CreateSurface()
        {
            return new MoonSurface(_surface, _seed);
        }

        /// <summary>Returns null when every settings block is valid, otherwise the first problem found.</summary>
        public string Validate()
        {
            return _surface.Validate() ?? _mesh.Validate() ?? _scatter.Validate() ?? _relays.Validate()
                ?? _skyCycle.Validate();
        }

        private void OnValidate()
        {
            string error = Validate();
            if (error != null)
            {
                Debug.LogError($"{nameof(WorldSettings)} '{name}': {error}", this);
            }
        }
    }
}

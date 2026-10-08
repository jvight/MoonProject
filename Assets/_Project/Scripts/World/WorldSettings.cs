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

        [Tooltip("Camera look, written into the URP Volume profile by the World builder.")]
        [SerializeField] private PostProcessSettings _postProcessing = new PostProcessSettings();

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

        /// <summary>Builds the analytic surface for these settings (throws on invalid settings).</summary>
        public MoonSurface CreateSurface()
        {
            return new MoonSurface(_surface, _seed);
        }

        /// <summary>Returns null when every settings block is valid, otherwise the first problem found.</summary>
        public string Validate()
        {
            return _surface.Validate() ?? _mesh.Validate() ?? _scatter.Validate() ?? _relays.Validate();
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

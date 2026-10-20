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

        // The ruin piece that lies off the cluster on its own footprint.
        private const string FallenArrayRuin = "FallenSolarArray";

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

        [Tooltip("Where the old west relay mast (relay.1) and 06's spot (micro.06) stand.")]
        [SerializeField] private RelaySettings _relays = new RelaySettings();

        [Tooltip("Earthlight, ambient and fog.")]
        [SerializeField] private AtmosphereSettings _atmosphere = new AtmosphereSettings();

        [Tooltip("Camera look, written into the URP Volume profile by the World builder.")]
        [SerializeField] private PostProcessSettings _postProcessing = new PostProcessSettings();

        [Tooltip("Marks pressed into the dust: 07's worn tracks, the landing-blast scar, bootprints (M3-18 V4).")]
        [SerializeField] private GroundMarkSettings _groundMarks = new GroundMarkSettings();

        [Tooltip("Where the crew's remnants from Art's landmark kit stand (M3-18 L2, L4, L7).")]
        [SerializeField] private LandmarkSettings _landmarks = new LandmarkSettings();

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

        public GroundMarkSettings GroundMarks => _groundMarks;

        public LandmarkSettings Landmarks => _landmarks;

        public SkyCycleSettings SkyCycle => _skyCycle;

        /// <summary>Builds the analytic surface for these settings (throws on invalid settings).</summary>
        public MoonSurface CreateSurface()
        {
            return new MoonSurface(_surface, _seed);
        }

        /// <summary>Returns null when every settings block is valid, otherwise the first problem found.</summary>
        public string Validate()
        {
            return _surface.Validate() ?? _mesh.Validate() ?? _scatter.Validate()
                ?? _skyCycle.Validate() ?? FallenArrayOnItsSite() ?? CallPointUnderBloom()
                ?? RelayLampUnderBloom();
        }

        // The call's point of light must glow, never flare: its core and its ring together stay under the bloom.
        private string CallPointUnderBloom()
        {
            CallSkyPointSettings point = _sky.CallPoint;
            if (point.Peak + point.RingBrightness >= _postProcessing.BloomThreshold)
            {
                return "Sky: the call's point of light (peak + ring) reaches the bloom threshold and would flare.";
            }

            if (point.ToneOff >= point.ToneOn)
            {
                return "Sky: the call's point of light ends a tone (off) at or above where it begins one (on).";
            }

            return null;
        }

        // relay.1's lamp wears the module lamp's amber halo: at the top of a key its brightest channel stays under the
        // bloom, so it glows as a lamp and never flares.
        private string RelayLampUnderBloom()
        {
            Color amber = _landmarks.ModuleLamp.HaloColor;
            float brightest = Mathf.Max(amber.r, Mathf.Max(amber.g, amber.b));
            if (_landmarks.RelayLamp.HaloIntensity * brightest >= _postProcessing.BloomThreshold)
            {
                return "Landmarks: relay.1's lamp reaches the bloom threshold at the top of a key and would flare.";
            }

            return null;
        }

        // The array's ruin placement and its shaped, cleared footprint are tuned apart: they must agree.
        private string FallenArrayOnItsSite()
        {
            SiteSettings sites = _surface.Sites;
            RuinPlacement[] ruins = _landmarks.Ruins;
            for (int i = 0; ruins != null && i < ruins.Length; i++)
            {
                Vector2 spot = sites.RuinsCenter + ruins[i].Offset;
                if (ruins[i].Id == FallenArrayRuin
                    && Vector2.Distance(spot, sites.FallenArrayCenter) > sites.FallenArrayRadius)
                {
                    return "Landmarks: the fallen solar array lies off its own footprint (Sites' fallen array offset).";
                }
            }

            return null;
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

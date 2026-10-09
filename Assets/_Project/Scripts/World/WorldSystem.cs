using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Owns the moon: builds the analytic surface, the terrain (with Whispering Canyon's gate slabs), the rock scatter
    /// and Earth, applies the atmosphere and sky, and registers <see cref="ITerrainQuery"/>,
    /// <see cref="IWorldLayout"/> and <see cref="IWorldAnchors"/> in the context. In edit mode it also builds a
    /// preview (HideFlags.DontSave: never saved into the scene, destroyed in OnDisable) so the scene view and scene
    /// captures show the world, and other domains' edit-mode previews read its anchors through
    /// <see cref="IWorldPreview"/>; <see cref="Initialize"/> is the only play-mode path.
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldSystem : MonoBehaviour, IGameSystem, IWorldPreview
    {
        private const string GeneratedRootName = "Generated";

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset).")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Material drawing the terrain (Generated/World/M_Ground.mat, shader MoonProject/World/LofiTerrain).")]
        [SerializeField] private Material _groundMaterial;

        [Tooltip("Shared low-poly palette material for the scatter rocks and gate slabs " +
            "(Generated/Art/Palette/M_LowPoly.mat).")]
        [SerializeField] private Material _paletteMaterial;

        [Tooltip("Art rock meshes the pebbles are made of (Generated/Art/Rocks, CPU-readable).")]
        [SerializeField] private Mesh[] _pebbleRocks = new Mesh[0];

        [Tooltip("Art rock meshes the boulders are made of; also their convex colliders.")]
        [SerializeField] private Mesh[] _boulderRocks = new Mesh[0];

        [Tooltip("Material drawing Earth (Generated/World/M_Earth.mat, shader MoonProject/World/LofiEarth).")]
        [SerializeField] private Material _earthMaterial;

        [Tooltip("The beacon on The Peak; placed on the summit and breathed by the beacon settings.")]
        [SerializeField] private PeakBeacon _peakBeacon;

        [Tooltip("The directional earthlight; its colour, intensity and angle come from the atmosphere settings.")]
        [SerializeField] private Light _earthlight;

        [Tooltip("The shadowless directional fill opposite the earthlight; its colour, intensity and angle come " +
            "from the atmosphere settings.")]
        [SerializeField] private Light _fillLight;

        [Tooltip("Build the world while editing (never saved into the scene) so the scene view shows it.")]
        [SerializeField] private bool _previewInEditMode = true;

        private readonly TerrainBuilder _terrainBuilder = new TerrainBuilder();
        private readonly ScatterBuilder _scatterBuilder = new ScatterBuilder();
        private readonly CanyonGateBuilder _gateBuilder = new CanyonGateBuilder();
        private GameObject _generatedRoot;
        private Mesh _earthMesh;

        public MoonSurface Surface { get; private set; }

        public WorldLayout Layout { get; private set; }

        public WorldAnchors Anchors { get; private set; }

        IWorldAnchors IWorldPreview.Anchors => Anchors;

        public TerrainBuildReport LastBuild { get; private set; }

        public ScatterBuildReport LastScatter { get; private set; }

        /// <summary>True while generated world objects exist (play mode or the edit-mode preview).</summary>
        public bool HasGeneratedWorld => _generatedRoot != null;

        public void Initialize(GameContext context)
        {
            if (!HasValidSetup(true))
            {
                enabled = false;
                return;
            }

            Generate(HideFlags.None);
            context.Register<ITerrainQuery>(Surface);
            context.Register<IWorldLayout>(Layout);
            context.Register<IWorldAnchors>(Anchors);
            Debug.Log($"{nameof(WorldSystem)}: terrain {LastBuild}; scatter {LastScatter}", this);
        }

        /// <summary>Rebuilds the edit-mode preview, e.g. after tuning the settings asset.</summary>
        [ContextMenu("Rebuild Preview")]
        public void RebuildPreview()
        {
            if (!Application.isPlaying && HasValidSetup(true))
            {
                Generate(HideFlags.DontSave);
            }
        }

        private void OnEnable()
        {
            // Unwired is normal while a scene builder is still assigning references: stay quiet until then.
            if (!Application.isPlaying && _previewInEditMode && HasValidSetup(false))
            {
                Generate(HideFlags.DontSave);
            }
        }

        private void OnDisable()
        {
            DestroyGenerated();
        }

        private void Generate(HideFlags hideFlags)
        {
            DestroyGenerated();
            MoonSurface surface = _settings.CreateSurface();
            Surface = surface;
            Layout = new WorldLayout(surface, _settings.Sky);
            Anchors = new WorldAnchors(surface, _settings.Surface.Canyon, _settings.Relays);

            // Scatter planning only reads the analytic surface: it runs on a worker while the terrain is meshed.
            ScatterSettings scatterSettings = _settings.Scatter;
            Task<List<ScatterInstance>> scatterPlan =
                Task.Run(() => new ScatterPlanner(surface, scatterSettings, Anchors).Plan());
            WorldAtmosphere.Apply(_settings.Atmosphere, _settings.Sky, _earthlight, _fillLight);
            SkyShaderGlobals.Apply(_settings.Sky);

            _generatedRoot = new GameObject(GeneratedRootName) { hideFlags = hideFlags };
            _generatedRoot.transform.SetParent(transform, false);
            Vector3 toLight = WorldAtmosphere.LightSourceDirection(_settings.Atmosphere, _settings.Sky);
            LastBuild = _terrainBuilder.Build(_generatedRoot.transform, Surface, _settings.Mesh, _settings.Paint,
                toLight, _groundMaterial, hideFlags);
            _gateBuilder.Build(_generatedRoot.transform, surface.Canyon.Slabs, _paletteMaterial, hideFlags);
            LastScatter = _scatterBuilder.Build(_generatedRoot.transform, scatterPlan.GetAwaiter().GetResult(),
                scatterSettings, _settings.Mesh.ChunkSize, _pebbleRocks, _boulderRocks, _paletteMaterial, hideFlags);
            BuildEarth(hideFlags);
            _peakBeacon.transform.position = Layout.PeakPosition;
            _peakBeacon.Configure(_settings.Beacon);
        }

        private void BuildEarth(HideFlags hideFlags)
        {
            _earthMesh = EarthMeshBuilder.Build(_settings.Sky, _settings.Seed);
            _earthMesh.hideFlags = hideFlags;
            var earth = new GameObject("Earth") { hideFlags = hideFlags };
            earth.transform.SetParent(_generatedRoot.transform, false);
            earth.AddComponent<MeshFilter>().sharedMesh = _earthMesh;
            var meshRenderer = earth.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _earthMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void DestroyGenerated()
        {
            if (_generatedRoot != null)
            {
                Release(_generatedRoot);
            }

            if (_earthMesh != null)
            {
                Release(_earthMesh);
            }

            _generatedRoot = null;
            _earthMesh = null;
            _terrainBuilder.DestroyMeshes();
            _scatterBuilder.DestroyMeshes();
            _gateBuilder.DestroyMeshes();
        }

        private static void Release(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static string RockProblem(Mesh[] rocks, string label)
        {
            if (rocks == null || rocks.Length == 0)
            {
                return label + " has no meshes.";
            }

            foreach (Mesh rock in rocks)
            {
                if (rock == null || !rock.isReadable)
                {
                    return label + " has a missing or non-readable mesh.";
                }
            }

            return null;
        }

        private static bool IsWorldAligned(Transform target)
        {
            return target.position == Vector3.zero && target.rotation == Quaternion.identity
                && target.lossyScale == Vector3.one;
        }

        private bool HasValidSetup(bool report)
        {
            string problem = SetupProblem();
            if (problem != null && report)
            {
                Debug.LogError($"{nameof(WorldSystem)}: {problem}", this);
            }

            return problem == null;
        }

        private string SetupProblem()
        {
            if (_settings == null)
            {
                return "World Settings is not assigned.";
            }

            if (_groundMaterial == null || _paletteMaterial == null || _earthMaterial == null)
            {
                return "Ground Material, Palette Material and Earth Material must be assigned.";
            }

            string rocks = RockProblem(_pebbleRocks, "Pebble Rocks") ?? RockProblem(_boulderRocks, "Boulder Rocks");
            if (rocks != null)
            {
                return rocks;
            }

            if (_peakBeacon == null)
            {
                return "Peak Beacon is not assigned.";
            }

            if (_earthlight == null || _earthlight.transform.IsChildOf(transform))
            {
                return "Earthlight must be assigned and must not be this object or its child (it gets rotated).";
            }

            if (_fillLight == null || _fillLight == _earthlight || _fillLight.transform.IsChildOf(transform))
            {
                return "Fill Light must be assigned, must not be the earthlight and must not be this object or its " +
                    "child (it gets rotated).";
            }

            if (!IsWorldAligned(transform))
            {
                return "WorldSystem must sit at the origin with no rotation or scale: the terrain is generated in " +
                    "world space to match ITerrainQuery.";
            }

            return _settings.Validate();
        }
    }
}

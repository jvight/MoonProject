using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.World.Map;

namespace MoonProject.World
{
    /// <summary>
    /// Owns the moon: builds the analytic surface, the terrain (with Whispering Canyon's gate slabs), the rock scatter
    /// and Earth, applies the atmosphere and sky, and registers <see cref="ITerrainQuery"/>,
    /// <see cref="IWorldLayout"/>, <see cref="IWorldAnchors"/>, the <see cref="ISightline"/> and itself as the
    /// <see cref="IScrapField"/> (the cuttable slots down Kestrel-3's lane, 0.5.5 C5) in the context. In edit mode it
    /// also builds a preview (HideFlags.DontSave: never saved into the scene, destroyed in OnDisable) so the scene view
    /// and scene captures show the world, and other domains' edit-mode previews read its anchors through
    /// <see cref="IWorldPreview"/> and the scrap field's slots through <see cref="IScrapField"/>;
    /// <see cref="Initialize"/> is the only play-mode path.
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldSystem : MonoBehaviour, IGameSystem, IWorldPreview, IScrapField
    {
        private const string GeneratedRootName = "Generated";

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset).")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Material drawing the terrain (Generated/World/M_Ground.mat, shader MoonProject/World/R1Terrain).")]
        [SerializeField] private Material _groundMaterial;

        [Tooltip("Shared R1 basalt material for the scatter rocks and gate slabs " +
            "(Generated/Art/R1/Materials/R1_Basalt.mat).")]
        [SerializeField] private Material _paletteMaterial;

        [Tooltip("R1 rock meshes the pebbles are made of (Art/R1/Models/Rocks, CPU-readable).")]
        [SerializeField] private Mesh[] _pebbleRocks = new Mesh[0];

        [Tooltip("R1 rock meshes the boulders are made of; also their convex colliders.")]
        [SerializeField] private Mesh[] _boulderRocks = new Mesh[0];

        [Tooltip("Material drawing Earth (Generated/World/M_Earth.mat, shader MoonProject/World/LofiEarth).")]
        [SerializeField] private Material _earthMaterial;

        [Tooltip("Material of the marks pressed into the dust (Generated/World/M_GroundMarks, the rover's track " +
            "shader).")]
        [SerializeField] private Material _groundMarkMaterial;

        [Tooltip("Art's landmark kit (Generated/Art/Landmarks): route posts, the chasm bridge, the dish frames.")]
        [SerializeField] private LandmarkPrefabs _landmarkPrefabs = new LandmarkPrefabs();

        [Tooltip("Material of the orange vent's bead field (Generated/World/M_VentBeads, the rover's track shader " +
            "tinted orange).")]
        [SerializeField] private Material _ventBeadMaterial;

        [Tooltip("Material of 07's old charging cable on the dust (Generated/Art/R1/Materials/R1_Rubber).")]
        [SerializeField] private Material _cableMaterial;

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
        private Mesh _tracksMesh;
        private Mesh _ventMesh;
        private Mesh _cableMesh;
        private List<ScrapPlate> _cuttables = new List<ScrapPlate>();

        public MoonSurface Surface { get; private set; }

        public WorldLayout Layout { get; private set; }

        public WorldAnchors Anchors { get; private set; }

        IWorldAnchors IWorldPreview.Anchors => Anchors;

        public TerrainBuildReport LastBuild { get; private set; }

        public ScatterBuildReport LastScatter { get; private set; }

        /// <summary>Cuttable slots down Kestrel-3's lane; none until the world is generated.</summary>
        public int Count => _cuttables.Count;

        /// <summary>The scrap field's dust tint, linear (Gameplay tints its cuttable fragments with it too).</summary>
        public Color Dust => _settings.ScrapField.Dust.linear;

        public ScrapPlateInfo Slot(int index)
        {
            ScrapPlate slot = _cuttables[index];
            return new ScrapPlateInfo(slot.Position, slot.Rotation, slot.Variant, slot.Scale);
        }

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
            context.Register<ISightline>(new Sightline(Surface));
            context.Register<IScrapField>(this);
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
            surface.AttachCraterField(CreateCraterField(surface));

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
            ScrapFieldBuilder.Build(_generatedRoot.transform, surface, _settings.ScrapField, _settings.Seed,
                _landmarkPrefabs.HullFragments, hideFlags);
            _cuttables = ScrapFieldPlanner.PlanCuttables(surface, _settings.ScrapField,
                _landmarkPrefabs.HullFragments.Length, _settings.Seed);
            List<ScatterInstance> scatterInstances = scatterPlan.GetAwaiter().GetResult();
            AddHomeApronStones(scatterInstances, surface);
            LastScatter = _scatterBuilder.Build(_generatedRoot.transform, scatterInstances,
                scatterSettings, _settings.Mesh.ChunkSize, _pebbleRocks, _boulderRocks, _paletteMaterial, hideFlags);
            BuildEarth(hideFlags);
            BuildWornTracks(hideFlags);
            BuildWakeCable(hideFlags);
            BuildVentField(hideFlags);
            LandmarkPlacer.Build(_generatedRoot.transform, surface, Anchors, _settings.Landmarks, _settings.Seed,
                _landmarkPrefabs, hideFlags);
            _peakBeacon.transform.position = Layout.PeakPosition;
            _peakBeacon.Configure(_settings.Beacon);
        }

        public static void AddHomeApronStones(List<ScatterInstance> instances, MoonSurface surface)
        {
            // 1. Deterministic companion cobbles and spall pebbles around large boulders on the basin floor
            // so every major boulder sits in a fragmented lunar boulder family with smaller spall stones at its base.
            int baseCount = instances.Count;
            for (int i = 0; i < baseCount; i++)
            {
                ScatterInstance b = instances[i];
                if (b.Kind != ScatterKind.Boulder || b.Size < 1.22f)
                {
                    continue;
                }

                Vector2 center2D = new Vector2(b.Position.x, b.Position.z);
                float distHome = center2D.magnitude;
                if (distHome < 36f || distHome > 275f)
                {
                    continue;
                }

                uint h0 = Hashing.Hash(Mathf.RoundToInt(center2D.x * 100f), Mathf.RoundToInt(center2D.y * 100f), 0x85EBCA6Bu);
                for (int k = 0; k < 7; k++)
                {
                    uint hk = Hashing.Mix(h0 ^ (uint)(k * 0x9E3779B9u));
                    bool subBoulder = k < 3;
                    float baseAngle = (h0 & 0xFFFFu) * (Mathf.PI * 2f / 65536f);
                    float angle = baseAngle + k * 0.8976f + (Hashing.ToUnit(hk) - 0.5f) * 0.42f;
                    float radius = subBoulder
                        ? b.Size * Mathf.Lerp(0.88f, 1.55f, Hashing.ToUnit(Hashing.Mix(hk ^ 0x13579BDFu)))
                        : b.Size * Mathf.Lerp(0.62f, 1.85f, Hashing.ToUnit(Hashing.Mix(hk ^ 0x2468ACE0u)));
                    float sx = center2D.x + Mathf.Cos(angle) * radius;
                    float sz = center2D.y + Mathf.Sin(angle) * radius;
                    GroundShape ruins = surface.Shaping.Ruins;
                    if (Vector2.Distance(new Vector2(sx, sz), ruins.Center) < ruins.Radius + 3.5f)
                    {
                        continue;
                    }

                    Vector3 sNorm = surface.SampleNormal(sx, sz);
                    if (sNorm.y < 0.78f)
                    {
                        continue;
                    }

                    float sSize = subBoulder
                        ? Mathf.Lerp(0.38f, 0.72f, Hashing.ToUnit(Hashing.Mix(hk ^ 0x5bd1e995u)))
                        : Mathf.Lerp(0.14f, 0.28f, Hashing.ToUnit(Hashing.Mix(hk ^ 0x5bd1e995u)));
                    float sYaw = Hashing.ToUnit(Hashing.Mix(hk ^ 0x27d4eb2du)) * 360f;
                    float ax = Mathf.Lerp(0.82f, 1.24f, Hashing.ToUnit(Hashing.Mix(hk ^ 0x165667b1u)));
                    float ay = Mathf.Lerp(0.68f, 0.92f, Hashing.ToUnit(Hashing.Mix(hk ^ 0xd3a2646cu)));
                    float az = Mathf.Lerp(0.82f, 1.20f, Hashing.ToUnit(Hashing.Mix(hk ^ 0xfd7046c5u)));
                    float sBurial = sSize * ay * Mathf.Lerp(0.06f, 0.16f, Hashing.ToUnit(Hashing.Mix(hk ^ 0x3c6ef372u)));

                    instances.Add(new ScatterInstance(
                        ScatterKind.Pebble,
                        new Vector3(sx, surface.SampleHeight(sx, sz), sz),
                        sNorm,
                        sYaw,
                        sSize,
                        (int)((hk >> 3) & 0x7FFFFFFFu),
                        new Vector3(ax, ay, az),
                        sBurial));
                }
            }

            // 2. Visual-only stones (ScatterKind.Pebble: no physics collider) around the home pad margin and foreground
            // (relative to 07's dock at (1.8, -4.5) and camera at (+1.8, -10.4)) so the ground around 07
            // and the lander has weathered breccia lumps and small cobbles matching the concept.
            (float x, float z, float size, int variant, float yaw, Vector3 aspect, float burial)[] apronStones =
            {
                // Lower-left foreground boulder lump and companion cobbles in the opening/home camera view:
                (-0.78f, -7.25f, 0.78f, 2, 34f, new Vector3(1.26f, 0.72f, 1.06f), 0.08f),
                (-0.32f, -7.58f, 0.24f, 4, 118f, new Vector3(1.18f, 0.74f, 0.94f), 0.03f),
                (-1.28f, -6.75f, 0.28f, 1, 214f, new Vector3(1.15f, 0.70f, 1.08f), 0.035f),
                // Sunlit foreground stones in front of and beside 07's charging pad:
                (0.42f, -6.95f, 0.44f, 3, 48f, new Vector3(1.22f, 0.72f, 1.04f), 0.045f),
                (0.78f, -7.22f, 0.18f, 1, 134f, new Vector3(1.16f, 0.74f, 0.96f), 0.02f),
                (0.55f, -7.85f, 0.15f, 0, 62f, new Vector3(1.14f, 0.75f, 0.96f), 0.02f),
                (1.42f, -7.10f, 0.19f, 3, 184f, new Vector3(1.20f, 0.70f, 1.04f), 0.025f),
                (2.15f, -6.35f, 0.22f, 1, 275f, new Vector3(1.16f, 0.74f, 0.95f), 0.03f),
                (2.68f, -6.85f, 0.36f, 4, 212f, new Vector3(1.24f, 0.70f, 1.02f), 0.04f),
                (0.18f, -6.40f, 0.13f, 2, 95f, new Vector3(1.12f, 0.76f, 0.98f), 0.02f),
                (1.85f, -5.45f, 0.16f, 4, 142f, new Vector3(1.22f, 0.72f, 1.02f), 0.02f),
                (-1.65f, -5.40f, 0.21f, 0, 312f, new Vector3(1.18f, 0.74f, 1.05f), 0.025f),
                // Mid-ground stones beside 07 and near the lander legs:
                (-3.45f, -1.25f, 0.82f, 2, 32f, new Vector3(1.24f, 0.72f, 1.06f), 0.09f),
                (-3.05f, -1.75f, 0.22f, 4, 118f, new Vector3(1.18f, 0.74f, 0.94f), 0.03f),
                (-4.15f, -0.45f, 0.28f, 1, 214f, new Vector3(1.15f, 0.70f, 1.08f), 0.04f),
                (-5.80f, 2.90f, 0.64f, 4, 146f, new Vector3(1.20f, 0.75f, 1.04f), 0.07f),
                (-2.15f, -2.65f, 0.16f, 0, 64f, new Vector3(1.12f, 0.76f, 0.96f), 0.02f),
                (-1.35f, -2.95f, 0.13f, 3, 192f, new Vector3(1.22f, 0.68f, 1.02f), 0.02f),
                (0.75f, -2.65f, 0.15f, 1, 285f, new Vector3(1.16f, 0.72f, 0.95f), 0.02f),
                (1.65f, -2.10f, 0.19f, 2, 42f, new Vector3(1.20f, 0.74f, 1.05f), 0.03f),
                (2.35f, -0.85f, 0.15f, 0, 155f, new Vector3(1.10f, 0.78f, 0.92f), 0.02f),
                (-2.55f, 1.45f, 0.18f, 3, 88f, new Vector3(1.18f, 0.72f, 1.04f), 0.025f),
                (-4.65f, 1.85f, 0.25f, 0, 310f, new Vector3(1.24f, 0.70f, 0.98f), 0.035f),
                (1.45f, 2.15f, 0.15f, 4, 174f, new Vector3(1.14f, 0.75f, 0.96f), 0.02f),
                (2.85f, 3.10f, 0.24f, 1, 23f, new Vector3(1.20f, 0.70f, 1.06f), 0.03f),
                (-6.90f, -2.20f, 0.34f, 3, 128f, new Vector3(1.22f, 0.72f, 1.02f), 0.045f),
                (3.90f, 0.65f, 0.28f, 4, 77f, new Vector3(1.16f, 0.74f, 1.08f), 0.035f),
            };

            for (int i = 0; i < apronStones.Length; i++)
            {
                var s = apronStones[i];
                float y = surface.SampleHeight(s.x, s.z);
                Vector3 normal = surface.SampleNormal(s.x, s.z);
                instances.Add(new ScatterInstance(
                    ScatterKind.Pebble,
                    new Vector3(s.x, y, s.z),
                    normal,
                    s.yaw,
                    s.size,
                    s.variant,
                    s.aspect,
                    s.burial));
            }
        }

        private CraterField CreateCraterField(MoonSurface surface)
        {
            SurfaceSettings ground = _settings.Surface;
            return new CraterField(ground.CraterField, _settings.Seed, ground.FloorRadius - ground.RimWarpAmplitude,
                ground.PadRadius + ground.PadBlend, Anchors, surface.Canyon.Bounds);
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

        /// <summary>The old vent's orange bead field (M3-27), pressed into the ground.</summary>
        private void BuildVentField(HideFlags hideFlags)
        {
            _ventMesh = LandmarkPlacer.BuildVentField(Surface, Anchors, _settings.Landmarks, _settings.Seed);
            _ventMesh.hideFlags = hideFlags;
            var field = new GameObject("VentBeads") { hideFlags = hideFlags };
            field.transform.SetParent(_generatedRoot.transform, false);
            field.AddComponent<MeshFilter>().sharedMesh = _ventMesh;
            var meshRenderer = field.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _ventBeadMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        /// <summary>07's worn tracks from home out to the places it goes (M3-18 V4), following the ground.</summary>
        private void BuildWornTracks(HideFlags hideFlags)
        {
            GroundMarkSettings settings = _settings.GroundMarks;
            var marks = new GroundMarkMesh();
            MoonSurface surface = Surface;
            System.Func<float, float, float> height = (x, z) => surface.Sample(x, z).Height;
            for (int i = 1; i < settings.Route.Length; i++)
            {
                if (!RouteAnchor(settings.Route[i - 1], out WorldAnchor start) || !RouteAnchor(settings.Route[i],
                        out WorldAnchor end))
                {
                    continue;
                }

                var from = new Vector2(start.Position.x, start.Position.z);
                var to = new Vector2(end.Position.x, end.Position.z);
                Vector2 heading = (to - from).normalized;
                GroundMarks.AddTrack(marks, from + heading * start.Radius, to - heading * end.Radius, settings,
                    Hashing.Mix((uint)(_settings.Seed + i)), height);
            }

            _tracksMesh = marks.ToMesh("WornTracks");
            _tracksMesh.hideFlags = hideFlags;
            var tracks = new GameObject("WornTracks") { hideFlags = hideFlags };
            tracks.transform.SetParent(_generatedRoot.transform, false);
            tracks.AddComponent<MeshFilter>().sharedMesh = _tracksMesh;
            var meshRenderer = tracks.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = _groundMarkMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private bool RouteAnchor(string id, out WorldAnchor anchor)
        {
            if (Anchors.TryGet(id, out anchor))
            {
                return true;
            }

            Debug.LogError($"{nameof(WorldSystem)}: worn track route point '{id}' is not a world anchor.", this);
            return false;
        }

        /// <summary>
        /// 07's old charging cable on the dust (opening v2 §4): from its dead post at place.wake to the lander's port,
        /// on the hull's side facing the apron.
        /// </summary>
        private void BuildWakeCable(HideFlags hideFlags)
        {
            if (!Anchors.TryGet(WorldAnchorIds.PlaceWake, out WorldAnchor wake)
                || !Anchors.TryGet(WorldAnchorIds.PlaceLander, out WorldAnchor lander))
            {
                Debug.LogError($"{nameof(WorldSystem)}: the wake cable needs place.wake and place.lander.", this);
                return;
            }

            GroundCableSettings tuning = _settings.Landmarks.WakeCable;
            Vector3 port = lander.Position + lander.Forward * tuning.PortReach;
            MoonSurface surface = Surface;
            _cableMesh = GroundCable.Build(new Vector2(wake.Position.x, wake.Position.z), new Vector2(port.x, port.z),
                tuning, (x, z) => surface.Sample(x, z).Height);
            _cableMesh.hideFlags = hideFlags;
            var cable = new GameObject("WakeCable") { hideFlags = hideFlags };
            cable.transform.SetParent(_generatedRoot.transform, false);
            cable.AddComponent<MeshFilter>().sharedMesh = _cableMesh;
            cable.AddComponent<MeshRenderer>().sharedMaterial = _cableMaterial;
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

            if (_tracksMesh != null)
            {
                Release(_tracksMesh);
            }

            if (_cableMesh != null)
            {
                Release(_cableMesh);
            }

            _cableMesh = null;

            if (_ventMesh != null)
            {
                Release(_ventMesh);
            }

            _ventMesh = null;

            _tracksMesh = null;

            _generatedRoot = null;
            _earthMesh = null;
            _cuttables.Clear();
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

            if (_groundMaterial == null || _paletteMaterial == null || _earthMaterial == null
                || _groundMarkMaterial == null || _ventBeadMaterial == null || _cableMaterial == null)
            {
                return "Ground, Palette, Earth, Ground Mark, Vent Bead and Cable materials must be assigned.";
            }

            string rocks = RockProblem(_pebbleRocks, "Pebble Rocks") ?? RockProblem(_boulderRocks, "Boulder Rocks")
                ?? _landmarkPrefabs.Problem();
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

using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Owns the moon: builds the analytic surface, the terrain meshes and Earth, applies the atmosphere and sky, and
    /// registers <see cref="ITerrainQuery"/> and <see cref="IWorldLayout"/> in the context. In edit mode it also
    /// builds a preview (HideFlags.DontSave: never saved into the scene, destroyed in OnDisable) so the scene view
    /// and scene captures show the world; <see cref="Initialize"/> is the only play-mode path.
    /// </summary>
    [ExecuteAlways]
    public sealed class WorldSystem : MonoBehaviour, IGameSystem
    {
        private const string GeneratedRootName = "Generated";

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset).")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("Shared low-poly palette material (Generated/Art/Palette/M_LowPoly.mat).")]
        [SerializeField] private Material _terrainMaterial;

        [Tooltip("Material drawing Earth (Generated/World/M_Earth.mat, shader MoonProject/World/LofiEarth).")]
        [SerializeField] private Material _earthMaterial;

        [Tooltip("The directional earthlight; its colour, intensity and angle come from the atmosphere settings.")]
        [SerializeField] private Light _earthlight;

        [Tooltip("Build the world while editing (never saved into the scene) so the scene view shows it.")]
        [SerializeField] private bool _previewInEditMode = true;

        private readonly TerrainBuilder _terrainBuilder = new TerrainBuilder();
        private GameObject _generatedRoot;
        private Mesh _earthMesh;

        public MoonSurface Surface { get; private set; }

        public WorldLayout Layout { get; private set; }

        public TerrainBuildReport LastBuild { get; private set; }

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
            Debug.Log($"{nameof(WorldSystem)}: terrain {LastBuild}", this);
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
            Surface = _settings.CreateSurface();
            Layout = new WorldLayout(Surface, _settings.Sky);
            WorldAtmosphere.Apply(_settings.Atmosphere, _settings.Sky, _earthlight);
            SkyShaderGlobals.Apply(_settings.Sky);

            _generatedRoot = new GameObject(GeneratedRootName) { hideFlags = hideFlags };
            _generatedRoot.transform.SetParent(transform, false);
            LastBuild = _terrainBuilder.Build(_generatedRoot.transform, Surface, _settings.Mesh, _settings.Paint,
                _terrainMaterial, hideFlags);
            BuildEarth(hideFlags);
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

        private static bool IsWorldAligned(Transform target)
        {
            return target.position == Vector3.zero && target.rotation == Quaternion.identity
                && target.lossyScale == Vector3.one;
        }

        private bool HasValidSetup(bool report)
        {
            string problem = _settings == null ? "World Settings is not assigned."
                : _terrainMaterial == null ? "Terrain Material is not assigned."
                : _earthMaterial == null ? "Earth Material is not assigned."
                : _earthlight == null ? "Earthlight is not assigned."
                : _earthlight.transform.IsChildOf(transform) ? "The earthlight must not be this object or its child: " +
                    "the atmosphere rotates it."
                : !IsWorldAligned(transform) ? "WorldSystem must sit at the origin with no rotation or scale: the " +
                    "terrain is generated in world space to match ITerrainQuery."
                : _settings.Validate();
            if (problem != null && report)
            {
                Debug.LogError($"{nameof(WorldSystem)}: {problem}", this);
            }

            return problem == null;
        }
    }
}

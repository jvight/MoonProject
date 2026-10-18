using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The picture darkened by 07's lid (docs/design/cutscenes.md §1.8), registered as <see cref="ILensView"/>. While
    /// Rover applies a state it owns two things of its own: a global URP volume above the world's look whose depth of
    /// field follows the lens' focus and whose exposure adds the eye's gain to the world's own, and a quad held in
    /// front of the view camera whose shader draws the curved soft lid (<see cref="LensViewModel"/>). Released, both
    /// switch off and the world's own look is untouched: this never edits the world's post-processing profile.
    /// The view camera is registered by Rover after World initialises, so it is picked up on the first apply.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LensView : MonoBehaviour, IGameSystem, ILensView
    {
        /// <summary>The overlay quad sits this far (m) in front of the camera, well inside the frustum.</summary>
        private const float QuadDistance = 1f;

        private static readonly int LidEdgeId = Shader.PropertyToID("_LidEdge");
        private static readonly int LidSoftnessId = Shader.PropertyToID("_LidSoftness");
        private static readonly int LidCurveId = Shader.PropertyToID("_LidCurve");

        [Tooltip("World settings (Assets/_Project/Data/Tuning/WorldSettings.asset); the lens look is its LensView block.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("MoonProject/World/LofiLensView: draws the lid.")]
        [SerializeField] private Shader _shader;

        private GameContext _context;
        private LensViewModel _model;
        private MaterialPropertyBlock _block;
        private Material _material;
        private Mesh _quad;
        private MeshRenderer _overlay;
        private VolumeProfile _profile;
        private Volume _volume;
        private DepthOfField _focus;
        private ColorAdjustments _exposure;
        private Camera _camera;
        private bool _applied;

        /// <summary>True between an apply and the next release (for tests and tooling).</summary>
        public bool IsApplied => _applied;

        /// <summary>The look the last apply produced (for tests and tooling).</summary>
        public LensViewModel Model => _model;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _shader == null)
            {
                Debug.LogError($"{nameof(LensView)}: the world settings or the LofiLensView shader is not assigned.",
                    this);
                enabled = false;
                return;
            }

            _context = context;
            _model = new LensViewModel(_settings.LensView);
            _block = new MaterialPropertyBlock();
            CreateVolume();
            context.Register<ILensView>(this);
        }

        public void Apply(in LensViewState state)
        {
            if (_model == null || !EnsureOverlay())
            {
                return;
            }

            _model.Step(state);
            LensViewSettings look = _settings.LensView;
            _block.SetFloat(LidEdgeId, _model.LidEdge);
            _block.SetFloat(LidSoftnessId, look.LidSoftness);
            _block.SetFloat(LidCurveId, look.LidCurve);
            _overlay.SetPropertyBlock(_block);
            _overlay.enabled = true;

            _focus.focusDistance.Override(_model.FocusDistance);
            _exposure.postExposure.Override(_settings.PostProcessing.PostExposure + _model.Gain);
            _volume.weight = 1f;
            _volume.enabled = true;
            _applied = true;
        }

        public void Warm()
        {
            if (_model != null)
            {
                EnsureOverlay();
            }
        }

        public void Release()
        {
            _applied = false;
            _model?.Reset();
            if (_overlay != null)
            {
                _overlay.enabled = false;
            }

            if (_volume != null)
            {
                _volume.weight = 0f;
                _volume.enabled = false;
            }
        }

        private void CreateVolume()
        {
            LensViewSettings look = _settings.LensView;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "LensView";
            _focus = _profile.Add<DepthOfField>(true);
            _focus.mode.Override(DepthOfFieldMode.Bokeh);
            _focus.focalLength.Override(look.FocalLength);
            _focus.aperture.Override(look.Aperture);
            _focus.focusDistance.Override(look.NearestFocus);
            _exposure = _profile.Add<ColorAdjustments>();
            _exposure.postExposure.Override(_settings.PostProcessing.PostExposure);

            var host = new GameObject("LensView Volume");
            host.transform.SetParent(transform, false);
            _volume = host.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = look.VolumePriority;
            _volume.sharedProfile = _profile;
            _volume.weight = 0f;
            _volume.enabled = false;
        }

        /// <summary>Builds the overlay quad under the view camera the first time the lens is applied.</summary>
        private bool EnsureOverlay()
        {
            if (_overlay != null)
            {
                return true;
            }

            if (!_context.TryGet(out IViewCamera view) || view.Camera == null)
            {
                Debug.LogError($"{nameof(LensView)}: applied before Rover registered the view camera.", this);
                return false;
            }

            _camera = view.Camera;
            _quad = new Mesh { name = "LensViewQuad" };
            _quad.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f),
            });
            _quad.SetUVs(0, new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one });
            _quad.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            _quad.RecalculateBounds();

            var host = new GameObject("LensView Overlay");
            host.transform.SetParent(_camera.transform, false);
            host.transform.localPosition = Vector3.forward * QuadDistance;
            host.AddComponent<MeshFilter>().sharedMesh = _quad;
            _material = new Material(_shader) { name = "LensView" };
            _overlay = host.AddComponent<MeshRenderer>();
            _overlay.sharedMaterial = _material;
            _overlay.shadowCastingMode = ShadowCastingMode.Off;
            _overlay.receiveShadows = false;
            _overlay.lightProbeUsage = LightProbeUsage.Off;
            _overlay.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _overlay.enabled = false;
            return true;
        }

        private void OnDestroy()
        {
            if (_overlay != null)
            {
                Destroy(_overlay.gameObject);
            }

            DestroyOwned(_material);
            DestroyOwned(_quad);
            DestroyOwned(_profile);
        }

        private static void DestroyOwned(Object owned)
        {
            if (owned != null)
            {
                Destroy(owned);
            }
        }
    }
}

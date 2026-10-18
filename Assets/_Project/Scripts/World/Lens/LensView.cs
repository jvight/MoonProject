using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// The picture through 07's own eye (docs/design/cutscenes.md §1.8), registered as <see cref="ILensView"/>. While
    /// Rover applies a state it owns two things of its own: a global URP volume above the world's look whose depth of
    /// field follows the lens' focus and whose exposure adds the eye's gain to the world's own, and a quad held in front of the view camera whose shader draws the curved soft
    /// lid, the exposure dips and roll band, the warm lift and the dust speck (<see cref="LensViewModel"/>), and, while
    /// the opening asks, 07's faint boot readout in the lid's dark corner (localized through ILocalization). Released,
    /// both switch off and the world's own look is untouched: this never edits the world's post-processing profile.
    /// The view camera is registered by Rover after World initialises, so it is picked up on the first apply.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LensView : MonoBehaviour, IGameSystem, ILensView
    {
        /// <summary>The overlay quad sits this far (m) in front of the camera, well inside the frustum.</summary>
        private const float QuadDistance = 1f;

        /// <summary>The readout draws after the lens overlay, so the lid never covers it.</summary>
        private const int ReadoutQueue = 4001;

        /// <summary>Text mesh font size: a TextMesh line is about a tenth of it in local units at character size 1.</summary>
        private const int ReadoutFontSize = 64;
        private const float LineUnitsPerFontSize = 0.1f;

        /// <summary>The readout's lines (docs/design/gdd-v3.md §8), in order.</summary>
        private static readonly string[] ReadoutKeys = { "lens.boot.signal", "lens.boot.uplink", "lens.boot.recovery" };

        private static readonly int LidEdgeId = Shader.PropertyToID("_LidEdge");
        private static readonly int LidSoftnessId = Shader.PropertyToID("_LidSoftness");
        private static readonly int LidCurveId = Shader.PropertyToID("_LidCurve");
        private static readonly int DipId = Shader.PropertyToID("_Dip");
        private static readonly int RollBandId = Shader.PropertyToID("_RollBand");
        private static readonly int RollHeightId = Shader.PropertyToID("_RollHeight");
        private static readonly int RollDarknessId = Shader.PropertyToID("_RollDarkness");
        private static readonly int WarmId = Shader.PropertyToID("_Warm");
        private static readonly int SpeckId = Shader.PropertyToID("_Speck");
        private static readonly int SpeckLookId = Shader.PropertyToID("_SpeckLook");
        private static readonly int SpeckRimColorId = Shader.PropertyToID("_SpeckRimColor");

        [Tooltip("World settings (Assets/_Project/Data/Tuning/WorldSettings.asset); the lens look is its LensView block.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("MoonProject/World/LofiLensView: draws the lid, dips, roll band, warm lift and speck.")]
        [SerializeField] private Shader _shader;

        [Tooltip("The crew's terminal type for the boot readout (Be Vietnam Pro, so Vietnamese reads too).")]
        [SerializeField] private Font _readoutFont;

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
        private TextMesh _readout;
        private MeshRenderer _readoutRenderer;
        private Material _readoutMaterial;
        private bool _readoutComposed;
        private bool _localizationMissing;

        /// <summary>True between an apply and the next release (for tests and tooling).</summary>
        public bool IsApplied => _applied;

        /// <summary>The look the last apply produced (for tests and tooling).</summary>
        public LensViewModel Model => _model;

        public void Initialize(GameContext context)
        {
            if (_settings == null || _shader == null || _readoutFont == null)
            {
                Debug.LogError($"{nameof(LensView)}: the world settings, the LofiLensView shader or the readout font " +
                    "is not assigned.",
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

            _model.Step(state, Time.deltaTime);
            LensViewSettings look = _settings.LensView;
            _block.SetFloat(LidEdgeId, _model.LidEdge);
            _block.SetFloat(LidSoftnessId, look.LidSoftness);
            _block.SetFloat(LidCurveId, look.LidCurve);
            _block.SetFloat(DipId, _model.Dip);
            _block.SetFloat(RollBandId, _model.RollBand);
            _block.SetFloat(RollHeightId, look.RollBandHeight);
            _block.SetFloat(RollDarknessId, look.RollBandDarkness);
            Color warm = look.WarmTint * (look.WarmLift * _model.Warmth);
            _block.SetVector(WarmId, new Vector4(warm.r, warm.g, warm.b, 0f));
            Vector2 speck = state.SpeckOffset;
            _block.SetVector(SpeckId, new Vector4(speck.x, speck.y, look.SpeckRadius, _model.SpeckSharpness));
            _block.SetVector(SpeckLookId, new Vector4(look.SpeckDarkness, look.SpeckRim, look.SpeckBlurGrowth, 0f));
            _block.SetColor(SpeckRimColorId, look.SpeckRimColor);
            _overlay.SetPropertyBlock(_block);
            _overlay.enabled = true;

            ShowReadout(state.Readout01, look);
            _focus.focusDistance.Override(_model.FocusDistance);
            _exposure.postExposure.Override(_settings.PostProcessing.PostExposure + _model.Gain);
            _volume.weight = 1f;
            _volume.enabled = true;
            _applied = true;
        }

        public void Release()
        {
            _applied = false;
            _model?.Reset();
            if (_overlay != null)
            {
                _overlay.enabled = false;
            }

            if (_readoutRenderer != null)
            {
                _readoutRenderer.enabled = false;
                _readoutComposed = false;
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

        /// <summary>
        /// 07's boot readout: held in the lid's dark corner at a fixed size on screen, faint, fading with the readout's
        /// share and dipping with the flicker. The lines are read from the tables once each time it appears.
        /// </summary>
        private void ShowReadout(float readout01, LensViewSettings look)
        {
            float share = Mathf.Clamp01(readout01);
            if (share <= 0f)
            {
                if (_readoutRenderer != null)
                {
                    _readoutRenderer.enabled = false;
                }

                return;
            }

            if (!_readoutComposed && !ComposeReadout())
            {
                return;
            }

            float height = 2f * look.ReadoutDistance * Mathf.Tan(0.5f * _camera.fieldOfView * Mathf.Deg2Rad);
            float width = height * _camera.aspect;
            Vector2 corner = look.ReadoutCorner;
            Transform node = _readout.transform;
            node.localPosition = new Vector3((corner.x - 0.5f) * width, (corner.y - 0.5f) * height,
                look.ReadoutDistance);
            float scale = look.ReadoutLineHeight * height / (ReadoutFontSize * LineUnitsPerFontSize);
            node.localScale = new Vector3(scale, scale, scale);
            Color colour = look.ReadoutColor;
            colour.a = look.ReadoutOpacity * share * (1f - _model.Dip);
            _readout.color = colour;
            _readoutRenderer.enabled = true;
        }

        private bool ComposeReadout()
        {
            if (!_context.TryGet(out ILocalization localization))
            {
                if (!_localizationMissing)
                {
                    _localizationMissing = true;
                    Debug.LogError($"{nameof(LensView)}: no ILocalization is registered, so 07's boot readout is " +
                        "not shown.", this);
                }

                return false;
            }

            if (_readout == null)
            {
                var host = new GameObject("LensView Readout");
                host.transform.SetParent(_camera.transform, false);
                _readout = host.AddComponent<TextMesh>();
                _readout.font = _readoutFont;
                _readout.fontSize = ReadoutFontSize;
                _readout.characterSize = 1f;
                _readout.anchor = TextAnchor.UpperLeft;
                _readout.alignment = TextAlignment.Left;
                _readoutRenderer = host.GetComponent<MeshRenderer>();
                _readoutMaterial = new Material(_readoutFont.material) { name = "LensViewReadout" };
                _readoutMaterial.renderQueue = ReadoutQueue;
                _readoutRenderer.sharedMaterial = _readoutMaterial;
                _readoutRenderer.shadowCastingMode = ShadowCastingMode.Off;
                _readoutRenderer.receiveShadows = false;
            }

            _readout.text = string.Join("\n", localization.Get(ReadoutKeys[0]), localization.Get(ReadoutKeys[1]),
                localization.Get(ReadoutKeys[2]));
            _readoutComposed = true;
            return true;
        }

        private void OnDestroy()
        {
            if (_readout != null)
            {
                Destroy(_readout.gameObject);
            }

            DestroyOwned(_readoutMaterial);
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

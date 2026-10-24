using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Thin playable-preview component for the 0.6 Earthrise light tint preset (docs/design/0.6-living-moon.md §2).
    /// Smoothly blends directional key light, warm counter-rim light, trilight ambient settings, horizon haze shader
    /// globals, and an optional rising Earth visual disc from a <see cref="EarthriseLightPresetTuning"/> asset with
    /// zero per-frame garbage collection.
    /// </summary>
    public sealed class EarthriseLightPresetPreview : MonoBehaviour
    {
        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");

        [Tooltip("Tuning asset defining the warm-blue Earthrise stillness lighting and colour-grade preset.")]
        [SerializeField] private EarthriseLightPresetTuning _tuning;

        [Tooltip("Directional key light driven by the rising Earth during the preview.")]
        [SerializeField] private Light _keyLight;

        [Tooltip("Directional counter-rim fill light providing the warm golden-ivory crest bounce.")]
        [SerializeField] private Light _rimLight;

        [Tooltip("Optional preview renderer for the rising warm-blue Earth disc and atmospheric limb.")]
        [SerializeField] private MeshRenderer _earthDiscRenderer;

        [Tooltip("Azimuth yaw angle in degrees of the rising Earth above the horizon ridge.")]
        [Range(-180f, 180f)]
        [SerializeField] private float _earthAzimuthDegrees = 142f;

        [Tooltip("Whether the preview advances its playback clock automatically during Update.")]
        [SerializeField] private bool _autoPlay = true;

        [Tooltip("Whether playback loops after TotalDurationSeconds.")]
        [SerializeField] private bool _loop = true;

        private MaterialPropertyBlock _discBlock;
        private Color _baseKeyColor = new Color(0.58f, 0.68f, 0.95f, 1.0f);
        private float _baseKeyIntensity = 0.48f;
        private float _baseKeyElevation = 4.5f;
        private Color _baseRimColor = new Color(0.42f, 0.52f, 0.78f, 1.0f);
        private float _baseRimIntensity = 0.22f;
        private Color _baseAmbientSky = new Color(0.11f, 0.14f, 0.22f, 1.0f);
        private Color _baseAmbientEquator = new Color(0.08f, 0.10f, 0.16f, 1.0f);
        private Color _baseAmbientGround = new Color(0.05f, 0.06f, 0.10f, 1.0f);
        private Color _baseSkyBackground = new Color(0.05f, 0.07f, 0.11f, 1.0f);
        private float _elapsedSeconds;
        private bool _configured;

        public EarthriseLightPresetTuning Tuning => _tuning;
        public float ElapsedSeconds => _elapsedSeconds;
        public EarthriseLightState CurrentState { get; private set; }

        /// <summary>
        /// Wires the preview component's serialized references in editor builders or tests and captures the current
        /// baseline lighting state.
        /// </summary>
        public void Configure(
            EarthriseLightPresetTuning tuning,
            Light keyLight,
            Light rimLight,
            MeshRenderer earthDiscRenderer)
        {
            _tuning = tuning;
            _keyLight = keyLight;
            _rimLight = rimLight;
            _earthDiscRenderer = earthDiscRenderer;
            EnsureBuffers();
            CaptureBaselineFromScene();
        }

        /// <summary>
        /// Captures the current scene lighting and RenderSettings as the baseline (weight = 0) state.
        /// </summary>
        public void CaptureBaselineFromScene()
        {
            if (_keyLight != null)
            {
                _baseKeyColor = _keyLight.color;
                _baseKeyIntensity = _keyLight.intensity;
                _baseKeyElevation = _tuning != null ? _tuning.StartElevationDegrees : _keyLight.transform.eulerAngles.x;
            }

            if (_rimLight != null)
            {
                _baseRimColor = _rimLight.color;
                _baseRimIntensity = _rimLight.intensity;
            }

            _baseAmbientSky = RenderSettings.ambientSkyColor;
            _baseAmbientEquator = RenderSettings.ambientEquatorColor;
            _baseAmbientGround = RenderSettings.ambientGroundColor;
        }

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            EnsureBuffers();
            CaptureBaselineFromScene();
            Sample(_elapsedSeconds);
        }

        private void Update()
        {
            if (!_configured && !EnsureBuffers())
            {
                return;
            }

            if (_autoPlay)
            {
                _elapsedSeconds += Time.deltaTime;
                float total = _tuning.TotalDurationSeconds;
                if (_loop && total > 0.01f && _elapsedSeconds > total)
                {
                    _elapsedSeconds %= total;
                }
            }

            Sample(_elapsedSeconds);
        }

        /// <summary>
        /// Evaluates and applies the Earthrise lighting and warm-blue grade preset at an explicit
        /// <paramref name="timeSeconds"/>. Zero GC allocations after initial setup.
        /// </summary>
        public void Sample(float timeSeconds)
        {
            if (!_configured && !EnsureBuffers())
            {
                return;
            }

            _elapsedSeconds = Mathf.Max(0f, timeSeconds);
            float weight = _tuning.EvaluateBlendWeight(_elapsedSeconds);
            ApplyBlend(weight);
        }

        /// <summary>
        /// Directly applies a normalized <paramref name="blendWeight"/> (0 = baseline look, 1 = full warm-blue
        /// Earthrise stillness preset). Zero GC allocations.
        /// </summary>
        public void ApplyBlend(float blendWeight)
        {
            if (!_configured && !EnsureBuffers())
            {
                return;
            }

            EarthriseLightState state = _tuning.EvaluateState(
                blendWeight,
                _baseKeyColor,
                _baseKeyIntensity,
                _baseKeyElevation,
                _baseRimColor,
                _baseRimIntensity,
                _baseAmbientSky,
                _baseAmbientEquator,
                _baseAmbientGround,
                _baseSkyBackground);

            CurrentState = state;

            if (_keyLight != null)
            {
                _keyLight.color = state.KeyLightColor;
                _keyLight.intensity = state.KeyLightIntensity;
                _keyLight.transform.rotation = Quaternion.Euler(state.KeyElevationDegrees, _earthAzimuthDegrees, 0f);
            }

            if (_rimLight != null)
            {
                _rimLight.color = state.RimLightColor;
                _rimLight.intensity = state.RimLightIntensity;
                _rimLight.transform.rotation = Quaternion.Euler(18f, _earthAzimuthDegrees - 165f, 0f);
            }

            RenderSettings.ambientSkyColor = state.AmbientSkyColor;
            RenderSettings.ambientEquatorColor = state.AmbientEquatorColor;
            RenderSettings.ambientGroundColor = state.AmbientGroundColor;

            Shader.SetGlobalVector(
                MoonHazeColorId,
                new Vector4(state.HorizonHazeColor.r, state.HorizonHazeColor.g, state.HorizonHazeColor.b, state.HorizonHazeColor.a * 0.55f));
            Shader.SetGlobalVector(
                MoonHazeId,
                new Vector4(0.012f * state.BlendWeight, 0.25f, 0f, 0f));

            if (_earthDiscRenderer != null)
            {
                float riseOffset = Mathf.Lerp(1.6f, 5.4f, state.BlendWeight);
                Vector3 localPos = _earthDiscRenderer.transform.localPosition;
                localPos.y = riseOffset;
                _earthDiscRenderer.transform.localPosition = localPos;

                _discBlock.Clear();
                _discBlock.SetColor(BaseColorId, _tuning.EarthKeyColor);
                _discBlock.SetColor(RimColorId, _tuning.WarmRimColor);
                _discBlock.SetFloat(IntensityId, Mathf.Lerp(0.18f, 1.05f, state.BlendWeight));
                _earthDiscRenderer.SetPropertyBlock(_discBlock);
            }
        }

        private bool EnsureBuffers()
        {
            if (_tuning == null)
            {
                return false;
            }

            if (_discBlock == null)
            {
                _discBlock = new MaterialPropertyBlock();
            }

            _configured = true;
            return true;
        }

        private bool ValidateReferences()
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(EarthriseLightPresetPreview)}: Tuning asset must be assigned.", this);
                return false;
            }

            return true;
        }
    }
}

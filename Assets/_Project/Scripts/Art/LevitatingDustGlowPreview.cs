using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Thin playable-preview component for the 0.6 levitating dust glow lunar phenomenon
    /// (docs/design/0.6-living-moon.md §2). Drives the soft terminator horizon haze band, thin vertical mote-column
    /// ribbons, and slowly rising electrostatic mote particles from a <see cref="LevitatingDustGlowTuning"/> asset
    /// with zero per-frame garbage collection.
    /// </summary>
    public sealed class LevitatingDustGlowPreview : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int NightColorId = Shader.PropertyToID("_NightColor");
        private static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int NightBlendId = Shader.PropertyToID("_NightBlend");
        private static readonly int PhaseId = Shader.PropertyToID("_Phase");
        private static readonly int BloomCeilingId = Shader.PropertyToID("_BloomCeiling");

        [Tooltip("Tuning asset governing envelope timing, terminator haze dimensions, and rising mote columns.")]
        [SerializeField] private LevitatingDustGlowTuning _tuning;

        [Tooltip("MeshRenderer for the soft horizontal terminator haze band.")]
        [SerializeField] private MeshRenderer _horizonBandRenderer;

        [Tooltip("MeshRenderer for the thin vertical electrostatic mote column filaments.")]
        [SerializeField] private MeshRenderer _moteColumnsRenderer;

        [Tooltip("ParticleSystem emitting discrete elongated crystalline dust motes rising in columns.")]
        [SerializeField] private ParticleSystem _moteParticleSystem;

        [Tooltip("Blend between Day C grazing sunlight (0) and Night A cool earthshine (1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _nightBlend;

        [Tooltip("Whether the preview advances its playback clock automatically during Update.")]
        [SerializeField] private bool _autoPlay = true;

        [Tooltip("Whether playback loops seamlessly after TotalDurationSeconds.")]
        [SerializeField] private bool _loop = true;

        private MaterialPropertyBlock _bandBlock;
        private MaterialPropertyBlock _columnsBlock;
        private ParticleSystem.Particle[] _particles;
        private float _elapsedSeconds;
        private bool _configured;

        public LevitatingDustGlowTuning Tuning => _tuning;
        public float ElapsedSeconds => _elapsedSeconds;
        public float CurrentEnvelope { get; private set; }
        public float NightBlend
        {
            get => _nightBlend;
            set => _nightBlend = Mathf.Clamp01(value);
        }

        /// <summary>
        /// Wires the preview component's serialized references in editor builders or tests and preallocates its
        /// runtime buffers.
        /// </summary>
        public void Configure(
            LevitatingDustGlowTuning tuning,
            MeshRenderer horizonBandRenderer,
            MeshRenderer moteColumnsRenderer,
            ParticleSystem moteParticleSystem)
        {
            _tuning = tuning;
            _horizonBandRenderer = horizonBandRenderer;
            _moteColumnsRenderer = moteColumnsRenderer;
            _moteParticleSystem = moteParticleSystem;
            EnsureBuffers();
        }

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            EnsureBuffers();
            Sample(_elapsedSeconds, _nightBlend);
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

            Sample(_elapsedSeconds, _nightBlend);
        }

        /// <summary>
        /// Evaluates and applies the levitating dust glow state at an explicit <paramref name="timeSeconds"/> and
        /// <paramref name="nightBlend"/> (0 = Day C, 1 = Night A). Zero GC allocations after initial buffer setup.
        /// </summary>
        public void Sample(float timeSeconds, float nightBlend = -1f)
        {
            if (!_configured && !EnsureBuffers())
            {
                return;
            }

            _elapsedSeconds = Mathf.Max(0f, timeSeconds);
            if (nightBlend >= 0f)
            {
                _nightBlend = Mathf.Clamp01(nightBlend);
            }

            float envelope = _tuning.EvaluateEnvelope(_elapsedSeconds);
            CurrentEnvelope = envelope;
            float phase = _elapsedSeconds * _tuning.ShimmerFrequencyHz;

            if (_horizonBandRenderer != null)
            {
                _bandBlock.Clear();
                _bandBlock.SetColor(BaseColorId, _tuning.DayBandColor);
                _bandBlock.SetColor(NightColorId, _tuning.NightBandColor);
                _bandBlock.SetColor(RimColorId, _tuning.ShimmerRimColor);
                _bandBlock.SetFloat(IntensityId, envelope * _tuning.BandMaxIntensity);
                _bandBlock.SetFloat(NightBlendId, _nightBlend);
                _bandBlock.SetFloat(PhaseId, phase);
                _bandBlock.SetFloat(BloomCeilingId, _tuning.BloomCeiling);
                _horizonBandRenderer.SetPropertyBlock(_bandBlock);
            }

            if (_moteColumnsRenderer != null)
            {
                _columnsBlock.Clear();
                _columnsBlock.SetColor(BaseColorId, _tuning.DayBandColor);
                _columnsBlock.SetColor(NightColorId, _tuning.NightBandColor);
                _columnsBlock.SetColor(RimColorId, _tuning.ShimmerRimColor);
                _columnsBlock.SetFloat(IntensityId, envelope * _tuning.MoteMaxIntensity);
                _columnsBlock.SetFloat(NightBlendId, _nightBlend);
                _columnsBlock.SetFloat(PhaseId, phase);
                _columnsBlock.SetFloat(BloomCeilingId, _tuning.BloomCeiling);
                _moteColumnsRenderer.SetPropertyBlock(_columnsBlock);
            }

            if (_moteParticleSystem != null)
            {
                UpdateMoteParticles(_elapsedSeconds, envelope, _nightBlend);
            }
        }

        private void UpdateMoteParticles(float timeSeconds, float envelope, float nightBlend)
        {
            int columns = _tuning.ColumnCount;
            int motesPerCol = _tuning.MotesPerColumn;
            int total = columns * motesPerCol;
            if (_particles == null || _particles.Length < total)
            {
                _particles = new ParticleSystem.Particle[total];
            }

            Color baseTint = Color.Lerp(_tuning.DayBandColor, _tuning.NightBandColor, nightBlend);
            Color moteTint = Color.Lerp(baseTint, _tuning.ShimmerRimColor, 0.35f);
            float baseScale = _tuning.MoteScaleMeters;

            int index = 0;
            for (int c = 0; c < columns; c++)
            {
                for (int m = 0; m < motesPerCol; m++)
                {
                    Vector3 localPos = _tuning.EvaluateMotePosition(c, m, timeSeconds, out float moteAlpha);
                    ParticleSystem.Particle particle = default;
                    particle.position = localPos;
                    particle.velocity = new Vector3(0f, _tuning.RiseSpeedMetersPerSecond, 0f);
                    float scaleVariation = 0.75f + 0.50f * ((c * 7 + m * 13) % 5) * 0.25f;
                    particle.startSize3D = new Vector3(
                        baseScale * 0.55f * scaleVariation,
                        baseScale * 2.10f * scaleVariation,
                        baseScale * 0.55f * scaleVariation);
                    particle.startSize = baseScale * scaleVariation;
                    particle.remainingLifetime = 10f;
                    particle.startLifetime = 10f;

                    Color32 color32 = new Color(
                        moteTint.r,
                        moteTint.g,
                        moteTint.b,
                        Mathf.Clamp01(moteAlpha * envelope));
                    particle.startColor = color32;
                    _particles[index++] = particle;
                }
            }

            _moteParticleSystem.SetParticles(_particles, total);
        }

        private bool EnsureBuffers()
        {
            if (_tuning == null)
            {
                return false;
            }

            if (_bandBlock == null)
            {
                _bandBlock = new MaterialPropertyBlock();
            }

            if (_columnsBlock == null)
            {
                _columnsBlock = new MaterialPropertyBlock();
            }

            int required = _tuning.TotalMoteCount;
            if (_particles == null || _particles.Length < required)
            {
                _particles = new ParticleSystem.Particle[required];
            }

            _configured = true;
            return true;
        }

        private bool ValidateReferences()
        {
            if (_tuning == null || _horizonBandRenderer == null || _moteColumnsRenderer == null || _moteParticleSystem == null)
            {
                Debug.LogError(
                    $"{nameof(LevitatingDustGlowPreview)}: Tuning, HorizonBandRenderer, MoteColumnsRenderer, and " +
                    "MoteParticleSystem must all be assigned.",
                    this);
                return false;
            }

            return true;
        }
    }
}

using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Thin playable-preview component for the 0.6 gentle moonquake dust slide lunar phenomenon
    /// (docs/design/0.6-living-moon.md §2). Drives the down-slope sheeting regolith ribbons, the low ground-skimming
    /// toe settle veil, and discrete ballistic grain particles following clean parabolic arcs under lunar gravity
    /// (1.62 m/s^2) with zero per-frame garbage collection.
    /// </summary>
    public sealed class MoonquakeDustSlidePreview : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int NightColorId = Shader.PropertyToID("_NightColor");
        private static readonly int ShadowColorId = Shader.PropertyToID("_ShadowColor");
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int ScrollOffsetId = Shader.PropertyToID("_ScrollOffset");
        private static readonly int NightBlendId = Shader.PropertyToID("_NightBlend");
        private static readonly int BloomCeilingId = Shader.PropertyToID("_BloomCeiling");

        [Tooltip("Tuning asset governing slide envelope timing, slope geometry, and lunar ballistic arcs.")]
        [SerializeField] private MoonquakeDustSlideTuning _tuning;

        [Tooltip("MeshRenderer for the down-slope sheeting regolith stream ribbons.")]
        [SerializeField] private MeshRenderer _sheetStreamRenderer;

        [Tooltip("MeshRenderer for the low ballistic settle apron at the toe of the slope.")]
        [SerializeField] private MeshRenderer _toeSettleRenderer;

        [Tooltip("ParticleSystem for discrete angular regolith grain clusters arcing down the slope.")]
        [SerializeField] private ParticleSystem _ballisticGrainParticles;

        [Tooltip("Blend between Day C grazing sunlight (0) and Night A cool earthshine (1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _nightBlend;

        [Tooltip("Whether the preview advances its playback clock automatically during Update.")]
        [SerializeField] private bool _autoPlay = true;

        [Tooltip("Whether playback loops after TotalDurationSeconds.")]
        [SerializeField] private bool _loop = true;

        private MaterialPropertyBlock _sheetBlock;
        private MaterialPropertyBlock _settleBlock;
        private ParticleSystem.Particle[] _particles;
        private float _elapsedSeconds;
        private bool _configured;

        public MoonquakeDustSlideTuning Tuning => _tuning;
        public float ElapsedSeconds => _elapsedSeconds;
        public float CurrentSheetEnvelope { get; private set; }
        public float CurrentToeSettleEnvelope { get; private set; }

        /// <summary>
        /// Wires the preview component's serialized references in editor builders or tests and preallocates its
        /// runtime buffers.
        /// </summary>
        public void Configure(
            MoonquakeDustSlideTuning tuning,
            MeshRenderer sheetStreamRenderer,
            MeshRenderer toeSettleRenderer,
            ParticleSystem ballisticGrainParticles)
        {
            _tuning = tuning;
            _sheetStreamRenderer = sheetStreamRenderer;
            _toeSettleRenderer = toeSettleRenderer;
            _ballisticGrainParticles = ballisticGrainParticles;
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
        /// Evaluates and applies the moonquake dust slide state at an explicit <paramref name="timeSeconds"/> and
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

            float sheetEnvelope = _tuning.EvaluateSheetEnvelope(_elapsedSeconds);
            float settleEnvelope = _tuning.EvaluateToeSettleEnvelope(_elapsedSeconds);
            CurrentSheetEnvelope = sheetEnvelope;
            CurrentToeSettleEnvelope = settleEnvelope;

            float scroll = _elapsedSeconds * (_tuning.SheetSpeedMetersPerSecond / Mathf.Max(1f, _tuning.SlopeLengthMeters)) * 4f;

            if (_sheetStreamRenderer != null)
            {
                _sheetBlock.Clear();
                _sheetBlock.SetColor(BaseColorId, _tuning.DayDustColor);
                _sheetBlock.SetColor(NightColorId, _tuning.NightDustColor);
                _sheetBlock.SetColor(ShadowColorId, _tuning.ShadowDustColor);
                _sheetBlock.SetFloat(OpacityId, sheetEnvelope * _tuning.MaxOpacity);
                _sheetBlock.SetFloat(ScrollOffsetId, scroll);
                _sheetBlock.SetFloat(NightBlendId, _nightBlend);
                _sheetBlock.SetFloat(BloomCeilingId, _tuning.BloomCeiling);
                _sheetStreamRenderer.SetPropertyBlock(_sheetBlock);
            }

            if (_toeSettleRenderer != null)
            {
                _settleBlock.Clear();
                _settleBlock.SetColor(BaseColorId, _tuning.DayDustColor);
                _settleBlock.SetColor(NightColorId, _tuning.NightDustColor);
                _settleBlock.SetColor(ShadowColorId, _tuning.ShadowDustColor);
                _settleBlock.SetFloat(OpacityId, settleEnvelope * _tuning.MaxOpacity * 0.88f);
                _settleBlock.SetFloat(ScrollOffsetId, scroll);
                _settleBlock.SetFloat(NightBlendId, _nightBlend);
                _settleBlock.SetFloat(BloomCeilingId, _tuning.BloomCeiling);
                _toeSettleRenderer.SetPropertyBlock(_settleBlock);

                // Scale vertical height of the toe settle apron strictly within SettleCloudMaxHeightMeters
                Vector3 localScale = _toeSettleRenderer.transform.localScale;
                localScale.y = Mathf.Max(0.02f, settleEnvelope);
                _toeSettleRenderer.transform.localScale = localScale;
            }

            if (_ballisticGrainParticles != null)
            {
                UpdateBallisticGrains(_elapsedSeconds, _nightBlend);
            }
        }

        private void UpdateBallisticGrains(float timeSeconds, float nightBlend)
        {
            int count = _tuning.GrainCount;
            if (_particles == null || _particles.Length < count)
            {
                _particles = new ParticleSystem.Particle[count];
            }

            Color dustTone = Color.Lerp(_tuning.DayDustColor, _tuning.NightDustColor, nightBlend);
            float baseSize = _tuning.GrainSizeMeters;
            int activeCount = 0;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = _tuning.EvaluateBallisticGrain(i, timeSeconds, out float alpha);
                if (alpha <= 0.001f)
                {
                    continue;
                }

                ParticleSystem.Particle particle = default;
                particle.position = pos;
                float scaleVariation = 0.7f + 0.6f * ((i * 11) % 5) * 0.25f;
                particle.startSize = baseSize * scaleVariation;
                particle.remainingLifetime = 5f;
                particle.startLifetime = 5f;
                particle.rotation = (i * 47f + timeSeconds * 65f) % 360f;
                particle.startColor = new Color(dustTone.r, dustTone.g, dustTone.b, alpha * _tuning.MaxOpacity);
                _particles[activeCount++] = particle;
            }

            _ballisticGrainParticles.SetParticles(_particles, activeCount);
        }

        private bool EnsureBuffers()
        {
            if (_tuning == null)
            {
                return false;
            }

            if (_sheetBlock == null)
            {
                _sheetBlock = new MaterialPropertyBlock();
            }

            if (_settleBlock == null)
            {
                _settleBlock = new MaterialPropertyBlock();
            }

            int required = _tuning.GrainCount;
            if (_particles == null || _particles.Length < required)
            {
                _particles = new ParticleSystem.Particle[required];
            }

            _configured = true;
            return true;
        }

        private bool ValidateReferences()
        {
            if (_tuning == null || _sheetStreamRenderer == null || _toeSettleRenderer == null || _ballisticGrainParticles == null)
            {
                Debug.LogError(
                    $"{nameof(MoonquakeDustSlidePreview)}: Tuning, SheetStreamRenderer, ToeSettleRenderer, and " +
                    "BallisticGrainParticles must all be assigned.",
                    this);
                return false;
            }

            return true;
        }
    }
}

using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Thin playable-preview component for the 0.6 micrometeorite flash &amp; cooling impact glass lunar phenomenon
    /// (docs/design/0.6-living-moon.md §2). Drives the initial compact white bloom flash burst, vacuum ballistic
    /// ejecta shards, and the 10-minute (600 s) emissive cool-down curve of the fresh crater's impact glass down to its
    /// permanent glint floor with zero per-frame garbage collection.
    /// </summary>
    public sealed class MicrometeoriteFlashPreview : MonoBehaviour
    {
        private static readonly int HeatId = Shader.PropertyToID("_Heat");
        private static readonly int HeatIntensityId = Shader.PropertyToID("_HeatIntensity");
        private static readonly int GlintFloorId = Shader.PropertyToID("_GlintFloor");
        private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");
        private static readonly int HotCoreColorId = Shader.PropertyToID("_HotCoreColor");
        private static readonly int WarmAmberColorId = Shader.PropertyToID("_WarmAmberColor");
        private static readonly int DeepEmberColorId = Shader.PropertyToID("_DeepEmberColor");
        private static readonly int PermanentGlintColorId = Shader.PropertyToID("_PermanentGlintColor");

        [Tooltip("Tuning asset governing the flash burst, vacuum ejecta arcs, and 10-minute glass cool-down curve.")]
        [SerializeField] private MicrometeoriteFlashTuning _tuning;

        [Tooltip("MeshRenderer for the small fresh crater bowl and fused impact glass shards.")]
        [SerializeField] private MeshRenderer _craterAndGlassRenderer;

        [Tooltip("MeshRenderer for the brief white impact flash star burst.")]
        [SerializeField] private MeshRenderer _flashBurstRenderer;

        [Tooltip("ParticleSystem for the vacuum ballistic ejecta spray.")]
        [SerializeField] private ParticleSystem _ejectaParticleSystem;

        [Tooltip("Optional local point light for the brief impact flash and warm molten glow.")]
        [SerializeField] private Light _impactLight;

        [Tooltip("Time scale multiplier for previewing the 10-minute cool-down (1 = real-time, 60 = 10 min in 10 s).")]
        [Range(0.1f, 120f)]
        [SerializeField] private float _previewTimeScale = 1.0f;

        [Tooltip("Whether the preview advances its playback clock automatically during Update.")]
        [SerializeField] private bool _autoPlay = true;

        private MaterialPropertyBlock _glassBlock;
        private MaterialPropertyBlock _flashBlock;
        private ParticleSystem.Particle[] _ejectaParticles;
        private float _elapsedSeconds;
        private bool _configured;

        public MicrometeoriteFlashTuning Tuning => _tuning;
        public float ElapsedSeconds => _elapsedSeconds;
        public float CurrentFlashEnvelope { get; private set; }
        public float CurrentNormalizedHeat { get; private set; }
        public float CurrentEffectiveEmissionIntensity { get; private set; }

        /// <summary>
        /// Wires the preview component's serialized references in editor builders or tests and preallocates its
        /// runtime buffers.
        /// </summary>
        public void Configure(
            MicrometeoriteFlashTuning tuning,
            MeshRenderer craterAndGlassRenderer,
            MeshRenderer flashBurstRenderer,
            ParticleSystem ejectaParticleSystem,
            Light impactLight)
        {
            _tuning = tuning;
            _craterAndGlassRenderer = craterAndGlassRenderer;
            _flashBurstRenderer = flashBurstRenderer;
            _ejectaParticleSystem = ejectaParticleSystem;
            _impactLight = impactLight;
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
                _elapsedSeconds += Time.deltaTime * Mathf.Max(0f, _previewTimeScale);
            }

            Sample(_elapsedSeconds);
        }

        /// <summary>
        /// Evaluates and applies the micrometeorite flash, vacuum ejecta, and impact glass cooling state at an
        /// explicit <paramref name="timeSeconds"/> (0..600+ s). Zero GC allocations after initial buffer setup.
        /// </summary>
        public void Sample(float timeSeconds)
        {
            if (!_configured && !EnsureBuffers())
            {
                return;
            }

            _elapsedSeconds = Mathf.Max(0f, timeSeconds);
            float flashEnvelope = _tuning.EvaluateFlashEnvelope(_elapsedSeconds);
            float normalizedHeat = _tuning.EvaluateNormalizedHeat(_elapsedSeconds);
            _tuning.EvaluateGlassEmission(_elapsedSeconds, out float effectiveIntensity);

            CurrentFlashEnvelope = flashEnvelope;
            CurrentNormalizedHeat = normalizedHeat;
            CurrentEffectiveEmissionIntensity = effectiveIntensity;

            if (_craterAndGlassRenderer != null)
            {
                _glassBlock.Clear();
                _glassBlock.SetFloat(HeatId, normalizedHeat);
                _glassBlock.SetFloat(HeatIntensityId, _tuning.InitialHeatIntensity);
                _glassBlock.SetFloat(GlintFloorId, _tuning.PermanentGlintFloor);
                _glassBlock.SetColor(HotCoreColorId, _tuning.HotCoreColor);
                _glassBlock.SetColor(WarmAmberColorId, _tuning.WarmAmberColor);
                _glassBlock.SetColor(DeepEmberColorId, _tuning.DeepEmberColor);
                _glassBlock.SetColor(PermanentGlintColorId, _tuning.PermanentGlintColor);
                _craterAndGlassRenderer.SetPropertyBlock(_glassBlock);
            }

            if (_flashBurstRenderer != null)
            {
                bool showFlash = flashEnvelope > 0.001f;
                if (_flashBurstRenderer.enabled != showFlash)
                {
                    _flashBurstRenderer.enabled = showFlash;
                }

                if (showFlash)
                {
                    float scale = _tuning.FlashRadiusMeters * Mathf.Lerp(0.38f, 1.05f, flashEnvelope);
                    // Flattened low-profile scale so the flash reads as a compact bloom flash, never a vertical pillar
                    _flashBurstRenderer.transform.localScale = new Vector3(scale, scale * 0.42f, scale);
                    _flashBlock.Clear();
                    _flashBlock.SetFloat(FlashIntensityId, flashEnvelope * _tuning.FlashPeakIntensity);
                    _flashBlock.SetColor(HotCoreColorId, _tuning.FlashColor);
                    _flashBlock.SetColor(WarmAmberColorId, _tuning.WarmAmberColor);
                    _flashBurstRenderer.SetPropertyBlock(_flashBlock);
                }
            }

            if (_impactLight != null)
            {
                float lightFlash = flashEnvelope * _tuning.FlashPeakIntensity * 1.15f;
                float lightHeat = normalizedHeat * 0.85f;
                float totalLight = lightFlash + lightHeat;
                _impactLight.enabled = totalLight > 0.01f;
                if (_impactLight.enabled)
                {
                    _impactLight.intensity = totalLight;
                    _impactLight.color = Color.Lerp(_tuning.WarmAmberColor, _tuning.FlashColor, Mathf.Clamp01(flashEnvelope * 1.5f));
                    _impactLight.range = Mathf.Lerp(2.2f, 4.8f, flashEnvelope);
                }
            }

            if (_ejectaParticleSystem != null)
            {
                UpdateEjectaParticles(_elapsedSeconds, normalizedHeat);
            }
        }

        private void UpdateEjectaParticles(float timeSeconds, float normalizedHeat)
        {
            int total = _tuning.EjectaCount;
            if (_ejectaParticles == null || _ejectaParticles.Length < total)
            {
                _ejectaParticles = new ParticleSystem.Particle[total];
            }

            if (timeSeconds <= 0f || timeSeconds >= _tuning.EjectaDurationSeconds)
            {
                _ejectaParticleSystem.SetParticles(_ejectaParticles, 0);
                return;
            }

            int activeCount = 0;
            float shardSize = _tuning.EjectaShardSizeMeters;
            Color shardColor = Color.Lerp(_tuning.WarmAmberColor, _tuning.HotCoreColor, Mathf.Clamp01(normalizedHeat * 1.1f));

            for (int i = 0; i < total; i++)
            {
                Vector3 pos = _tuning.EvaluateEjectaShard(i, timeSeconds, out float alpha);
                if (alpha <= 0.001f)
                {
                    continue;
                }

                ParticleSystem.Particle particle = default;
                particle.position = pos;
                float sizeScale = 0.7f + 0.6f * ((i * 5) % 4) * 0.25f;
                particle.startSize = shardSize * sizeScale;
                particle.remainingLifetime = _tuning.EjectaDurationSeconds - timeSeconds;
                particle.startLifetime = _tuning.EjectaDurationSeconds;
                particle.startColor = new Color(shardColor.r, shardColor.g, shardColor.b, alpha);
                _ejectaParticles[activeCount++] = particle;
            }

            _ejectaParticleSystem.SetParticles(_ejectaParticles, activeCount);
        }

        private bool EnsureBuffers()
        {
            if (_tuning == null)
            {
                return false;
            }

            if (_glassBlock == null)
            {
                _glassBlock = new MaterialPropertyBlock();
            }

            if (_flashBlock == null)
            {
                _flashBlock = new MaterialPropertyBlock();
            }

            int required = _tuning.EjectaCount;
            if (_ejectaParticles == null || _ejectaParticles.Length < required)
            {
                _ejectaParticles = new ParticleSystem.Particle[required];
            }

            _configured = true;
            return true;
        }

        private bool ValidateReferences()
        {
            if (_tuning == null || _craterAndGlassRenderer == null || _flashBurstRenderer == null || _ejectaParticleSystem == null)
            {
                Debug.LogError(
                    $"{nameof(MicrometeoriteFlashPreview)}: Tuning, CraterAndGlassRenderer, FlashBurstRenderer, and " +
                    "EjectaParticleSystem must all be assigned.",
                    this);
                return false;
            }

            return true;
        }
    }
}

using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's vacuum scan lens emitter VFX (Task SCAN v2): in lunar vacuum a laser fan is invisible in flight, so what
    /// shows at the emitter is a soft cyan glow at 07's lens, a few tiny motes sparkling right at the lens (never a
    /// mid-air sheet), and a warm-amber lens pulse on the completion ping. Zero allocations per frame.
    /// </summary>
    public sealed class ScanBeam
    {
        private const int MoteCount = 6;
        private const int PointSlots = 3 + MoteCount;
        private const float VisibleThreshold = 1e-4f;
        private const float MinFadeSeconds = 1e-3f;
        private const float MinDistance = 1e-4f;
        private const float LensSurfaceStandOff = 0.024f;
        private const float MinWidthShare = 0.65f;
        private const float DefaultPingDuration = 0.38f;

        private static readonly Color DefaultCyanColor = new Color(0.20f, 0.86f, 0.96f, 1f);
        private static readonly Color DefaultAmberPingColor = new Color(1.00f, 0.682f, 0.220f, 1f);

        private readonly FeelVfxTuning _tuning;
        private readonly ScanTuning _scanTuning;
        private readonly FeelSprites _points;

        private Vector3 _target;
        private float _progress;
        private float _rate;
        private float _level;
        private float _elapsed;
        private float _pingRemaining;
        private float _pingDuration = DefaultPingDuration;
        private bool _active;

        public ScanBeam(Transform parent, FeelVfxTuning tuning, Material beamMaterial, Material spriteMaterial,
            ScanTuning scanTuning = null)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _scanTuning = scanTuning;
            if (beamMaterial == null)
            {
                throw new ArgumentNullException(nameof(beamMaterial), "ScanBeam: the scan beam material is missing.");
            }

            _points = new FeelSprites("ScanBeamPoints", parent, spriteMaterial, PointSlots, false, 0f);
        }

        /// <summary>True while the lens emitter glow or its completion ping is active or easing out.</summary>
        public bool Shown => _level > VisibleThreshold || _pingRemaining > VisibleThreshold;

        /// <summary>Current eased visibility level (0..1).</summary>
        public float Level => Ease.InOutSine(_level);

        /// <summary>True while the completion ping flash is active.</summary>
        public bool IsPinging => _pingRemaining > VisibleThreshold;

        /// <summary>Starts or resumes the scan emitter glow at <paramref name="progress"/> (0..1) over <paramref name="remainingSeconds"/>.</summary>
        public void Begin(Vector3 target, Vector3 span, float progress, float remainingSeconds)
        {
            _target = target;
            _progress = Mathf.Clamp01(progress);
            _rate = remainingSeconds > MinFadeSeconds ? (1f - _progress) / remainingSeconds : 0f;
            _pingRemaining = 0f;
            _active = true;
        }

        /// <summary>Keeps progress in step with quarter beats.</summary>
        public void Sync(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }

        /// <summary>Eases the lens glow out on let-go.</summary>
        public void End()
        {
            _active = false;
        }

        /// <summary>Triggers the single clean completion ping pulse at the lens, then fades out.</summary>
        public void Complete()
        {
            _progress = 1f;
            _active = false;
            _pingDuration = _scanTuning != null ? _scanTuning.PingDurationSeconds : DefaultPingDuration;
            _pingRemaining = _pingDuration;
        }

        public void Step(float deltaTime, Vector3 emitter)
        {
            _elapsed += deltaTime;
            float fadeSeconds = _active ? _tuning.ScanEaseInSeconds : _tuning.ScanEaseOutSeconds;
            _level = Mathf.MoveTowards(_level, _active ? 1f : 0f, deltaTime / Mathf.Max(fadeSeconds, MinFadeSeconds));
            if (_active)
            {
                _progress = Mathf.Clamp01(_progress + _rate * deltaTime);
            }

            if (_pingRemaining > 0f)
            {
                _pingRemaining = Mathf.Max(0f, _pingRemaining - deltaTime);
            }

            _points.Begin();
            if (Shown)
            {
                StepLensEmitterAndMotes(emitter);
            }

            _points.End();
        }

        private void StepLensEmitterAndMotes(Vector3 emitter)
        {
            Color cyanColor = _scanTuning != null ? _scanTuning.ScanLineColor : DefaultCyanColor;
            Color amberColor = _scanTuning != null ? _scanTuning.CompletionPingColor : DefaultAmberPingColor;
            float coreSize = _scanTuning != null ? _scanTuning.LensGlowSize : _tuning.ScanLensCoreSize;
            float coreAlpha = _scanTuning != null ? _scanTuning.LensGlowAlpha : _tuning.ScanLensCoreAlpha;
            float haloSize = _scanTuning != null ? _scanTuning.LensHaloSize : _tuning.ScanLensHaloSize;
            float haloAlpha = _scanTuning != null ? _scanTuning.LensHaloAlpha : _tuning.ScanLensHaloAlpha;
            float moteRadius = _scanTuning != null ? _scanTuning.LensMoteRadius : 0.042f;
            float moteSizeBase = _scanTuning != null ? _scanTuning.LensMoteSize : 0.014f;
            float moteAlphaBase = _scanTuning != null ? _scanTuning.LensMoteAlpha : 0.16f;

            float eased = Ease.InOutSine(_level);
            float ping01 = _pingRemaining > 0f
                ? Mathf.Sin((1f - _pingRemaining / Mathf.Max(_pingDuration, MinFadeSeconds)) * Mathf.PI)
                : 0f;
            float visibility = Mathf.Max(eased, ping01);
            float widthScale = Mathf.Lerp(MinWidthShare, 1f, visibility);

            Vector3 toTarget = _target - emitter;
            float dist = toTarget.magnitude;
            Vector3 forward = dist > MinDistance ? toTarget / dist : Vector3.forward;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward);
            right = right.sqrMagnitude > MinDistance ? right.normalized : Vector3.right;
            Vector3 lensFront = emitter + forward * LensSurfaceStandOff;

            Color activeTint = Color.Lerp(cyanColor, amberColor, ping01 * 0.85f);
            Color core = Color.Lerp(activeTint, Color.white, 0.28f);
            core.a = coreAlpha * visibility;
            _points.Add(lensFront, coreSize * widthScale, core, Vector3.zero);

            Color halo = activeTint;
            halo.a = haloAlpha * visibility;
            _points.Add(lensFront, haloSize * widthScale, halo, Vector3.zero);

            if (moteAlphaBase <= VisibleThreshold || eased <= VisibleThreshold)
            {
                return;
            }

            for (int i = 0; i < MoteCount; i++)
            {
                float phase = _elapsed * 0.95f + i * 0.41f;
                float life01 = phase - Mathf.Floor(phase);
                float env = Mathf.Sin(life01 * Mathf.PI);
                float angle = i * 1.0471976f + _elapsed * 0.55f;
                float radial = moteRadius * (0.35f + 0.65f * life01);
                Vector3 offset = (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radial
                    + forward * (0.012f * life01);
                Color moteColor = (i == 0 && ping01 > 0.1f) ? amberColor : cyanColor;
                moteColor.a = moteAlphaBase * env * eased;
                float size = moteSizeBase * Mathf.Lerp(0.75f, 1.15f, i & 1) * widthScale;
                _points.Add(lensFront + offset, size, moteColor, Vector3.zero);
            }
        }
    }
}

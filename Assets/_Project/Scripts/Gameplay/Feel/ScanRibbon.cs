using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The scan's closing look (docs/design/0.5.5-owner-feedback.md §1.2, D2), started by the Scan beat's Reward as
    /// the fragment's dissolve begins: once its solid has faded, its white lines stream into 07's lens as a ribbon of
    /// light on an ease-in, and the lens flares once as the last of them arrives. The fragment's own dissolve is its
    /// shader's; this draws only the light between it and 07. Pooled: steady state allocates nothing.
    /// </summary>
    public sealed class ScanRibbon
    {
        // The ribbon's tail beads are this share of the head's size and opacity.
        private const float TailShare = 0.3f;

        // The flare swells by this share of its size as it fades.
        private const float FlareSwell = 0.4f;

        private readonly FeelVfxTuning _tuning;
        private readonly ScanTuning _scanTuning;
        private readonly FeelSprites _sprites;
        private readonly FeelSprites _flare;
        private readonly LightPulse _flareLight;
        private Vector3 _from;
        private Vector3 _span = new Vector3(0f, 0.65f, 0f);
        private float _age = float.PositiveInfinity;
        private bool _flared;

        public ScanRibbon(Transform parent, FeelVfxTuning tuning, Material sprite, ScanTuning scanTuning = null)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _scanTuning = scanTuning;
            _sprites = new FeelSprites("ScanRibbon", parent, sprite, tuning.ScanRibbonBeads, true, 1f);
            _flare = new FeelSprites("ScanFlare", parent, sprite, 1, false, 0f);
            _flareLight = new LightPulse("ScanFlareLight", parent, tuning.ScanRibbonColor, tuning.EmitterRange);
        }

        /// <summary>True from the Reward until the flare has faded.</summary>
        public bool Active => _age < End;

        /// <summary>The ribbon's beads drawn this frame.</summary>
        public int Shown => _sprites.Count;

        /// <summary>True while the lens's flare is drawn.</summary>
        public bool Flaring => _flare.Count > 0;

        public LightPulse FlareLight => _flareLight;

        private float StreamEnd => _tuning.ScanRibbonDelay + _tuning.ScanRibbonSeconds;

        private float End => StreamEnd + _tuning.ScanFlareSeconds;

        /// <summary>The dissolve begins on the fragment whose scan point is <paramref name="from"/> (world).</summary>
        public void Begin(Vector3 from)
        {
            Begin(from, new Vector3(0f, 0.65f, 0f));
        }

        /// <summary>The dissolve begins on the fragment at <paramref name="from"/> with vertical <paramref name="span"/>.</summary>
        public void Begin(Vector3 from, Vector3 span)
        {
            _from = from;
            _span = span.sqrMagnitude > 1e-4f ? span : new Vector3(0f, 0.65f, 0f);
            _age = 0f;
            _flared = false;
        }

        /// <summary>Advances the ribbon toward <paramref name="lens"/>, 07's lens this frame (world).</summary>
        public void Step(float deltaTime, Vector3 lens)
        {
            Vector3 toFrom = _from - lens;
            float distance = toFrom.magnitude;
            Vector3 forward = distance > 1e-4f ? toFrom / distance : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            right = right.sqrMagnitude > 1e-4f ? right.normalized : Vector3.right;
            Vector3 aperture = distance > 1e-4f
                ? lens - right * 0.074f - forward * 0.056f
                : lens;
            _flareLight.MoveTo(aperture);
            _flareLight.Step(deltaTime);
            _sprites.Begin();
            _flare.Begin();
            if (_age < End)
            {
                _age += deltaTime;
                if (_age >= StreamEnd)
                {
                    Flare(aperture, (_age - StreamEnd) / _tuning.ScanFlareSeconds);
                }
                else if (_age >= _tuning.ScanRibbonDelay)
                {
                    Stream(aperture, right, forward, (_age - _tuning.ScanRibbonDelay) / _tuning.ScanRibbonSeconds);
                }
            }

            _sprites.End();
            _flare.End();
        }

        private void Stream(Vector3 lens, Vector3 right, Vector3 forward, float t)
        {
            int beads = _tuning.ScanRibbonBeads;
            ScanVariant variant = _scanTuning != null ? _scanTuning.Variant : ScanVariant.ProfilerSweep;
            float shellRadius = Mathf.Clamp(Mathf.Abs(_span.y) * 0.42f, 0.16f, 0.46f);
            float baseSize = _scanTuning != null ? Mathf.Max(_scanTuning.LensMoteSize * 1.6f, 0.012f) : 0.014f;
            Color cyanColor = _scanTuning != null ? _scanTuning.ScanLineColor : _tuning.ScanRibbonColor;
            Color amberColor = _scanTuning != null ? _scanTuning.CompletionPingColor : _tuning.ScanRibbonColor;

            // Hold on the target's 3D blueprint shell during the initial completion ping peak, then collapse into the lens
            float collapseT = Mathf.InverseLerp(0.12f, 1.0f, Mathf.Clamp01(t));
            Vector2 span = FeelCurves.RibbonSpan(collapseT, _tuning.ScanRibbonLength);
            float env = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

            for (int i = 0; i < beads; i++)
            {
                float toHead = (float)i / (beads - 1);
                float weight = Mathf.Lerp(TailShare, 1f, toHead);
                float s = Mathf.Lerp(span.x, span.y, toHead);

                float angle = i * 2.3999632f + collapseT * 2.2f;
                float elev = ((i % 5) - 2f) * 0.22f;
                Vector3 shellOffset = (right * Mathf.Cos(angle) + Vector3.up * elev - forward * (0.35f * Mathf.Sin(angle)))
                    * (shellRadius * (1f - s * 0.88f));
                Vector3 startPos = _from + shellOffset;
                Vector3 pos = FeelCurves.RibbonPoint(startPos, lens, _tuning.ScanRibbonArc, s);

                Vector3 toLens = lens - pos;
                float distToLens = toLens.magnitude;
                Vector3 dirToLens = distToLens > 1e-4f ? toLens / distToLens : -forward;
                float stretchScale = variant == ScanVariant.WireframeReveal ? 0.055f : 0.085f;
                Vector3 stretch = dirToLens * (Mathf.Lerp(0.022f, stretchScale, Mathf.Sin(s * Mathf.PI)) * weight);

                Color color = variant == ScanVariant.WireframeReveal
                    ? Color.Lerp(amberColor, Color.white, 0.24f * toHead)
                    : Color.Lerp(cyanColor, amberColor, Mathf.Lerp(0.35f, 0.75f, 1f - s));
                color.a = Mathf.Min(0.22f, (_scanTuning != null ? _scanTuning.LensMoteAlpha * 0.55f : 0.16f) * weight * env);
                _sprites.Add(pos, baseSize * weight, color, stretch);
            }

            if (t > 0.35f)
            {
                float catchWeight = Mathf.InverseLerp(0.35f, 1f, t);
                Color lensCatch = variant == ScanVariant.WireframeReveal
                    ? amberColor
                    : Color.Lerp(cyanColor, amberColor, 0.55f);
                lensCatch.a = Mathf.Min(0.28f, _tuning.ScanLensCoreAlpha * (0.75f + 0.25f * catchWeight) * env);
                _flare.Add(lens, _tuning.ScanLensCoreSize * (1f + 0.15f * catchWeight), lensCatch, Vector3.zero);
            }
        }

        private void Flare(Vector3 lens, float t)
        {
            if (!_flared)
            {
                _flared = true;
                _flareLight.Ramp(0f, 0f, false);
            }

            float u = Mathf.Clamp01(t);
            Color color = _tuning.ScanCoreColor;
            color.a = _tuning.ScanLensCoreAlpha * (1f - u * u);
            _flare.Add(lens, _tuning.ScanFlareSize * (1f + FlareSwell * u), color, Vector3.zero);
        }
    }
}

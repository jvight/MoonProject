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
            _from = from;
            _age = 0f;
            _flared = false;
        }

        /// <summary>Advances the ribbon toward <paramref name="lens"/>, 07's lens this frame (world).</summary>
        public void Step(float deltaTime, Vector3 lens)
        {
            Vector3 toFrom = _from - lens;
            float distance = toFrom.magnitude;
            Vector3 aperture = distance > 1e-4f
                ? lens + (toFrom / distance) * 0.025f
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
                    Stream(aperture, (_age - _tuning.ScanRibbonDelay) / _tuning.ScanRibbonSeconds);
                }
            }

            _sprites.End();
            _flare.End();
        }

        private void Stream(Vector3 lens, float t)
        {
            int beads = _tuning.ScanRibbonBeads;
            float radius = _scanTuning != null ? _scanTuning.LensMoteRadius : 0.042f;
            float moteSize = _scanTuning != null ? _scanTuning.LensMoteSize : 0.014f;
            Color baseColor = _scanTuning != null ? _scanTuning.CompletionPingColor : _tuning.ScanRibbonColor;
            float env = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            for (int i = 0; i < beads; i++)
            {
                float toHead = (float)i / (beads - 1);
                float weight = Mathf.Lerp(TailShare, 1f, toHead);
                float angle = i * 2.3999632f + t * 4f;
                float r = radius * (1f - 0.7f * Mathf.Clamp01(t)) * (0.25f + 0.75f * toHead);
                var offset = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0f);
                Color color = baseColor;
                color.a = (_scanTuning != null ? _scanTuning.LensMoteAlpha : 0.14f) * weight * env;
                _sprites.Add(lens + offset, moteSize * weight, color, Vector3.zero);
            }

            if (t > 0.35f)
            {
                float catchWeight = Mathf.InverseLerp(0.35f, 1f, t);
                Color lensCatch = baseColor;
                lensCatch.a = _tuning.ScanLensCoreAlpha * (0.85f + 0.35f * catchWeight) * env;
                _flare.Add(lens, _tuning.ScanLensCoreSize * (1f + 0.15f * catchWeight), lensCatch, Vector3.zero);
            }
        }

        private void Flare(Vector3 lens, float t)
        {
            if (!_flared)
            {
                _flared = true;
                _flareLight.Ramp(_tuning.ScanFlareLight, 0f, false);
                _flareLight.Ramp(0f, _tuning.ScanFlareSeconds, false);
            }

            float u = Mathf.Clamp01(t);
            Color color = _tuning.ScanCoreColor;
            color.a = _tuning.ScanLensCoreAlpha * (1f - u * u);
            _flare.Add(lens, _tuning.ScanFlareSize * (1f + FlareSwell * u), color, Vector3.zero);
        }
    }
}

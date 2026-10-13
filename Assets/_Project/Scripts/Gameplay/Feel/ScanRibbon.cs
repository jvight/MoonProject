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

        // Each bead stretches along the path over this many gaps to the next, so the beads join into one stream.
        private const float Overlap = 1.6f;

        private readonly FeelVfxTuning _tuning;
        private readonly FeelSprites _sprites;
        private readonly FeelSprites _flare;
        private readonly LightPulse _flareLight;
        private Vector3 _from;
        private float _age = float.PositiveInfinity;
        private bool _flared;

        public ScanRibbon(Transform parent, FeelVfxTuning tuning, Material sprite)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
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
            _flareLight.MoveTo(lens);
            _flareLight.Step(deltaTime);
            _sprites.Begin();
            _flare.Begin();
            if (_age < End)
            {
                _age += deltaTime;
                if (_age >= StreamEnd)
                {
                    Flare(lens, (_age - StreamEnd) / _tuning.ScanFlareSeconds);
                }
                else if (_age >= _tuning.ScanRibbonDelay)
                {
                    Stream(lens, (_age - _tuning.ScanRibbonDelay) / _tuning.ScanRibbonSeconds);
                }
            }

            _sprites.End();
            _flare.End();
        }

        private void Stream(Vector3 lens, float t)
        {
            Vector2 span = FeelCurves.RibbonSpan(t, _tuning.ScanRibbonLength);
            int beads = _tuning.ScanRibbonBeads;
            float arc = _tuning.ScanRibbonArc;
            float gap = (span.y - span.x) / (beads - 1);
            Color color = _tuning.ScanRibbonColor;
            for (int i = 0; i < beads; i++)
            {
                float toHead = (float)i / (beads - 1);
                float along = Mathf.Lerp(span.x, span.y, toHead);
                float weight = Mathf.Lerp(TailShare, 1f, toHead);
                Vector3 at = FeelCurves.RibbonPoint(_from, lens, arc, along);
                Vector3 ahead = FeelCurves.RibbonPoint(_from, lens, arc, along + gap);
                color.a = weight;
                _sprites.Add(at, _tuning.ScanRibbonSize * weight, color, (ahead - at) * Overlap);
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
            Color color = _tuning.ScanRibbonColor;
            color.a = 1f - u * u;
            _flare.Add(lens, _tuning.ScanFlareSize * (1f + FlareSwell * u), color, Vector3.zero);
        }
    }
}

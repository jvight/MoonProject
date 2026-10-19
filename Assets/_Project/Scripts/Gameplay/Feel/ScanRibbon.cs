using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The scan's closing look (docs/design/0.5.5-owner-feedback.md §1.2, D2), started by the Scan beat's Reward as
    /// the fragment's dissolve begins: once its solid has faded, its white lines stream into 07's lens as a ribbon of
    /// light on an ease-in, and the lens flares once as the last of them arrives. The fragment's own dissolve is its
    /// shader's; this draws only the light between it and 07. Its shape is <see cref="ScanBeamLook"/>'s ribbon.
    /// Pooled: steady state allocates nothing.
    /// </summary>
    public sealed class ScanRibbon
    {
        // The ribbon's tail beads are this share of the head's size and opacity.
        private const float TailShare = 0.3f;

        // The flare swells by this share of its size as it fades.
        private const float FlareSwell = 0.4f;

        // Beads leave the target's shell a golden angle apart, so they never line up.
        private const float GoldenAngle = 2.3999632f;

        private const float MinDistance = 1e-4f;

        private readonly FeelVfxTuning _tuning;
        private readonly ScanTuning _scanTuning;
        private readonly ScanBeamLook _look;
        private readonly FeelSprites _sprites;
        private readonly FeelSprites _flare;
        private Vector3 _from;
        private Vector3 _span;
        private float _age = float.PositiveInfinity;

        public ScanRibbon(Transform parent, FeelVfxTuning tuning, Material sprite, ScanTuning scanTuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _scanTuning = scanTuning != null ? scanTuning : throw new ArgumentNullException(nameof(scanTuning));
            _look = scanTuning.BeamLook;
            _span = new Vector3(0f, _look.RibbonFallbackSpan, 0f);
            _sprites = new FeelSprites("ScanRibbon", parent, sprite, tuning.ScanRibbonBeads, true, 1f);
            _flare = new FeelSprites("ScanFlare", parent, sprite, 1, false, 0f);
        }

        /// <summary>The ribbon's beads drawn this frame.</summary>
        public int Shown => _sprites.Count;

        private float StreamEnd => _tuning.ScanRibbonDelay + _tuning.ScanRibbonSeconds;

        private float End => StreamEnd + _tuning.ScanFlareSeconds;

        /// <summary>The dissolve begins on the fragment at <paramref name="from"/> with vertical <paramref name="span"/>.</summary>
        public void Begin(Vector3 from, Vector3 span)
        {
            _from = from;
            _span = span.sqrMagnitude > MinDistance ? span : new Vector3(0f, _look.RibbonFallbackSpan, 0f);
            _age = 0f;
        }

        /// <summary>Advances the ribbon toward <paramref name="lens"/>, 07's lens this frame (world).</summary>
        public void Step(float deltaTime, Vector3 lens)
        {
            Vector3 toFrom = _from - lens;
            float distance = toFrom.magnitude;
            Vector3 forward = distance > MinDistance ? toFrom / distance : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            right = right.sqrMagnitude > MinDistance ? right.normalized : Vector3.right;
            Vector2 apertureOffset = _look.ApertureOffset;
            Vector3 aperture = distance > MinDistance
                ? lens - right * apertureOffset.x - forward * apertureOffset.y
                : lens;
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
            ScanBeamLook look = _look;
            int beads = _tuning.ScanRibbonBeads;
            Vector2 shellRange = look.RibbonShellRange;
            float shellRadius = Mathf.Clamp(Mathf.Abs(_span.y) * look.RibbonShellShare, shellRange.x, shellRange.y);
            float baseSize = Mathf.Max(_scanTuning.LensMoteSize * look.RibbonBeadShare, look.RibbonBeadMin);
            float beadAlpha = _scanTuning.LensMoteAlpha * look.RibbonAlphaShare;
            Color pingColor = _scanTuning.CompletionPingColor;
            int rows = look.RibbonShellRows;
            float middleRow = (rows - 1) * 0.5f;

            // Hold on the target's 3D blueprint shell during the initial completion ping peak, then collapse into the lens
            float collapseT = Mathf.InverseLerp(look.RibbonHold, 1.0f, Mathf.Clamp01(t));
            Vector2 span = FeelCurves.RibbonSpan(collapseT, _tuning.ScanRibbonLength);
            float env = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

            for (int i = 0; i < beads; i++)
            {
                float toHead = (float)i / (beads - 1);
                float weight = Mathf.Lerp(TailShare, 1f, toHead);
                float s = Mathf.Lerp(span.x, span.y, toHead);

                float angle = i * GoldenAngle + collapseT * look.RibbonCollapseTurn;
                float elev = ((i % rows) - middleRow) * look.RibbonRowSpacing;
                Vector3 shellOffset = (right * Mathf.Cos(angle) + Vector3.up * elev
                                       - forward * (look.RibbonShellDepth * Mathf.Sin(angle)))
                                      * (shellRadius * (1f - s * look.RibbonShellCollapse));
                Vector3 startPos = _from + shellOffset;
                Vector3 pos = FeelCurves.RibbonPoint(startPos, lens, _tuning.ScanRibbonArc, s);

                Vector3 toLens = lens - pos;
                float distToLens = toLens.magnitude;
                Vector3 dirToLens = distToLens > MinDistance ? toLens / distToLens : -forward;
                float stretchLength = Mathf.Lerp(look.RibbonStretch.x, look.RibbonStretch.y, Mathf.Sin(s * Mathf.PI));
                Vector3 stretch = dirToLens * (stretchLength * weight);
                Color color = Color.Lerp(pingColor, Color.white, look.RibbonHeadWhite * toHead);
                color.a = Mathf.Min(look.RibbonAlphaCap, beadAlpha * weight * env);
                _sprites.Add(pos, baseSize * weight, color, stretch);
            }

            if (t > look.CatchStart)
            {
                float catchWeight = Mathf.InverseLerp(look.CatchStart, 1f, t);
                Color lensCatch = pingColor;
                float catchAlpha = _tuning.ScanLensCoreAlpha * Mathf.Lerp(look.CatchAlphaStart, 1f, catchWeight);
                lensCatch.a = Mathf.Min(look.CatchAlphaCap, catchAlpha * env);
                float catchSize = _tuning.ScanLensCoreSize * (1f + look.CatchSwell * catchWeight);
                _flare.Add(lens, catchSize, lensCatch, Vector3.zero);
            }
        }

        private void Flare(Vector3 lens, float t)
        {
            float u = Mathf.Clamp01(t);
            Color color = _tuning.ScanCoreColor;
            color.a = _tuning.ScanLensCoreAlpha * (1f - u * u);
            _flare.Add(lens, _tuning.ScanFlareSize * (1f + FlareSwell * u), color, Vector3.zero);
        }
    }
}

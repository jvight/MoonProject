using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Turns a <see cref="LensViewState"/> into what the lens shader and the depth of field draw: the lid's edge, the
    /// exposure dip (capped at the settings' maximum, never above 30%), where the roll band is, and how sharp the
    /// dust speck is. The roll band only travels while the picture dips, so a flicker passes it down the frame once.
    /// </summary>
    public sealed class LensViewModel
    {
        private readonly LensViewSettings _settings;

        public LensViewModel(LensViewSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            RollBand = 1f + settings.RollBandHeight;
        }

        /// <summary>Height of the lid's edge in the middle of the frame (viewport heights; at or below 0 = shut).</summary>
        public float LidEdge { get; private set; }

        /// <summary>Fraction of the light the old electronics take away right now (0..MaxDip).</summary>
        public float Dip { get; private set; }

        /// <summary>Centre of the roll band, in viewport heights from the bottom; it starts just above the frame.</summary>
        public float RollBand { get; private set; }

        /// <summary>0 = the speck is fully blurred, 1 = it sits sharp on the glass.</summary>
        public float SpeckSharpness { get; private set; }

        /// <summary>Focus distance for the depth of field (m), never nearer than the lens can hunt.</summary>
        public float FocusDistance { get; private set; }

        /// <summary>How much of the warm lift shows (0..1).</summary>
        public float Warmth { get; private set; }

        /// <summary>
        /// Viewport height of the lid's edge at the frame's middle for an openness of <paramref name="lid01"/>: 0 is
        /// below the frame (shut), 1 lifts the edge, its arc and its softness all clear of the top.
        /// </summary>
        public static float EdgeFor(float lid01, LensViewSettings settings)
        {
            return Mathf.Clamp01(lid01) * (1f + settings.LidSoftness + settings.LidCurve);
        }

        public void Step(in LensViewState state, float deltaTime)
        {
            LidEdge = EdgeFor(state.Lid01, _settings);
            float flicker = Mathf.Clamp01(state.Flicker01);
            Dip = flicker * _settings.MaxDip;
            if (flicker > 0f)
            {
                float span = 1f + 2f * _settings.RollBandHeight;
                float travelled = 1f + _settings.RollBandHeight - RollBand
                    + flicker * _settings.RollBandSpeed * Mathf.Max(0f, deltaTime);
                RollBand = 1f + _settings.RollBandHeight - Mathf.Repeat(travelled, span);
            }

            FocusDistance = Mathf.Max(_settings.NearestFocus, state.FocusDistance);
            SpeckSharpness = 1f - Mathf.InverseLerp(_settings.SpeckSharpDistance, _settings.SpeckBlurDistance,
                FocusDistance);
            Warmth = Mathf.Clamp01(state.Warmth01);
        }
    }
}

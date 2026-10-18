using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Turns a <see cref="LensViewState"/> into what the lens shader and the depth of field draw: the lid's edge, the
    /// focus and the eye's gain while the lid is barely open. The gain follows the widest the lid has opened since it
    /// was last shut (or the last <see cref="Reset"/>), so a blink never brings it back.
    /// </summary>
    public sealed class LensViewModel
    {
        /// <summary>A lid at or below this counts as shut: the next opening starts the eye's gain afresh.</summary>
        private const float ShutLid = 0.01f;

        private readonly LensViewSettings _settings;
        private float _widestLid;

        public LensViewModel(LensViewSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Height of the lid's edge in the middle of the frame (viewport heights; at or below 0 = shut).</summary>
        public float LidEdge { get; private set; }

        /// <summary>Focus distance for the depth of field (m), never nearer than the lens can hunt.</summary>
        public float FocusDistance { get; private set; }

        /// <summary>Exposure (EV) the eye adds over the world's own: full while the lid is barely open, then gone.</summary>
        public float Gain { get; private set; }

        /// <summary>
        /// Viewport height of the lid's edge at the frame's middle for an openness of <paramref name="lid01"/>: 0 is
        /// below the frame (shut), 1 lifts the edge, its arc and its softness all clear of the top.
        /// </summary>
        public static float EdgeFor(float lid01, LensViewSettings settings)
        {
            return Mathf.Pow(Mathf.Clamp01(lid01), settings.LidOpenGamma)
                * (1f + settings.LidSoftness + settings.LidCurve);
        }

        /// <summary>Starts a new look through the lens: the eye's gain is full again.</summary>
        public void Reset()
        {
            _widestLid = 0f;
        }

        public void Step(in LensViewState state)
        {
            LidEdge = EdgeFor(state.Lid01, _settings);
            float lid = Mathf.Clamp01(state.Lid01);
            _widestLid = lid <= ShutLid ? 0f : Mathf.Max(_widestLid, lid);
            Gain = _settings.WakeGain
                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_settings.GainHoldLid, _settings.GainGoneLid,
                    _widestLid)));
            FocusDistance = Mathf.Max(_settings.NearestFocus, state.FocusDistance);
        }
    }
}

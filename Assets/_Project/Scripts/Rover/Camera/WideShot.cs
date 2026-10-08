using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// When the lonely wide shot opens and how far open it is, independent of Cinemachine so it can be tested:
    /// <list type="bullet">
    /// <item>It counts quiet time: seconds 07 has rested (<c>IRoverStillness</c>) with nothing else going on. Busy
    /// (a camera moment, the tether) or held (the pause menu, a card held back by <see cref="HoldBack"/>) time does not
    /// count. After the tuned delay <see cref="Step"/> cues <see cref="WideShotCue.Open"/>.</item>
    /// <item>Once open, any end of the rest (drive or look input, 07 moving) or anything busy cues
    /// <see cref="WideShotCue.HandBack"/>; held time leaves an open frame alone.</item>
    /// <item><see cref="Weight"/> follows a critically damped spring: slow toward the wide frame (settling over the
    /// open time), quick back (the hand-back time). The spring keeps its velocity when the target flips, so a
    /// hand-back mid-opening turns round smoothly instead of jerking.</item>
    /// </list>
    /// </summary>
    public sealed class WideShot
    {
        /// <summary>A critically damped spring from rest covers 95% of the way once omega * t reaches this.</summary>
        public const float SettleOmegaTime = 4.744f;

        private const float CriticalDamping = 1f;

        private readonly WideShotSettings _settings;
        private DampedSpring _weight;
        private float _quiet;
        private float _holdBack;

        public WideShot(WideShotSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True from <see cref="Open"/> until <see cref="HandBack"/>: the frame is opening or open.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>How far the camera is toward the wide frame, 0 (chase camera) .. 1 (settled wide).</summary>
        public float Weight => Mathf.Clamp01(_weight.Value);

        /// <summary>Seconds of uninterrupted rest counted toward opening.</summary>
        public float QuietSeconds => _quiet;

        /// <summary>The frame being opened to (set by <see cref="Open"/>).</summary>
        public WideShotFrame Frame { get; private set; }

        /// <summary>
        /// Seconds since the frame last opened: drives the breathing, which keeps going while it hands back so the
        /// sway fades with the weight instead of jumping.
        /// </summary>
        public float BreathSeconds { get; private set; }

        /// <summary>Spring frequency (Hz) that covers 95% of the way in <paramref name="settleSeconds"/>.</summary>
        public static float Frequency(float settleSeconds)
        {
            return SettleOmegaTime / (2f * Mathf.PI * Mathf.Max(settleSeconds, 1e-3f));
        }

        /// <summary>
        /// Keeps the frame from opening for at least <paramref name="seconds"/> (a card to read, an interaction just
        /// happened); an open frame is left alone.
        /// </summary>
        public void HoldBack(float seconds)
        {
            _holdBack = Mathf.Max(_holdBack, seconds);
        }

        /// <param name="stillSeconds">How long 07 has rested (<c>IRoverStillness.StillSeconds</c>).</param>
        /// <param name="busy">Something needs the normal camera now: an open frame hands back.</param>
        /// <param name="held">Opening must wait, but an open frame stays (the pause menu).</param>
        public WideShotCue Step(float stillSeconds, bool busy, bool held, float deltaTime)
        {
            float step = Mathf.Max(deltaTime, 0f);
            _holdBack = Mathf.Max(0f, _holdBack - step);
            bool resting = stillSeconds > 0f && !busy;
            bool quiet = resting && !held && _holdBack <= 0f;
            _quiet = quiet ? Mathf.Min(_quiet + step, stillSeconds) : 0f;

            float settle = IsOpen ? _settings.OpenSeconds : _settings.HandBackSeconds;
            _weight.Step(IsOpen ? 1f : 0f, Frequency(settle), CriticalDamping, step);
            _weight.Clamp(0f, 1f);
            BreathSeconds += step;
            if (IsOpen)
            {
                return resting ? WideShotCue.None : WideShotCue.HandBack;
            }

            return _quiet >= _settings.Delay ? WideShotCue.Open : WideShotCue.None;
        }

        /// <summary>Starts easing out to <paramref name="frame"/>.</summary>
        public void Open(WideShotFrame frame)
        {
            Frame = frame;
            IsOpen = true;
            BreathSeconds = 0f;
        }

        /// <summary>Starts easing back to the chase camera; the rest must start over before it opens again.</summary>
        public void HandBack()
        {
            IsOpen = false;
            _quiet = 0f;
        }

        /// <summary>Back to the chase camera at once, no ease (07 was placed elsewhere in the dark).</summary>
        public void Close()
        {
            HandBack();
            _weight.Reset(0f);
        }
    }
}

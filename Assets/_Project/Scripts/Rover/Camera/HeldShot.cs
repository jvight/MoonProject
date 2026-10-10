using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// A framed view the game opens and closes rather than 07's rest (the lift's deck lookout, the dock's evening),
    /// independent of Cinemachine so it can be tested:
    /// <list type="bullet">
    /// <item><see cref="Open"/> eases out to its frame over its settings' open time; <see cref="Close"/> eases back
    /// over their hand-back time. The weight is a critically damped spring like the wide shot's, so closing
    /// mid-opening turns round smoothly.</item>
    /// <item>With a hold, <see cref="HoldRanOut"/> says when to close it; else it stays open until closed.</item>
    /// </list>
    /// </summary>
    public sealed class HeldShot
    {
        private const float CriticalDamping = 1f;

        /// <summary>A closing frame this near the chase camera is back (the spring would only creep).</summary>
        private const float RestWeight = 1e-3f;

        private DampedSpring _weight;
        private float _hold;

        /// <summary>True from <see cref="Open"/> until <see cref="Close"/>: the frame is opening or open.</summary>
        public bool IsOpen { get; private set; }

        /// <summary>How far the camera is toward the frame, 0 (chase camera) .. 1 (settled).</summary>
        public float Weight => Mathf.Clamp01(_weight.Value);

        /// <summary>The frame being opened to.</summary>
        public WideShotFrame Frame { get; private set; }

        /// <summary>The look of the shot opened last: its ease times, lens and breathing.</summary>
        public WideShotSettings Settings { get; private set; }

        /// <summary>Seconds since it last opened: drives the breathing and the hold.</summary>
        public float Seconds { get; private set; }

        /// <summary>An open frame with a hold whose time has run (its owner closes it).</summary>
        public bool HoldRanOut => IsOpen && _hold > 0f && Seconds >= _hold;

        /// <param name="holdSeconds">Seconds open before <see cref="HoldRanOut"/>; 0 holds until closed.</param>
        public void Open(WideShotSettings settings, WideShotFrame frame, float holdSeconds)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Frame = frame;
            _hold = Mathf.Max(holdSeconds, 0f);
            Seconds = 0f;
            IsOpen = true;
        }

        /// <summary>Starts easing back to the chase camera.</summary>
        public void Close()
        {
            IsOpen = false;
        }

        /// <summary>Back to the chase camera at once, no ease (07 was placed elsewhere in the dark).</summary>
        public void Reset()
        {
            IsOpen = false;
            _weight.Reset(0f);
        }

        public void Step(float deltaTime)
        {
            if (Settings == null)
            {
                return;
            }

            float step = Mathf.Max(deltaTime, 0f);
            Seconds += step;
            float settle = IsOpen ? Settings.OpenSeconds : Settings.HandBackSeconds;
            _weight.Step(IsOpen ? 1f : 0f, WideShot.Frequency(settle), CriticalDamping, step);
            _weight.Clamp(0f, 1f);
            if (!IsOpen && _weight.Value < RestWeight)
            {
                _weight.Reset(0f);
            }
        }
    }
}

using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The hand-off's lamp pulse (docs/features/M3-19 §5): a soft bump of light that runs along the conduit's beads from
    /// the reader to the bay over the hand-off's seconds, then a short afterglow at the bay end. Plain logic.
    /// </summary>
    public sealed class HandOffPulse
    {
        private float _startedAt = float.NegativeInfinity;
        private float _seconds = 1f;

        /// <summary>A pulse starts at <paramref name="now"/> and reaches the bay <paramref name="seconds"/> later.</summary>
        public void Begin(float now, float seconds)
        {
            _startedAt = now;
            _seconds = Mathf.Max(0.01f, seconds);
        }

        /// <summary>0 at the reader .. 1 at the bay; negative before any pulse.</summary>
        public float Progress(float now)
        {
            return (now - _startedAt) / _seconds;
        }

        /// <summary>True from the start until the afterglow has faded.</summary>
        public bool IsRunning(float now, float afterglow)
        {
            return Progress(now) >= 0f && now - _startedAt <= _seconds + afterglow;
        }

        /// <summary>
        /// The pulse's strength at <paramref name="now"/>: 0 before it starts, 1 as it travels, then fading to 0 over
        /// <paramref name="afterglow"/> seconds once it reaches the bay.
        /// </summary>
        public float Strength(float now, float afterglow)
        {
            float progress = Progress(now);
            if (progress < 0f)
            {
                return 0f;
            }

            if (progress <= 1f)
            {
                return 1f;
            }

            return Mathf.Clamp01(1f - (now - _startedAt - _seconds) / Mathf.Max(afterglow, 0.01f));
        }

        /// <summary>
        /// How lit bead <paramref name="index"/> of <paramref name="count"/> is at <paramref name="now"/>: the bump is
        /// <paramref name="width"/> of the whole path wide; once it arrives it holds at the bay end and fades over
        /// <paramref name="afterglow"/> seconds.
        /// </summary>
        public float BeadLevel(int index, int count, float now, float width, float afterglow)
        {
            if (count <= 0 || !IsRunning(now, afterglow))
            {
                return 0f;
            }

            float at = count == 1 ? 1f : (float)index / (count - 1);
            float distance = (Mathf.Min(Progress(now), 1f) - at) / Mathf.Max(width, 0.01f);
            return Mathf.Exp(-distance * distance) * Strength(now, afterglow);
        }
    }
}

using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A big piece cuts in segments (docs/features/M3-26 §4.1): its progress may not run past the end of the segment it
    /// is in; there the beam stops for its slide to the next one. Pure maths.
    /// </summary>
    public static class CutSegments
    {
        /// <summary>A cut resting exactly on a segment's end counts as in the next one.</summary>
        private const float Epsilon = 1e-4f;

        /// <summary>Where the segment that <paramref name="progress"/> is in ends, 0..1 of the whole piece.</summary>
        public static float End(float progress, int segments)
        {
            int count = Mathf.Max(1, segments);
            int segment = Mathf.FloorToInt(progress * count + Epsilon);
            return Mathf.Min(1f, (segment + 1f) / count);
        }

        /// <summary>
        /// The cut went from <paramref name="before"/> to <paramref name="progress"/>: true (and the progress held at
        /// the segment's end) when it reached the end of a segment that is not the last.
        /// </summary>
        public static bool ReachedSegmentEnd(float before, ref float progress, int segments)
        {
            float end = End(before, segments);
            if (progress < end || end >= 1f)
            {
                return false;
            }

            progress = end;
            return true;
        }
    }
}

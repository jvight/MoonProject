using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The tow rope's shape as pure functions: how far a slack rope of a given length sags between two points, and the
    /// points along it. The sag follows the parabola's arc length, so the sagged rope is as long as the rope is.
    /// </summary>
    public static class TowCableShape
    {
        /// <summary>
        /// Metres the middle of a rope of <paramref name="length"/> sags below the line between two points
        /// <paramref name="distance"/> apart: 0 when it is taut (the points are a rope's length apart or more), the
        /// arc-length result for a shallow sag otherwise, never more than <paramref name="maxSag"/>.
        /// </summary>
        public static float Sag(float distance, float length, float maxSag)
        {
            float slack = length - distance;
            if (slack <= 0f || distance <= 0f)
            {
                return slack > 0f ? Mathf.Min(maxSag, length * 0.5f) : 0f;
            }

            return Mathf.Min(maxSag, Mathf.Sqrt(3f * distance * slack / 8f));
        }

        /// <summary>
        /// Fills <paramref name="points"/> from <paramref name="from"/> to <paramref name="to"/>, the middle sagged
        /// down by <paramref name="sag"/> along a parabola.
        /// </summary>
        public static void Fill(Vector3 from, Vector3 to, float sag, Vector3[] points)
        {
            int last = points.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                float t = i / (float)last;
                points[i] = Vector3.Lerp(from, to, t) + Vector3.down * (sag * 4f * t * (1f - t));
            }
        }
    }
}

using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where an old power cable lies on the dust between two points (opening v2 §4): a chain of joints spaced about one
    /// cable segment apart, settled in a few slow side-to-side bends and following the ground, so Art's tileable
    /// segments laid joint to joint read as one cable. Plain logic.
    /// </summary>
    public static class CableRoute
    {
        /// <summary>
        /// The joints from <paramref name="from"/> to <paramref name="to"/> (world XZ, both ends included), about
        /// <paramref name="segmentLength"/> apart, wandering up to <paramref name="wander"/> metres to the side over
        /// <paramref name="bends"/> slow bends, on the ground given by <paramref name="groundHeight"/>.
        /// </summary>
        public static Vector3[] Joints(Vector2 from, Vector2 to, float segmentLength, float wander, float bends,
            Func<float, float, float> groundHeight)
        {
            if (groundHeight == null)
            {
                throw new ArgumentNullException(nameof(groundHeight));
            }

            if (segmentLength <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(segmentLength), segmentLength, "Segments need a length.");
            }

            Vector2 along = to - from;
            float length = along.magnitude;
            var side = length > 0f ? new Vector2(along.y, -along.x) / length : Vector2.zero;
            int segments = Mathf.Max(1, Mathf.RoundToInt(length / segmentLength));
            var joints = new Vector3[segments + 1];
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float sway = Mathf.Sin(t * Mathf.PI * bends) * Mathf.Sin(t * Mathf.PI) * wander;
                Vector2 flat = from + along * t + side * sway;
                joints[i] = new Vector3(flat.x, groundHeight(flat.x, flat.y), flat.y);
            }

            return joints;
        }
    }
}

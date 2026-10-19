using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The path a base cable takes between two ports: it falls from each port in a slack curve to the ground and lies
    /// on the terrain between, following its rises and dips. The build ghost's preview and the built cable share it,
    /// so the cable a piece gets is the one its preview promised. Plain logic, allocation-free.
    /// </summary>
    public static class BaseCablePath
    {
        /// <summary>The most points a path has (long cables space their points out).</summary>
        public const int MaxPoints = 96;

        /// <summary>
        /// Writes the path from <paramref name="from"/> to <paramref name="to"/> into <paramref name="points"/> (at
        /// least <see cref="MaxPoints"/> long) and returns how many points it has.
        /// </summary>
        public static int Write(Vector3 from, Vector3 to, ITerrainQuery terrain, BuildingTuning tuning,
            Vector3[] points)
        {
            float length = SurfaceRules.HorizontalDistance(from, to);
            int count = Mathf.Clamp(Mathf.CeilToInt(length / tuning.CableStep) + 1, 2, MaxPoints);
            float drop = Mathf.Min(tuning.CableDrop, length * 0.5f);
            float radius = tuning.CableRadius;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float along = t * length;
                Vector3 at = Vector3.Lerp(from, to, t);
                float ground = terrain.SampleHeight(at.x, at.z) + radius;
                float y = ground;
                if (drop > 1e-4f)
                {
                    // (1 - s)^2 leaves the port and meets the ground level and tangent: a slack fall, not a kink.
                    float nearStart = Mathf.Clamp01(1f - along / drop);
                    float nearEnd = Mathf.Clamp01(1f - (length - along) / drop);
                    y += Mathf.Max(0f, from.y - ground) * nearStart * nearStart
                        + Mathf.Max(0f, to.y - ground) * nearEnd * nearEnd;
                }

                points[i] = new Vector3(at.x, y, at.z);
            }

            return count;
        }
    }
}

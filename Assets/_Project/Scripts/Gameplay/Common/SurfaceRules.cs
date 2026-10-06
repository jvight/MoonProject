using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>Shared placement tests against the analytic surface (pure, allocation-free).</summary>
    public static class SurfaceRules
    {
        /// <summary>Unit XZ direction of a bearing in degrees measured from +Z toward +X.</summary>
        public static Vector3 BearingDirection(float bearingDegrees)
        {
            float radians = bearingDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        /// <summary>Bearing in degrees (from +Z toward +X) of the horizontal vector <paramref name="direction"/>.</summary>
        public static float Bearing(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        public static bool Inside(Rect area, float x, float z, float margin)
        {
            return x >= area.xMin + margin && x <= area.xMax - margin && z >= area.yMin + margin &&
                   z <= area.yMax - margin;
        }

        /// <summary>
        /// True when the ground at (x, z) and at four points <paramref name="probeRadius"/> around it is no steeper
        /// than the slope whose normal has this up component (cos of the max slope).
        /// </summary>
        public static bool IsFlat(ITerrainQuery terrain, float x, float z, float minNormalY, float probeRadius)
        {
            return terrain.SampleNormal(x, z).y >= minNormalY &&
                   terrain.SampleNormal(x + probeRadius, z).y >= minNormalY &&
                   terrain.SampleNormal(x - probeRadius, z).y >= minNormalY &&
                   terrain.SampleNormal(x, z + probeRadius).y >= minNormalY &&
                   terrain.SampleNormal(x, z - probeRadius).y >= minNormalY;
        }

        /// <summary>Up component of the normal of a slope of <paramref name="degrees"/>.</summary>
        public static float MinNormalY(float degrees)
        {
            return Mathf.Cos(degrees * Mathf.Deg2Rad);
        }

        /// <summary>
        /// Distance from <paramref name="origin"/> (inside <paramref name="area"/>) along the horizontal unit
        /// <paramref name="direction"/> to the area's edge.
        /// </summary>
        public static float DistanceToEdge(Rect area, Vector3 origin, Vector3 direction)
        {
            float distance = float.MaxValue;
            if (direction.x > 1e-5f)
            {
                distance = Mathf.Min(distance, (area.xMax - origin.x) / direction.x);
            }
            else if (direction.x < -1e-5f)
            {
                distance = Mathf.Min(distance, (area.xMin - origin.x) / direction.x);
            }

            if (direction.z > 1e-5f)
            {
                distance = Mathf.Min(distance, (area.yMax - origin.z) / direction.z);
            }
            else if (direction.z < -1e-5f)
            {
                distance = Mathf.Min(distance, (area.yMin - origin.z) / direction.z);
            }

            return Mathf.Max(0f, distance);
        }

        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static float HorizontalDistanceSquared(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Point on the surface at (x, z).</summary>
        public static Vector3 OnSurface(ITerrainQuery terrain, float x, float z)
        {
            return new Vector3(x, terrain.SampleHeight(x, z), z);
        }
    }
}

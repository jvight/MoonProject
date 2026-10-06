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

        /// <summary>
        /// True when (x, z) is drivable floor and so are four points <paramref name="margin"/> metres around it, so
        /// whatever stands there keeps a margin from the rim.
        /// </summary>
        public static bool InsideDrivable(ITerrainQuery terrain, float x, float z, float margin)
        {
            if (!terrain.IsDrivable(x, z))
            {
                return false;
            }

            return margin <= 0f || (terrain.IsDrivable(x + margin, z) && terrain.IsDrivable(x - margin, z) &&
                                    terrain.IsDrivable(x, z + margin) && terrain.IsDrivable(x, z - margin));
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
        /// How far (m) one can travel from <paramref name="origin"/> along the horizontal unit
        /// <paramref name="direction"/> while staying on drivable floor <paramref name="margin"/> metres from its
        /// edge, marching in <paramref name="step"/> metre steps up to <paramref name="maxDistance"/>.
        /// </summary>
        public static float DrivableReach(ITerrainQuery terrain, Vector3 origin, Vector3 direction, float margin,
            float step, float maxDistance)
        {
            if (step <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(step), step, "The march step must be positive.");
            }

            float reach = 0f;
            for (float distance = step; distance <= maxDistance; distance += step)
            {
                if (!InsideDrivable(terrain, origin.x + direction.x * distance, origin.z + direction.z * distance,
                        margin))
                {
                    break;
                }

                reach = distance;
            }

            return reach;
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

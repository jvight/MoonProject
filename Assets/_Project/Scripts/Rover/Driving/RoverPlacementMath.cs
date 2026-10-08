using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Where <c>IRoverPlacement.PlaceAt</c> sets 07 down, as pure functions: the heading a rotation asks for, and the
    /// resting centre of the physics sphere on the analytic surface (its height and slope, not the requested y).
    /// </summary>
    public static class RoverPlacementMath
    {
        /// <summary>Heading (deg, 0..360, 0 = +Z, 90 = +X) of <paramref name="rotation"/>'s forward.</summary>
        public static float Yaw(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            return Mathf.Repeat(Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg, 360f);
        }

        /// <summary>
        /// The surface point under <paramref name="position"/> (x/z kept, y from the terrain) and its normal: where 07
        /// rests.
        /// </summary>
        public static Vector3 Ground(ITerrainQuery terrain, Vector3 position, out Vector3 normal)
        {
            normal = terrain.SampleNormal(position.x, position.z);
            return new Vector3(position.x, terrain.SampleHeight(position.x, position.z), position.z);
        }

        /// <summary>
        /// Centre of the physics sphere of <paramref name="radius"/> resting on the surface at
        /// <paramref name="ground"/>: lifted along the surface <paramref name="normal"/>, so it touches the slope at
        /// that point.
        /// </summary>
        public static Vector3 RestingCentre(Vector3 ground, Vector3 normal, float radius)
        {
            return ground + normal * radius;
        }
    }
}

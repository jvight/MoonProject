using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Small seam over the Unity physics queries used by <see cref="ObstacleProbe"/>, so the 360-degree bumper
    /// overlap and ray-fan decisions can be tested in EditMode without scene colliders.
    /// </summary>
    public interface IObstacleQuery
    {
        /// <summary>
        /// Fills <paramref name="closestPoints"/> with the closest surface point to <paramref name="origin"/> on
        /// each overlapped Prop/Relic collider within <paramref name="radius"/> that supports closest-point queries,
        /// excluding the rover's own colliders. Returns the number of points written.
        /// </summary>
        int OverlapClosestPoints(Vector3 origin, float radius, Vector3[] closestPoints);

        /// <summary>
        /// Casts a ray of <paramref name="maxDistance"/> from <paramref name="origin"/> along
        /// <paramref name="direction"/> against driveable and relic colliders.
        /// </summary>
        bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out ObstacleRayHit hit);
    }
}

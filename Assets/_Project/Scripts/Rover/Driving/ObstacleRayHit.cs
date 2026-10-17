using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Value-type result of a single obstacle probe raycast, decoupled from <see cref="RaycastHit"/>'s collider
    /// reference so <see cref="ObstacleProbe"/>'s hit classification is pure and EditMode-testable.
    /// </summary>
    public readonly struct ObstacleRayHit
    {
        public ObstacleRayHit(Vector3 point, Vector3 normal, float distance, int layer)
        {
            Point = point;
            Normal = normal;
            Distance = distance;
            Layer = layer;
        }

        public Vector3 Point { get; }

        public Vector3 Normal { get; }

        public float Distance { get; }

        public int Layer { get; }
    }
}

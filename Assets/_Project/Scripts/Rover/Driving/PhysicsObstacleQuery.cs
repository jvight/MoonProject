using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Zero-allocation Unity physics implementation of <see cref="IObstacleQuery"/> for runtime obstacle probing.
    /// </summary>
    public sealed class PhysicsObstacleQuery : IObstacleQuery
    {
        private const int MaxOverlapColliders = 16;
        private const int OverlapMask = Layers.PropMask | Layers.RelicMask;
        private const int RaycastMask = Layers.DriveableMask | Layers.RelicMask;

        private readonly Collider _ignoredPrimary;
        private readonly Collider _ignoredSecondary;
        private readonly Collider[] _overlapColliders = new Collider[MaxOverlapColliders];

        public PhysicsObstacleQuery(Collider ignoredPrimary, Collider ignoredSecondary)
        {
            _ignoredPrimary = ignoredPrimary;
            _ignoredSecondary = ignoredSecondary;
        }

        public int OverlapClosestPoints(Vector3 origin, float radius, Vector3[] closestPoints)
        {
            int count = Physics.OverlapSphereNonAlloc(origin, radius, _overlapColliders, OverlapMask,
                QueryTriggerInteraction.Ignore);
            int written = 0;
            int capacity = closestPoints.Length;
            for (int i = 0; i < count && written < capacity; i++)
            {
                Collider col = _overlapColliders[i];
                if (col == null || col == _ignoredPrimary || col == _ignoredSecondary || !SupportsClosestPoint(col))
                {
                    continue;
                }

                closestPoints[written++] = col.ClosestPoint(origin);
            }

            return written;
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out ObstacleRayHit hit)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit rawHit, maxDistance, RaycastMask,
                    QueryTriggerInteraction.Ignore)
                && rawHit.collider != null)
            {
                hit = new ObstacleRayHit(rawHit.point, rawHit.normal, rawHit.distance,
                    rawHit.collider.gameObject.layer);
                return true;
            }

            hit = default;
            return false;
        }

        private static bool SupportsClosestPoint(Collider col)
        {
            if (col is BoxCollider || col is SphereCollider || col is CapsuleCollider)
            {
                return true;
            }

            return col is MeshCollider mesh && mesh.convex;
        }
    }
}

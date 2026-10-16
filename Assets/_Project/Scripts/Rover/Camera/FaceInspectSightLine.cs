using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Sight-line queries against both solid scenery (<see cref="Layers.PropMask"/>) and terrain colliders
    /// (<see cref="Layers.GroundMask"/>) plus optional analytic terrain height clearance (<see cref="ITerrainQuery"/>)
    /// for 07's face-inspect camera.
    /// </summary>
    public sealed class FaceInspectSightLine : ISightLine
    {
        private const int ObstacleMask = Layers.GroundMask | Layers.PropMask;

        private ITerrainQuery _terrain;

        public void SetTerrain(ITerrainQuery terrain)
        {
            _terrain = terrain;
        }

        public float Clear(Vector3 from, Vector3 to, float radius)
        {
            Vector3 line = to - from;
            float length = line.magnitude;
            if (length <= 0f)
            {
                return 0f;
            }

            Vector3 direction = line / length;
            float clear = length;
            if (radius > 0f)
            {
                if (Physics.SphereCast(from, radius, direction, out RaycastHit ball, length, ObstacleMask,
                        QueryTriggerInteraction.Ignore))
                {
                    clear = ball.distance;
                }
            }
            else if (Physics.Raycast(from, direction, out RaycastHit ray, length, ObstacleMask,
                         QueryTriggerInteraction.Ignore))
            {
                clear = ray.distance;
            }

            if (_terrain != null)
            {
                float floor = _terrain.SampleHeight(to.x, to.z) + radius;
                if (to.y < floor)
                {
                    return 0f;
                }
            }

            return clear;
        }

        public bool IsFree(Vector3 point, float radius)
        {
            if (Physics.CheckSphere(point, radius, ObstacleMask, QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            if (_terrain != null && point.y < _terrain.SampleHeight(point.x, point.z) + radius)
            {
                return false;
            }

            return true;
        }
    }
}

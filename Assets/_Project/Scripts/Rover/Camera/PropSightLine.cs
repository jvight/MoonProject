using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Sight lines against the solid scenery on the Prop layer (rocks, the base and the Rover Bay's walls and roof),
    /// one raycast, sphere cast or overlap test each, allocation-free.
    /// </summary>
    public sealed class PropSightLine : ISightLine
    {
        public float Clear(Vector3 from, Vector3 to, float radius)
        {
            Vector3 line = to - from;
            float length = line.magnitude;
            if (length <= 0f)
            {
                return 0f;
            }

            Vector3 direction = line / length;
            if (radius > 0f)
            {
                return Physics.SphereCast(from, radius, direction, out RaycastHit ball, length, Layers.PropMask,
                    QueryTriggerInteraction.Ignore)
                    ? ball.distance
                    : length;
            }

            return Physics.Raycast(from, direction, out RaycastHit ray, length, Layers.PropMask,
                QueryTriggerInteraction.Ignore)
                ? ray.distance
                : length;
        }

        public bool IsFree(Vector3 point, float radius)
        {
            return !Physics.CheckSphere(point, radius, Layers.PropMask, QueryTriggerInteraction.Ignore);
        }
    }
}

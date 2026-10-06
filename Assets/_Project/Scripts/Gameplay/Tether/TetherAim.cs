using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Generous aiming: a target counts when it lies inside a soft cone around the view direction, and the best one
    /// is the most central, with nearer ones winning ties. Pure maths; line of sight is checked by the caller.
    /// </summary>
    public static class TetherAim
    {
        /// <summary>
        /// Score (lower is better) of <paramref name="target"/> seen from <paramref name="eye"/> looking along
        /// <paramref name="forward"/> (unit). False when it is behind, out of range or outside the cone.
        /// </summary>
        public static bool TryScore(Vector3 eye, Vector3 forward, Vector3 target, float coneDegrees, float range,
            float distanceWeight, out float score)
        {
            Vector3 toTarget = target - eye;
            float distance = toTarget.magnitude;
            score = float.MaxValue;
            if (distance < 1e-4f || distance > range)
            {
                return false;
            }

            float cos = Vector3.Dot(forward, toTarget) / distance;
            if (cos <= 0f)
            {
                return false;
            }

            float angle = Mathf.Acos(Mathf.Min(1f, cos)) * Mathf.Rad2Deg;
            if (angle > coneDegrees)
            {
                return false;
            }

            score = angle + distance * distanceWeight;
            return true;
        }
    }
}

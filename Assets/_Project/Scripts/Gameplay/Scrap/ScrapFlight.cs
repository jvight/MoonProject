using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The path a scrap piece takes into 07: it eases from where it floated toward the (moving) cargo socket, lifting
    /// on a soft arc while spiralling around its line of travel. Lift and spiral grow from zero and fade back to
    /// zero, so the flight leaves the idle pose and meets the socket without a jolt.
    /// </summary>
    public static class ScrapFlight
    {
        /// <param name="start">Where the piece was when it lifted off.</param>
        /// <param name="target">The cargo socket now (it moves with 07).</param>
        /// <param name="progress">0 at lift-off, 1 on arrival.</param>
        /// <param name="spiralPhase">Starting angle (radians) of the spiral, different per piece.</param>
        /// <param name="spiralDirection">+1 or -1: which way it turns.</param>
        public static Vector3 Evaluate(Vector3 start, Vector3 target, float progress, float spiralPhase,
            float spiralDirection, float lift, float spiralRadius, float spiralTurns)
        {
            float p = Mathf.Clamp01(progress);
            Vector3 travel = Vector3.LerpUnclamped(start, target, Ease.InOutCubic(p));

            Vector3 axis = target - start;
            if (axis.sqrMagnitude < 1e-6f)
            {
                axis = Vector3.up;
            }

            axis.Normalize();
            Vector3 helper = Mathf.Abs(axis.y) > 0.9f ? Vector3.forward : Vector3.up;
            Vector3 u = Vector3.Cross(axis, helper).normalized;
            Vector3 v = Vector3.Cross(axis, u);

            float angle = spiralPhase + spiralDirection * spiralTurns * 2f * Mathf.PI * p;
            float radius = spiralRadius * Ease.Hump(p);
            Vector3 spiral = (u * Mathf.Cos(angle) + v * Mathf.Sin(angle)) * radius;
            float rise = lift * Ease.Hump(p);
            return travel + spiral + Vector3.up * rise;
        }

        /// <summary>Flight time (s) for a piece <paramref name="distance"/> metres from the socket.</summary>
        public static float Duration(float distance, float baseDuration, float secondsPerMetre)
        {
            return baseDuration + Mathf.Max(0f, distance) * secondsPerMetre;
        }
    }
}

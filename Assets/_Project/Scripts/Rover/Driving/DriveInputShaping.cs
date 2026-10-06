using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Turns the drive vector into independent throttle and steering. The input arrives clamped to the unit circle
    /// (normalised WASD composites, round stick gates), so W+D reads as (0.71, 0.71) and would silently drop 07 to 71%
    /// throttle in every turn. Stretching the circle onto the square keeps the direction and makes the longer axis
    /// carry the full magnitude: W+D becomes (1, 1), a half-pressed diagonal (0.5, 0.5).
    /// </summary>
    public static class DriveInputShaping
    {
        private const float Epsilon = 1e-5f;

        public static Vector2 CircleToSquare(Vector2 input)
        {
            float magnitude = Mathf.Min(1f, input.magnitude);
            float longest = Mathf.Max(Mathf.Abs(input.x), Mathf.Abs(input.y));
            if (longest < Epsilon)
            {
                return Vector2.zero;
            }

            return input * (magnitude / longest);
        }
    }
}

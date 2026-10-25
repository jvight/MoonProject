using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The easing of the face screen's camera move (pure): a short soft start, a constant-speed middle, a short soft
    /// stop. A smoothstep spends its whole move at up to 1.5x the average speed in the middle; for the half-orbit round
    /// 07 that closing the screen makes (about 175 degrees) that was a whirl of several hundred degrees a second.
    /// </summary>
    public static class FaceInspectEase
    {
        private const float MinRamp = 0.05f;
        private const float MaxRamp = 0.49f;

        /// <summary>The move's progress (0..1) at <paramref name="t"/> (0..1) of its time.</summary>
        /// <param name="ramp">Share of the time the speed spends rising, and again falling (0.05..0.49).</param>
        public static float Weight(float t, float ramp)
        {
            t = Mathf.Clamp01(t);
            float r = Mathf.Clamp(ramp, MinRamp, MaxRamp);
            float speed = 1f / (1f - r);
            if (t < r)
            {
                return speed * t * t / (2f * r);
            }

            if (t < 1f - r)
            {
                return speed * (t - r * 0.5f);
            }

            float left = 1f - t;
            return 1f - speed * left * left / (2f * r);
        }

        /// <summary>The fastest the move goes, as a multiple of its average speed.</summary>
        public static float PeakSpeed(float ramp)
        {
            return 1f / (1f - Mathf.Clamp(ramp, MinRamp, MaxRamp));
        }
    }
}

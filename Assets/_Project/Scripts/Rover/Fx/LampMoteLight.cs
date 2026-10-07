using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How visible a dust mote hanging in front of 07's headlamp is, as pure functions: dust is only seen where the
    /// beam catches it, so a mote's opacity is the spot's soft cone at its direction, fading right at the lens and
    /// thinning with distance; it fades in and out over its life, glints slowly as it turns, and all motes fade away
    /// once 07 drives faster than a crawl.
    /// </summary>
    public static class LampMoteLight
    {
        private const float MinDistance = 1e-4f;

        /// <summary>Target visibility (0..1) of all motes while 07 moves at <paramref name="speed"/> m/s.</summary>
        public static float SpeedVisibility(RoverFxTuning tuning, float speed)
        {
            return 1f - Smoothing.SmoothStep(tuning.MoteFadeSpeed, tuning.MoteGoneSpeed, speed);
        }

        /// <summary>
        /// How much of the lamp's light (0..1) reaches a mote at <paramref name="offset"/> from the lens, for a spot
        /// along <paramref name="axis"/> (unit) with the given outer and inner half-angle cosines.
        /// </summary>
        public static float Beam(RoverFxTuning tuning, Vector3 offset, Vector3 axis, float cosOuter, float cosInner)
        {
            float distance = offset.magnitude;
            if (distance < MinDistance)
            {
                return 0f;
            }

            float cone = Smoothing.SmoothStep(cosOuter, cosInner, Vector3.Dot(offset, axis) / distance);
            float near = Smoothing.SmoothStep(0f, tuning.MoteNearFade, distance);
            float thinning = tuning.MoteFalloff * Smoothing.SmoothStep(tuning.MoteNearFade, tuning.MoteReach, distance);
            return cone * near * (1f - thinning);
        }

        /// <summary>Fade (0..1) at <paramref name="age"/> (0..1 of its life): in, steady, then out.</summary>
        public static float Life(RoverFxTuning tuning, float age)
        {
            float fade = tuning.MoteFade;
            return Smoothing.SmoothStep(0f, fade, age) * Smoothing.SmoothStep(0f, fade, 1f - age);
        }

        /// <summary>
        /// A slow glint (1 - depth .. 1) as a mote turns in the light; <paramref name="phase"/> (0..1) keeps the motes
        /// out of step with each other.
        /// </summary>
        public static float Glint(RoverFxTuning tuning, float time, float phase)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * (time / tuning.MoteGlintPeriod + phase));
            return 1f - tuning.MoteGlintDepth * wave;
        }
    }
}

using UnityEngine;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Pure C# easing and smooth interpolation helpers for Showcase camera transitions, node animations, and lamps.
    /// Every transition eases continuously and never snaps.
    /// </summary>
    public static class ShowcaseEasing
    {
        /// <summary>Cubic Hermite smoothstep on [0, 1] with zero first derivatives at both endpoints.</summary>
        public static float SmoothStep01(float t)
        {
            float clamped = Mathf.Clamp01(t);
            return clamped * clamped * (3f - 2f * clamped);
        }

        /// <summary>Quintic Perlin smootherstep on [0, 1] with zero first and second derivatives at endpoints.</summary>
        public static float SmootherStep01(float t)
        {
            float clamped = Mathf.Clamp01(t);
            return clamped * clamped * clamped * (clamped * (clamped * 6f - 15f) + 10f);
        }

        /// <summary>
        /// Advances a normalized [0, 1] progress value toward <paramref name="target01"/> over
        /// <paramref name="durationSeconds"/>. Never snaps when <paramref name="durationSeconds"/> is positive.
        /// </summary>
        public static float AdvanceNormalized(float current01, float target01, float durationSeconds, float deltaTime)
        {
            float clampedTarget = Mathf.Clamp01(target01);
            float clampedCurrent = Mathf.Clamp01(current01);
            if (deltaTime <= 0f)
            {
                return clampedCurrent;
            }

            float safeDuration = Mathf.Max(0.05f, durationSeconds);
            float maxDelta = deltaTime / safeDuration;
            return Mathf.MoveTowards(clampedCurrent, clampedTarget, maxDelta);
        }

        /// <summary>Evaluates an eased value between <paramref name="restValue"/> and <paramref name="activeValue"/>.</summary>
        public static float EvaluateEased(float normalizedProgress, float restValue, float activeValue)
        {
            return Mathf.Lerp(restValue, activeValue, SmootherStep01(normalizedProgress));
        }

        /// <summary>
        /// Smoothly moves a scalar value toward <paramref name="target"/> using an exponential decay parameterized by
        /// <paramref name="easeSeconds"/> (the approximate 95% settling time).
        /// </summary>
        public static float SmoothScalar(float current, float target, float easeSeconds, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return current;
            }

            float safeEase = Mathf.Max(0.05f, easeSeconds);
            float factor = 1f - Mathf.Exp(-3f * deltaTime / safeEase);
            return Mathf.Lerp(current, target, factor);
        }

        /// <summary>
        /// Smoothly moves an angle in degrees toward <paramref name="targetDegrees"/> across the -180..180 wrap seam.
        /// </summary>
        public static float SmoothAngle(float currentDegrees, float targetDegrees, float easeSeconds, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return currentDegrees;
            }

            float safeEase = Mathf.Max(0.05f, easeSeconds);
            float factor = 1f - Mathf.Exp(-3f * deltaTime / safeEase);
            float delta = Mathf.DeltaAngle(currentDegrees, targetDegrees);
            return currentDegrees + delta * factor;
        }

        /// <summary>Normalizes any angle in degrees into the signed [-180, 180] interval.</summary>
        public static float NormalizeSignedAngle(float degrees)
        {
            return Mathf.DeltaAngle(0f, degrees);
        }

        /// <summary>
        /// Evaluates the vertical offset in metres for a build stage rising from <c>-riseDepthMeters</c>, gently
        /// cresting to <c>+settleOvershootMeters</c>, and settling softly to <c>0</c> at <paramref name="progress01"/> = 1.
        /// </summary>
        public static float EvaluateRiseAndSettleVerticalOffset(
            float progress01,
            float riseDepthMeters,
            float settleOvershootMeters)
        {
            float p = Mathf.Clamp01(progress01);
            const float crestPoint = 0.72f;
            float depth = Mathf.Max(0f, riseDepthMeters);
            float overshoot = Mathf.Max(0f, settleOvershootMeters);

            if (p <= crestPoint)
            {
                float riseT = SmoothStep01(p / crestPoint);
                return Mathf.Lerp(-depth, overshoot, riseT);
            }

            float settleT = SmoothStep01((p - crestPoint) / (1f - crestPoint));
            return Mathf.Lerp(overshoot, 0f, settleT);
        }
    }
}

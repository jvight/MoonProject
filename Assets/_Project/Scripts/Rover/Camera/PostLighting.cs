using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How much of the self-test's light is in at each moment, as pure functions: the earthshine rim comes in over the
    /// pull-out and goes out as the camera settles into the chase frame; the warm fill from 07's own screen grows once
    /// the checks start, so the body reads while the world stays in its eclipse.
    /// </summary>
    public static class PostLighting
    {
        /// <summary>The share (0..1) of the rim light at progress <paramref name="phaseShare"/> of <paramref name="phase"/>.</summary>
        public static float Rim(OpeningPhase phase, float phaseShare)
        {
            float u = Mathf.Clamp01(phaseShare);
            switch (phase)
            {
                case OpeningPhase.PullOut:
                    return WakeOpening.SmoothStepC2(0f, 1f, u);
                case OpeningPhase.Post:
                case OpeningPhase.OverShoulder:
                    return 1f;
                case OpeningPhase.ChaseEase:
                    return 1f - WakeOpening.SmoothStepC2(0f, 1f, u);
                default:
                    return 0f;
            }
        }

        /// <summary>The share (0..1) of the screen's fill: it follows the rim, and ramps in over <paramref name="rampSeconds"/> of the checks.</summary>
        public static float Fill(OpeningPhase phase, float phaseShare, float secondsIntoChecks, float rampSeconds)
        {
            float rim = Rim(phase, phaseShare);
            if (phase == OpeningPhase.PullOut)
            {
                return 0f;
            }

            float ramp = rampSeconds > 1e-4f ? WakeOpening.SmoothStepC2(0f, 1f, secondsIntoChecks / rampSeconds) : 1f;
            return rim * ramp;
        }
    }
}

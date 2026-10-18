using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How much of the wake's light is in at each moment, as pure functions: the earthshine rim comes in as the picture
    /// opens and eases out as the camera moves round to the chase frame; the warm fill from 07's own screen grows as
    /// the face wakes, so the body reads while the world stays in its eclipse.
    /// </summary>
    public static class PostLighting
    {
        /// <summary>The share (0..1) of the rim light at progress <paramref name="phaseShare"/> of <paramref name="phase"/>.</summary>
        public static float Rim(OpeningPhase phase, float phaseShare)
        {
            float u = Mathf.Clamp01(phaseShare);
            switch (phase)
            {
                case OpeningPhase.FadeIn:
                    return WakeOpening.SmoothStepC2(0f, 1f, u);
                case OpeningPhase.EyesOpen:
                case OpeningPhase.Post:
                case OpeningPhase.Ready:
                case OpeningPhase.PullBack:
                case OpeningPhase.Perform:
                    return 1f;
                case OpeningPhase.MoveBehind:
                    return 1f - WakeOpening.SmoothStepC2(0f, 1f, u);
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// The share (0..1) of the screen's fill: it follows the rim, and ramps in over <paramref name="rampSeconds"/>
        /// from the moment the face screen flickers on (<paramref name="secondsSinceFaceOn"/>).
        /// </summary>
        public static float Fill(OpeningPhase phase, float phaseShare, float secondsSinceFaceOn, float rampSeconds)
        {
            if (phase == OpeningPhase.FadeIn)
            {
                return 0f;
            }

            float ramp = rampSeconds > 1e-4f ? WakeOpening.SmoothStepC2(0f, 1f, secondsSinceFaceOn / rampSeconds) : 1f;
            return Rim(phase, phaseShare) * ramp;
        }
    }
}

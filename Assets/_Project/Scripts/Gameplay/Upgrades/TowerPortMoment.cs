using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The radio tower's service port once 07 has fed its hopper, over t (seconds since the last bundle dropped in):
    /// the hatch swings open, 07's beam stitches up the tower from the hatch to the beacon while the new section
    /// rises, then the hatch swings shut. Pure curves.
    /// </summary>
    public static class TowerPortMoment
    {
        /// <summary>Seconds the beam stitches: while the beacon flares and the new stage grows in.</summary>
        public static float StitchDuration(float flareDuration, float growDuration)
        {
            return flareDuration + growDuration;
        }

        /// <summary>Seconds from the hopper fed to the hatch shut again.</summary>
        public static float Duration(float hatchTime, float stitchDuration)
        {
            return hatchTime + stitchDuration + hatchTime;
        }

        /// <summary>True while the beam stitches (the hatch is fully open).</summary>
        public static bool Stitching(float t, float hatchTime, float stitchDuration)
        {
            return t >= hatchTime && t < hatchTime + stitchDuration;
        }

        /// <summary>How far open the hatch is: 0 shut .. 1 open.</summary>
        public static float Openness(float t, float hatchTime, float stitchDuration)
        {
            if (t < 0f)
            {
                return 0f;
            }

            if (hatchTime <= 0f)
            {
                return t < stitchDuration ? 1f : 0f;
            }

            float opening = Ease.InOutSine(Mathf.Clamp01(t / hatchTime));
            float closing = Ease.InOutSine(Mathf.Clamp01((t - hatchTime - stitchDuration) / hatchTime));
            return opening * (1f - closing);
        }

        /// <summary>How far up the tower the stitch has climbed: 0 at the hatch .. 1 at the beacon.</summary>
        public static float Climb(float t, float hatchTime, float stitchDuration)
        {
            return Ease.InOutSine(Mathf.Clamp01((t - hatchTime) / Mathf.Max(1e-3f, stitchDuration)));
        }
    }
}

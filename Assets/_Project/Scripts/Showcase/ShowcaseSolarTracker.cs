using UnityEngine;
using MoonProject.Core.Events;
using MoonProject.World;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Pure C# solar-tracking and night/eclipse wing-fold calculator for the Solar Array and Cargo Lander exhibits.
    /// Converts the world-space Sun direction into local turntable azimuth, elevation tilt, and wing pitch/fold
    /// targets without snapping.
    /// </summary>
    public static class ShowcaseSolarTracker
    {
        public const float RestElevationDegrees = 35f;
        public const float MinElevationDegrees = 10f;
        public const float MaxElevationDegrees = 75f;
        public const float StowedElevationDegrees = 15f;
        public const float FoldedWingDegrees = 55f;

        /// <summary>
        /// Returns true when the Solar Array should fold its wings automatically: during Earthlit night, during
        /// total/partial eclipse, or when direct sunlight is absent.
        /// </summary>
        public static bool ShouldFoldForSky(
            SkyPhase phase,
            bool isHoldingEclipse,
            float eclipseFactor,
            float sunlightFactor)
        {
            if (isHoldingEclipse || phase == SkyPhase.Eclipse || phase == SkyPhase.Earthlit)
            {
                return true;
            }

            if (eclipseFactor >= 0.35f || sunlightFactor <= 0.05f)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Computes target angles for the Solar Array in <c>Stage_4</c>'s local frame (where +X is right, +Y is
        /// horizontal forward, and -Z is world up because the FBX root has a +90 degree X rotation).
        /// </summary>
        /// <param name="localSunInStage4">Sun direction vector transformed into <c>Stage_4</c> local space.</param>
        /// <param name="shouldFold">True when the wings should fold (night, eclipse, or manual fold toggle).</param>
        /// <param name="azimuthDeg">Target rotation around <c>Stage_4/Azimuth</c>'s local +Z axis, degrees.</param>
        /// <param name="elevationOffsetDeg">
        /// Target rotation offset around <c>Stage_4/Azimuth/Elevation</c>'s local +X axis relative to its authored
        /// 35-degree rest pose, degrees (-25..+40).
        /// </param>
        /// <param name="wingFoldDeg">Target fold angle for <c>Wing_L</c> (+Y) and <c>Wing_R</c> (-Y), degrees.</param>
        public static void ComputeSolarArrayTargets(
            Vector3 localSunInStage4,
            bool shouldFold,
            out float azimuthDeg,
            out float elevationOffsetDeg,
            out float wingFoldDeg)
        {
            if (shouldFold)
            {
                azimuthDeg = 0f;
                elevationOffsetDeg = StowedElevationDegrees - RestElevationDegrees;
                wingFoldDeg = FoldedWingDegrees;
                return;
            }

            float horiz = Mathf.Sqrt(localSunInStage4.x * localSunInStage4.x + localSunInStage4.y * localSunInStage4.y);
            azimuthDeg = horiz > 1e-4f
                ? Mathf.Atan2(-localSunInStage4.x, localSunInStage4.y) * Mathf.Rad2Deg
                : 0f;

            float upComponent = Mathf.Max(0.01f, -localSunInStage4.z);
            float zenithDeg = Mathf.Atan2(horiz, upComponent) * Mathf.Rad2Deg;
            float clampedAbsoluteElevation = Mathf.Clamp(zenithDeg, MinElevationDegrees, MaxElevationDegrees);
            elevationOffsetDeg = clampedAbsoluteElevation - RestElevationDegrees;
            wingFoldDeg = 0f;
        }

        /// <summary>
        /// Computes the spanwise solar-tracking pitch angle (around local +X) for the Cargo Lander's solar wings when
        /// deployed, given the Sun direction in the upright exhibit root frame (+Y up, +Z forward).
        /// </summary>
        public static float ComputeLanderWingSunPitch(Vector3 localSunUpright, bool isFolded, bool isSunlit)
        {
            if (isFolded || !isSunlit)
            {
                return 0f;
            }

            float upComponent = Mathf.Max(0.1f, localSunUpright.y);
            float rawPitch = Mathf.Atan2(localSunUpright.z, upComponent) * Mathf.Rad2Deg * 0.55f;
            return Mathf.Clamp(rawPitch, -32f, 32f);
        }
    }
}

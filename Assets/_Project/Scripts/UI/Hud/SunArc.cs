using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// How much of the Sun is left above the rim, read from its direction (pure, EditMode-tested): the arc fills in
    /// proportion to the Sun's elevation against the highest it climbs, and the arc is of use only while the Sun is up.
    /// </summary>
    internal static class SunArc
    {
        /// <summary>The Sun's elevation above the horizon in degrees (negative below it).</summary>
        public static float ElevationDegrees(Vector3 sunDirection)
        {
            return Mathf.Asin(Mathf.Clamp(sunDirection.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>0 at the horizon (or below it) to 1 at <paramref name="fullElevationDegrees"/> and above.</summary>
        public static float Fraction(Vector3 sunDirection, float fullElevationDegrees)
        {
            return Mathf.Clamp01(ElevationDegrees(sunDirection) / fullElevationDegrees);
        }

        public static bool IsUp(Vector3 sunDirection)
        {
            return ElevationDegrees(sunDirection) > 0f;
        }

        /// <summary>
        /// True once the Sun has set (<see cref="Core.ISunlight.SecondsToSunset"/> zero; infinity means the opening
        /// holds it): the arc has nothing left to show.
        /// </summary>
        public static bool HasSet(float secondsToSunset)
        {
            return secondsToSunset <= 0f;
        }
    }
}

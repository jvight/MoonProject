using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The orbit below 07, looking up at the sky over it. The camera sinks only until it is a set height above 07's
    /// ground; the rest of the requested elevation becomes an upward tilt of the aim, so it never digs into the dust.
    /// </summary>
    public static class LowOrbit
    {
        /// <summary>The aim never tips past this (deg above the horizon), however low the orbit is asked.</summary>
        private const float MaxAimPitch = 80f;

        /// <summary>
        /// Lowest elevation (deg) at which a camera <paramref name="radius"/> m from a follow point
        /// <paramref name="targetHeight"/> m above the ground stays <paramref name="lowestHeight"/> m above it.
        /// </summary>
        public static float Floor(float radius, float targetHeight, float lowestHeight)
        {
            return Mathf.Asin(Mathf.Clamp((lowestHeight - targetHeight) / radius, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Height (m) above the follow point to aim at so a camera held at <paramref name="floorElevation"/> looks
        /// <paramref name="tilt"/> degrees higher than it would looking at the follow point itself.
        /// </summary>
        public static float AimLift(float floorElevation, float tilt, float radius)
        {
            if (tilt <= 0f)
            {
                return 0f;
            }

            float floor = floorElevation * Mathf.Deg2Rad;
            float aim = Mathf.Min(tilt - floorElevation, MaxAimPitch) * Mathf.Deg2Rad;
            return radius * (Mathf.Sin(floor) + Mathf.Cos(floor) * Mathf.Tan(aim));
        }
    }
}

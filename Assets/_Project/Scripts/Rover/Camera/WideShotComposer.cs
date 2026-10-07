using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// The wide shot's composition as pure functions (testable without a scene): which way to turn (gently, toward
    /// Earth or The Peak), how shut in 07 is (a canyon gets a gentler, closer frame looking up the way between the
    /// walls), how high and how far the camera can sit without the analytic terrain getting in the way (it rises
    /// first, then comes closer), and its slow breathing.
    /// </summary>
    public static class WideShotComposer
    {
        /// <summary>The middle candidate turns half as far as the full swing.</summary>
        private const float HalfSwing = 0.5f;

        /// <summary>Bearing (deg, 0 = +Z, 90 = +X) of a direction on the ground plane.</summary>
        public static float Bearing(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        /// <summary>Horizontal unit vector along <paramref name="bearing"/> (deg).</summary>
        public static Vector3 Direction(float bearing)
        {
            float radians = bearing * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(radians), 0f, Mathf.Cos(radians));
        }

        /// <summary>
        /// The turn (deg, + = to the right) from <paramref name="viewYaw"/> toward Earth (bearing
        /// <paramref name="earthYaw"/>) or The Peak (<paramref name="peakYaw"/>): a share of the way, never more than
        /// the largest swing. Earth is preferred unless The Peak is clearly nearer the view; a subject beyond the
        /// largest subject angle is not turned to, and with neither in reach the view stays where the player left it.
        /// </summary>
        public static float SubjectSwing(WideShotSettings settings, float viewYaw, float earthYaw, float peakYaw)
        {
            float toEarth = Mathf.DeltaAngle(viewYaw, earthYaw);
            float toPeak = Mathf.DeltaAngle(viewYaw, peakYaw);
            float reach = settings.MaxSubjectAngle;
            bool earthInReach = Mathf.Abs(toEarth) <= reach;
            bool peakInReach = Mathf.Abs(toPeak) <= reach;
            bool peakClearlyNearer = Mathf.Abs(toPeak) + settings.EarthPreference < Mathf.Abs(toEarth);
            float delta;
            if (peakInReach && (!earthInReach || peakClearlyNearer))
            {
                delta = toPeak;
            }
            else if (earthInReach)
            {
                delta = toEarth;
            }
            else
            {
                return 0f;
            }

            return Mathf.Clamp(delta * settings.YawShare, -settings.MaxYawSwing, settings.MaxYawSwing);
        }

        /// <summary>
        /// The lowest elevation (deg) at which the line from <paramref name="follow"/> back along
        /// <paramref name="yaw"/> to <paramref name="reach"/> metres (horizontal) stays clear of the ground: the
        /// sight clearance near 07 growing to the camera clearance at the far end.
        /// </summary>
        public static float ClearingElevation(WideShotSettings settings, ITerrainQuery terrain, Vector3 follow,
            float yaw, float reach)
        {
            Vector3 back = -Direction(yaw);
            int samples = Mathf.Max(1, settings.TerrainSamples);
            float steepest = float.NegativeInfinity;
            for (int i = 1; i <= samples; i++)
            {
                float t = i / (float)samples;
                float along = reach * t;
                float x = follow.x + back.x * along;
                float z = follow.z + back.z * along;
                float clearance = Mathf.Lerp(settings.SightClearance, settings.Clearance, t);
                float rise = terrain.SampleHeight(x, z) + clearance - follow.y;
                steepest = Mathf.Max(steepest, rise / along);
            }

            return Mathf.Atan(steepest) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// The frame along <paramref name="yaw"/> for how shut in 07 is (0..1, see <see cref="ShutIn"/>): the full
        /// distance at the tuned elevation if the ground allows, else as high as needed (with room for the breathing to
        /// dip), else closer, down to the closest distance at the highest elevation.
        /// </summary>
        public static WideShotFrame Fit(WideShotSettings settings, ITerrainQuery terrain, Vector3 follow, float yaw,
            float shutIn)
        {
            float lowest = Mathf.Lerp(settings.Elevation, settings.ShutInElevation, shutIn);
            float widest = Mathf.Lerp(settings.Distance, settings.ShutInDistance, shutIn);
            float screenY = Mathf.Lerp(settings.ScreenY, settings.ShutInScreenY, shutIn);
            float spread = Mathf.Cos(lowest * Mathf.Deg2Rad) * (1f + settings.BreathDistance);
            int steps = Mathf.Max(1, settings.DistanceSteps);
            for (int step = 0; step <= steps; step++)
            {
                float distance = Mathf.Lerp(widest, settings.MinDistance, step / (float)steps);
                float clearing = ClearingElevation(settings, terrain, follow, yaw, distance * spread)
                    + settings.BreathElevation;
                float elevation = Mathf.Max(lowest, clearing);
                if (elevation <= settings.MaxElevation)
                {
                    return new WideShotFrame(yaw, elevation, distance, screenY);
                }
            }

            return new WideShotFrame(yaw, settings.MaxElevation, settings.MinDistance, screenY);
        }

        /// <summary>
        /// How shut in 07 is, 0 (open ground: a far, low skyline) .. 1 (a canyon: high ground close all round), from
        /// the mean elevation of the terrain seen from <paramref name="follow"/> at the skyline radius.
        /// </summary>
        public static float ShutIn(WideShotSettings settings, ITerrainQuery terrain, Vector3 follow)
        {
            int samples = Mathf.Max(1, settings.SkylineSamples);
            float radius = settings.SkylineRadius;
            float total = 0f;
            for (int i = 0; i < samples; i++)
            {
                Vector3 point = follow + Direction(360f * i / samples) * radius;
                float rise = terrain.SampleHeight(point.x, point.z) - follow.y;
                total += Mathf.Max(0f, Mathf.Atan2(rise, radius) * Mathf.Rad2Deg);
            }

            return Smoothing.SmoothStep(settings.OpenSkyline, settings.ShutInSkyline, total / samples);
        }

        /// <summary>
        /// The wide shot for 07 resting under <paramref name="follow"/> with the view along
        /// <paramref name="viewYaw"/>, as open or as shut in as the ground around it (<see cref="ShutIn"/>): turned
        /// toward the subject by <paramref name="swing"/> (less the more shut in: a canyon frame looks along the way)
        /// if the ground there allows the widest frame, else by half, else not at all (the widest of the three wins;
        /// ties go to the bigger turn).
        /// </summary>
        public static WideShotFrame Solve(WideShotSettings settings, ITerrainQuery terrain, Vector3 follow,
            float viewYaw, float swing, float shutIn)
        {
            float turn = swing * (1f - shutIn);
            WideShotFrame best = Fit(settings, terrain, follow, viewYaw + turn, shutIn);
            WideShotFrame half = Fit(settings, terrain, follow, viewYaw + turn * HalfSwing, shutIn);
            if (half.Distance > best.Distance)
            {
                best = half;
            }

            WideShotFrame straight = Fit(settings, terrain, follow, viewYaw, shutIn);
            return straight.Distance > best.Distance ? straight : best;
        }

        /// <summary>A slow sine sway of <paramref name="amplitude"/>, starting from 0 so opening never jumps.</summary>
        public static float Breath(float amplitude, float period, float time)
        {
            return amplitude * Mathf.Sin(2f * Mathf.PI * time / period);
        }
    }
}

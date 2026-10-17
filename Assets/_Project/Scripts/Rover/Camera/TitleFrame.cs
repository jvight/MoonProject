using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Which way the title's frame over a save looks, as a pure function: 07 is framed where it was left, from behind,
    /// looking along the line from 07 toward the landed lander (the base is built on its apron, and the wake pad is
    /// beside it). Standing too near the lander for the line to mean anything, the frame keeps 07's own heading.
    /// </summary>
    public static class TitleFrame
    {
        /// <summary>The bearing (deg, 0 = +Z, 90 = +X) the title frame looks along.</summary>
        /// <param name="rover">Where 07 was left.</param>
        /// <param name="lander">The lander, or null when the world has none (the frame keeps 07's heading).</param>
        /// <param name="roverBearing">07's own heading as a bearing.</param>
        /// <param name="minDistance">Nearer than this (m, on the ground) the line to the lander is not used.</param>
        public static float Bearing(Vector3 rover, Vector3? lander, float roverBearing, float minDistance)
        {
            if (!lander.HasValue)
            {
                return roverBearing;
            }

            Vector3 toLander = lander.Value - rover;
            toLander.y = 0f;
            return toLander.sqrMagnitude < minDistance * minDistance
                ? roverBearing
                : WideShotComposer.Bearing(toLander);
        }
    }
}

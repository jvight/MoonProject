using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The chase camera while 07 tows a load (docs/design/opening-v3-plan.md 3.2b follow-up), pure and EditMode-tested.
    /// The load trails behind 07, which is where the chase camera sits: it would hide 07 or sit inside the load. Given
    /// the load's bounds as an obstacle, this finds the least the camera has to rise and draw back for the line from it
    /// to 07 to clear the load, with its own eye outside the load, whichever way the player has swung it.
    /// </summary>
    public static class TowFraming
    {
        private const int ElevationSteps = 12;
        private const int RadiusSteps = 8;
        private const float RiseCostWeight = 2f;
        private const float YawCostWeight = 0.6f;
        private const float OtherSideCost = 0.4f;
        private const int YawSteps = 3;

        /// <summary>
        /// The elevation (deg) and the radius scale (>= 1) that clear the load, the cheapest of them: how far up and
        /// how far back are weighed against their limits, rising twice as dear: a camera that draws back and looks along
        /// the ground keeps the rope and the load in the frame, where one that rises steeply sees only 07 and the ground.
        /// </summary>
        /// <param name="target">The point the camera looks at on 07.</param>
        /// <param name="cameraYaw">World yaw (deg) of the camera's bearing from 07: it sits at the opposite side of that heading.</param>
        /// <param name="elevation">The chase elevation (deg) the player has.</param>
        /// <param name="radius">The chase distance (m).</param>
        /// <param name="load">The load's bounds, the obstacle.</param>
        /// <param name="margin">Metres of clearance kept round the load.</param>
        /// <param name="maxElevation">The highest the camera may rise (deg).</param>
        /// <param name="maxRadiusScale">The furthest back it may draw, as a share of <paramref name="radius"/>.</param>
        public static void Solve(Vector3 target, float cameraYaw, float elevation, float radius, Bounds load,
            float margin, float maxElevation, float maxRadiusScale, out float solvedElevation,
            out float solvedRadiusScale)
        {
            Bounds obstacle = load;
            obstacle.Expand(margin * 2f);
            solvedElevation = elevation;
            solvedRadiusScale = 1f;
            if (Clear(target, cameraYaw, elevation, radius, obstacle))
            {
                return;
            }

            float bestCost = float.MaxValue;
            float highest = Mathf.Max(elevation, maxElevation);
            for (int r = 0; r <= RadiusSteps; r++)
            {
                float scale = Mathf.Lerp(1f, maxRadiusScale, r / (float)RadiusSteps);
                for (int e = 0; e <= ElevationSteps; e++)
                {
                    float candidate = Mathf.Lerp(elevation, highest, e / (float)ElevationSteps);
                    float cost = RiseCostWeight * (candidate - elevation) / Mathf.Max(1f, highest - elevation) +
                                 (scale - 1f) / Mathf.Max(0.01f, maxRadiusScale - 1f);
                    if (cost < bestCost && Clear(target, cameraYaw, candidate, radius * scale, obstacle))
                    {
                        bestCost = cost;
                        solvedElevation = candidate;
                        solvedRadiusScale = scale;
                    }
                }
            }

            if (bestCost == float.MaxValue)
            {
                solvedElevation = highest;
                solvedRadiusScale = maxRadiusScale;
            }
        }

        /// <summary>
        /// As <see cref="Solve"/>, but the camera may also swing round to one side of the line behind 07 (a 3/4 rear
        /// view): from there the load and the rope stay in the frame beside 07 and it takes less of a rise to clear the
        /// load. The least swing that clears wins, a swing to the side the camera is already on preferred.
        /// </summary>
        /// <param name="preferredSide">+1 or -1: the side of the last solution, kept unless the other is much cheaper.</param>
        /// <param name="maxYawSwing">The widest the camera may swing to a side (deg).</param>
        public static void SolveSide(Vector3 target, float cameraYaw, float elevation, float radius, Bounds load,
            float margin, float maxElevation, float maxRadiusScale, float maxYawSwing, float preferredSide,
            out float solvedElevation, out float solvedRadiusScale, out float solvedYaw)
        {
            Solve(target, cameraYaw, elevation, radius, load, margin, maxElevation, maxRadiusScale,
                out solvedElevation, out solvedRadiusScale);
            solvedYaw = 0f;
            Bounds obstacle = load;
            obstacle.Expand(margin * 2f);
            float highest = Mathf.Max(elevation, maxElevation);
            float bestCost = Cost(solvedElevation, solvedRadiusScale, 0f, elevation, highest, maxRadiusScale,
                maxYawSwing);
            if (Clear(target, cameraYaw, solvedElevation, radius * solvedRadiusScale, obstacle) && bestCost <= 0f)
            {
                return;
            }

            for (int y = 1; y <= YawSteps; y++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float swing = side * maxYawSwing * y / YawSteps;
                    Solve(target, cameraYaw + swing, elevation, radius, load, margin, maxElevation, maxRadiusScale,
                        out float e, out float s);
                    if (!Clear(target, cameraYaw + swing, e, radius * s, obstacle))
                    {
                        continue;
                    }

                    float cost = Cost(e, s, swing, elevation, highest, maxRadiusScale, maxYawSwing) +
                                 (side == Mathf.Sign(preferredSide) ? 0f : OtherSideCost);
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        solvedElevation = e;
                        solvedRadiusScale = s;
                        solvedYaw = swing;
                    }
                }
            }
        }

        private static float Cost(float candidate, float scale, float swing, float elevation, float highest,
            float maxRadiusScale, float maxYawSwing)
        {
            return RiseCostWeight * (candidate - elevation) / Mathf.Max(1f, highest - elevation) +
                   (scale - 1f) / Mathf.Max(0.01f, maxRadiusScale - 1f) +
                   YawCostWeight * Mathf.Abs(swing) / Mathf.Max(1f, maxYawSwing);
        }

        /// <summary>Where the camera sits for a bearing, an elevation and a distance from <paramref name="target"/>.</summary>
        public static Vector3 EyeAt(Vector3 target, float cameraYaw, float elevation, float radius)
        {
            float up = Mathf.Sin(elevation * Mathf.Deg2Rad);
            float back = Mathf.Cos(elevation * Mathf.Deg2Rad);
            return target + Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(0f, up, -back) * radius;
        }

        /// <summary>
        /// True when the eye is outside the obstacle and the line from it to the target never crosses it.
        /// </summary>
        public static bool Clear(Vector3 target, float cameraYaw, float elevation, float radius, Bounds obstacle)
        {
            Vector3 eye = EyeAt(target, cameraYaw, elevation, radius);
            if (obstacle.Contains(eye))
            {
                return false;
            }

            Vector3 line = target - eye;
            float length = line.magnitude;
            if (length < 1e-4f)
            {
                return true;
            }

            var ray = new Ray(eye, line / length);
            return !obstacle.IntersectRay(ray, out float distance) || distance > length;
        }
    }
}

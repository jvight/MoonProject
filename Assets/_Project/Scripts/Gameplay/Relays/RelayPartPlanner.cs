using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where a relay mast's one missing part lies (docs/features/M3-06): on gentle, drivable ground within the tuning's
    /// distance range of the mast's pad (at most 40 m), the preferred distance first, and only where 07 can drive to it
    /// from the pad (the grid search Bell walks home by), so a part never lies beyond a wall or below a ledge and
    /// relay.3's part stays past the canyon's gate with its mast. Deterministic: each mast's rings start at an angle
    /// drawn from its seed. Runs once at initialisation (allocates).
    /// </summary>
    public static class RelayPartPlanner
    {
        private const float FullTurn = 360f;

        /// <summary>
        /// True with the part's spot on the surface; false with the problem when no spot in range qualifies (a world
        /// or tuning error the caller reports).
        /// </summary>
        /// <param name="paths">The walking grid's tuning (cell, climb, search size).</param>
        /// <param name="seed">Seeds where on each ring the search starts (the mast's index).</param>
        public static bool TryPlan(ITerrainQuery terrain, WorldAnchor mast, RelayTuning tuning, FriendTuning paths,
            int seed, out Vector3 spot, out string problem)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            Vector2 range = tuning.PartDistance;
            float minNormalY = SurfaceRules.MinNormalY(tuning.PartMaxSlope);
            var random = new DeterministicRandom(seed);
            float startAngle = random.Range(0f, FullTurn);
            int rings = Mathf.Max(1, Mathf.CeilToInt((range.y - range.x) / tuning.PartRingStep) + 1);
            int tries = 0;
            for (int ring = 0; ring < rings * 2; ring++)
            {
                float distance = RingDistance(tuning, ring);
                if (distance < range.x || distance > range.y)
                {
                    continue;
                }

                for (float angle = 0f; angle < FullTurn; angle += tuning.PartAngleStep)
                {
                    Vector3 direction = SurfaceRules.BearingDirection(startAngle + angle);
                    Vector3 point = mast.Position + direction * distance;
                    if (!SurfaceRules.InsideDrivable(terrain, point.x, point.z, tuning.PartMargin) ||
                        !SurfaceRules.IsFlat(terrain, point.x, point.z, minNormalY, tuning.PartMargin))
                    {
                        continue;
                    }

                    if (WalkPathPlanner.Plan(terrain, mast.Position, point, paths) != null)
                    {
                        spot = SurfaceRules.OnSurface(terrain, point.x, point.z);
                        problem = null;
                        return true;
                    }

                    if (++tries >= tuning.PartPathTries)
                    {
                        spot = Vector3.zero;
                        problem = $"none of {tries} gentle spots around '{mast.Id}' can be driven to from its pad";
                        return false;
                    }
                }
            }

            spot = Vector3.zero;
            problem = $"no gentle, drivable spot {range.x:F0}-{range.y:F0} m from '{mast.Id}'";
            return false;
        }

        /// <summary>Ring <paramref name="ring"/>'s distance: the preferred one, then farther, nearer, ...</summary>
        private static float RingDistance(RelayTuning tuning, int ring)
        {
            int step = (ring + 1) / 2;
            float sign = ring % 2 == 1 ? 1f : -1f;
            return tuning.PartPreferred + sign * step * tuning.PartRingStep;
        }
    }
}

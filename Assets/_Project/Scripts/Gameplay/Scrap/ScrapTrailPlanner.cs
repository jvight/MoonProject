using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The scrap trail to Whispering Canyon (docs/features/M3-04): a line of the cheapest scrap from where the way from
    /// home to the canyon's take-off lip leaves the playable area, up the mouth lane the World keeps clear, to just
    /// short of the lip, gently zigzagging so the glints read as a path that draws the eye to the chasm. It only adds
    /// to the basin's scrap (VISION ruling 5 is a floor, never a cap). Pieces never lie off the drivable floor.
    /// Deterministic; runs once at initialisation (allocates).
    /// </summary>
    public static class ScrapTrailPlanner
    {
        /// <summary>Metres per step when marching out to the playable area's edge.</summary>
        private const float EdgeStep = 2f;

        /// <summary>Golden angle (degrees): neighbouring pieces never turn or bob in step.</summary>
        private const float GoldenAngle = 137.50776f;

        /// <summary>
        /// The trail's pieces, or null with <paramref name="problem"/> when the World publishes no take-off lip.
        /// </summary>
        public static List<ScrapSpawn> Plan(ITerrainQuery terrain, IWorldLayout layout, IWorldAnchors anchors,
            ScrapTuning tuning, IReadOnlyList<ScrapVariant> variants, out string problem)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (variants == null || variants.Count == 0)
            {
                throw new ArgumentException("At least one scrap variant is needed.", nameof(variants));
            }

            if (!anchors.TryGet(WorldAnchorIds.CanyonLip, out WorldAnchor lip))
            {
                problem = $"the World publishes no '{WorldAnchorIds.CanyonLip}' for the scrap trail";
                return null;
            }

            problem = null;
            var spawns = new List<ScrapSpawn>();
            Vector3 home = layout.BasePosition;
            Vector3 toLip = lip.Position - home;
            toLip.y = 0f;
            float length = toLip.magnitude;
            int pieces = tuning.CanyonTrailPieces;
            if (pieces == 0 || length <= tuning.BaseClearRadius + tuning.CanyonTrailLipGap)
            {
                return spawns;
            }

            Vector3 direction = toLip / length;
            var side = new Vector3(direction.z, 0f, -direction.x);
            float end = length - tuning.CanyonTrailLipGap;
            float start = Mathf.Min(TrailStart(terrain, home, direction, length, tuning), end);
            int variant = Cheapest(variants);
            for (int i = 0; i < pieces; i++)
            {
                float along = pieces == 1 ? end : Mathf.Lerp(start, end, (float)i / (pieces - 1));
                float sway = (i % 2 == 0 ? 1f : -1f) * tuning.CanyonTrailWobble;
                Vector3 point = home + direction * along + side * sway;
                if (!terrain.IsDrivable(point.x, point.z))
                {
                    continue;
                }

                var position = new Vector3(point.x, terrain.SampleHeight(point.x, point.z) + tuning.HoverHeight,
                    point.z);
                spawns.Add(new ScrapSpawn(position, variant, Mathf.Repeat(i * GoldenAngle, 360f) * Mathf.Deg2Rad,
                    Mathf.Repeat(i * GoldenAngle, 360f)));
            }

            return spawns;
        }

        /// <summary>
        /// Metres from home where the way to the lip leaves the playable area (or the base pad's edge when the lip
        /// lies inside it).
        /// </summary>
        private static float TrailStart(ITerrainQuery terrain, Vector3 home, Vector3 direction, float length,
            ScrapTuning tuning)
        {
            Rect area = terrain.PlayableArea;
            float inside = tuning.BaseClearRadius;
            for (float along = inside; along < length; along += EdgeStep)
            {
                Vector3 point = home + direction * along;
                if (!area.Contains(new Vector2(point.x, point.z)))
                {
                    return inside;
                }

                inside = along;
            }

            return tuning.BaseClearRadius;
        }

        private static int Cheapest(IReadOnlyList<ScrapVariant> variants)
        {
            int cheapest = 0;
            for (int i = 1; i < variants.Count; i++)
            {
                if (variants[i].Value < variants[cheapest].Value)
                {
                    cheapest = i;
                }
            }

            return cheapest;
        }
    }
}

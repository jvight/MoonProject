using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Chooses where a basin cassette waits (Dust &amp; Honey), deterministically from its seed and the analytic
    /// surface: a gentle, drivable spot inside the playable area, in the asked distance band from home, clear of every
    /// relic site, friend site and friend part, preferably tucked against the inner slope of a small crater rim (the
    /// ground rises a little within a few metres on one side). The tape leans back against that rim with its label
    /// toward open ground. The best spot is taken when none is tucked (the result says so). Runs once at
    /// initialisation; throws when the surface leaves no valid spot at all (broken world tuning).
    /// </summary>
    public static class CassetteSitePlanner
    {
        /// <summary>A tucked spot always beats an open one; among tucked spots the clearer rim wins.</summary>
        private const float TuckedWeight = 100f;

        public static CassetteSite Plan(ITerrainQuery terrain, IWorldLayout layout, CassetteTuning tuning, int seed,
            IReadOnlyList<Vector3> keepClear)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (keepClear == null)
            {
                throw new ArgumentNullException(nameof(keepClear));
            }

            var random = new DeterministicRandom(seed);
            Vector3 home = layout.BasePosition;
            Rect area = terrain.PlayableArea;
            Vector2 distances = tuning.Distance;
            Vector2 rise = tuning.RimRise;
            float minNormalY = SurfaceRules.MinNormalY(tuning.MaxSlope);
            float bestScore = float.MinValue;
            CassetteSite best = default;
            bool found = false;
            for (int attempt = 0; attempt < tuning.Attempts; attempt++)
            {
                Vector3 direction = SurfaceRules.BearingDirection(random.Range(0f, 360f));
                float distance = random.Range(distances.x, distances.y);
                float x = home.x + direction.x * distance;
                float z = home.z + direction.z * distance;
                if (!area.Contains(new Vector2(x, z)) ||
                    !SurfaceRules.InsideDrivable(terrain, x, z, tuning.EdgeMargin) ||
                    !SurfaceRules.IsFlat(terrain, x, z, minNormalY, tuning.FlatProbe) ||
                    Near(x, z, keepClear, tuning.Clearance))
                {
                    continue;
                }

                float rimRise = Rim(terrain, x, z, tuning.RimProbe, tuning.RimDirections, out Vector3 toRim);
                bool tucked = rimRise >= rise.x && rimRise <= rise.y;
                float shelter = rimRise <= rise.y ? rimRise : 2f * rise.y - rimRise;
                float score = (tucked ? TuckedWeight : 0f) + shelter;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new CassetteSite(SurfaceRules.OnSurface(terrain, x, z), -toRim, tucked);
                    found = true;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException(
                    $"{nameof(CassetteSitePlanner)}: no gentle, drivable spot {distances.x:F0}-{distances.y:F0} m " +
                    $"from home and {tuning.Clearance:F0} m clear of relics and friends (seed {seed}). " +
                    "Check the world surface or the cassette tuning.");
            }

            return best;
        }

        /// <summary>
        /// How much higher (m) the ground is <paramref name="probe"/> metres away in its highest direction, and that
        /// direction (horizontal unit vector).
        /// </summary>
        public static float Rim(ITerrainQuery terrain, float x, float z, float probe, int directions,
            out Vector3 toRim)
        {
            float here = terrain.SampleHeight(x, z);
            float highest = float.MinValue;
            toRim = Vector3.forward;
            int count = Mathf.Max(1, directions);
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = SurfaceRules.BearingDirection(360f * i / count);
                float rise = terrain.SampleHeight(x + direction.x * probe, z + direction.z * probe) - here;
                if (rise > highest)
                {
                    highest = rise;
                    toRim = direction;
                }
            }

            return highest;
        }

        private static bool Near(float x, float z, IReadOnlyList<Vector3> points, float clearance)
        {
            var point = new Vector3(x, 0f, z);
            for (int i = 0; i < points.Count; i++)
            {
                if (SurfaceRules.HorizontalDistance(point, points[i]) < clearance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

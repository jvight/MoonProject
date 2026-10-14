using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Chooses where a broken friend lies, deterministically from its placement seed and the analytic surface: the site
    /// is a gentle, drivable spot in the asked distance band and bearing fan, preferably in a shallow hollow (a small
    /// crater) that can be seen from the edge of the base pad, away from the relic sites. The best spot is taken when
    /// no spot meets every preference (the result says which ones it meets). Runs once at initialisation (allocates);
    /// throws when the surface leaves no drivable spot at all (broken world tuning).
    /// </summary>
    public static class FriendSitePlanner
    {
        private const int RingSamples = 12;

        // Scores: the view matters most (the player must be able to see the silhouette), then lying in a crater;
        // among spots that have both, the clearer view wins, and a little depth breaks ties.
        private const float ViewWeight = 120f;
        private const float CraterWeight = 100f;
        private const float ClearanceCap = 2f;
        private const float DepthWeight = 0.25f;

        /// <summary>Plans the friend's site.</summary>
        public static FriendSite Plan(ITerrainQuery terrain, IWorldLayout layout, FriendPlacement placement,
            FriendTuning tuning, IReadOnlyList<RelicSite> relicSites)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (placement == null)
            {
                throw new ArgumentNullException(nameof(placement));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (relicSites == null)
            {
                throw new ArgumentNullException(nameof(relicSites));
            }

            var siteRandom = new DeterministicRandom(placement.Seed);
            Candidate site = ChooseSite(terrain, layout, placement, tuning, relicSites, ref siteRandom);
            Vector3 toHome = layout.BasePosition - site.Position;
            toHome.y = 0f;
            Vector3 facing = toHome.sqrMagnitude > 1e-6f ? toHome.normalized : Vector3.forward;
            return new FriendSite(site.Position, terrain.SampleNormal(site.Position.x, site.Position.z), facing,
                Array.Empty<Vector3>(), site.InCrater, site.Visible);
        }

        /// <summary>How much lower (m) (x, z) lies than the ring <paramref name="radius"/> around it.</summary>
        public static float Hollow(ITerrainQuery terrain, float x, float z, float radius)
        {
            float ring = 0f;
            for (int i = 0; i < RingSamples; i++)
            {
                float angle = 2f * Mathf.PI * i / RingSamples;
                ring += terrain.SampleHeight(x + Mathf.Cos(angle) * radius, z + Mathf.Sin(angle) * radius);
            }

            return ring / RingSamples - terrain.SampleHeight(x, z);
        }

        /// <summary>
        /// Smallest height (m) of the line from <paramref name="from"/> to <paramref name="to"/> above the ground
        /// (positive: clear view).
        /// </summary>
        public static float SightClearance(ITerrainQuery terrain, Vector3 from, Vector3 to, int samples)
        {
            float clearance = float.MaxValue;
            for (int i = 1; i < samples; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, (float)i / samples);
                clearance = Mathf.Min(clearance, point.y - terrain.SampleHeight(point.x, point.z));
            }

            return clearance;
        }

        private static Candidate ChooseSite(ITerrainQuery terrain, IWorldLayout layout, FriendPlacement placement,
            FriendTuning tuning, IReadOnlyList<RelicSite> relicSites, ref DeterministicRandom random)
        {
            Vector3 home = layout.BasePosition;
            float minNormalY = SurfaceRules.MinNormalY(tuning.SiteMaxSlope);
            Vector2 distances = placement.SiteDistance;
            Vector2 depths = placement.CraterDepth;
            Candidate best = default;
            float bestScore = float.MinValue;
            for (int attempt = 0; attempt < tuning.SiteAttempts; attempt++)
            {
                float bearing = placement.SiteBearing +
                                random.Range(-placement.SiteBearingSpread, placement.SiteBearingSpread);
                Vector3 direction = SurfaceRules.BearingDirection(bearing);
                float distance = random.Range(distances.x, distances.y);
                float x = home.x + direction.x * distance;
                float z = home.z + direction.z * distance;
                if (!SurfaceRules.InsideDrivable(terrain, x, z, tuning.SiteEdgeMargin) ||
                    !SurfaceRules.IsFlat(terrain, x, z, minNormalY, tuning.SiteFlatProbe) ||
                    Near(x, z, relicSites, tuning.SiteClearOfRelics))
                {
                    continue;
                }

                Vector3 floor = SurfaceRules.OnSurface(terrain, x, z);
                float hollow = Hollow(terrain, x, z, placement.CraterRadius);
                Vector3 edge = home + direction * tuning.BaseEdge;
                edge.y = terrain.SampleHeight(edge.x, edge.z) + tuning.ViewerEyeHeight;
                float clearance = SightClearance(terrain, edge, floor + Vector3.up * tuning.SiteViewHeight,
                    tuning.SightSamples);
                bool inCrater = hollow >= depths.x && hollow <= depths.y;
                bool visible = clearance >= 0f;
                float score = (inCrater ? CraterWeight : 0f) +
                              (visible || !placement.VisibleFromBase ? ViewWeight : 0f) +
                              Mathf.Min(clearance, ClearanceCap) + Mathf.Min(hollow, depths.y) * DepthWeight;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new Candidate(floor, inCrater, visible, true);
                }
            }

            if (!best.Found)
            {
                throw new InvalidOperationException(
                    $"{nameof(FriendSitePlanner)}: no gentle, drivable spot {distances.x:F0}-{distances.y:F0} m " +
                    $"from home around bearing {placement.SiteBearing:F0}. Check the world surface or the placement.");
            }

            return best;
        }

        private static bool Near(float x, float z, IReadOnlyList<RelicSite> sites, float clearance)
        {
            var point = new Vector3(x, 0f, z);
            for (int i = 0; i < sites.Count; i++)
            {
                if (SurfaceRules.HorizontalDistance(point, sites[i].Position) < clearance)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct Candidate
        {
            public Candidate(Vector3 position, bool inCrater, bool visible, bool found)
            {
                Position = position;
                InCrater = inCrater;
                Visible = visible;
                Found = found;
            }

            public Vector3 Position { get; }

            public bool InCrater { get; }

            public bool Visible { get; }

            public bool Found { get; }
        }
    }
}

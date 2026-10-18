using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Picks the rare big boulders' sites (<see cref="BigBoulderSettings"/>): Poisson-disk sites far apart over the
    /// basin floor, a seeded few of them kept, never near the pad, inside any anchored place's reach, in a route's
    /// corridor or on steep ground. Plain logic, deterministic for the seed.
    /// </summary>
    public static class BigBoulderPlanner
    {
        private const uint SiteSalt = 0x4A7C15E3u;
        private const uint KeepSalt = 0x9E6C63D1u;
        private const uint ShapeSalt = 0x2C1B3C6Du;

        // Sites hash on a centimetre lattice, so the plan never depends on float noise in the sampler.
        private const float HashQuantum = 100f;

        /// <summary>One big boulder: where it stands on the ground, its yaw (degrees) and which rock it is.</summary>
        public readonly struct Site
        {
            public Site(Vector3 position, float yaw, int rock)
            {
                Position = position;
                Yaw = yaw;
                Rock = rock;
            }

            public Vector3 Position { get; }
            public float Yaw { get; }
            public int Rock { get; }
        }

        /// <param name="routes">Route segments (pairs of world XZ points) whose corridors stay open.</param>
        public static List<Site> Plan(MoonSurface surface, IWorldAnchors anchors, BigBoulderSettings settings,
            IReadOnlyList<Vector2> routes)
        {
            if (surface == null || anchors == null || settings == null || routes == null)
            {
                throw new ArgumentNullException(surface == null ? nameof(surface)
                    : anchors == null ? nameof(anchors) : settings == null ? nameof(settings) : nameof(routes));
            }

            if (settings.Rocks == null || settings.Rocks.Length == 0)
            {
                throw new ArgumentException("The big boulder tier needs rocks.", nameof(settings));
            }

            uint seed = Hashing.Mix((uint)surface.Seed ^ SiteSalt);
            float maxSlopeCos = Mathf.Cos(settings.MaxSlope * Mathf.Deg2Rad);
            var sites = new List<Site>();
            foreach (Vector2 site in PoissonDiskSampler.SampleDisc(settings.Extent, settings.Spacing, seed))
            {
                int qx = Mathf.RoundToInt(site.x * HashQuantum);
                int qz = Mathf.RoundToInt(site.y * HashQuantum);
                if (Hashing.ToUnit(Hashing.Hash(qx, qz, seed ^ KeepSalt)) >= settings.Chance
                    || site.magnitude < settings.PadClearance || NearAnchor(site, anchors, settings.AnchorClearance)
                    || NearRoute(site, routes, settings.RouteClearance)
                    || InScoutCrater(site, surface.Shaping, settings.AnchorClearance)
                    || surface.SampleNormal(site.x, site.y).y < maxSlopeCos)
                {
                    continue;
                }

                uint shape = Hashing.Hash(qx, qz, seed ^ ShapeSalt);
                var ground = new Vector3(site.x, surface.SampleHeight(site.x, site.y), site.y);
                sites.Add(new Site(ground, Hashing.ToUnit(Hashing.Mix(shape)) * 360f,
                    (int)(shape % (uint)settings.Rocks.Length)));
            }

            return sites;
        }

        private static bool NearAnchor(Vector2 site, IWorldAnchors anchors, float clearance)
        {
            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                float reach = anchor.Radius + clearance;
                if ((site - new Vector2(anchor.Position.x, anchor.Position.z)).sqrMagnitude < reach * reach)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The Scout's crater (basin dressing §2) stays its own: no flat anchor, so checked apart.</summary>
        private static bool InScoutCrater(Vector2 site, GroundShaping shaping, float clearance)
        {
            float reach = shaping.ScoutCraterRadius + clearance;
            return (site - shaping.ScoutCraterCenter).sqrMagnitude < reach * reach;
        }

        private static bool NearRoute(Vector2 site, IReadOnlyList<Vector2> routes, float clearance)
        {
            for (int i = 1; i < routes.Count; i += 2)
            {
                Vector2 a = routes[i - 1];
                Vector2 b = routes[i];
                Vector2 span = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(site - a, span) / Mathf.Max(span.sqrMagnitude, 1e-4f));
                if (Vector2.Distance(site, a + span * t) < clearance)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

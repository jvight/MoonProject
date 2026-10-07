using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Places a friend at the World's anchors (Bell, docs/features/M3-05 "Meeting"): it lies at its site anchor facing
    /// back toward 07 arriving, part i waits at the numbered part anchor i (canyon.alcove_i), and parts left over
    /// when the World has fewer such anchors go along the driving line (from its start through the part anchors to
    /// the site) at least the line spacing apart from every other part and from the site, relaxed once if the line is
    /// short. Also lays out its way home: a walking line over drivable ground from its site anchor to the top of the
    /// way out (<see cref="WalkPathPlanner"/>), on past the step down to the basin floor, then home. Pure; a missing
    /// anchor is reported (a wiring bug), never invented.
    /// </summary>
    public static class FriendAnchorPlanner
    {
        /// <summary>Metres between the points tried along the driving line.</summary>
        private const float LineStep = 2f;

        /// <summary>The spacing is relaxed by this factor when the driving line is too short for it.</summary>
        private const float RelaxedSpacingFactor = 0.6f;

        /// <summary>
        /// The site, its facing and <paramref name="partCount"/> part spots, or null with
        /// <paramref name="problem"/> saying which anchor is missing or why the parts do not fit.
        /// </summary>
        public static FriendSite Plan(IWorldAnchors anchors, ITerrainQuery terrain, FriendAnchorPlacement placement,
            int partCount, out string problem)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (placement == null)
            {
                throw new ArgumentNullException(nameof(placement));
            }

            if (!placement.Site.TryResolve(anchors, terrain, out Vector3 site, out Vector3 facing))
            {
                problem = $"the World publishes no site anchor '{placement.Site.AnchorId}'";
                return null;
            }

            if (!anchors.TryGet(placement.LineStart, out WorldAnchor start))
            {
                problem = $"the World publishes no driving line start '{placement.LineStart}'";
                return null;
            }

            var parts = new Vector3[partCount];
            var line = new List<Vector3> { start.Position };
            int placed = 0;
            for (int i = 0; i < partCount; i++)
            {
                if (!anchors.TryGet(placement.PartAnchorPrefix + i, out WorldAnchor anchor))
                {
                    break;
                }

                parts[i] = SurfaceRules.OnSurface(terrain, anchor.Position.x, anchor.Position.z);
                line.Add(anchor.Position);
                placed++;
            }

            line.Add(site);
            float spacing = placement.LineSpacing;
            for (int pass = 0; pass < 2 && placed < partCount; pass++)
            {
                placed = AlongLine(terrain, line, site, spacing, parts, placed);
                spacing *= RelaxedSpacingFactor;
            }

            if (placed < partCount)
            {
                problem = $"the driving line from '{placement.LineStart}' is too short for {partCount} parts " +
                          $"{placement.LineSpacing * RelaxedSpacingFactor:F0} m apart";
                return null;
            }

            problem = null;
            return new FriendSite(site, terrain.SampleNormal(site.x, site.z), facing, parts, false, false);
        }

        /// <summary>
        /// Its way home from its site, or null with <paramref name="problem"/>: a walking line over drivable ground
        /// from the site anchor to the way out's anchor, then <paramref name="belowStep"/> metres on along it (down its
        /// step to the basin floor), then <paramref name="home"/>.
        /// </summary>
        public static Vector3[] WayHome(IWorldAnchors anchors, ITerrainQuery terrain, FriendAnchorPlacement placement,
            Vector3 home, float belowStep, FriendTuning tuning, out string problem)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (placement == null)
            {
                throw new ArgumentNullException(nameof(placement));
            }

            if (!anchors.TryGet(placement.Site.AnchorId, out WorldAnchor site))
            {
                problem = $"the World publishes no site anchor '{placement.Site.AnchorId}'";
                return null;
            }

            if (!anchors.TryGet(placement.Exit, out WorldAnchor exit))
            {
                problem = $"the World publishes no way out '{placement.Exit}'";
                return null;
            }

            Vector3[] walk = WalkPathPlanner.Plan(terrain, site.Position, exit.Position, tuning);
            if (walk == null)
            {
                problem = $"no drivable walk from '{placement.Site.AnchorId}' to '{placement.Exit}'";
                return null;
            }

            var route = new List<Vector3>(walk);
            Vector3 below = exit.Position + exit.Forward * belowStep;
            route.Add(SurfaceRules.OnSurface(terrain, below.x, below.z));
            route.Add(home);
            problem = null;
            return route.ToArray();
        }

        private static int AlongLine(ITerrainQuery terrain, List<Vector3> line, Vector3 site, float spacing,
            Vector3[] parts, int placed)
        {
            for (int segment = 0; segment < line.Count - 1 && placed < parts.Length; segment++)
            {
                Vector3 from = line[segment];
                Vector3 to = line[segment + 1];
                float length = SurfaceRules.HorizontalDistance(from, to);
                for (float along = 0f; along <= length && placed < parts.Length; along += LineStep)
                {
                    Vector3 point = Vector3.Lerp(from, to, length > 0f ? along / length : 0f);
                    if (SurfaceRules.HorizontalDistance(point, site) < spacing || Near(point, parts, placed, spacing))
                    {
                        continue;
                    }

                    parts[placed] = SurfaceRules.OnSurface(terrain, point.x, point.z);
                    placed++;
                }
            }

            return placed;
        }

        private static bool Near(Vector3 point, Vector3[] parts, int placed, float spacing)
        {
            for (int i = 0; i < placed; i++)
            {
                if (SurfaceRules.HorizontalDistance(point, parts[i]) < spacing)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

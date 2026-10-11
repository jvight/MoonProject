using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Map;

namespace MoonProject.World.Map
{
    /// <summary>
    /// Traces the crew map's <see cref="MapSheet"/> from the generated surface (docs/features/M3-07): contours every
    /// interval, the rim crest, Whispering Canyon's walls and its way out, the chasm's trough and the larger craters'
    /// rims, all thinned, plus every world anchor's position. Deterministic for a seed; edit time only.
    /// </summary>
    public static class MapSheetTracer
    {
        private const float WallLevel = 0.5f;

        public static void Trace(MapSheet sheet, MoonSurface surface, IWorldAnchors anchors, MapSheetTuning tuning)
        {
            if (sheet == null || surface == null || anchors == null || tuning == null)
            {
                throw new ArgumentNullException(sheet == null ? nameof(sheet) : surface == null ? nameof(surface)
                    : anchors == null ? nameof(anchors) : nameof(tuning));
            }

            Vector2 centre = surface.PlayableArea.center;
            float extent = tuning.Extent;
            var paper = new Rect(centre - Vector2.one * extent, Vector2.one * (2f * extent));
            var points = new List<Vector2>();
            var lines = new List<MapLine>();
            AddContours(surface, paper, tuning, points, lines);
            AddCrest(surface, centre, tuning, points, lines);
            AddCanyon(surface, paper, tuning, points, lines);
            AddCraters(surface, centre, tuning, points, lines);

            var ids = new string[anchors.Count];
            var positions = new Vector2[anchors.Count];
            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                ids[i] = anchor.Id;
                positions[i] = new Vector2(anchor.Position.x, anchor.Position.z);
            }

            sheet.Write(surface.Seed, centre, extent, tuning.ContourInterval, points.ToArray(), lines.ToArray(), ids,
                positions);
        }

        private static void AddContours(MoonSurface surface, Rect paper, MapSheetTuning tuning, List<Vector2> points,
            List<MapLine> lines)
        {
            var grid = new IsoGrid(paper, tuning.GridStep, surface.SampleHeight);
            float interval = tuning.ContourInterval;
            var traced = new List<List<Vector2>>();
            var closed = new List<bool>();
            for (float level = Mathf.Ceil(grid.Min / interval) * interval; level <= grid.Max; level += interval)
            {
                traced.Clear();
                closed.Clear();
                grid.Trace(level, traced, closed);
                for (int i = 0; i < traced.Count; i++)
                {
                    if (closed[i] && PolylineSimplifier.Length(traced[i], true) < tuning.MinLoopLength)
                    {
                        continue;
                    }

                    Add(MapLineKind.Contour, traced[i], closed[i], level, tuning, points, lines);
                }
            }
        }

        /// <summary>
        /// The highest point along each bearing within the crest band, its distance averaged with the neighbouring
        /// bearings', joined all the way round.
        /// </summary>
        private static void AddCrest(MoonSurface surface, Vector2 centre, MapSheetTuning tuning, List<Vector2> points,
            List<MapLine> lines)
        {
            int bearings = tuning.CrestBearings;
            var radii = new float[bearings];
            float inner = Mathf.Max(0f, surface.CrestRadius - tuning.CrestSearchBand);
            float outer = Mathf.Min(tuning.Extent, surface.CrestRadius + tuning.CrestSearchBand);
            for (int b = 0; b < bearings; b++)
            {
                Vector2 direction = MoonSurface.BearingToDirection(360f * b / bearings);
                float top = float.NegativeInfinity;
                for (float r = inner; r <= outer; r += tuning.CrestSearchStep)
                {
                    Vector2 point = centre + direction * r;
                    float sample = surface.SampleHeight(point.x, point.y);
                    if (sample > top)
                    {
                        top = sample;
                        radii[b] = r;
                    }
                }
            }

            var crest = new List<Vector2>(bearings);
            float height = 0f;
            int smoothing = tuning.CrestSmoothing;
            for (int b = 0; b < bearings; b++)
            {
                float radius = 0f;
                for (int k = -smoothing; k <= smoothing; k++)
                {
                    radius += radii[(b + k + bearings) % bearings];
                }

                Vector2 point = centre + MoonSurface.BearingToDirection(360f * b / bearings)
                    * (radius / (2 * smoothing + 1));
                crest.Add(point);
                height += surface.SampleHeight(point.x, point.y);
            }

            Add(MapLineKind.RimCrest, crest, true, height / bearings, tuning, points, lines);
        }

        /// <summary>
        /// The corridors' wall feet (where the floor weight crosses one half) and the chasm trough's outline, cut open
        /// where a corridor opens onto the basin.
        /// </summary>
        private static void AddCanyon(MoonSurface surface, Rect paper, MapSheetTuning tuning, List<Vector2> points,
            List<MapLine> lines)
        {
            Canyon canyon = surface.Canyon;
            Rect bounds = canyon.Bounds;
            Vector2 min = Vector2.Max(bounds.min, paper.min);
            Vector2 max = Vector2.Min(bounds.max, paper.max);
            if (max.x <= min.x || max.y <= min.y)
            {
                return;
            }

            var area = new Rect(min, max - min);
            var floors = new IsoGrid(area, tuning.CanyonGridStep, (x, z) => FloorWeight(canyon, x, z));
            var chasm = new IsoGrid(area, tuning.CanyonGridStep, (x, z) => ChasmWeight(canyon, x, z));
            var traced = new List<List<Vector2>>();
            var closed = new List<bool>();
            floors.Trace(WallLevel, traced, closed);
            for (int i = 0; i < traced.Count; i++)
            {
                foreach (List<Vector2> run in OpenAtMouths(canyon, traced[i], closed[i], tuning))
                {
                    AddWithHeight(surface, MapLineKind.CanyonWall, run, false, tuning, points, lines);
                }
            }

            traced.Clear();
            closed.Clear();
            chasm.Trace(WallLevel, traced, closed);
            for (int i = 0; i < traced.Count; i++)
            {
                AddWithHeight(surface, MapLineKind.Chasm, traced[i], closed[i], tuning, points, lines);
            }
        }

        private static void AddCraters(MoonSurface surface, Vector2 centre, MapSheetTuning tuning,
            List<Vector2> points, List<MapLine> lines)
        {
            var rim = new List<Vector2>(tuning.CraterSegments);
            foreach (Crater crater in surface.Craters)
            {
                if (crater.Radius < tuning.CraterMinRadius
                    || (crater.Center - centre).magnitude + crater.Radius > tuning.Extent)
                {
                    continue;
                }

                rim.Clear();
                float height = 0f;
                for (int s = 0; s < tuning.CraterSegments; s++)
                {
                    float angle = 2f * Mathf.PI * s / tuning.CraterSegments;
                    Vector2 point = crater.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * crater.Radius;
                    rim.Add(point);
                    height += surface.SampleHeight(point.x, point.y);
                }

                Add(MapLineKind.CraterRim, rim, true, height / tuning.CraterSegments, tuning, points, lines);
            }
        }

        private static float FloorWeight(Canyon canyon, float x, float z)
        {
            canyon.Apply(x, z, 0f, out float floor, out _);
            return floor;
        }

        private static float ChasmWeight(Canyon canyon, float x, float z)
        {
            canyon.Apply(x, z, 0f, out _, out float chasm);
            return chasm;
        }

        /// <summary>
        /// Splits a traced floor edge wherever it runs across a corridor's basin end (near a path's start and near
        /// its centre line), so no wall line closes a mouth.
        /// </summary>
        private static List<List<Vector2>> OpenAtMouths(Canyon canyon, List<Vector2> line, bool closed,
            MapSheetTuning tuning)
        {
            CanyonSettings settings = canyon.Settings;
            float mainCut = settings.MouthHalfWidth * tuning.CanyonMouthCut;
            float exitCut = settings.ExitHalfWidth * tuning.CanyonMouthCut;
            var runs = new List<List<Vector2>>();
            var run = new List<Vector2>();
            int count = closed ? line.Count + 1 : line.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = line[i % line.Count];
                if (AcrossMouth(canyon.MainPath, point, tuning.CanyonMouthZone, mainCut)
                    || AcrossMouth(canyon.ExitPath, point, tuning.CanyonMouthZone, exitCut))
                {
                    if (run.Count >= 2)
                    {
                        runs.Add(run);
                    }

                    run = new List<Vector2>();
                    continue;
                }

                run.Add(point);
            }

            if (run.Count >= 2)
            {
                runs.Add(run);
            }

            return runs;
        }

        private static bool AcrossMouth(CanyonPath path, Vector2 point, float zone, float cut)
        {
            return path.TryProject(point.x, point.y, out float arc, out float lateral)
                && arc <= path.StartArc + zone && Mathf.Abs(lateral) < cut;
        }

        private static void AddWithHeight(MoonSurface surface, MapLineKind kind, List<Vector2> line, bool closed,
            MapSheetTuning tuning, List<Vector2> points, List<MapLine> lines)
        {
            float height = 0f;
            foreach (Vector2 point in line)
            {
                height += surface.SampleHeight(point.x, point.y);
            }

            Add(kind, line, closed, line.Count > 0 ? height / line.Count : 0f, tuning, points, lines);
        }

        private static void Add(MapLineKind kind, List<Vector2> line, bool closed, float height, MapSheetTuning tuning,
            List<Vector2> points, List<MapLine> lines)
        {
            List<Vector2> thin = PolylineSimplifier.Simplify(line, closed, tuning.SimplifyTolerance);
            if (thin.Count < 2 || (closed && thin.Count < 3))
            {
                return;
            }

            lines.Add(new MapLine(kind, points.Count, thin.Count, closed, height));
            points.AddRange(thin);
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Whispering Canyon (docs/features/M3-04, design rulings 5 and 10): out of PlayableArea, drivable floors, a
    /// trough you can always drive out of, a real gate (no path in that bypasses the gap) and a one-way exit.
    /// </summary>
    public sealed class CanyonTests
    {
        // The rover treats faces steeper than 60 degrees as walls (RoverTuning.Ground.MaxGroundAngle).
        private const float WallSlope = 60f;
        private const float MaxDrivableSlope = 20f;
        private const float ExitRampSlope = 12f;
        private const float GridStep = 0.5f;

        // The rover climbs at most 45 degrees (RoverTuning.Ground's slope assist ends there); the climb-and-drop
        // sweep covers the whole basin out past its rim at a coarser step.
        private const float ClimbSlope = 45f;
        private const float ReachStep = 1f;
        private const float ReachExtent = 420f;
        private const float MinInteriorArea = 2500f;

        // Eye height of a view from the base pad, and how clear (m) the line of sight to the ledge's glow must be.
        private const float EyeHeight = 3f;
        private const float SightMargin = 0.5f;
        private const float SightTargetClearance = 2f;

        private SurfaceSettings _settings;
        private MoonSurface _surface;
        private Canyon _canyon;

        [SetUp]
        public void SetUp()
        {
            _settings = new SurfaceSettings();
            _surface = new MoonSurface(_settings, WorldSettings.DefaultSeed);
            _canyon = _surface.Canyon;
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(_settings.Canyon.Validate());
        }

        [Test]
        public void PlayableArea_IsUnchanged_AndUntouchedByTheCanyon()
        {
            float half = (_settings.FloorRadius - _settings.RimWarpAmplitude) * 0.70710678f;
            Assert.AreEqual(new Rect(-half, -half, half * 2f, half * 2f), _surface.PlayableArea);

            Rect area = _surface.PlayableArea;
            Rect overlap = Rect.MinMaxRect(Mathf.Max(area.xMin, _canyon.Bounds.xMin),
                Mathf.Max(area.yMin, _canyon.Bounds.yMin), Mathf.Min(area.xMax, _canyon.Bounds.xMax),
                Mathf.Min(area.yMax, _canyon.Bounds.yMax));
            for (float z = overlap.yMin; z <= overlap.yMax; z += 1f)
            {
                for (float x = overlap.xMin; x <= overlap.xMax; x += 1f)
                {
                    float world = _surface.SampleHeight(x, z);
                    Assert.AreEqual(world, _canyon.Apply(x, z, world, out float floor, out float _), 0f,
                        $"the canyon reshapes PlayableArea at ({x}, {z})");
                    Assert.AreEqual(0f, floor);
                    Assert.IsNull(_canyon.IsDrivable(x, z), $"the canyon decides drivability at ({x}, {z})");
                }
            }
        }

        [Test]
        public void Chasm_IsTooWideForAHop_AndTheApronIsOutOfReach()
        {
            Assert.That(_canyon.GapToFarFace, Is.InRange(18f, 22f), "lip to far face");
            Assert.GreaterOrEqual(_canyon.ApronHeight - _canyon.LipHeight, 1.5f,
                "the apron must sit above any normal hop (~1 m apex)");
            Assert.GreaterOrEqual(_canyon.ApronHeight - _canyon.TroughHeight, 4f, "far face height");
            for (float arc = 0f; arc < _canyon.LipArc; arc += 0.5f)
            {
                Assert.LessOrEqual(_canyon.MainFloor(arc), _canyon.LipHeight + 1e-4f, "the lip crowns the ramp");
            }
        }

        [Test]
        public void Trough_AlwaysHasADrivableWayBackOut()
        {
            CanyonPath main = _canyon.MainPath;
            float bottom = _canyon.FarFaceArc - Canyon.SlabHalfDepth - 1f;
            for (float lateral = -_settings.Canyon.ChasmHalfWidth + 2f; lateral <= _settings.Canyon.ChasmHalfWidth - 2f;
                 lateral += 2f)
            {
                for (float arc = bottom; arc >= _canyon.LipArc - 4f; arc -= 0.5f)
                {
                    Vector2 p = main.PointAt(arc) + main.RightAt(arc) * lateral;
                    Assert.IsTrue(_surface.IsDrivable(p.x, p.y),
                        $"trough not drivable at arc {arc}, lateral {lateral}");
                    Assert.LessOrEqual(Slope(p.x, p.y), MaxDrivableSlope, $"trough side too steep at arc {arc}");
                }
            }
        }

        [Test]
        public void CanyonFloors_StayWithinTheSlopeLimit()
        {
            Rect bounds = _canyon.Bounds;
            int samples = 0;
            for (float z = bounds.yMin; z <= bounds.yMax; z += 1f)
            {
                for (float x = bounds.xMin; x <= bounds.xMax; x += 1f)
                {
                    if (_surface.Sample(x, z).CanyonFloor < 0.5f || !_surface.IsDrivable(x, z))
                    {
                        continue;
                    }

                    samples++;
                    Assert.LessOrEqual(Slope(x, z), MaxDrivableSlope, $"canyon floor too steep at ({x}, {z})");
                }
            }

            Assert.Greater(samples, 5000, "the canyon should have a real floor");
        }

        [Test]
        public void ExitStep_IsSheerAlongItsWholeWidth()
        {
            CanyonPath exit = _canyon.ExitPath;
            float arc = _canyon.ExitStepArc;
            Vector2 along = exit.TangentAt(arc);
            float minSlope = Mathf.Tan(65f * Mathf.Deg2Rad);
            for (float lateral = -_settings.Canyon.ExitHalfWidth; lateral <= _settings.Canyon.ExitHalfWidth;
                 lateral += 0.5f)
            {
                Vector2 p = exit.PointAt(arc) + exit.RightAt(arc) * lateral;
                const float probe = 0.1f;
                Vector2 ahead = p + along * probe;
                Vector2 behind = p - along * probe;
                float rise = _surface.SampleHeight(ahead.x, ahead.y) - _surface.SampleHeight(behind.x, behind.y);
                Assert.Greater(rise / (2f * probe), minSlope, $"exit step not sheer at lateral {lateral}");
            }

            Assert.That(_canyon.ExitFloor(arc + Canyon.SlabHalfDepth) - _canyon.ExitFloor(arc - Canyon.SlabHalfDepth),
                Is.InRange(1.5f, 2.5f + 1e-3f), "the step down is at most 2.5 m");
        }

        [Test]
        public void Gate_HasNoDrivablePathIn_ThatBypassesTheGap()
        {
            // Everything outside the canyon's area counts as reachable. A path may only use ground no steeper than
            // the rover's wall angle, and only step to a neighbour whose rise or drop is within that angle too.
            Rect bounds = _canyon.Bounds;
            Grid grid = Grid.Sample(_surface, bounds, GridStep);
            bool[] reached = Flood(grid, WallSlope, false, grid.EdgeCells());
            AssertGateHolds(grid, reached, "driving");
        }

        [Test]
        public void Gate_HasNoWayIn_ByClimbingTheRimAndDroppingOffIt()
        {
            // From the base pad, climbing what the rover can climb and dropping off any edge (however high): the
            // basin's rim must hold the rover in, and nothing but the gap may lead into the canyon.
            Rect bounds = _canyon.Bounds;
            Rect area = Rect.MinMaxRect(Mathf.Min(bounds.xMin, -ReachExtent), Mathf.Min(bounds.yMin, -ReachExtent),
                Mathf.Max(bounds.xMax, ReachExtent), Mathf.Max(bounds.yMax, ReachExtent));
            Grid grid = Grid.Sample(_surface, area, ReachStep);
            bool[] reached = Flood(grid, ClimbSlope, true, new[] { grid.Cell(0f, 0f) });
            foreach (int cell in grid.EdgeCells())
            {
                Assert.IsFalse(reached[cell], $"the rover can climb out of the basin at {grid.Position(cell)}");
            }

            AssertGateHolds(grid, reached, "climbing the rim and dropping in");
        }

        [Test]
        public void Ledge_GlowsInPlainSightOfTheBasePad()
        {
            Vector2 glow = _canyon.GlowPoint;
            Vector2 ledge = _canyon.LedgeCenter;
            var target = new Vector3(glow.x, _surface.SampleHeight(ledge.x, ledge.y) + _settings.Canyon.GlowHeight,
                glow.y);
            var eye = new Vector3(0f, _surface.SampleHeight(0f, 0f) + EyeHeight, 0f);
            float length = Vector3.Distance(eye, target);
            for (float d = 0f; d < length - SightTargetClearance; d += 0.5f)
            {
                Vector3 p = Vector3.Lerp(eye, target, d / length);
                Assert.Greater(p.y, _surface.SampleHeight(p.x, p.z) + SightMargin,
                    $"the line of sight to the ledge's glow is blocked at ({p.x:F1}, {p.z:F1})");
            }

            Assert.That(_canyon.LedgeArc, Is.InRange(_settings.Canyon.BendStart,
                _settings.Canyon.BendStart + _settings.Canyon.BendLength), "the ledge sits in the bend's outer wall");
        }

        [Test]
        public void Interior_IsConnected_FromTheLandingToTheTerminusAndTheExitTop()
        {
            CanyonPath main = _canyon.MainPath;
            for (float arc = _canyon.FarFaceArc + Canyon.SlabHalfDepth + 1f; arc <= main.EndArc; arc += 1f)
            {
                Vector2 p = main.PointAt(arc);
                Assert.IsTrue(_surface.IsDrivable(p.x, p.y), $"canyon centre line blocked at arc {arc}");
            }

            CanyonPath exit = _canyon.ExitPath;
            for (float arc = _canyon.ExitStepArc + Canyon.SlabHalfDepth + 1f; arc <= exit.EndArc; arc += 1f)
            {
                Vector2 p = exit.PointAt(arc);
                Assert.IsTrue(_surface.IsDrivable(p.x, p.y), $"exit shelf blocked at arc {arc}");
                Assert.LessOrEqual(Slope(p.x, p.y), ExitRampSlope, $"exit ramp too steep at arc {arc}");
            }
        }

        /// <summary>
        /// Asserts no canyon floor past the gate (beyond the far face, or on the exit's shelf) was reached, while the
        /// chasm's trough and the exit's foot were (they belong to the basin side).
        /// </summary>
        private void AssertGateHolds(Grid grid, bool[] reached, string how)
        {
            int inside = 0;
            bool troughReached = false;
            bool exitFootReached = false;
            for (int cell = 0; cell < reached.Length; cell++)
            {
                Vector2 p = grid.Position(cell);
                if (!_canyon.Bounds.Contains(p))
                {
                    continue;
                }

                SurfaceSample sample = _surface.Sample(p.x, p.y);
                if (sample.CanyonFloor < 0.99f)
                {
                    continue;
                }

                bool r = reached[cell];
                troughReached |= r && sample.Chasm > 0.99f;
                if (!_canyon.TryFloor(p.x, p.y, out bool onMain, out float arc, out float _))
                {
                    continue;
                }

                bool belowStep = arc > 0f && arc < _canyon.ExitStepArc - Canyon.SlabHalfDepth - 1f;
                exitFootReached |= r && !onMain && belowStep;
                bool beyondFace = onMain && arc > _canyon.FarFaceArc + Canyon.SlabHalfDepth;
                bool onShelf = !onMain && arc > _canyon.ExitStepArc + Canyon.SlabHalfDepth;
                if (beyondFace || onShelf)
                {
                    inside++;
                    Assert.IsFalse(r, $"{p} inside the canyon is reachable by {how} without crossing the gap");
                }
            }

            Assert.Greater(inside * grid.Step * grid.Step, MinInteriorArea, "the canyon interior should be large");
            Assert.IsTrue(troughReached, "the chasm trough must be reachable (and left) from the basin");
            Assert.IsTrue(exitFootReached, "the exit's foot must be open to the basin");
        }

        /// <summary>
        /// Flood fill over <paramref name="grid"/> from <paramref name="seeds"/>. Climbing (or, without
        /// <paramref name="drops"/>, descending) onto a neighbour needs ground there no steeper than
        /// <paramref name="slope"/> and a rise within that angle; with <paramref name="drops"/> any step down is
        /// allowed (driving off an edge).
        /// </summary>
        private static bool[] Flood(Grid grid, float slope, bool drops, IEnumerable<int> seeds)
        {
            float tan = Mathf.Tan(slope * Mathf.Deg2Rad);
            float[] heights = grid.Heights;
            int width = grid.Width;
            var standable = new bool[heights.Length];
            for (int j = 1; j < grid.Height - 1; j++)
            {
                for (int i = 1; i < width - 1; i++)
                {
                    int cell = j * width + i;
                    float gx = (heights[cell + 1] - heights[cell - 1]) / (2f * grid.Step);
                    float gz = (heights[cell + width] - heights[cell - width]) / (2f * grid.Step);
                    standable[cell] = gx * gx + gz * gz <= tan * tan;
                }
            }

            var reached = new bool[heights.Length];
            var queue = new Queue<int>();
            foreach (int seed in seeds)
            {
                Seed(seed, reached, queue);
            }

            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                int ci = cell % width;
                int cj = cell / width;
                for (int dj = -1; dj <= 1; dj++)
                {
                    for (int di = -1; di <= 1; di++)
                    {
                        int ni = ci + di;
                        int nj = cj + dj;
                        if ((di == 0 && dj == 0) || ni < 0 || nj < 0 || ni >= width || nj >= grid.Height)
                        {
                            continue;
                        }

                        int next = nj * width + ni;
                        float run = (di != 0 && dj != 0 ? 1.41421356f : 1f) * grid.Step;
                        float rise = heights[next] - heights[cell];
                        bool passable = (drops && rise <= 0f) || (standable[next] && Mathf.Abs(rise) <= tan * run);
                        if (!reached[next] && passable)
                        {
                            Seed(next, reached, queue);
                        }
                    }
                }
            }

            return reached;
        }

        private static void Seed(int cell, bool[] reached, Queue<int> queue)
        {
            if (!reached[cell])
            {
                reached[cell] = true;
                queue.Enqueue(cell);
            }
        }

        private float Slope(float x, float z)
        {
            return Mathf.Acos(Mathf.Clamp(_surface.SampleNormal(x, z).y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>Surface heights sampled on a regular XZ grid (row = z).</summary>
        private sealed class Grid
        {
            private readonly Rect _area;

            private Grid(Rect area, float step)
            {
                _area = area;
                Step = step;
                Width = Mathf.CeilToInt(area.width / step) + 1;
                Height = Mathf.CeilToInt(area.height / step) + 1;
                Heights = new float[Width * Height];
            }

            public float Step { get; }

            public int Width { get; }

            public int Height { get; }

            public float[] Heights { get; }

            public static Grid Sample(MoonSurface surface, Rect area, float step)
            {
                var grid = new Grid(area, step);
                for (int cell = 0; cell < grid.Heights.Length; cell++)
                {
                    Vector2 p = grid.Position(cell);
                    grid.Heights[cell] = surface.SampleHeight(p.x, p.y);
                }

                return grid;
            }

            public Vector2 Position(int cell)
            {
                return new Vector2(_area.xMin + cell % Width * Step, _area.yMin + cell / Width * Step);
            }

            public int Cell(float x, float z)
            {
                int i = Mathf.Clamp(Mathf.RoundToInt((x - _area.xMin) / Step), 0, Width - 1);
                int j = Mathf.Clamp(Mathf.RoundToInt((z - _area.yMin) / Step), 0, Height - 1);
                return j * Width + i;
            }

            public IEnumerable<int> EdgeCells()
            {
                for (int i = 0; i < Width; i++)
                {
                    yield return i;
                    yield return (Height - 1) * Width + i;
                }

                for (int j = 1; j < Height - 1; j++)
                {
                    yield return j * Width;
                    yield return j * Width + Width - 1;
                }
            }
        }
    }
}

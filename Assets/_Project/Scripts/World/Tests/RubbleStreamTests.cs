using System;
using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The near-field rubble streamed in round the camera (<see cref="RubbleStream"/>): a chunk gives the same stones
    /// on every visit and from a fresh planner; driving at 07's top speed, no frame plans more than its budget and
    /// every chunk within the fade range is streamed in; and a warm stream allocates nothing per frame.
    /// </summary>
    public sealed class RubbleStreamTests
    {
        private const float Frame = 1f / 60f;

        // A stretch of open plain west of the first drive, inside the floor, with rubble patches along it.
        private static readonly Vector2 DriveFrom = new Vector2(-150f, 60f);
        private static readonly Vector2 DriveTo = new Vector2(90f, 60f);

        // Frames the stream gets to fill round a new place, and the spot a chunk is compared on after a revisit.
        private const int WarmFrames = 400;
        private static readonly Vector2 Revisited = new Vector2(-30f, 70f);
        private static readonly Vector2 FarAway = new Vector2(-30f, -200f);

        // After the first second of a drive the stream has caught up with the camera.
        private const float CatchUpSeconds = 1f;
        private const int ClodVariants = 6;

        // How many chunks east of the revisited spot to look for one that holds rubble.
        private const int SearchChunks = 20;

        private MoonSurface _surface;
        private ScatterSettings _settings;
        private List<ScatterInstance> _plan;
        private List<RubbleApron> _aprons;
        private ScatterPlanner _planner;
        private WorldAnchors _anchors;
        private Mesh[] _clods;
        private Material _material;
        private GameObject _root;

        [OneTimeSetUp]
        public void SetUp()
        {
            var surfaceSettings = new SurfaceSettings();
            _surface = new MoonSurface(surfaceSettings, WorldSettings.DefaultSeed);
            _anchors = new WorldAnchors(_surface, surfaceSettings.Canyon, new RelaySettings());
            _settings = new ScatterSettings();
            _planner = new ScatterPlanner(_surface, _settings, _anchors);
            _plan = _planner.Plan();
            _aprons = new List<RubbleApron>();
            _planner.CollectRubbleAprons(_plan, _aprons);
            _clods = new Mesh[ClodVariants];
            for (int i = 0; i < _clods.Length; i++)
            {
                _clods[i] = ClodMesh.Create(i, new Color32(90, 90, 96, 255));
            }

            _material = new Material(Shader.Find("Sprites/Default"));
            _root = new GameObject("RubbleStreamTestRoot");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            foreach (Mesh clod in _clods)
            {
                UnityEngine.Object.DestroyImmediate(clod);
            }

            UnityEngine.Object.DestroyImmediate(_material);
            UnityEngine.Object.DestroyImmediate(_root);
        }

        [Test]
        public void AChunk_GivesTheSameStones_FromAFreshPlanner_AndOnEveryVisit()
        {
            using (RubbleStream stream = NewStream())
            {
                Vector2Int chunk = stream.ChunkOf(At(Revisited));
                List<ScatterInstance> first = PlanChunk(_planner, stream.AreaOf(chunk));
                for (int step = 1; first.Count == 0 && step <= SearchChunks; step++)
                {
                    chunk += Vector2Int.right;
                    first = PlanChunk(_planner, stream.AreaOf(chunk));
                }

                Rect area = stream.AreaOf(chunk);
                List<ScatterInstance> fresh = PlanChunk(new ScatterPlanner(_surface, _settings, _anchors), area);
                Assert.Greater(first.Count, 0, "the chunk holds rubble");
                Assert.AreEqual(first.Count, fresh.Count, "a fresh planner plans the same stones");
                for (int i = 0; i < first.Count; i++)
                {
                    Assert.AreEqual(first[i].Position, fresh[i].Position, $"stone {i} lies in the same place");
                    Assert.AreEqual(first[i].Size, fresh[i].Size, $"stone {i} is the same size");
                }

                Warm(stream, Revisited);
                Assert.IsTrue(stream.TryGetChunk(chunk, out int stones, out Bounds bounds), "the chunk streamed in");
                Assert.AreEqual(first.Count, stones, "the stream lays exactly the planned stones");

                Warm(stream, FarAway);
                Assert.IsFalse(stream.TryGetChunk(chunk, out _, out _), "far away, the chunk is put away");
                Warm(stream, Revisited);
                Assert.IsTrue(stream.TryGetChunk(chunk, out int again, out Bounds boundsAgain), "and comes back");
                Assert.AreEqual(stones, again, "with the same stones");
                Assert.AreEqual(bounds.center, boundsAgain.center, "in the same places");
                Assert.AreEqual(bounds.size, boundsAgain.size);
            }
        }

        [Test]
        public void DrivingAtTopSpeed_StaysInTheFrameBudget_AndKeepsTheVisibleRubbleStreamedIn()
        {
            using (RubbleStream stream = NewStream())
            {
                Warm(stream, DriveFrom);
                float distance = Vector2.Distance(DriveFrom, DriveTo);
                float seconds = distance / _settings.RubbleStreamDesignSpeed;
                float fadeEnd = _settings.RubbleFadeRange.y;
                var frameMs = new List<double>();
                var watch = new Stopwatch();
                int most = 0;
                for (float t = 0f; t <= seconds; t += Frame)
                {
                    Vector2 here = Vector2.Lerp(DriveFrom, DriveTo, t / seconds);
                    watch.Restart();
                    stream.Tick(At(here));
                    frameMs.Add(watch.Elapsed.TotalMilliseconds);
                    most = Mathf.Max(most, stream.LastFrameWork);
                    Assert.LessOrEqual(stream.LastFrameWork, _settings.RubbleCellsPerFrame,
                        $"{t:F2} s in: a frame plans no more than its budget");
                    if (t >= CatchUpSeconds)
                    {
                        AssertStreamedWithin(stream, here, fadeEnd, t);
                    }
                }

                frameMs.Sort();
                TestContext.WriteLine($"{frameMs.Count} frames at {_settings.RubbleStreamDesignSpeed} m/s: the most " +
                                      $"work in a frame {most} of {_settings.RubbleCellsPerFrame} cells; frame time " +
                                      $"median {frameMs[frameMs.Count / 2]:F3} ms, 99th percentile " +
                                      $"{frameMs[frameMs.Count * 99 / 100]:F3} ms, worst {frameMs[frameMs.Count - 1]:F3}" +
                                      $" ms; {stream.ResidentChunks} chunks, {stream.ResidentStones} stones streamed in");
            }
        }

        [Test]
        public void AWarmStream_AllocatesNothingPerFrame()
        {
            using (RubbleStream stream = NewStream())
            {
                Drive(stream, DriveFrom, DriveTo);
                Drive(stream, DriveTo, DriveFrom);
                long before = GC.GetAllocatedBytesForCurrentThread();
                Drive(stream, DriveFrom, DriveTo);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated, "a warm stream allocates nothing while it streams");
            }
        }

        private RubbleStream NewStream()
        {
            return new RubbleStream(_planner, _aprons, _settings, _clods, _material, _surface.DrivableRadius,
                _root.transform, HideFlags.None);
        }

        private void Warm(RubbleStream stream, Vector2 place)
        {
            for (int i = 0; i < WarmFrames; i++)
            {
                stream.Tick(At(place));
            }
        }

        private void Drive(RubbleStream stream, Vector2 from, Vector2 to)
        {
            float seconds = Vector2.Distance(from, to) / _settings.RubbleStreamDesignSpeed;
            for (float t = 0f; t <= seconds; t += Frame)
            {
                stream.Tick(At(Vector2.Lerp(from, to, t / seconds)));
            }
        }

        private static void AssertStreamedWithin(RubbleStream stream, Vector2 here, float radius, float t)
        {
            Vector2Int middle = stream.ChunkOf(new Vector3(here.x, 0f, here.y));
            int reach = Mathf.CeilToInt(radius / stream.AreaOf(middle).width) + 1;
            for (int z = -reach; z <= reach; z++)
            {
                for (int x = -reach; x <= reach; x++)
                {
                    var chunk = new Vector2Int(middle.x + x, middle.y + z);
                    Rect area = stream.AreaOf(chunk);
                    var nearest = new Vector2(Mathf.Clamp(here.x, area.xMin, area.xMax),
                        Mathf.Clamp(here.y, area.yMin, area.yMax));
                    if (Vector2.Distance(nearest, here) < radius)
                    {
                        Assert.IsTrue(stream.TryGetChunk(chunk, out _, out _),
                            $"{t:F2} s in: chunk {chunk}, within {radius} m of the camera, is streamed in");
                    }
                }
            }
        }

        /// <summary>A chunk's stones as the stream plans them: its grid cells and the aprons that reach into it.</summary>
        private List<ScatterInstance> PlanChunk(ScatterPlanner planner, Rect area)
        {
            var stones = new List<ScatterInstance>();
            float cell = _settings.RubbleSpacing;
            for (int j = Mathf.FloorToInt(area.yMin / cell); j <= Mathf.FloorToInt(area.yMax / cell); j++)
            {
                for (int i = Mathf.FloorToInt(area.xMin / cell); i <= Mathf.FloorToInt(area.xMax / cell); i++)
                {
                    planner.PlanRubbleCell(i, j, stones);
                }
            }

            foreach (RubbleApron apron in _aprons)
            {
                planner.PlanRubbleApron(apron, area, stones);
            }

            stones.RemoveAll(stone => !area.Contains(new Vector2(stone.Position.x, stone.Position.z)));
            return stones;
        }

        private Vector3 At(Vector2 place)
        {
            return new Vector3(place.x, _surface.SampleHeight(place.x, place.y) + 2f, place.y);
        }
    }
}

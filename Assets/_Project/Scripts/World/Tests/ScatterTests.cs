using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.World.Editor;

namespace MoonProject.World.Tests
{
    public sealed class ScatterTests
    {
        private MoonSurface _surface;
        private ScatterSettings _settings;
        private ScatterPlanner _planner;
        private List<ScatterInstance> _plan;
        private double _planMs;

        [OneTimeSetUp]
        public void PlanOnce()
        {
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            _settings = new ScatterSettings();
            _planner = new ScatterPlanner(_surface, _settings, Anchors());
            var watch = Stopwatch.StartNew();
            _plan = _planner.Plan();
            _planMs = watch.Elapsed.TotalMilliseconds;
        }

        [Test]
        public void DefaultSettings_AreValid()
        {
            Assert.IsNull(_settings.Validate());
        }

        [Test]
        public void PoissonSampler_KeepsItsMinimumDistance_InsideTheDisc()
        {
            List<Vector2> points = PoissonDiskSampler.SampleDisc(60f, 4f, 11u);
            Assert.Greater(points.Count, 400, "the disc should be filled");
            for (int i = 0; i < points.Count; i++)
            {
                Assert.LessOrEqual(points[i].magnitude, 60f);
                for (int j = i + 1; j < points.Count; j++)
                {
                    Assert.GreaterOrEqual(Vector2.Distance(points[i], points[j]), 4f - 1e-4f);
                }
            }
        }

        [Test]
        public void Plan_IsDeterministic()
        {
            List<ScatterInstance> again = new ScatterPlanner(_surface, _settings, Anchors()).Plan();
            Assert.AreEqual(_plan.Count, again.Count);
            for (int i = 0; i < _plan.Count; i++)
            {
                Assert.AreEqual(_plan[i].Position, again[i].Position);
                Assert.AreEqual(_plan[i].Size, again[i].Size);
                Assert.AreEqual(_plan[i].Variant, again[i].Variant);
            }
        }

        [Test]
        public void Counts_StayWithinTheRenderingBudget()
        {
            int pebbles = Count(ScatterKind.Pebble);
            int boulders = Count(ScatterKind.Boulder);
            TestContext.WriteLine($"Scatter plan: {pebbles} pebbles, {boulders} boulders in {_planMs:0} ms");
            Assert.That(pebbles, Is.InRange(2000, 12000));
            Assert.That(boulders, Is.InRange(80, 600));
        }

        [Test]
        public void Rocks_SitOnTheSurface_InsideTheExtent_WithinTheirSizes()
        {
            foreach (ScatterInstance rock in _plan)
            {
                var flat = new Vector2(rock.Position.x, rock.Position.z);
                Assert.LessOrEqual(flat.magnitude, _settings.ExtentRadius);
                Assert.AreEqual(_surface.SampleHeight(flat.x, flat.y), rock.Position.y, 1e-4f);
                Vector2 range = rock.Kind == ScatterKind.Boulder ? _settings.BoulderSize : _settings.PebbleSize;
                Assert.That(rock.Size, Is.InRange(range.x, range.y));
                Assert.GreaterOrEqual(rock.Variant, 0);
            }
        }

        [Test]
        public void BasePad_IsClearOfRocks()
        {
            float clear = _surface.PadRadius + _settings.PadClearance;
            foreach (ScatterInstance rock in _plan)
            {
                Assert.Greater(new Vector2(rock.Position.x, rock.Position.z).magnitude, clear);
            }
        }

        [Test]
        public void Boulders_KeepLanesFeaturesCratersAndSteepGroundClear()
        {
            float maxSlopeCos = Mathf.Cos(_settings.BoulderMaxSlope * Mathf.Deg2Rad);
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind != ScatterKind.Boulder)
                {
                    continue;
                }

                var flat = new Vector2(rock.Position.x, rock.Position.z);
                Assert.GreaterOrEqual(_planner.DistanceToLanes(flat), _settings.LaneHalfWidth,
                    $"boulder in a lane at {flat}");
                Assert.AreEqual(0f, _surface.Sample(flat.x, flat.y).CraterBowl, $"boulder inside a crater at {flat}");
                Assert.GreaterOrEqual(rock.Normal.y, maxSlopeCos, $"boulder on a steep face at {flat}");
                foreach (Ramp ramp in _surface.Ramps)
                {
                    Assert.Greater(Vector2.Distance(flat, ramp.Crest), ramp.BoundingRadius,
                        $"boulder on a ramp at {flat}");
                }
            }
        }

        [Test]
        public void Canyon_KeepsItsFloorsClearOfBoulders_AndItsDrivingLineClearOfPebbles()
        {
            Canyon canyon = _surface.Canyon;
            foreach (ScatterInstance rock in _plan)
            {
                SurfaceSample sample = _surface.Sample(rock.Position.x, rock.Position.z);
                if (rock.Kind == ScatterKind.Boulder)
                {
                    Assert.AreEqual(0f, sample.CanyonFloor, $"boulder on a canyon floor at {rock.Position}");
                    continue;
                }

                Assert.AreEqual(0f, sample.Chasm, $"pebble in the chasm at {rock.Position}");
                if (canyon.TryFloor(rock.Position.x, rock.Position.z, out bool _, out float _, out float centre))
                {
                    Assert.GreaterOrEqual(centre, _settings.CanyonPebbleClear,
                        $"pebble on the canyon's driving line at {rock.Position}");
                }
            }
        }

        [Test]
        public void Kestrel_KeepsItsScorchedCraterAndFurrowClearOfRocks()
        {
            // M3-13: the furrow is kept clear for driving; no rock competes with it or blocks its driving line.
            foreach (ScatterInstance rock in _plan)
            {
                Assert.LessOrEqual(_surface.Sample(rock.Position.x, rock.Position.z).Scorch, _settings.ScorchClear,
                    $"{rock.Kind} on Kestrel-3's scorched ground at {rock.Position}");
            }

            KestrelImpact kestrel = _surface.Kestrel;
            float furrow = Vector2.Distance(kestrel.Center, kestrel.TrailEnd);
            for (float d = 0f; d <= furrow; d += 1f)
            {
                Vector2 p = kestrel.Center - kestrel.Fall * d;
                Assert.Greater(_surface.Sample(p.x, p.y).Scorch, _settings.ScorchClear,
                    $"the furrow's driving line at {p} is not kept clear");
            }
        }

        [Test]
        public void SalvageSitesOnTheFloor_EndADrivingLane()
        {
            WorldAnchors anchors = Anchors();
            foreach (string name in SalvageSiteTests.Sites)
            {
                Assert.IsTrue(anchors.TryGet(WorldAnchorIds.SitePrefix + name, out WorldAnchor site));
                var at = new Vector2(site.Position.x, site.Position.z);
                bool onTheFloor = !_surface.Canyon.Bounds.Contains(at);
                Assert.AreEqual(onTheFloor, _planner.DistanceToLanes(at) < 1e-3f,
                    onTheFloor ? $"no lane leads to {site.Id}" : $"a lane runs into the canyon to {site.Id}");
            }
        }

        [Test]
        public void SameClassRocks_KeepTheirPoissonSpacing()
        {
            AssertSpacing(ScatterKind.Boulder, _settings.BoulderSpacing);
            AssertSpacing(ScatterKind.Pebble, _settings.PebbleSpacing);
        }

        [Test]
        public void Boulders_GatherAtTheFootOfTheRim_AndOnCraterRims()
        {
            int talus = 0;
            int ejecta = 0;
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind != ScatterKind.Boulder)
                {
                    continue;
                }

                if (_surface.FloorEdgeDistance(rock.Position.x, rock.Position.z) < 0f)
                {
                    talus++;
                }

                if (_surface.Sample(rock.Position.x, rock.Position.z).CraterRim > 0.3f)
                {
                    ejecta++;
                }
            }

            Assert.Greater(talus, 40, "the foot of the rim should carry a talus of boulders");
            Assert.Greater(ejecta, 10, "raised crater rims should carry ejecta boulders");
        }

        [Test]
        public void CobbleColliderCutoff_GivesMediumCobblesSphereColliders_WhileTinyPebblesStayNonColliding()
        {
            Assert.AreEqual(0.48f, _settings.CobbleColliderMinSize, 1e-4f,
                "cutoff is 0.48 m (~0.69x 07's 0.70 m wheel diameter; exposed height >= 0.30 m vs 0.35 m wheel radius)");
            Assert.Greater(_settings.CobbleColliderMinSize, _settings.PebbleSize.y,
                "open-floor Poisson pebbles stay below the cobble collider cutoff");
            Assert.Less(_settings.CobbleColliderMinSize, _settings.BoulderSize.x,
                "cobble collider cutoff sits below the boulder class minimum");

            var parent = new GameObject("ScatterTestRoot");
            Mesh cubeMesh = CreateUnitRockMesh();
            var mat = new Material(Shader.Find("Sprites/Default"));
            var builder = new ScatterBuilder();
            try
            {
                var instances = new List<ScatterInstance>
                {
                    new ScatterInstance(ScatterKind.Pebble, new Vector3(0f, 0f, 0f), Vector3.up, 0f, 0.30f, 0),
                    new ScatterInstance(ScatterKind.Pebble, new Vector3(4f, 0f, 0f), Vector3.up, 0f, 0.62f, 0),
                    new ScatterInstance(ScatterKind.Boulder, new Vector3(8f, 0f, 0f), Vector3.up, 0f, 1.25f, 0),
                };

                ScatterBuildReport report = builder.Build(parent.transform, instances, _settings, 64f,
                    new[] { cubeMesh }, new[] { cubeMesh }, mat, HideFlags.None);
                Assert.AreEqual(2, report.Pebbles);
                Assert.AreEqual(1, report.SolidCobbles, "medium cobble >= 0.48 m gets a SphereCollider");
                Assert.AreEqual(1, report.Boulders);

                SphereCollider[] spheres = parent.GetComponentsInChildren<SphereCollider>();
                MeshCollider[] meshes = parent.GetComponentsInChildren<MeshCollider>();
                Assert.AreEqual(1, spheres.Length, "exactly one SphereCollider for the medium cobble");
                Assert.AreEqual(Layers.Prop, spheres[0].gameObject.layer);
                Assert.AreEqual(1, meshes.Length, "exactly one convex MeshCollider for the boulder");
                Assert.AreEqual(Layers.Prop, meshes[0].gameObject.layer);
            }
            finally
            {
                builder.DestroyMeshes();
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(cubeMesh);
                UnityEngine.Object.DestroyImmediate(mat);
            }
        }

        [Test]
        public void RuinColliderBuilder_BuildsSolidWallBoxes_WhileKeepingPortalAndCourtyardOpen()
        {
            var ruin = new GameObject("KenjiWorkshop_Test");
            Mesh shellMesh = CreateHollowWorkshopShellMesh();
            try
            {
                var shellChild = new GameObject("QuonsetShell");
                shellChild.transform.SetParent(ruin.transform, false);
                shellChild.AddComponent<MeshFilter>().sharedMesh = shellMesh;

                int count = RuinColliderBuilder.Fit(ruin);
                Assert.Greater(count, 4, "hollow shell must build compound BoxColliders for walls, jambs and arch");

                BoxCollider[] boxes = ruin.GetComponentsInChildren<BoxCollider>();
                Assert.AreEqual(count, boxes.Length);
                foreach (BoxCollider box in boxes)
                {
                    Assert.AreEqual(Layers.Prop, box.gameObject.layer);
                }

                // Doorway portal (x = 0, y = 0.8, z = -2.5) and central courtyard (x = 0, y = 0.8, z = 0) must not
                // lie inside any BoxCollider, while left/right/back walls must be enclosed by BoxColliders.
                Assert.IsFalse(ContainsPoint(boxes, new Vector3(0f, 0.8f, -2.5f)),
                    "front portal opening must stay open for 07");
                Assert.IsFalse(ContainsPoint(boxes, new Vector3(0f, 0.8f, 0f)),
                    "interior workshop courtyard must stay open for 07");
                Assert.IsTrue(ContainsPoint(boxes, new Vector3(-1.95f, 0.8f, 0f)),
                    "left wall must be solid");
                Assert.IsTrue(ContainsPoint(boxes, new Vector3(1.95f, 0.8f, 0f)),
                    "right wall must be solid");
                Assert.IsTrue(ContainsPoint(boxes, new Vector3(0f, 0.8f, 2.45f)),
                    "back wall must be solid");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(ruin);
                UnityEngine.Object.DestroyImmediate(shellMesh);
            }
        }

        private static bool ContainsPoint(BoxCollider[] boxes, Vector3 worldPoint)
        {
            for (int i = 0; i < boxes.Length; i++)
            {
                Vector3 local = boxes[i].transform.InverseTransformPoint(worldPoint) - boxes[i].center;
                Vector3 half = boxes[i].size * 0.5f;
                if (Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z)
                {
                    return true;
                }
            }

            return false;
        }

        private static Mesh CreateUnitRockMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(-0.35f, 0.7f, -0.35f),
                new Vector3(0.35f, 0.7f, -0.35f),
                new Vector3(0.35f, 0.7f, 0.35f),
                new Vector3(-0.35f, 0.7f, 0.35f),
            };
            mesh.triangles = new[]
            {
                0, 2, 1, 0, 3, 2,
                4, 5, 6, 4, 6, 7,
                0, 1, 5, 0, 5, 4,
                1, 2, 6, 1, 6, 5,
                2, 3, 7, 2, 7, 6,
                3, 0, 4, 3, 4, 7,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh CreateHollowWorkshopShellMesh()
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();

            // Left wall (x in [-2.1, -1.8], y in [0.2, 1.9], z in [-2.6, 2.6])
            AddBoxVerts(verts, tris, new Vector3(-2.1f, 0.2f, -2.6f), new Vector3(-1.8f, 1.9f, 2.6f));
            // Right wall (x in [1.8, 2.1], y in [0.2, 1.9], z in [-2.6, 2.6])
            AddBoxVerts(verts, tris, new Vector3(1.8f, 0.2f, -2.6f), new Vector3(2.1f, 1.9f, 2.6f));
            // Back wall (x in [-2.1, 2.1], y in [0.2, 1.9], z in [2.3, 2.6])
            AddBoxVerts(verts, tris, new Vector3(-2.1f, 0.2f, 2.3f), new Vector3(2.1f, 1.9f, 2.6f));
            // Front left jamb & right jamb leaving central portal x in [-1.2, 1.2] open
            AddBoxVerts(verts, tris, new Vector3(-2.1f, 0.2f, -2.6f), new Vector3(-1.3f, 1.9f, -2.3f));
            AddBoxVerts(verts, tris, new Vector3(1.3f, 0.2f, -2.6f), new Vector3(2.1f, 1.9f, -2.3f));
            // Overhead arch roof (y in [2.05, 2.75])
            AddBoxVerts(verts, tris, new Vector3(-2.1f, 2.05f, -2.6f), new Vector3(2.1f, 2.75f, 2.6f));

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddBoxVerts(List<Vector3> verts, List<int> tris, Vector3 min, Vector3 max)
        {
            int b = verts.Count;
            verts.Add(new Vector3(min.x, min.y, min.z));
            verts.Add(new Vector3(max.x, min.y, min.z));
            verts.Add(new Vector3(max.x, max.y, min.z));
            verts.Add(new Vector3(min.x, max.y, min.z));
            verts.Add(new Vector3(min.x, min.y, max.z));
            verts.Add(new Vector3(max.x, min.y, max.z));
            verts.Add(new Vector3(max.x, max.y, max.z));
            verts.Add(new Vector3(min.x, max.y, max.z));
            verts.Add(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, min.z));
            verts.Add(new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, max.z));
            tris.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2, b + 4, b + 5, b + 6, b + 4, b + 6, b + 7, b + 8, b + 9, b + 1 });
        }

        private void AssertSpacing(ScatterKind kind, float spacing)
        {
            var points = new List<Vector2>();
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind == kind)
                {
                    points.Add(new Vector2(rock.Position.x, rock.Position.z));
                }
            }

            // Cell hashing keeps the all-pairs check linear: only neighbouring cells can be closer than the spacing.
            var cells = new Dictionary<Vector2Int, List<Vector2>>();
            foreach (Vector2 point in points)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(point.x / spacing), Mathf.FloorToInt(point.y / spacing));
                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (cells.TryGetValue(cell + new Vector2Int(dx, dz), out List<Vector2> near))
                        {
                            foreach (Vector2 other in near)
                            {
                                Assert.GreaterOrEqual(Vector2.Distance(point, other), spacing - 1e-3f, kind.ToString());
                            }
                        }
                    }
                }

                if (!cells.TryGetValue(cell, out List<Vector2> list))
                {
                    list = new List<Vector2>();
                    cells.Add(cell, list);
                }

                list.Add(point);
            }
        }

        private int Count(ScatterKind kind)
        {
            int count = 0;
            foreach (ScatterInstance rock in _plan)
            {
                if (rock.Kind == kind)
                {
                    count++;
                }
            }

            return count;
        }

        private WorldAnchors Anchors()
        {
            return new WorldAnchors(_surface, new SurfaceSettings().Canyon, new RelaySettings());
        }
    }
}

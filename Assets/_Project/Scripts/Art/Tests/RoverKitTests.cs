using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// 07's visible kit and friends' gifts (M3-11, VISION ruling 11): the contract nodes and sockets, dark glow
    /// renderers lit through the glow-off material, gifts hidden until earned, every kit piece reading in silhouette
    /// from the chase camera and at 30 m without touching the wheels or the face, and repeatable builds.
    /// </summary>
    public sealed class RoverKitTests
    {
        // The default chase camera (RoverCameraTuning): 7.5 m out, 16 degrees up, looking at 1.1 m; seen from the
        // rear three-quarter on either side.
        private const float ChaseDistance = 7.5f;
        private const float ChasePitch = 16f;
        private const float ChaseYaw = 35f;
        private const float Broadside = 90f;
        private const float FarDistance = 30f;
        private const float Fov = 60f;
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;
        private static readonly Vector3 ChaseTarget = new Vector3(0f, 1.1f, 0f);

        // Pixels a kit piece must add to 07's outline at 1080p: an easy read up close, still a shape at 30 m.
        private const int MinChasePixels = 400;
        private const int MinFarPixels = 12;

        private const float WheelClearance = 0.06f;
        private const float StripePaint = 0.012f;
        private const float FaceClearance = 0.08f;

        private static readonly string[] Gifts = { "Body/Decal07Fresh", "SolarWing/CellFilled", "Antenna/Pennant" };

        private ModelNode _rover;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _rover = RoverModelBuilder.CreateModel();
        }

        [Test]
        public void Kit_HasTheContractNodes()
        {
            ModelNode bar = RoverKitBuilder.CreateLampBar();
            Assert.AreEqual("Kit_LampBar", bar.Name);
            CollectionAssert.AreEqual(new[] { "Lamp_0", "Lamp_1", "Lamp_2" }, bar.Children.Select(c => c.Name));

            ModelNode drum = RoverKitBuilder.CreateCapacitorDrum();
            Assert.AreEqual("Kit_CapacitorDrum", drum.Name);
            CollectionAssert.AreEqual(new[] { "Glow" }, drum.Children.Select(c => c.Name));

            ModelNode rack = RoverKitBuilder.CreateCargoRack();
            Assert.AreEqual("Kit_CargoRack", rack.Name);
            ModelNode seat = rack.Children.Single();
            Assert.AreEqual("RelicSeat", seat.Name);
            Assert.IsNull(seat.Mesh, "RelicSeat is an empty");
            Assert.AreEqual(Quaternion.identity, seat.LocalRotation, "RelicSeat's +Y is up");

            foreach (ModelNode kit in new[] { bar, drum, rack })
            {
                Assert.AreEqual(Vector3.zero, kit.LocalPosition, $"{kit.Name} parents to its socket with identity");
                Assert.AreEqual(Quaternion.identity, kit.LocalRotation, kit.Name);
                foreach (ModelNode node in Nodes(kit).Where(n => n.Mesh != null))
                {
                    MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                }
            }
        }

        [Test]
        public void DrumSockets_SitOnTheFlanks_PointingOut()
        {
            for (int side = -1; side <= 1; side += 2)
            {
                ModelNode socket = _rover.GetDescendant(side < 0 ? "DrumSocket_L" : "DrumSocket_R");
                Assert.IsNull(socket.Mesh);
                Assert.AreEqual(side, Mathf.Sign(socket.LocalPosition.x), socket.Name);
                Assert.Greater(side * (socket.LocalRotation * Vector3.right).x, 0.999f, $"{socket.Name} +X outward");
                Assert.Greater((socket.LocalRotation * Vector3.up).y, 0.999f, $"{socket.Name} +Y up");
            }

            Vector3 rack = RoverKitBuilder.CreateCargoRack().GetDescendant("RelicSeat").LocalPosition;
            Assert.Less(rack.z, 0f, "the relic rides behind the cargo socket");
        }

        [Test]
        public void GlowRenderers_AreDarkUntilLit_LampsWarm_DrumCyanLikeTheCoils()
        {
            ModelNode bar = RoverKitBuilder.CreateLampBar();
            ModelNode drum = RoverKitBuilder.CreateCapacitorDrum();
            PaletteSwatch coil = MeshChecks.Swatches(RoverModelBuilder.CreateHoverCoils().GetDescendant("Glow_FL")
                .Mesh.Geometry).Single();
            foreach (ModelNode node in Nodes(bar).Concat(Nodes(drum)).Concat(Nodes(RoverKitBuilder.CreateCargoRack()))
                .Where(n => n.Mesh != null))
            {
                List<PaletteSwatch> swatches = MeshChecks.Swatches(node.Mesh.Geometry);
                bool glow = node.Name.StartsWith("Lamp_") || node.Name == "Glow";
                Assert.AreEqual(glow ? ModelMaterial.PaletteGlowOff : ModelMaterial.Palette, node.Material, node.Name);
                Assert.IsTrue(glow ? swatches.All(Palette.IsEmissive) : !swatches.Any(Palette.IsEmissive),
                    $"{node.Name}: only the glow renderers glow");
                Assert.LessOrEqual(swatches.Count, 4, $"{node.Name} keeps to four swatches");
            }

            foreach (ModelNode lamp in bar.Children)
            {
                Color warm = Palette.GetGlow(MeshChecks.Swatches(lamp.Mesh.Geometry).Single());
                Assert.Greater(warm.r, Palette.BloomThreshold, "lit at 1 the lamp blooms");
                Assert.Greater(warm.r, warm.g, "warm");
                Assert.Greater(warm.g, warm.b, "warm");
            }

            Assert.AreEqual(coil, MeshChecks.Swatches(drum.GetDescendant("Glow").Mesh.Geometry).Single(),
                "the drum band glows the coils' cyan");
        }

        [Test]
        public void Gifts_AreHiddenUntilEarned_EverythingElseShows()
        {
            foreach (ModelNode node in Nodes(_rover))
            {
                bool gift = node.Name == "Decal07Fresh" || node.Name == "CellFilled" || node.Name == "Pennant";
                Assert.AreEqual(!gift, node.Active, node.Name);
                if (gift)
                {
                    MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                }
            }

            foreach (ModelNode kit in new[]
            {
                RoverKitBuilder.CreateLampBar(), RoverKitBuilder.CreateCapacitorDrum(),
                    RoverKitBuilder.CreateCargoRack(),
            })
            {
                Assert.IsTrue(Nodes(kit).All(n => n.Active), $"{kit.Name} shows whole once fitted");
            }
        }

        [Test]
        public void FreshSerial_BuriesTheFadedOne_AndCellFillsTheGap_AndPennantClipsToTheWhip()
        {
            LowPolyMeshBuilder body = _rover.GetDescendant("Body").Mesh.Geometry;
            LowPolyMeshBuilder fresh = _rover.GetDescendant("Decal07Fresh").Mesh.Geometry;
            var patches = new List<Bounds>();
            foreach (System.Func<Vector3, bool> region in new System.Func<Vector3, bool>[]
            {
                p => p.x > 0.3f, p => p.x < -0.3f, p => p.z < -0.5f,
            })
            {
                patches.Add(BoundsOf(fresh, PaletteSwatch.WarmAccent, region));
            }

            int faded = 0;
            for (int t = 0; t < body.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(body, t) != PaletteSwatch.FadedPaint)
                {
                    continue;
                }

                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = body.Positions[t * 3 + k];
                    faded++;
                    Assert.IsTrue(patches.Any(patch => patch.Contains(p)), $"faded serial point {p} shows through");
                }
            }

            Assert.Greater(faded, 0, "07 starts with a faded serial");
            CollectionAssert.Contains(MeshChecks.Swatches(fresh), PaletteSwatch.Cream, "the fresh numerals are crisp");

            ModelNode cell = _rover.GetDescendant("CellFilled");
            LowPolyMeshBuilder wing = _rover.GetDescendant("SolarWing").Mesh.Geometry;
            for (int t = 0; t < wing.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(wing, t) == PaletteSwatch.SkyHorizon)
                {
                    Vector3 p = wing.Positions[t * 3];
                    Assert.Greater(new Vector2(p.x - cell.LocalPosition.x, p.z - cell.LocalPosition.z).magnitude,
                        0.08f, "the filled cell sits in the gap, not over a cell");
                }
            }

            Assert.IsTrue(wing.Bounds.Contains(cell.LocalPosition), "the gap is inside the wing");
            Vector3 clip = _rover.GetDescendant("Pennant").LocalPosition;
            Assert.Less(DistanceToSurface(clip, _rover.GetDescendant("Antenna").Mesh.Geometry), 0.012f,
                "the pennant clips onto the whip");
        }

        [Test]
        public void Kit_KeepsClearOfTheWheelsAndTheFace()
        {
            Bounds bar = Mounted(RoverKitBuilder.CreateLampBar(), "HeadlampSocket");
            Bounds rack = Mounted(RoverKitBuilder.CreateCargoRack(), "CargoSocket");
            Bounds head = Mounted(_rover.GetDescendant("Head"), "Neck/Head");
            AssertApart(bar, head, FaceClearance, "lamp bar and head");
            foreach (string side in new[] { "L", "R" })
            {
                Bounds drum = Mounted(RoverKitBuilder.CreateCapacitorDrum(), "DrumSocket_" + side);
                Bounds middle = Mounted(_rover.GetDescendant("Wheel_M" + side), "Wheel_M" + side);
                Assert.GreaterOrEqual(drum.min.y - (middle.max.y + RoverModelBuilder.MiddleWheelTravel),
                    WheelClearance * 0.5f, $"drum {side} clears the middle wheel at the top of its travel");
                foreach (string row in new[] { "F", "M", "R" })
                {
                    Bounds wheel = Mounted(_rover.GetDescendant($"Wheel_{row}{side}"), $"Wheel_{row}{side}");
                    AssertApart(drum, wheel, WheelClearance, $"drum {side} and wheel {row}{side}");
                    AssertApart(bar, wheel, WheelClearance, $"lamp bar and wheel {row}{side}");
                    AssertApart(rack, wheel, WheelClearance, $"rack and wheel {row}{side}");
                }
            }
        }

        [Test]
        public void EveryKitPiece_ChangesTheSilhouette_FromTheChaseCamera()
        {
            AssertEveryPieceAdds(ChaseDistance, new[] { -ChaseYaw, ChaseYaw }, MinChasePixels);
        }

        [Test]
        public void EveryKitPiece_StillShapesTheOutline_At30Metres()
        {
            // From afar 07 is seen from any side: the rear three-quarter views and broadside.
            AssertEveryPieceAdds(FarDistance, new[] { -ChaseYaw, ChaseYaw, -Broadside, Broadside }, MinFarPixels);
        }

        [Test]
        public void Rover_AndKit_BuildIdentically()
        {
            MeshChecks.AssertSameModel(_rover, RoverModelBuilder.CreateModel());
            MeshChecks.AssertSameModel(RoverKitBuilder.CreateLampBar(), RoverKitBuilder.CreateLampBar());
            MeshChecks.AssertSameModel(RoverKitBuilder.CreateCapacitorDrum(), RoverKitBuilder.CreateCapacitorDrum());
            MeshChecks.AssertSameModel(RoverKitBuilder.CreateCargoRack(), RoverKitBuilder.CreateCargoRack());
        }

        private void AssertEveryPieceAdds(float distance, float[] yaws, int minimum)
        {
            var bare = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(_rover, Matrix4x4.identity, bare);
            var pieces = new Dictionary<string, List<Vector3>>
            {
                { "Kit_LampBar", Kit(RoverKitBuilder.CreateLampBar(), "HeadlampSocket") },
                { "Kit_CapacitorDrum", Kit(RoverKitBuilder.CreateCapacitorDrum(), "DrumSocket_L", "DrumSocket_R") },
                { "Kit_CargoRack", Kit(RoverKitBuilder.CreateCargoRack(), "CargoSocket") },
            };

            foreach (KeyValuePair<string, List<Vector3>> piece in pieces)
            {
                int best = 0;
                foreach (float yaw in yaws)
                {
                    SilhouetteCamera camera = SilhouetteCamera.Orbit(ChaseTarget, distance, ChasePitch, yaw, Fov,
                        ScreenWidth, ScreenHeight);
                    bool[] outline = camera.NewMask();
                    camera.Rasterize(bare, outline);
                    bool[] kitted = camera.NewMask();
                    camera.Rasterize(bare, kitted);
                    camera.Rasterize(piece.Value, kitted);
                    int added = SilhouetteCamera.Added(kitted, outline);
                    TestContext.WriteLine($"{piece.Key} adds {added} px to 07's outline at {distance} m, yaw {yaw}");
                    best = Mathf.Max(best, added);
                }

                Assert.GreaterOrEqual(best, minimum, $"{piece.Key} must read in silhouette at {distance} m");
            }
        }

        private List<Vector3> Kit(ModelNode kit, params string[] sockets)
        {
            var triangles = new List<Vector3>();
            foreach (string socket in sockets)
            {
                SilhouetteCamera.CollectTriangles(kit, SilhouetteCamera.WorldOf(_rover, socket), triangles);
            }

            return triangles;
        }

        /// <summary>
        /// World bounds of <paramref name="node"/>'s visible meshes placed at the rover node at path.
        /// </summary>
        private Bounds Mounted(ModelNode node, string path)
        {
            Matrix4x4 parent = SilhouetteCamera.WorldOf(_rover, path) * node.LocalMatrix.inverse;
            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(node, parent, triangles);
            var bounds = new Bounds(triangles[0], Vector3.zero);
            foreach (Vector3 p in triangles)
            {
                bounds.Encapsulate(p);
            }

            return bounds;
        }

        private static void AssertApart(Bounds a, Bounds b, float gap, string what)
        {
            Vector3 separation = Vector3.Max(a.min - b.max, b.min - a.max);
            float apart = Mathf.Max(separation.x, Mathf.Max(separation.y, separation.z));
            Assert.GreaterOrEqual(apart, gap, $"{what} are only {apart:F3} m apart");
        }

        private static Bounds BoundsOf(LowPolyMeshBuilder geometry, PaletteSwatch swatch,
            System.Func<Vector3, bool> region)
        {
            Bounds? bounds = null;
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(geometry, t) != swatch || !region(geometry.Positions[t * 3]))
                {
                    continue;
                }

                for (int k = 0; k < 3; k++)
                {
                    Vector3 p = geometry.Positions[t * 3 + k];
                    if (bounds.HasValue)
                    {
                        Bounds grown = bounds.Value;
                        grown.Encapsulate(p);
                        bounds = grown;
                    }
                    else
                    {
                        bounds = new Bounds(p, Vector3.zero);
                    }
                }
            }

            Assert.IsTrue(bounds.HasValue, "a fresh patch is missing");
            // The old numerals sit half sunk in the stripe; whatever lies inside the stripe paint is hidden anyway.
            Bounds result = bounds.Value;
            result.Expand(2f * StripePaint);
            return result;
        }

        /// <summary>
        /// Distance from <paramref name="point"/> to the nearest triangle of <paramref name="geometry"/>.
        /// </summary>
        private static float DistanceToSurface(Vector3 point, LowPolyMeshBuilder geometry)
        {
            float best = float.MaxValue;
            for (int v = 0; v < geometry.VertexCount; v += 3)
            {
                Vector3 closest = ClosestOnTriangle(point, geometry.Positions[v], geometry.Positions[v + 1],
                    geometry.Positions[v + 2]);
                best = Mathf.Min(best, Vector3.Distance(point, closest));
            }

            return best;
        }

        /// <summary>
        /// Closest point to <paramref name="p"/> on triangle abc (Ericson, Real-Time Collision Detection).
        /// </summary>
        private static Vector3 ClosestOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            Vector3 ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0f && d2 <= 0f)
            {
                return a;
            }

            Vector3 bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0f && d4 <= d3)
            {
                return b;
            }

            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0f && d1 >= 0f && d3 <= 0f)
            {
                return a + ab * (d1 / (d1 - d3));
            }

            Vector3 cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0f && d5 <= d6)
            {
                return c;
            }

            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0f && d2 >= 0f && d6 <= 0f)
            {
                return a + ac * (d2 / (d2 - d6));
            }

            float va = d3 * d6 - d5 * d4;
            if (va <= 0f && d4 - d3 >= 0f && d5 - d6 >= 0f)
            {
                return b + (c - b) * ((d4 - d3) / (d4 - d3 + (d5 - d6)));
            }

            float denominator = 1f / (va + vb + vc);
            return a + ab * (vb * denominator) + ac * (vc * denominator);
        }

        private static IEnumerable<ModelNode> Nodes(ModelNode node)
        {
            yield return node;
            foreach (ModelNode child in node.Children)
            {
                foreach (ModelNode descendant in Nodes(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}

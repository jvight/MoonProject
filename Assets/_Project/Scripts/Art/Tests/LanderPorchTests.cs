using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// What was built for 07 at the habitat lander (M3-14, M3-19; VISION rulings 12 and 14): the charging dock 07
    /// rests on with its nose at the contacts, and the belly lift that carries it up through the floor hatch into the
    /// crew's lab, jammed on its stops. Contract nodes, clearances for 07 at rest and along the lift's whole travel
    /// and turning on the turntable at the top (the paths as cylinders through the shaft), dark glows and repeatable
    /// builds.
    /// </summary>
    public sealed class LanderPorchTests
    {
        // 07 rides this far above whatever it stands on in the overlap checks, so resting wheels do not count.
        private const float Rest = 0.01f;

        // The dock's contacts stop at most this short of 07's nose.
        private const float ContactReach = 0.06f;

        // 07's body spans these heights over its pivot; the dock's contacts lie within this reach of its middle.
        private const float BodyBottom = 0.34f;
        private const float BodyTop = 0.84f;
        private const float ContactSpan = 0.4f;

        // The belly clears 07 (about 2 m tall with its head up) with headroom to spare (M3-19 §3).
        private const float Headroom = 0.4f;

        // The platform and 07 keep at least this far from anything fixed on their way up the shaft and turning at
        // the top (the platform's path is a cylinder as wide as its widest part, 07's as wide as it turns).
        private const float PathClearance = 0.015f;

        private ModelNode _lander;
        private ModelNode _lift;
        private List<Vector3> _rover;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _lander = BaseModelBuilder.CreateLander();
            _lift = _lander.GetDescendant(BaseModelBuilder.BellyLiftName);
            _rover = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(RoverModelBuilder.CreateModel(), Matrix4x4.identity, _rover);
        }

        [Test]
        public void Lander_HasTheDockAndBellyLiftNodes()
        {
            ModelNode dock = _lander.GetDescendant("DockAnchor");
            Assert.IsNull(dock.Mesh, "DockAnchor is an empty");
            Assert.IsTrue(_lander.Children.Contains(dock));
            ModelNode glow = _lander.GetDescendant("DockGlow");
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material, "dark until 07 charges");
            Assert.IsTrue(MeshChecks.Swatches(glow.Mesh.Geometry).SequenceEqual(new[] { PaletteSwatch.LampGlass }));

            Assert.IsTrue(_lander.Children.Contains(_lift), "BellyLift sits right under the lander");
            Assert.AreEqual(Vector3.zero, _lift.LocalPosition, "on the ground under the shaft's axis");
            CollectionAssert.AreEqual(
                new[]
                {
                    BaseModelBuilder.PlatformDownName, BaseModelBuilder.PlatformUpName, BaseModelBuilder.PlatformName,
                    BaseModelBuilder.HatchLeafLeftName, BaseModelBuilder.HatchLeafRightName,
                    BaseModelBuilder.HatchRimName, BaseModelBuilder.BellyLampName, BaseModelBuilder.CableLeftName,
                    BaseModelBuilder.CableRightName, BaseModelBuilder.CableJamName,
                },
                _lift.Children.Select(child => child.Name));
            ModelNode platform = _lift.GetDescendant(BaseModelBuilder.PlatformName);
            CollectionAssert.AreEqual(
                new[]
                {
                    BaseModelBuilder.RoverSpotName, BaseModelBuilder.RimLampsName, BaseModelBuilder.PadGlowName,
                    "Weather_Paint", "Weather_Rust", "Weather_Dust",
                },
                platform.Children.Select(child => child.Name));
            foreach (string empty in new[] { BaseModelBuilder.PlatformDownName, BaseModelBuilder.PlatformUpName,
                         BaseModelBuilder.RoverSpotName, BaseModelBuilder.StitchPointName })
            {
                Assert.IsNull(_lift.GetDescendant(empty).Mesh, $"{empty} is an empty");
            }

            foreach (string lamp in new[] { BaseModelBuilder.RimLampsName, BaseModelBuilder.PadGlowName,
                         BaseModelBuilder.HatchRimName, BaseModelBuilder.BellyLampName })
            {
                ModelNode node = _lift.GetDescendant(lamp);
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, node.Material, $"{lamp} starts dark");
                CollectionAssert.AreEqual(new[] { PaletteSwatch.LampGlass }, MeshChecks.Swatches(node.Mesh.Geometry),
                    lamp);
            }

            foreach (string moving in new[] { BaseModelBuilder.PlatformName, BaseModelBuilder.HatchLeafLeftName,
                         BaseModelBuilder.HatchLeafRightName, BaseModelBuilder.CableLeftName,
                         BaseModelBuilder.CableJamName })
            {
                ModelNode node = _lift.GetDescendant(moving);
                MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                Assert.AreEqual(ModelMaterial.Palette, node.Material, moving);
                Assert.IsFalse(MeshChecks.Swatches(node.Mesh.Geometry).Any(Palette.IsEmissive), moving);
            }
        }

        [Test]
        public void Dock_Rests07_NoseToTheContacts_ClearOfEverything_InFrontOfTheBelly()
        {
            ModelNode anchor = _lander.GetDescendant("DockAnchor");
            Assert.That(anchor.LocalPosition.y, Is.InRange(0.02f, 0.1f), "a low pad");
            Assert.Less((anchor.LocalRotation * Vector3.forward).z, -0.99f, "07 rests facing the lander");
            Matrix4x4 pose = Matrix4x4.TRS(anchor.LocalPosition + Vector3.up * Rest, anchor.LocalRotation, Vector3.one);
            List<Vector3> rover = Posed(_rover, pose);
            AssertClear(rover, Solid(_lander, string.Empty), "07 at the dock");
            Bounds docked = BoundsOf(rover, 0, rover.Count);
            float stageFront = HullPoints(PaletteSwatch.Honey).Where(p => p.y > 2f).Max(p => p.z);
            Assert.Greater(docked.min.z, stageFront, "the dock stands out on the apron, clear in front of the belly");
            Assert.Greater(Mathf.Abs(anchor.LocalPosition.x), 1.7f, "beside the way in under the belly");

            Vector3 rest = anchor.LocalPosition;
            List<Vector3> contacts = HullPoints(PaletteSwatch.Honey)
                .Where(p => Mathf.Abs(p.x - rest.x) < ContactSpan && p.z < rest.z && p.z > rest.z - 1.2f).ToList();
            Assert.IsNotEmpty(contacts, "brass contacts on the dock");
            foreach (Vector3 contact in contacts)
            {
                Assert.That(contact.y - rest.y, Is.InRange(BodyBottom, BodyTop), "at 07's own height");
                Assert.That(FrontOf(rover, contact) - contact.z, Is.InRange(0f, ContactReach), "the contacts reach 07");
            }
        }

        [Test]
        public void Lift_RisesFromTheDust_IntoTheCabin_JammedOnItsStops()
        {
            Vector3 bottom = _lift.GetDescendant(BaseModelBuilder.PlatformDownName).LocalPosition;
            Vector3 top = _lift.GetDescendant(BaseModelBuilder.PlatformUpName).LocalPosition;
            ModelNode platform = _lift.GetDescendant(BaseModelBuilder.PlatformName);
            ModelNode lab = _lander.GetDescendant(BaseModelBuilder.LabName);
            Assert.That(bottom.y, Is.InRange(0.15f, 0.35f), "07 drives on from the dust over a low rim");
            Assert.AreEqual(lab.LocalPosition.y, top.y, 1e-4f, "flush with the cabin floor at the top");
            Assert.AreEqual(new Vector2(bottom.x, bottom.z), new Vector2(top.x, top.z), "straight up the shaft");
            Assert.That(Quaternion.Angle(platform.LocalRotation, BaseModelBuilder.BellyPlatformLevel),
                Is.InRange(3f, 8f), "jammed: one side hangs about 0.3 m low");
            Assert.Greater((platform.LocalRotation * Vector3.forward).z, 0.999f, "it only rolls");
            Assert.Less(Vector3.Distance(platform.LocalPosition, bottom), 0.2f, "it sits on its stops");
            Assert.Less((platform.GetDescendant(BaseModelBuilder.RoverSpotName).LocalRotation * Vector3.forward).z,
                -0.99f, "07 drives on from the front, heading for the lander's back");

            ModelNode left = _lift.GetDescendant(BaseModelBuilder.CableLeftName);
            Vector3 foot = left.LocalPosition + Vector3.down * BaseModelBuilder.BellyCableBuiltLength;
            List<Vector3> deck = Solid(platform, BaseModelBuilder.RoverSpotName);
            Assert.Less(deck.Min(p => Vector3.Distance(p, foot)), 0.08f, "the left cable hangs down to its eye");
            Assert.Greater(left.LocalPosition.y, top.y, "the sheaves stand above the platform at the top");
            Assert.IsFalse(_lift.GetDescendant(BaseModelBuilder.CableRightName).Active, "the right one snapped");
            Assert.IsTrue(_lift.GetDescendant(BaseModelBuilder.CableJamName).Active, "the jam reads at a glance");
            Vector3 stitch = _lift.GetDescendant(BaseModelBuilder.StitchPointName).LocalPosition;
            Assert.Less(stitch.y, BaseModelBuilder.CreateLander().Mesh.Geometry.Bounds.max.y, "below the belly");
            Assert.That(stitch.y, Is.InRange(1.6f, 2.6f), "the frayed end hangs out of the shaft in 07's sight");
        }

        [Test]
        public void Belly_Clears07_WithHeadroom()
        {
            Vector3 bottom = _lift.GetDescendant(BaseModelBuilder.PlatformDownName).LocalPosition;
            ModelNode leaf = _lift.GetDescendant(BaseModelBuilder.HatchLeafLeftName);
            float belly = leaf.Mesh.Geometry.Bounds.min.y;
            float tallest = _rover.Max(p => p.y);
            Assert.Greater(belly - (bottom.y + tallest), Headroom, "07 fits under the belly with room to spare");
        }

        [Test]
        public void Lift_Carries07_ClearOfTheLander_AllTheWay_AndTurnsAtTheTop()
        {
            ModelNode platform = _lift.GetDescendant(BaseModelBuilder.PlatformName);
            List<Vector3> deck = Solid(platform, string.Empty, Matrix4x4.identity);
            float deckRadius = deck.Max(Flat);
            float deckBelow = -deck.Min(p => p.y);
            float down = _lift.GetDescendant(BaseModelBuilder.PlatformDownName).LocalPosition.y;
            float up = _lift.GetDescendant(BaseModelBuilder.PlatformUpName).LocalPosition.y;
            float deckAbove = deck.Max(p => p.y);
            float roverRadius = _rover.Max(Flat);
            float roverTop = _rover.Max(p => p.y);
            Assert.Less(roverRadius, deckRadius, "07 turns within the turntable's own circle");
            List<Vector3> fixedParts = Fixed();
            for (int i = 0; i < fixedParts.Count; i += 3)
            {
                foreach (Vector3 q in Samples(fixedParts[i], fixedParts[i + 1], fixedParts[i + 2]))
                {
                    float r = Flat(q);
                    bool inDeckPath = r < deckRadius + PathClearance && q.y > down - deckBelow + Rest * 0.5f
                                      && q.y < up + deckAbove + PathClearance;
                    bool inRoverPath = r < roverRadius + PathClearance && q.y > down
                                       && q.y < up + roverTop + PathClearance;
                    Assert.IsFalse(inDeckPath, $"{q} stands in the platform's way up the shaft");
                    Assert.IsFalse(inRoverPath, $"{q} stands in 07's way up the shaft or round the turntable");
                }
            }
        }

        [Test]
        public void Lander_BuildsIdentically()
        {
            MeshChecks.AssertSameModel(_lander, BaseModelBuilder.CreateLander());
        }

        /// <summary>
        /// Everything that stays put while the lift runs: the lander and its lab, without the belly lift's moving
        /// parts (the platform and its cables, the jam hidden once stitched) and with the hatch leaves slid open.
        /// </summary>
        private List<Vector3> Fixed()
        {
            List<Vector3> parts = Solid(_lander, BaseModelBuilder.BellyLiftName);
            foreach (ModelNode child in _lift.Children)
            {
                bool leaf = child.Name == BaseModelBuilder.HatchLeafLeftName
                            || child.Name == BaseModelBuilder.HatchLeafRightName;
                if (leaf)
                {
                    float side = child.Name == BaseModelBuilder.HatchLeafLeftName ? -1f : 1f;
                    var open = new List<Vector3>();
                    SilhouetteCamera.CollectTriangles(child,
                        Matrix4x4.Translate(Vector3.right * (side * BaseModelBuilder.HatchLeafTravel)), open);
                    parts.AddRange(open);
                }
                else if (child.Name == BaseModelBuilder.HatchRimName || child.Name == BaseModelBuilder.BellyLampName)
                {
                    SilhouetteCamera.CollectTriangles(child, Matrix4x4.identity, parts);
                }
            }

            return parts;
        }

        /// <summary>
        /// How near 07's front (posed facing -Z) comes to the lander in the column through <paramref name="point"/>:
        /// the least z of its triangles that cover the point's x and y.
        /// </summary>
        private static float FrontOf(List<Vector3> rover, Vector3 point)
        {
            float front = float.MaxValue;
            for (int i = 0; i < rover.Count; i += 3)
            {
                Bounds face = BoundsOf(rover, i, 3);
                if (Mathf.Abs(face.center.x - point.x) <= face.extents.x
                    && Mathf.Abs(face.center.y - point.y) <= face.extents.y)
                {
                    front = Mathf.Min(front, face.min.z);
                }
            }

            return front;
        }

        /// <summary>The vertices of the lander hull's triangles painted <paramref name="swatch"/>.</summary>
        private IEnumerable<Vector3> HullPoints(PaletteSwatch swatch)
        {
            LowPolyMeshBuilder hull = _lander.Mesh.Geometry;
            for (int t = 0; t < hull.TriangleCount; t++)
            {
                if (MeshChecks.SwatchOf(hull, t) == swatch)
                {
                    for (int v = 0; v < 3; v++)
                    {
                        yield return hull.Positions[t * 3 + v];
                    }
                }
            }
        }

        /// <summary>
        /// The triangles of <paramref name="root"/> and its active descendants in its parent's space, leaving out the
        /// weather layers and every child of the root whose name starts with <paramref name="skip"/> (when not empty).
        /// </summary>
        private static List<Vector3> Solid(ModelNode root, string skip)
        {
            return Solid(root, skip, root.LocalMatrix);
        }

        private static List<Vector3> Solid(ModelNode root, string skip, Matrix4x4 rootPose)
        {
            var triangles = new List<Vector3>();
            AddMesh(root, rootPose, triangles);
            foreach (ModelNode child in root.Children)
            {
                if (child.Name.StartsWith("Weather_") || (skip.Length > 0 && child.Name.StartsWith(skip)))
                {
                    continue;
                }

                var part = new List<Vector3>();
                SilhouetteCamera.CollectTriangles(child, rootPose, part);
                triangles.AddRange(part);
            }

            return triangles;
        }

        private static void AddMesh(ModelNode node, Matrix4x4 pose, List<Vector3> triangles)
        {
            if (node.Mesh == null)
            {
                return;
            }

            foreach (Vector3 p in node.Mesh.Geometry.Positions)
            {
                triangles.Add(pose.MultiplyPoint3x4(p));
            }
        }

        private static float Flat(Vector3 p)
        {
            return new Vector2(p.x, p.z).magnitude;
        }

        /// <summary>A triangle's corners, its edges' midpoints and its centre.</summary>
        private static IEnumerable<Vector3> Samples(Vector3 a, Vector3 b, Vector3 c)
        {
            yield return a;
            yield return b;
            yield return c;
            yield return (a + b) * 0.5f;
            yield return (b + c) * 0.5f;
            yield return (c + a) * 0.5f;
            yield return (a + b + c) / 3f;
        }

        private static List<Vector3> Posed(List<Vector3> points, Matrix4x4 pose)
        {
            return points.Select(p => pose.MultiplyPoint3x4(p)).ToList();
        }

        /// <summary>
        /// Fails when any triangle of <paramref name="mover"/> comes within touching distance (bounding boxes, so
        /// erring on the safe side) of a triangle of <paramref name="still"/>.
        /// </summary>
        private static void AssertClear(List<Vector3> mover, List<Vector3> still, string what)
        {
            Bounds reach = BoundsOf(mover, 0, mover.Count);
            var near = new List<Bounds>();
            for (int i = 0; i < still.Count; i += 3)
            {
                Bounds face = BoundsOf(still, i, 3);
                if (face.Intersects(reach))
                {
                    near.Add(face);
                }
            }

            for (int i = 0; i < mover.Count && near.Count > 0; i += 3)
            {
                Bounds face = BoundsOf(mover, i, 3);
                foreach (Bounds other in near)
                {
                    if (face.Intersects(other))
                    {
                        Assert.Fail($"{what}: {face.center} touches {other.center}");
                    }
                }
            }
        }

        private static Bounds BoundsOf(List<Vector3> points, int start, int count)
        {
            var bounds = new Bounds(points[start], Vector3.zero);
            for (int i = start + 1; i < start + count; i++)
            {
                bounds.Encapsulate(points[i]);
            }

            return bounds;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// What M3-14 built for 07 at the lander (VISION rulings 12 and 14): the charging dock 07 rests on with its nose
    /// at the contacts, and the crew's cable lift that carries it up to the deck, jammed halfway. Contract nodes,
    /// clearances for 07 at rest and along the lift's whole travel, a dark charging glow and repeatable builds.
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

        private static readonly float[] Travel = { 0f, 0.25f, 0.5f, 0.75f, 1f };

        private ModelNode _lander;
        private List<Vector3> _rover;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _lander = BaseModelBuilder.CreateLander();
            _rover = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(RoverModelBuilder.CreateModel(), Matrix4x4.identity, _rover);
        }

        [Test]
        public void Lander_HasTheDockAndLiftNodes()
        {
            foreach (string name in new[] { "DockAnchor", "LiftBottom", "LiftTop" })
            {
                ModelNode node = _lander.GetDescendant(name);
                Assert.IsNotNull(node, name);
                Assert.IsNull(node.Mesh, $"{name} is an empty");
                Assert.IsTrue(_lander.Children.Contains(node), $"{name} sits right under the lander");
            }

            ModelNode glow = _lander.GetDescendant("DockGlow");
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material, "dark until 07 charges");
            Assert.IsTrue(MeshChecks.Swatches(glow.Mesh.Geometry).SequenceEqual(new[] { PaletteSwatch.LampGlass }));
            ModelNode platform = _lander.GetDescendant("LiftPlatform");
            CollectionAssert.AreEqual(new[] { "RoverSpot", "Weather_Paint", "Weather_Rust", "Weather_Dust" },
                platform.Children.Select(child => child.Name));
            Assert.IsNull(platform.GetDescendant("RoverSpot").Mesh);
            var moving = new[] { platform, _lander.GetDescendant("LiftCable_L"), _lander.GetDescendant("LiftCable_R") };
            foreach (ModelNode node in moving)
            {
                Assert.IsTrue(_lander.Children.Contains(node), $"{node.Name} sits right under the lander");
                MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                Assert.AreEqual(ModelMaterial.Palette, node.Material, node.Name);
                Assert.IsFalse(MeshChecks.Swatches(node.Mesh.Geometry).Any(Palette.IsEmissive), node.Name);
            }
        }

        [Test]
        public void Dock_Rests07_NoseToTheContacts_ClearOfEverything()
        {
            ModelNode anchor = _lander.GetDescendant("DockAnchor");
            Assert.That(anchor.LocalPosition.y, Is.InRange(0.02f, 0.1f), "a low pad");
            Assert.Less((anchor.LocalRotation * Vector3.forward).z, -0.99f, "07 rests facing the lander");
            Matrix4x4 pose = Matrix4x4.TRS(anchor.LocalPosition + Vector3.up * Rest, anchor.LocalRotation, Vector3.one);
            List<Vector3> rover = Posed(_rover, pose);
            AssertClear(rover, Solid(_lander, string.Empty), "07 at the dock");

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
        public void Lift_RisesFromTheDust_ToTheDeck_JammedHalfway()
        {
            Vector3 bottom = _lander.GetDescendant("LiftBottom").LocalPosition;
            Vector3 top = _lander.GetDescendant("LiftTop").LocalPosition;
            ModelNode platform = _lander.GetDescendant("LiftPlatform");
            Assert.That(bottom.y, Is.InRange(0.02f, 0.25f), "07 drives on from the dust, over the ramp lip");
            Assert.AreEqual(BaseModelBuilder.LanderDeckTop, top.y, 1e-4f, "level with the deck at the top");
            Assert.AreEqual(new Vector2(bottom.x, bottom.z), new Vector2(top.x, top.z), "straight up");
            Assert.Less(Vector3.Distance(platform.LocalPosition, Vector3.Lerp(bottom, top, 0.5f)), 1e-4f, "jammed");
            Assert.AreEqual(Quaternion.identity, platform.LocalRotation);
            Assert.Less((platform.GetDescendant("RoverSpot").LocalRotation * Vector3.forward).z, -0.99f,
                "07 rides facing the lander");

            List<Vector3> deck = Solid(platform, string.Empty);
            foreach (string name in new[] { "LiftCable_L", "LiftCable_R" })
            {
                ModelNode cable = _lander.GetDescendant(name);
                Vector3 end = cable.LocalPosition + Vector3.up * cable.Mesh.Geometry.Bounds.min.y;
                Assert.Less(deck.Min(p => Vector3.Distance(p, end)), 0.1f, $"{name} hangs down to the carriage");
                Assert.Greater(cable.LocalPosition.y, top.y + 1.2f, $"{name} still hangs with the platform at the top");
            }
        }

        [Test]
        public void Lift_Carries07_ClearOfTheLander_AllTheWay()
        {
            ModelNode platform = _lander.GetDescendant("LiftPlatform");
            Matrix4x4 spot = platform.GetDescendant("RoverSpot").LocalMatrix;
            List<Vector3> deck = Solid(platform, string.Empty, Matrix4x4.identity);
            List<Vector3> fixedParts = Solid(_lander, "Lift");
            Vector3 bottom = _lander.GetDescendant("LiftBottom").LocalPosition;
            Vector3 top = _lander.GetDescendant("LiftTop").LocalPosition;
            foreach (float t in Travel)
            {
                Matrix4x4 at = Matrix4x4.Translate(Vector3.Lerp(bottom, top, t));
                List<Vector3> carried = Posed(_rover, at * spot * Matrix4x4.Translate(Vector3.up * Rest));
                List<Vector3> moved = Posed(deck, at);
                AssertClear(moved, fixedParts, $"the platform at {t:P0} of its travel");
                AssertClear(carried, fixedParts, $"07 on the lift at {t:P0} of its travel");
                AssertClear(carried, moved, $"07 inside the platform's railings at {t:P0} of its travel");
            }
        }

        [Test]
        public void Lander_BuildsIdentically()
        {
            MeshChecks.AssertSameModel(_lander, BaseModelBuilder.CreateLander());
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

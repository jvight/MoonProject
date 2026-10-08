using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Kenji's Rover Bay (M3-14, VISION ruling 14; docs/ARCHITECTURE.md, "Contract: Kenji's Rover Bay"): the node
    /// contract rover's IK drives, a turntable disc 07 fits on with room all round and a hole the floor arm lifts the
    /// coils through, arms rigged the way rover reads them with links long enough to reach, folded at rest under the
    /// roof and clear above 07, a clear way in, a hopper a parked 07's beam can see, dark work lamps and repeatable
    /// builds.
    /// </summary>
    public sealed class RoverBayTests
    {
        private const float TurntableRoom = 0.25f;
        private const float HeadRoom = 0.3f;
        private const float SideRoom = 0.15f;
        private const float WallRoom = 0.05f;
        private const float HoleRoom = 0.01f;
        private const float MinHopperHeight = 0.9f;
        private const float MaxHopperHeight = 1.5f;

        // Rover's contract: link lengths that reach every kit socket, its joint tolerance and its floor alignment.
        private const float MinUpper = 1.3f;
        private const float MinLower = 1.2f;
        private const float MinTip = 0.26f;
        private const float JointTolerance = 1e-3f;
        private const float FloorAlign = 0.25f;

        // The beam leaves a little in front of 07's lens and is judged until just short of the mouth's centre.
        private const float BeamStart = 0.03f;
        private const float MouthStop = 0.05f;

        private static readonly string[] Joints = { "Yaw", "Upper", "Lower", "Tip", "SparkSocket" };

        private ModelNode _bay;
        private ModelNode _rover;
        private Bounds _roverBounds;

        [OneTimeSetUp]
        public void BuildOnce()
        {
            _bay = BaseModelBuilder.CreateRoverBay();
            _rover = RoverModelBuilder.CreateModel();
            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(_rover, Matrix4x4.identity, triangles);
            _roverBounds = new Bounds(triangles[0], Vector3.zero);
            foreach (Vector3 p in triangles)
            {
                _roverBounds.Encapsulate(p);
            }
        }

        [Test]
        public void Bay_HasTheContractNodes()
        {
            Assert.AreEqual("RoverBay", _bay.Name);
            var expected = new List<string>
            {
                "Turntable", "FloorArm", "FloorArm/FloorLift", "FloorArm/FloorLift/FloorTip", "HopperMouth",
                "BaySign", "Lamp_0", "Lamp_1",
            };
            for (int i = 0; i < 3; i++)
            {
                string path = $"Arm_{i}";
                expected.Add(path);
                foreach (string joint in Joints)
                {
                    path += "/" + joint;
                    expected.Add(path);
                }
            }

            var paths = new List<string>();
            Collect(_bay, string.Empty, paths);
            CollectionAssert.AreEquivalent(expected, paths);
            CollectionAssert.AreEqual(new[] { "Weather_Paint", "Weather_Rust", "Weather_Dust" },
                _bay.Children.Where(c => c.Name.StartsWith("Weather_")).Select(c => c.Name));
            Assert.AreEqual(0f, _bay.Mesh.Geometry.Bounds.min.y, 0.02f, "stands on the ground at its anchor");
            Assert.IsNull(_bay.GetDescendant("HopperMouth").Mesh);
            Assert.IsNull(_bay.GetDescendant("SparkSocket").Mesh);
            Assert.IsNull(_bay.GetDescendant("FloorArm").Mesh);
            foreach (ModelNode node in Nodes(_bay).Where(node => node.Mesh != null))
            {
                MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                bool lamp = node.Name.StartsWith("Lamp_");
                bool sign = node.Name == "BaySign";
                bool weather = node.Name.StartsWith("Weather_");
                ModelMaterial material = lamp || sign ? ModelMaterial.PaletteGlowOff
                    : weather ? ModelMaterial.PaletteWeather : ModelMaterial.Palette;
                Assert.AreEqual(material, node.Material, node.Name);
                List<PaletteSwatch> swatches = MeshChecks.Swatches(node.Mesh.Geometry);
                Assert.IsTrue(lamp ? swatches.SequenceEqual(new[] { PaletteSwatch.LampGlass })
                    : sign ? swatches.Where(Palette.IsEmissive).SequenceEqual(new[] { PaletteSwatch.LampGlass })
                    : !swatches.Any(Palette.IsEmissive), $"{node.Name}: only the lamps and the sign glow, when lit");
            }
        }

        [Test]
        public void MovingParts_AreWeatheredLikeTheBay()
        {
            string[] moving = { "Turntable", "Upper", "Lower", "Tip", "FloorLift", "FloorTip" };
            foreach (string name in moving)
            {
                foreach (ModelNode node in Nodes(_bay).Where(node => node.Name == name))
                {
                    ModelNode[] skins = node.Children.Where(c => c.Name.StartsWith("Weather_")).ToArray();
                    Assert.IsNotEmpty(skins, $"{name} wears the bay's age");
                    foreach (ModelNode skin in skins)
                    {
                        Assert.AreEqual(ModelMaterial.PaletteWeather, skin.Material, $"{name}/{skin.Name}");
                        Assert.IsTrue(skin.Mesh.Geometry.HasVertexColours, $"{name}/{skin.Name} carries colours");
                    }
                }
            }
        }

        [Test]
        public void Turntable_Holds07_WithRoomAllRound()
        {
            ModelNode turntable = _bay.GetDescendant("Turntable");
            float radius = turntable.Mesh.Geometry.Bounds.extents.x;
            float reach = 0f;
            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(_rover, Matrix4x4.identity, triangles);
            foreach (Vector3 p in triangles)
            {
                reach = Mathf.Max(reach, new Vector2(p.x, p.z).magnitude);
            }

            Assert.GreaterOrEqual(radius - reach, TurntableRoom, "07 (2.3 m long) fits on the turntable with room");
            Assert.Less((turntable.LocalRotation * Vector3.forward).z, -0.99f, "07 parks facing in, the way it drove");
        }

        [Test]
        public void Turntable_IsATurningDisc_ItsHazardRimTurnsWithIt_AndItsHoleLetsTheCoilsThrough()
        {
            ModelNode turntable = _bay.GetDescendant("Turntable");
            Assert.Contains(turntable, _bay.Children.ToList(), "its own node on the bay");
            Assert.Greater((turntable.LocalRotation * Vector3.up).y, 0.9999f, "turns about local Y only");
            Assert.Greater(turntable.LocalPosition.y, BaseModelBuilder.RoverBayInside.min.y, "proud of the floor");
            CollectionAssert.Contains(MeshChecks.Swatches(turntable.Mesh.Geometry), PaletteSwatch.Honey,
                "the hazard rim is painted on the disc, so it turns with it");

            float hole = Hole(turntable.Mesh.Geometry);
            float coils = Reach(MeshChecks.Points(RoverModelBuilder.CreateHoverCoils(), Matrix4x4.identity, false));
            float floorArm = Reach(MeshChecks.Points(_bay.GetDescendant("FloorLift"), Matrix4x4.identity));
            Assert.Greater(hole, coils + HoleRoom, "the Hover-Jump coils ride up through the hole");
            Assert.Greater(hole, floorArm + HoleRoom, "the floor arm rises through the hole without touching it");
        }

        [Test]
        public void Arms_AreRiggedTheWayRoverReadsThem_WithLinksLongEnoughToReach()
        {
            for (int i = 0; i < 3; i++)
            {
                ModelNode arm = _bay.GetDescendant($"Arm_{i}");
                ModelNode yaw = Child(arm, "Yaw");
                ModelNode upper = Child(yaw, "Upper");
                ModelNode lower = Child(upper, "Lower");
                ModelNode tip = Child(lower, "Tip");
                ModelNode socket = Child(tip, "SparkSocket");

                Assert.Greater((arm.LocalRotation * Vector3.up).y, 0.9999f, $"{arm.Name} rides the rail level");
                Assert.Greater((yaw.LocalRotation * Vector3.up).y, 0.9999f, $"{arm.Name}'s Yaw turns about local Y");
                Assert.Less(yaw.LocalPosition.magnitude, JointTolerance, $"{arm.Name}'s Yaw on the shoulder");
                Assert.Less(upper.LocalPosition.magnitude, JointTolerance, $"{arm.Name}'s Upper on the shoulder");
                foreach (ModelNode joint in new[] { upper, lower, tip })
                {
                    Assert.Less(Vector3.Distance(joint.LocalRotation * Vector3.right, Vector3.right), 1e-4f,
                        $"{arm.Name}/{joint.Name} pitches about local X only");
                }

                foreach (ModelNode link in new[] { lower, tip })
                {
                    Assert.Less(new Vector2(link.LocalPosition.x, link.LocalPosition.z).magnitude, JointTolerance,
                        $"{arm.Name}/{link.Name} hangs along its parent's -Y");
                }

                Assert.GreaterOrEqual(-lower.LocalPosition.y, MinUpper - 1e-4f, $"{arm.Name}'s upper link");
                Assert.GreaterOrEqual(-tip.LocalPosition.y, MinLower - 1e-4f, $"{arm.Name}'s lower link");
                Assert.GreaterOrEqual(-socket.LocalPosition.y, MinTip - 1e-4f, $"{arm.Name}'s tip");
            }
        }

        [Test]
        public void Arms_AtRest_FoldUnderTheRoof_InsideTheWalls_ClearAbove07()
        {
            float padTop = _bay.GetDescendant("Turntable").LocalPosition.y;
            float clear = padTop + _roverBounds.max.y + HeadRoom;
            Bounds inside = BaseModelBuilder.RoverBayInside;
            inside.Expand(new Vector3(-WallRoom, 0f, -WallRoom) * 2f);
            foreach (ModelNode arm in _bay.Children.Where(child => child.Name.StartsWith("Arm_")))
            {
                foreach (Vector3 p in MeshChecks.Points(arm, Matrix4x4.identity, false))
                {
                    Assert.Greater(p.y, clear, $"{arm.Name} hangs into 07's way at {p}");
                    Assert.Less(p.y, inside.max.y, $"{arm.Name} pokes through the roof at {p}");
                    Assert.That(p.x, Is.InRange(inside.min.x, inside.max.x), $"{arm.Name} pokes through a side at {p}");
                    Assert.That(p.z, Is.InRange(inside.min.z, inside.max.z),
                        $"{arm.Name} pokes through the back or the front at {p}");
                }
            }
        }

        [Test]
        public void FloorArm_WaitsInThePitUnderTheHole_BelowAParked07sCoilSocket()
        {
            ModelNode turntable = _bay.GetDescendant("Turntable");
            ModelNode floorArm = _bay.GetDescendant("FloorArm");
            ModelNode lift = Child(floorArm, "FloorLift");
            ModelNode tip = Child(lift, "FloorTip");

            Vector2 axis = new Vector2(turntable.LocalPosition.x, turntable.LocalPosition.z);
            Assert.Less(Vector2.Distance(new Vector2(floorArm.LocalPosition.x, floorArm.LocalPosition.z), axis),
                JointTolerance, "under the turntable's axis");
            Assert.Less(Quaternion.Angle(Quaternion.identity, floorArm.LocalRotation), 0.01f, "level");
            Assert.Less(Quaternion.Angle(Quaternion.identity, lift.LocalRotation), 0.01f, "rises along local Y");
            Assert.Less(new Vector2(lift.LocalPosition.x, lift.LocalPosition.z).magnitude, JointTolerance);
            Assert.Less(new Vector2(tip.LocalPosition.x, tip.LocalPosition.z).magnitude, JointTolerance);

            float tipTop = floorArm.LocalPosition.y + lift.LocalPosition.y + tip.LocalPosition.y;
            Assert.Less(tipTop, turntable.LocalPosition.y, "at rest the cradle waits under the disc's top");
            Assert.Greater(tipTop, BaseModelBuilder.RoverBayInside.min.y, "in sight in the hole, not lost in the pit");

            Vector3 socket = turntable.LocalMatrix.MultiplyPoint3x4(RoverModelBuilder.CoilSocket)
                - floorArm.LocalPosition;
            Assert.LessOrEqual(new Vector2(socket.x, socket.z).magnitude, FloorAlign, "07's coil socket over the arm");
            Assert.Greater(socket.y, tipTop, "the arm rises to it");
        }

        [Test]
        public void TheWayIn_IsClear_FromTheRampToTheTurntable()
        {
            ModelNode turntable = _bay.GetDescendant("Turntable");
            float padTop = turntable.LocalPosition.y;
            float front = _bay.Mesh.Geometry.Bounds.max.z;
            var corridor = new Bounds();
            corridor.SetMinMax(
                new Vector3(-_roverBounds.extents.x - SideRoom, padTop + 0.05f, turntable.LocalPosition.z),
                new Vector3(_roverBounds.extents.x + SideRoom, padTop + _roverBounds.max.y + 0.1f, front));
            foreach (ModelNode node in Nodes(_bay).Where(node => node.Mesh != null && node.Name != "Turntable"
                && !node.Name.StartsWith("Weather_")))
            {
                Matrix4x4 world = WorldOf(node) * node.LocalMatrix;
                IReadOnlyList<Vector3> points = node.Mesh.Geometry.Positions;
                for (int v = 0; v < points.Count; v += 3)
                {
                    var face = new Bounds(world.MultiplyPoint3x4(points[v]), Vector3.zero);
                    face.Encapsulate(world.MultiplyPoint3x4(points[v + 1]));
                    face.Encapsulate(world.MultiplyPoint3x4(points[v + 2]));
                    Assert.IsFalse(corridor.Intersects(face), $"{node.Name} blocks the way in at {face.center}");
                }
            }
        }

        [Test]
        public void Hopper_IsAtRoverHeight_FacingTheTurntable()
        {
            ModelNode mouth = _bay.GetDescendant("HopperMouth");
            Assert.That(mouth.LocalPosition.y, Is.InRange(MinHopperHeight, MaxHopperHeight), "07's beam reaches it");
            Vector3 toPad = _bay.GetDescendant("Turntable").LocalPosition - mouth.LocalPosition;
            Vector3 facing = mouth.LocalRotation * Vector3.forward;
            Assert.Greater(Vector2.Dot(new Vector2(facing.x, facing.z).normalized,
                new Vector2(toPad.x, toPad.z).normalized),
                0.95f, "the mouth faces the turntable");
        }

        [Test]
        public void Hopper_IsInPlainSight_OfAParked07sBeam()
        {
            Matrix4x4 parked = _bay.GetDescendant("Turntable").LocalMatrix;
            Vector3 eye = (parked * SilhouetteCamera.WorldOf(_rover, "Neck/Head/Eye/TetherOrigin")).GetColumn(3);
            Vector3 mouth = _bay.GetDescendant("HopperMouth").LocalPosition;
            Vector3 along = (mouth - eye).normalized;
            Vector3 from = eye + along * BeamStart;
            Vector3 to = mouth - along * MouthStop;

            var triangles = new List<Vector3>();
            SilhouetteCamera.CollectTriangles(_bay, Matrix4x4.identity, triangles);
            AssertClear(triangles, from, to, "the bay");
            triangles.Clear();
            SilhouetteCamera.CollectTriangles(_rover, parked, triangles);
            AssertClear(triangles, from, to, "07 itself");
        }

        [Test]
        public void Bay_BuildsIdentically()
        {
            MeshChecks.AssertSameModel(_bay, BaseModelBuilder.CreateRoverBay());
        }

        /// <summary>Fails if the segment <paramref name="from"/>..<paramref name="to"/> crosses a triangle.</summary>
        private static void AssertClear(List<Vector3> triangles, Vector3 from, Vector3 to, string what)
        {
            Vector3 direction = to - from;
            for (int t = 0; t + 2 < triangles.Count; t += 3)
            {
                Vector3 a = triangles[t];
                Vector3 edge1 = triangles[t + 1] - a;
                Vector3 edge2 = triangles[t + 2] - a;
                Vector3 p = Vector3.Cross(direction, edge2);
                float det = Vector3.Dot(edge1, p);
                if (Mathf.Abs(det) < 1e-9f)
                {
                    continue;
                }

                Vector3 s = from - a;
                float u = Vector3.Dot(s, p) / det;
                Vector3 q = Vector3.Cross(s, edge1);
                float v = Vector3.Dot(direction, q) / det;
                float hit = Vector3.Dot(edge2, q) / det;
                Assert.IsFalse(u >= 0f && v >= 0f && u + v <= 1f && hit >= 0f && hit <= 1f,
                    $"{what} blocks the beam to the hopper at {from + direction * hit}");
            }
        }

        /// <summary>The radius of the hole round the turntable's axis (to its nearest edge, not corner).</summary>
        private static float Hole(LowPolyMeshBuilder disc)
        {
            IReadOnlyList<Vector3> points = disc.Positions;
            float hole = float.MaxValue;
            for (int t = 0; t + 2 < points.Count; t += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    Vector3 a = points[t + e];
                    Vector3 b = points[t + (e + 1) % 3];
                    hole = Mathf.Min(hole, FromAxis(new Vector2(a.x, a.z), new Vector2(b.x, b.z)));
                }
            }

            return hole;
        }

        /// <summary>How far the edge <paramref name="a"/>..<paramref name="b"/> passes from the axis.</summary>
        private static float FromAxis(Vector2 a, Vector2 b)
        {
            Vector2 run = b - a;
            float along = run.sqrMagnitude < 1e-12f ? 0f : Mathf.Clamp01(-Vector2.Dot(a, run) / run.sqrMagnitude);
            return (a + run * along).magnitude;
        }

        /// <summary>How far <paramref name="points"/> reach out from their local Y axis.</summary>
        private static float Reach(IEnumerable<Vector3> points)
        {
            return points.Max(p => new Vector2(p.x, p.z).magnitude);
        }

        private static ModelNode Child(ModelNode node, string name)
        {
            ModelNode child = node.Children.FirstOrDefault(c => c.Name == name);
            Assert.IsNotNull(child, $"{node.Name} has no child {name}");
            return child;
        }

        /// <summary>
        /// The matrix from <paramref name="target"/>'s parent space to bay space (identity for the root).
        /// </summary>
        private Matrix4x4 WorldOf(ModelNode target)
        {
            return FindParent(_bay, target, Matrix4x4.identity) ?? Matrix4x4.identity;
        }

        private static Matrix4x4? FindParent(ModelNode node, ModelNode target, Matrix4x4 parent)
        {
            Matrix4x4 local = parent * node.LocalMatrix;
            foreach (ModelNode child in node.Children)
            {
                if (child == target)
                {
                    return local;
                }

                Matrix4x4? hit = FindParent(child, target, local);
                if (hit.HasValue)
                {
                    return hit;
                }
            }

            return null;
        }

        /// <summary>Every node path under <paramref name="node"/>, leaving out the Weather_* skins.</summary>
        private static void Collect(ModelNode node, string prefix, List<string> paths)
        {
            foreach (ModelNode child in node.Children.Where(c => !c.Name.StartsWith("Weather_")))
            {
                string path = prefix.Length == 0 ? child.Name : $"{prefix}/{child.Name}";
                paths.Add(path);
                Collect(child, path, paths);
            }
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

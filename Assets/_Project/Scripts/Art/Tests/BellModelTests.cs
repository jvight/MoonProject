using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The Bell rig contract (M3-05), her broken variant and her part pickups.</summary>
    public sealed class BellModelTests
    {
        private static readonly string[] Corners = { "FL", "FR", "RL", "RR" };

        // The tuning knob: chunky (its skirt's half width), proud of the face, within this of 07's beam height.
        private const float ChunkyKnob = 0.08f;
        private const float KnobProud = 0.06f;
        private const float KnobBeamReach = 0.2f;

        private static readonly string[] GlowNodes =
        {
            "DialLamp", "PartLamp_0", "PartLamp_1", "PartLamp_2", "PartLamp_3",
        };

        [TestCase(false)]
        [TestCase(true)]
        public void Bell_HasExactlyTheContractNodes_UnderTheirParents(bool broken)
        {
            ModelNode bell = BellModelBuilder.CreateBell(broken);
            var expected = new Dictionary<string, string>
            {
                { "Body", bell.Name },
                { "Lid", "Body" },
                { "DialFace", "Body" },
                { "Needle", "DialFace" },
                { "DialLamp", "DialFace" },
                { "Knob", "DialFace" },
                { "Speaker", "Body" },
                { "TapeSlot", "Body" },
                { "Antenna", "Body" },
                { "PartLamp_0", "Body" },
                { "PartLamp_1", "Body" },
                { "PartLamp_2", "Body" },
                { "PartLamp_3", "Body" },
            };
            foreach (string corner in Corners)
            {
                expected.Add("Leg_" + corner, bell.Name);
                expected.Add("Shin_" + corner, "Leg_" + corner);
            }

            var parents = new Dictionary<string, string>();
            CollectParents(bell, parents);

            Assert.AreEqual(broken ? "Bell_Broken" : "Bell", bell.Name);
            CollectionAssert.AreEquivalent(expected, parents);
            Assert.IsNull(bell.Mesh, "the root is an empty on the ground");
            Assert.IsNull(bell.GetDescendant("TapeSlot").Mesh, "TapeSlot is an empty");
            foreach (string name in expected.Keys.Where(name => name != "TapeSlot"))
            {
                ModelNode node = bell.GetDescendant(name);
                Assert.IsNotNull(node.Mesh, $"{name} renders");
                MeshChecks.AssertWellFormed(node.Mesh.Geometry);
            }
        }

        [Test]
        public void Bell_StandsOnHerFeet_ALittleTallerThan07()
        {
            ModelNode bell = BellModelBuilder.CreateBell(false);
            Bounds bounds = Measure(bell);
            ModelNode body = bell.GetDescendant("Body");
            ModelNode lid = body.GetDescendant("Lid");
            float cabinetTop = body.LocalPosition.y + lid.LocalPosition.y + lid.Mesh.Geometry.Bounds.max.y;
            float roverHead = MeshChecks.Points(RoverModelBuilder.CreateModel().GetDescendant("Neck"),
                Matrix4x4.identity, false).Max(p => p.y);

            Assert.AreEqual(0f, bounds.min.y, 1e-3f, "feet on the ground at the pivot");
            Assert.That(body.LocalPosition.y, Is.InRange(0.7f, 0.8f), "on ~0.75 m legs");
            Assert.AreEqual(Quaternion.identity, body.LocalRotation, "upright at rest");
            Assert.That(cabinetTop, Is.InRange(1.5f, 1.7f), "~1.6 m tall overall");
            Assert.Greater(cabinetTop, roverHead, "a little taller than 07");
            Assert.Less(bounds.max.y, 2.1f, "the antenna stays slender");
            Bounds cabinet = body.Mesh.Geometry.Bounds;
            Assert.That(cabinet.size.x, Is.InRange(0.85f, 0.95f), "~0.9 m wide");
            Assert.That(cabinet.size.y, Is.InRange(0.65f, 0.8f), "~0.8 m tall with its lid");
            Assert.That(Mathf.Max(bounds.size.x, bounds.size.z), Is.LessThan(1.3f), "slender");
            Assert.That(Triangles(bell), Is.InRange(2500, 7000), "triangle budget");
        }

        [Test]
        public void Pivots_MoveTheWayGameplayAnimatesThem()
        {
            ModelNode bell = BellModelBuilder.CreateBell(false);
            ModelNode body = bell.GetDescendant("Body");
            Bounds cabinet = body.Mesh.Geometry.Bounds;

            Assert.AreEqual(0f, cabinet.min.y, 1e-3f, "Body pivots at the centre of its underside");
            Assert.AreEqual(0f, cabinet.center.x, 0.02f);

            ModelNode lid = body.GetDescendant("Lid");
            Assert.Less(lid.LocalPosition.z, -0.2f, "hinged at the back");
            Bounds lidBounds = lid.Mesh.Geometry.Bounds;
            Assert.Greater(lidBounds.min.z, -0.03f, "the lid lies forward of its hinge");
            Assert.Greater(Front(lid, Quaternion.Euler(-60f, 0f, 0f)).y, Front(lid, Quaternion.identity).y + 0.3f,
                "negative X rotation lifts the lid open");

            ModelNode dial = body.GetDescendant("DialFace");
            Assert.AreEqual(Quaternion.identity, dial.LocalRotation, "+Z out of the dial");
            Assert.Greater(dial.LocalPosition.z, cabinet.max.z - 0.1f, "on the front");
            ModelNode needle = dial.GetDescendant("Needle");
            Vector3 tip = Tip(needle.Mesh.Geometry);
            Assert.Greater(tip.x, 0f, "at rest the needle points at the band's left end (seen from the front)");
            Assert.Greater(tip.y, 0f);
            Vector3 swept = Quaternion.Euler(0f, 0f, BellModelBuilder.NeedleSweepDegrees) * tip;
            Assert.Less(swept.x, 0f, "+Z rotation sweeps it to the right end");
            Assert.Greater(swept.y, 0f);
            Assert.Less(dial.GetDescendant("DialLamp").Mesh.Geometry.Bounds.max.z,
                needle.Mesh.Geometry.Bounds.min.z, "the lamp glows behind the needle");

            Bounds speaker = body.GetDescendant("Speaker").Mesh.Geometry.Bounds;
            Assert.Less(new Vector2(speaker.center.x, speaker.center.y).magnitude, 0.01f, "pivot on the cone centre");
            Assert.Greater(body.GetDescendant("TapeSlot").LocalPosition.z, cabinet.max.z - 0.1f, "door on the front");
            Bounds antenna = body.GetDescendant("Antenna").Mesh.Geometry.Bounds;
            Assert.Greater(antenna.max.y, 0.4f, "the whip rises from its swivel");
            Assert.Greater(antenna.min.y, -0.03f);
        }

        [Test]
        public void Knob_IsChunky_BesideTheDial_AtRoverBeamHeight()
        {
            ModelNode bell = BellModelBuilder.CreateBell(false);
            ModelNode body = bell.GetDescendant("Body");
            ModelNode dial = body.GetDescendant("DialFace");
            ModelNode knob = dial.GetDescendant("Knob");
            Bounds shape = knob.Mesh.Geometry.Bounds;
            var placed = new Bounds(shape.center + knob.LocalPosition, shape.size);
            float beam = SilhouetteCamera.WorldOf(RoverModelBuilder.CreateModel(), "Neck/Head/Eye/TetherOrigin")
                .GetColumn(3).y;
            float height = SilhouetteCamera.WorldOf(bell, "Body/DialFace/Knob").GetColumn(3).y;

            Assert.AreEqual(Quaternion.identity, knob.LocalRotation, "+Z out of the dial; Bell turns it about local Z");
            Assert.Less(new Vector2(shape.center.x, shape.center.y).magnitude, 0.005f, "pivot on its axis");
            Assert.GreaterOrEqual(shape.extents.x, ChunkyKnob, "chunky, twice the volume knob");
            Assert.GreaterOrEqual(shape.max.z, KnobProud, "stands proud of the face for 07's beam to tap");
            Assert.AreEqual(beam, height, KnobBeamReach, "at 07's beam height");
            Assert.IsFalse(placed.Intersects(dial.Mesh.Geometry.Bounds), "beside the dial, clear of its bezel");
            Assert.LessOrEqual(Mathf.Abs(placed.min.x + dial.LocalPosition.x), body.Mesh.Geometry.Bounds.max.x,
                "on the cabinet's face");
        }

        [Test]
        public void Legs_HangFromHipsUnderTheBody_WithKneesAndFeet()
        {
            ModelNode bell = BellModelBuilder.CreateBell(false);
            ModelNode body = bell.GetDescendant("Body");
            for (int i = 0; i < Corners.Length; i++)
            {
                ModelNode leg = bell.GetDescendant("Leg_" + Corners[i]);
                ModelNode shin = leg.GetDescendant("Shin_" + Corners[i]);
                float side = i % 2 == 0 ? -1f : 1f;
                float front = i < 2 ? 1f : -1f;

                Assert.AreEqual(Quaternion.identity, leg.LocalRotation, leg.Name);
                Assert.AreEqual(Quaternion.identity, shin.LocalRotation, shin.Name);
                Assert.That(body.LocalPosition.y - leg.LocalPosition.y, Is.InRange(0f, 0.03f), "hip under Body");
                Assert.Less(Mathf.Abs(leg.LocalPosition.x), 0.45f, "hip under the cabinet");
                Assert.Less(Mathf.Abs(leg.LocalPosition.z), 0.25f, "hip under the cabinet");
                Assert.Greater(leg.LocalPosition.x * side, 0f, $"{leg.Name} on its side");
                Assert.Greater(leg.LocalPosition.z * front, 0f, $"{leg.Name} at its end");
                Assert.That(-shin.LocalPosition.y, Is.InRange(0.3f, 0.45f), "knee halfway down");
                float foot = leg.LocalPosition.y + shin.LocalPosition.y + shin.Mesh.Geometry.Bounds.min.y;
                Assert.AreEqual(0f, foot, 1e-3f, "the foot is the shin's bottom end, on the ground");
                Assert.Greater(Vector3.Dot(shin.LocalPosition, new Vector3(side, 0f, front)), 0f, "splayed out");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GlowRenderers_StartDark_AndNothingGlowsCyan(bool broken)
        {
            ModelNode bell = BellModelBuilder.CreateBell(broken);
            foreach (ModelNode node in Nodes(bell).Where(node => node.Mesh != null))
            {
                List<PaletteSwatch> swatches = MeshChecks.Swatches(node.Mesh.Geometry);
                CollectionAssert.DoesNotContain(swatches, PaletteSwatch.TechGlow, node.Name);
                CollectionAssert.DoesNotContain(swatches, PaletteSwatch.EyeGlass, node.Name);
                if (GlowNodes.Contains(node.Name))
                {
                    Assert.AreEqual(ModelMaterial.PaletteGlowOff, node.Material, $"{node.Name} starts dark");
                    CollectionAssert.AreEqual(new[] { PaletteSwatch.LampGlass }, swatches, node.Name);
                }
                else
                {
                    Assert.AreEqual(Weathered(node) ? ModelMaterial.PaletteWeather : ModelMaterial.Palette,
                        node.Material, node.Name);
                }
            }
        }

        [Test]
        public void Broken_LeansBackAgainstAWall_LidOpen_OneLegFoldedUnder()
        {
            ModelNode rest = BellModelBuilder.CreateBell(false);
            ModelNode bell = BellModelBuilder.CreateBell(true);
            ModelNode body = bell.GetDescendant("Body");

            Assert.Less((body.LocalRotation * Vector3.up).z, -0.45f, "tipped back");
            Assert.Greater((body.GetDescendant("Lid").LocalRotation * Vector3.forward).y, 0.8f, "lid flopped open");
            Assert.Less((body.GetDescendant("Needle").LocalRotation * Vector3.right).y, 0f, "needle slumped");
            Assert.AreEqual(-BellModelBuilder.BrokenSink, Measure(bell).min.y, 1e-3f, "settled in the dust");

            Vector3 restBody = rest.GetDescendant("Body").LocalPosition;
            int folded = 0;
            for (int i = 0; i < Corners.Length; i++)
            {
                ModelNode leg = bell.GetDescendant("Leg_" + Corners[i]);
                Vector3 hip = rest.GetDescendant(leg.Name).LocalPosition - restBody;
                Assert.Less(Vector3.Distance(body.LocalMatrix.MultiplyPoint3x4(hip), leg.LocalPosition), 1e-4f,
                    $"{leg.Name} hangs from Body");
                Quaternion knee = leg.GetDescendant("Shin_" + Corners[i]).LocalRotation;
                float bend = Quaternion.Angle(Quaternion.identity, knee);
                if (bend > 120f)
                {
                    folded++;
                    Assert.AreEqual(BellModelBuilder.FoldedLeg, i);
                    continue;
                }

                float lowest = MeshChecks.Points(leg, Matrix4x4.identity).Min(p => p.y);
                Assert.AreEqual(-BellModelBuilder.BrokenSink, lowest, 0.005f, $"{leg.Name}'s foot rests in the dust");
            }

            Assert.AreEqual(1, folded, "one leg folded under");
        }

        [Test]
        public void Broken_SharesTheRig_ButLostAKnobAValveAndTheSpeakerCone()
        {
            ModelNode bell = BellModelBuilder.CreateBell(false);
            ModelNode broken = BellModelBuilder.CreateBell(true);

            foreach (string shared in new[] { "Lid", "DialFace", "Needle", "DialLamp", "Leg_FL", "Shin_RR" })
            {
                Assert.AreEqual(bell.GetDescendant(shared).Mesh.Name, broken.GetDescendant(shared).Mesh.Name, shared);
            }

            foreach (string changed in new[] { "Body", "Knob", "Speaker", "Antenna" })
            {
                Assert.AreNotEqual(bell.GetDescendant(changed).Mesh.Name, broken.GetDescendant(changed).Mesh.Name);
            }

            Assert.Less(broken.GetDescendant("Body").Mesh.Geometry.TriangleCount,
                bell.GetDescendant("Body").Mesh.Geometry.TriangleCount, "a valve is missing");
            Assert.Less(broken.GetDescendant("Knob").Mesh.Geometry.TriangleCount,
                bell.GetDescendant("Knob").Mesh.Geometry.TriangleCount, "the tuning knob is gone to a bare shaft");
            CollectionAssert.Contains(MeshChecks.Swatches(bell.GetDescendant("Body").Mesh.Geometry),
                PaletteSwatch.LampGlass, "her valves glow under the lid");
            foreach (PaletteSwatch swatch in MeshChecks.Swatches(broken.GetDescendant("Body").Mesh.Geometry))
            {
                Assert.IsFalse(Palette.IsEmissive(swatch), $"a dead cabinet does not glow ({swatch})");
            }

            Assert.Less(broken.GetDescendant("Speaker").Mesh.Geometry.TriangleCount,
                bell.GetDescendant("Speaker").Mesh.Geometry.TriangleCount, "the cone is gone");
        }

        [Test]
        public void Parts_AreSmall_AmberNotCyan_AndPivotedOnTheirCentreOfMass()
        {
            CollectionAssert.AreEqual(new[] { "Part_BellKnob", "Part_BellCone", "Part_BellValve" },
                BellModelBuilder.PartNames);
            foreach (string name in BellModelBuilder.PartNames)
            {
                ModelNode part = BellModelBuilder.CreatePart(name);
                LowPolyMeshBuilder geometry = part.Mesh.Geometry;
                Vector3 size = geometry.Bounds.size;

                Assert.AreEqual(name, part.Name);
                Assert.AreEqual(0, part.Children.Count);
                MeshChecks.AssertWellFormed(geometry);
                Assert.That(Mathf.Max(size.x, Mathf.Max(size.y, size.z)), Is.InRange(0.27f, 0.36f), name);
                Assert.Less(geometry.VolumeCentroid().magnitude, 1e-3f, $"{name} pivot");
                List<PaletteSwatch> swatches = MeshChecks.Swatches(geometry);
                CollectionAssert.Contains(swatches, PaletteSwatch.WarmLamp, $"{name} glints amber");
                CollectionAssert.DoesNotContain(swatches, PaletteSwatch.TechGlow, $"{name} must not read as scrap");
                CollectionAssert.DoesNotContain(swatches, PaletteSwatch.EyeGlass, name);
            }

            Assert.Throws<System.ArgumentException>(() => BellModelBuilder.CreatePart("Part_BellBell"));
        }

        [Test]
        public void Builders_AreDeterministic()
        {
            foreach (bool broken in new[] { false, true })
            {
                MeshChecks.AssertSameModel(BellModelBuilder.CreateBell(broken), BellModelBuilder.CreateBell(broken));
            }

            foreach (string name in BellModelBuilder.PartNames)
            {
                MeshChecks.AssertSameModel(BellModelBuilder.CreatePart(name), BellModelBuilder.CreatePart(name));
            }
        }

        private static void CollectParents(ModelNode node, Dictionary<string, string> parents)
        {
            foreach (ModelNode child in node.Children.Where(child => !Weathered(child)))
            {
                Assert.IsFalse(parents.ContainsKey(child.Name), $"{child.Name} appears twice");
                parents.Add(child.Name, node.Name);
                CollectParents(child, parents);
            }
        }

        /// <summary>Bell's weather skins (on her body, as on everything left alone): outside her node contract.</summary>
        private static bool Weathered(ModelNode node)
        {
            return node.Name.StartsWith("Weather_", System.StringComparison.Ordinal);
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

        private static Bounds Measure(ModelNode model)
        {
            var bounds = new Bounds();
            bool any = false;
            foreach (Vector3 p in MeshChecks.Points(model, Matrix4x4.identity))
            {
                if (any)
                {
                    bounds.Encapsulate(p);
                }
                else
                {
                    bounds = new Bounds(p, Vector3.zero);
                    any = true;
                }
            }

            return bounds;
        }

        private static int Triangles(ModelNode model)
        {
            return Nodes(model).Where(node => node.Mesh != null).Sum(node => node.Mesh.Geometry.TriangleCount);
        }

        /// <summary>The lid's front-most point, turned by <paramref name="rotation"/> about its hinge.</summary>
        private static Vector3 Front(ModelNode lid, Quaternion rotation)
        {
            Vector3 front = Vector3.zero;
            foreach (Vector3 p in lid.Mesh.Geometry.Positions)
            {
                if (p.z > front.z)
                {
                    front = p;
                }
            }

            return rotation * front;
        }

        /// <summary>The vertex farthest from the hub.</summary>
        private static Vector3 Tip(LowPolyMeshBuilder needle)
        {
            Vector3 tip = Vector3.zero;
            foreach (Vector3 p in needle.Positions)
            {
                if (new Vector2(p.x, p.y).sqrMagnitude > new Vector2(tip.x, tip.y).sqrMagnitude)
                {
                    tip = p;
                }
            }

            return tip;
        }
    }
}

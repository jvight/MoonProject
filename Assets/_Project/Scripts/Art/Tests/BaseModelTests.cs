using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>The base part of the M2 content contract: names, anchors, sockets and pivots.</summary>
    public sealed class BaseModelTests
    {
        // A base model costs at most this many triangles over its root and children (the habitat lander, riveted
        // panel by panel and carrying 07's dock, the belly lift's frame, the porthole's lamps and the lab's room, is
        // the most; the lab's stations under it have their own budget in LabModelTests).
        private const int TriangleBudget = 14000;

        // Home's three masses (the lander, the tower, the bay) keep at least this much open, lit ground between them
        // (M3-19 D9).
        private const float MassGap = 4f;

        // The removable weather skins (patchwork paint over every face, rust, dust over both) cost at most this much
        // more on top of a model: about three times the lander they cover.
        private const int WeatherBudget = 24000;

        [Test]
        public void Lander_HasAnchorsAndLampSockets_OnTheGroundPivot()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();

            Assert.AreEqual("Lander", lander.Name);
            AssertEmpty(lander, "TowerAnchor");
            Assert.AreEqual(0f, lander.GetDescendant("TowerAnchor").LocalPosition.y, 1e-5f);
            Assert.IsNull(lander.GetDescendant("ShelfAnchor"), "the memory shelf went inside, to the lab");
            for (int i = 0; i < 4; i++)
            {
                AssertEmpty(lander, $"LampSocket_{i}");
            }

            Assert.IsNull(lander.GetDescendant("LampSocket_4"));
            Assert.AreEqual(0f, lander.Mesh.Geometry.Bounds.min.y, 1e-3f, "the lander stands on its pivot");
            AssertGlowsOnly(lander.GetDescendant("Windows"));
        }

        [Test]
        public void LanderWindows_AndPorchLamps_AreEachTheirOwnDarkGlow_ToWakeOneByOne()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            ModelNode windows = lander.GetDescendant("Windows");
            var glows = new List<ModelNode> { windows };
            glows.AddRange(windows.Children);
            CollectionAssert.AreEqual(new[] { "Pane_1", "Pane_2" }, windows.Children.Select(c => c.Name));
            glows.Add(lander.GetDescendant(BaseModelBuilder.BigPortholeName));
            foreach (ModelNode socket in lander.Children.Where(c => c.Name.StartsWith("LampSocket_")))
            {
                glows.Add(socket.Children.Single(c => c.Name == BaseModelBuilder.BulbName));
            }

            foreach (ModelNode glow in glows)
            {
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material, $"{glow.Name} starts dark");
                CollectionAssert.AreEqual(new[] { PaletteSwatch.LampGlass }, MeshChecks.Swatches(glow.Mesh.Geometry),
                    glow.Name);
            }
        }

        [Test]
        public void ChartPen_DrawsAcrossThePaper_WithThePlottersOwnPenHead()
        {
            ModelNode table = BaseModelBuilder.CreateLander().GetDescendant(BaseModelBuilder.LightTableName);
            ModelNode carriage = table.GetDescendant(BaseModelBuilder.ChartPenCarriageName);
            ModelNode head = carriage.GetDescendant(BaseModelBuilder.ChartPenHeadName);
            Bounds sheet = table.GetDescendant(BaseModelBuilder.MapSheetName).Mesh.Geometry.Bounds;
            Vector3 start = carriage.LocalPosition;
            Vector3 end = start + new Vector3(BaseModelBuilder.ChartDrawWidth, BaseModelBuilder.ChartDrawHeight, 0f);
            foreach (Vector3 corner in new[] { start, end })
            {
                Assert.That(corner.x, Is.InRange(sheet.min.x, sheet.max.x), "the pen stays on the paper across");
                Assert.That(corner.y, Is.InRange(sheet.min.y, sheet.max.y), "and up");
            }

            float tip = start.z + head.Mesh.Geometry.Bounds.min.z;
            Assert.AreEqual(sheet.max.z, tip, 0.004f, "the pen's tip touches the paper");
            ModelNode plotterHead = BaseModelBuilder.CreateRoverBay().GetDescendant(BaseModelBuilder.PenHeadName);
            Assert.AreEqual(plotterHead.Mesh.Geometry.TriangleCount, head.Mesh.Geometry.TriangleCount,
                "the chart pen reuses the plotter's pen head");
            CollectionAssert.Contains(MeshChecks.Swatches(table.GetDescendant(BaseModelBuilder.ChartPenName).Mesh
                .Geometry), PaletteSwatch.Honey, "brass, the plotter's smaller sibling");
        }

        [Test]
        public void StationPads_LieFlat_InBrokenPaint_WithDarkLampsRoundTheRim()
        {
            for (int size = 0; size < BaseModelBuilder.PadRadii.Length; size++)
            {
                ModelNode pad = BaseModelBuilder.CreateStationPad(size);
                Bounds paint = pad.Mesh.Geometry.Bounds;
                Assert.Less(paint.max.y, 0.05f, $"{pad.Name} lies on the dust");
                Assert.AreEqual(BaseModelBuilder.PadRadii[size], paint.extents.x, 0.15f, $"{pad.Name}'s radius");
                MeshChecks.AssertWellFormed(pad.Mesh.Geometry);
                CollectionAssert.AreEqual(Enumerable.Range(0, 6).Select(i => "PadLamp_" + i),
                    pad.Children.Select(c => c.Name));
                foreach (ModelNode lamp in pad.Children)
                {
                    Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, $"{lamp.Name} starts dark");
                    Assert.Less(new Vector2(lamp.LocalPosition.x, lamp.LocalPosition.z).magnitude,
                        BaseModelBuilder.PadRadii[size], "inside the circle");
                }
            }
        }

        [Test]
        public void PowerCables_RunFromTheTowersFoot_OnTheDust_AndLightInOrder()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            foreach (string target in new[] { "Home", "Bay", "Chart" })
            {
                ModelNode cable = lander.GetDescendant(BaseModelBuilder.PowerCablePrefix + target);
                Bounds sheath = cable.Mesh.Geometry.Bounds;
                Assert.Less(sheath.min.y, 0.02f, $"{cable.Name} lies half sunk in the dust");
                Assert.Greater(cable.Children.Count, 1, $"{cable.Name} lights along its length");
                Vector3 previous = BaseModelBuilder.TowerAnchor;
                float link = 2.5f;
                foreach (ModelNode glow in cable.Children)
                {
                    Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material, $"{glow.Name} starts dark");
                    Vector3 centre = glow.Mesh.Geometry.Bounds.center;
                    Assert.Less(new Vector2(centre.x - previous.x, centre.z - previous.z).magnitude, link,
                        $"{cable.Name}/{glow.Name} follows on from the one before, from the tower out");
                    previous = centre;
                    link = 2.2f;
                }
            }
        }

        [Test]
        public void Home_IsThreeMasses_WithLitGroundBetween()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            Bounds hull = Footprint(lander.Mesh.Geometry.Positions.Where(p => p.y > 0.3f));
            Bounds tower = Footprint(MeshChecks.Points(BaseModelBuilder.CreateTower(3),
                Matrix4x4.Translate(BaseModelBuilder.TowerAnchor), false));
            Bounds bay = Footprint(MeshChecks.Points(BaseModelBuilder.CreateRoverBay(),
                Matrix4x4.Translate(BaseModelBuilder.WorkshopAnchor), false));
            Assert.GreaterOrEqual(hull.min.x - tower.max.x, MassGap, "lit ground between the tower and the lander");
            Assert.GreaterOrEqual(bay.min.x - hull.max.x, MassGap, "lit ground between the lander and the bay");
        }

        [Test]
        public void Tower_StaysTheTallestThingAtHome()
        {
            Bounds lander = BaseModelBuilder.CreateLander().Mesh.Geometry.Bounds;
            Assert.Greater(lander.max.y, BaseModelBuilder.LanderRoofTop, "the dish stands on the roof");
            for (int level = 1; level <= 3; level++)
            {
                float height = BaseModelBuilder.CreateTower(level).Mesh.Geometry.Bounds.max.y;
                Assert.GreaterOrEqual(height, BaseModelBuilder.LanderRoofTop * 1.25f,
                    $"L{level} stands 1.25 times the lander's height to its roof");
                Assert.Greater(height, lander.max.y, $"L{level} tops even the lander's dish");
            }
        }

        [Test]
        public void Towers_ShareAFootprint_GrowTaller_AndCarryTheBeaconOnTop()
        {
            float previousHeight = 0f;
            for (int level = 1; level <= 3; level++)
            {
                ModelNode tower = BaseModelBuilder.CreateTower(level);
                Bounds bounds = tower.Mesh.Geometry.Bounds;
                ModelNode beacon = AssertEmpty(tower, "BeaconSocket");

                Assert.AreEqual($"RadioTower_L{level}", tower.Name);
                Assert.AreEqual(0f, bounds.min.y, 1e-3f, "stands on its pivot");
                AssertFootprint(tower.Mesh.Geometry);
                Assert.Greater(bounds.max.y, previousHeight + 2f, $"L{level} is clearly taller");
                Assert.Greater(beacon.LocalPosition.y, bounds.max.y - 1.6f, "beacon near the top");
                AssertGlowsOnly(tower.GetDescendant("Lights"));
                AssertRingsClimb(tower);
                previousHeight = bounds.max.y;
            }

            Assert.Throws<System.ArgumentOutOfRangeException>(() => BaseModelBuilder.CreateTower(4));
        }

        [Test]
        public void EveryBaseMesh_IsWellFormed_AndWithinBudget()
        {
            var models = new List<ModelNode> { BaseModelBuilder.CreateLander() };
            for (int level = 1; level <= 3; level++)
            {
                models.Add(BaseModelBuilder.CreateTower(level));
            }

            foreach (ModelNode model in models)
            {
                int triangles = 0;
                int weather = 0;
                foreach (ModelNode node in new[] { model }.Concat(model.Children))
                {
                    if (node.Mesh != null)
                    {
                        MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                        if (node.Name.StartsWith("Weather_"))
                        {
                            weather += node.Mesh.Geometry.TriangleCount;
                        }
                        else
                        {
                            triangles += node.Mesh.Geometry.TriangleCount;
                        }
                    }
                }

                TestContext.WriteLine($"{model.Name}: {triangles} triangles, {weather} in its weather layers");
                Assert.That(triangles, Is.InRange(300, TriangleBudget), $"{model.Name} triangle budget");
                Assert.LessOrEqual(weather, WeatherBudget, $"{model.Name} weather layers budget");
            }
        }

        [Test]
        public void TowerStages_ShareARoverHeightServicePort_FacingThePad()
        {
            ModelNode first = BaseModelBuilder.CreateTower(1);
            for (int level = 1; level <= 3; level++)
            {
                ModelNode tower = BaseModelBuilder.CreateTower(level);
                ModelNode mouth = AssertEmpty(tower, "HopperMouth");
                ModelNode hatch = tower.GetDescendant("ServiceHatch");
                Assert.IsNotNull(hatch, $"{tower.Name} has a ServiceHatch");
                MeshChecks.AssertWellFormed(hatch.Mesh.Geometry);
                Assert.IsFalse(MeshChecks.Swatches(hatch.Mesh.Geometry).Any(Palette.IsEmissive));
                Assert.AreEqual(first.GetDescendant("HopperMouth").LocalPosition, mouth.LocalPosition, "identical");
                Assert.AreEqual(first.GetDescendant("ServiceHatch").LocalPosition, hatch.LocalPosition, "identical");
                Assert.That(mouth.LocalPosition.y, Is.InRange(0.9f, 1.5f), "07's beam reaches the hopper");
                Assert.Greater((mouth.LocalRotation * Vector3.forward).z, 0.7f, "the mouth faces the pad, tipped up");
                Bounds door = hatch.Mesh.Geometry.Bounds;
                Assert.That(hatch.LocalPosition.y + door.center.y, Is.InRange(0.3f, 1.2f), "the hatch at rover height");
                Assert.Greater(hatch.LocalPosition.z, BaseModelBuilder.TowerPlinthSize * 0.5f, "before the plinth");
            }
        }

        /// <summary>The flat (x, z) extent of <paramref name="points"/>.</summary>
        private static Bounds Footprint(IEnumerable<Vector3> points)
        {
            var bounds = new Bounds();
            bool first = true;
            foreach (Vector3 p in points)
            {
                var flat = new Vector3(p.x, 0f, p.z);
                if (first)
                {
                    bounds = new Bounds(flat, Vector3.zero);
                    first = false;
                }
                else
                {
                    bounds.Encapsulate(flat);
                }
            }

            return bounds;
        }

        private static ModelNode AssertEmpty(ModelNode model, string name)
        {
            ModelNode node = model.GetDescendant(name);
            Assert.IsNotNull(node, $"{model.Name} needs {name}");
            Assert.IsNull(node.Mesh, $"{name} must be an empty");
            return node;
        }


        [Test]
        public void Shelves_StandUnderTheBelly_OneColumnPerMaterial_EachWithADarkLamp()
        {
            ModelNode shelves = BaseModelBuilder.CreateLander().GetDescendant(BaseModelBuilder.ShelvesName);
            Assert.AreEqual(BaseModelBuilder.ShelfCount, shelves.Children.Count);
            int crates = BaseModelBuilder.ShelfMaterials.Count * BaseModelBuilder.ShelfTiers;
            foreach (ModelNode shelf in shelves.Children)
            {
                StringAssert.StartsWith(BaseModelBuilder.ShelfPrefix, shelf.Name);
                Bounds rack = shelf.Mesh.Geometry.Bounds;
                Assert.Less(rack.max.y, BaseModelBuilder.LanderBellyHeight, $"{shelf.Name} fits under the belly");
                float radius = new Vector2(shelf.LocalPosition.x, shelf.LocalPosition.z).magnitude;
                Assert.That(radius, Is.InRange(2.4f, 3.6f), $"{shelf.Name} is on the belly ring, clear of the lift");
                Assert.Greater(Mathf.Abs(shelf.LocalPosition.x), Mathf.Abs(shelf.LocalPosition.z),
                    $"{shelf.Name} keeps out of the lift's lane on Z");
                ModelNode lamp = shelf.GetDescendant(BaseModelBuilder.ShelfLampName);
                Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, $"{shelf.Name}'s lamp starts dark");
                AssertGlowsOnly(lamp);
                Assert.AreEqual(crates + 1, shelf.Children.Count, $"{shelf.Name}: its lamp and every crate slot");
                foreach (string material in BaseModelBuilder.ShelfMaterials)
                {
                    for (int tier = 0; tier < BaseModelBuilder.ShelfTiers; tier++)
                    {
                        ModelNode crate = shelf.GetDescendant($"{BaseModelBuilder.ShelfCratePrefix}{material}_{tier}");
                        Assert.IsNotNull(crate, $"{shelf.Name} has {material} tier {tier}");
                        Bounds box = crate.Mesh.Geometry.Bounds;
                        box.center += crate.LocalPosition;
                        Assert.IsTrue(rack.Contains(box.min) && rack.Contains(box.max - Vector3.up * 0.05f),
                            $"{shelf.Name}/{crate.Name} sits inside its rack");
                    }
                }
            }
        }

        /// <summary>
        /// The stage's lamps light ring by ring as power climbs (feel-pass §8.2): Lights is ring 0, Ring_1..3 sit
        /// higher and higher, each a glow renderer; the offline plaque's lamp starts dark.
        /// </summary>
        private static void AssertRingsClimb(ModelNode tower)
        {
            ModelNode lights = tower.GetDescendant(BaseModelBuilder.TowerLightsName);
            float below = lights.Mesh.Geometry.Bounds.max.y;
            for (int ring = 1; ring < BaseModelBuilder.TowerLightRings; ring++)
            {
                ModelNode node = lights.GetDescendant(BaseModelBuilder.TowerLightRingPrefix + ring);
                AssertGlowsOnly(node);
                Bounds bounds = node.Mesh.Geometry.Bounds;
                Assert.Greater(bounds.min.y, below, $"{tower.Name} ring {ring} sits above the ring under it");
                below = bounds.max.y;
            }

            ModelNode lamp = tower.GetDescendant(BaseModelBuilder.OfflineLampName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, "the offline lamp starts dark");
            Assert.IsTrue(tower.Children.Contains(tower.GetDescendant(BaseModelBuilder.OfflinePlaqueName)));
        }

        private static void AssertGlowsOnly(ModelNode node)
        {
            Assert.IsNotNull(node?.Mesh, "missing glow renderer");
            LowPolyMeshBuilder geometry = node.Mesh.Geometry;
            for (int t = 0; t < geometry.TriangleCount; t++)
            {
                Assert.IsTrue(Palette.IsEmissive(MeshChecks.SwatchOf(geometry, t)), $"{node.Name} has a dull face");
            }
        }

        /// <summary>Everything touching the ground lies on the shared square plinth.</summary>
        private static void AssertFootprint(LowPolyMeshBuilder geometry)
        {
            float half = BaseModelBuilder.TowerPlinthSize * 0.5f + 1e-3f;
            for (int v = 0; v < geometry.VertexCount; v++)
            {
                Vector3 p = geometry.Positions[v];
                if (p.y < BaseModelBuilder.TowerPlinthHeight - 1e-3f)
                {
                    Assert.LessOrEqual(Mathf.Abs(p.x), half);
                    Assert.That(p.z, Is.InRange(-half, BaseModelBuilder.TowerFootprintFront + 1e-3f));
                }
            }
        }
    }
}

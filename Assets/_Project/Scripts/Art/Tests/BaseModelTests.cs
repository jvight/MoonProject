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
        // A base model costs at most this many triangles (the lander, carrying 07's dock and the lift, is the most).
        private const int TriangleBudget = 8000;

        // The removable weather skins (patchwork paint over every face, rust, dust over both) cost at most this much
        // more on top of a model: about three times the lander they cover.
        private const int WeatherBudget = 24000;

        [Test]
        public void Lander_HasAnchorsAndLampSockets_OnTheGroundPivot()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();

            Assert.AreEqual("Lander", lander.Name);
            AssertEmpty(lander, "ShelfAnchor");
            AssertEmpty(lander, "TowerAnchor");
            Assert.AreEqual(0f, lander.GetDescendant("ShelfAnchor").LocalPosition.y, 1e-5f);
            Assert.AreEqual(0f, lander.GetDescendant("TowerAnchor").LocalPosition.y, 1e-5f);
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
            ModelNode hatch = lander.GetDescendant(BaseModelBuilder.DeckHatchName);
            glows.Add(hatch.GetDescendant(BaseModelBuilder.PortholeName));
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
        public void DeckHatch_IsACrewDoorOnItsHinge_ThatItsMotorSwingsOut()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            ModelNode hatch = lander.GetDescendant(BaseModelBuilder.DeckHatchName);
            Assert.IsTrue(lander.Children.Contains(hatch), "the hatch hangs right under the lander");
            Assert.AreEqual(BaseModelBuilder.DeckHatchShut, hatch.LocalRotation, "shut while the station sleeps");
            Bounds door = hatch.Mesh.Geometry.Bounds;
            Assert.AreEqual(0.8f, door.size.x, 0.03f, "a crew door, 0.8 m wide");
            Assert.AreEqual(1.85f, door.size.y, 0.03f, "and 1.85 m tall");
            float deck = lander.GetDescendant("LiftTop").LocalPosition.y;
            Assert.AreEqual(deck, hatch.LocalPosition.y, 0.05f, "it opens onto the deck, where the lift stops");
            Vector3 farEdge = new Vector3(door.max.x, 0f, door.max.z);
            Vector3 shut = hatch.LocalPosition + farEdge;
            Vector3 open = hatch.LocalPosition + BaseModelBuilder.DeckHatchOpen * farEdge;
            Assert.Greater(open.z, shut.z + 0.6f, "the motor swings it out onto the deck, clearing the doorway");
            Vector3 glass = hatch.GetDescendant(BaseModelBuilder.PortholeName).Mesh.Geometry.Bounds.center;
            Assert.AreEqual(1.5f, glass.y, 0.05f, "the porthole sits at a person's eye height");
            Assert.IsNotNull(hatch.GetDescendant("Weather_Dust"), "dusty while it sleeps");
        }

        [Test]
        public void LightTable_StandsInTheDoorway_AtTheLensHeight_LitFromUnderThePaper()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            ModelNode hatch = lander.GetDescendant(BaseModelBuilder.DeckHatchName);
            ModelNode table = lander.GetDescendant(BaseModelBuilder.LightTableName);
            Assert.IsTrue(lander.Children.Contains(table));
            float deck = lander.GetDescendant("LiftTop").LocalPosition.y;
            Vector3 centre = table.LocalPosition;
            Assert.That(centre.y - deck, Is.InRange(0.8f, 1.3f), "07's lens on the deck looks onto it");
            Assert.Less(Mathf.Abs(centre.x - (hatch.LocalPosition.x + 0.4f)), 0.05f, "centred in the doorway");
            Assert.Less(centre.z, hatch.LocalPosition.z, "inside, behind the hatch");
            Vector3 facing = table.LocalRotation * Vector3.forward;
            Assert.Greater(facing.z, 0.5f, "its top faces out through the doorway");
            Assert.Greater(facing.y, 0.3f, "tipped back so the lens looks down onto it");
            ModelNode glow = table.GetDescendant(BaseModelBuilder.LightTableGlowName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, glow.Material, "dark until the map wakes");
            CollectionAssert.AreEqual(new[] { PaletteSwatch.LampGlass }, MeshChecks.Swatches(glow.Mesh.Geometry));
            Bounds panel = glow.Mesh.Geometry.Bounds;
            Bounds sheet = table.GetDescendant(BaseModelBuilder.MapSheetName).Mesh.Geometry.Bounds;
            Assert.GreaterOrEqual(sheet.min.z, panel.max.z - 1e-4f, "the paper lies on the lit panel");
            Assert.Less(sheet.size.x, panel.size.x, "a rim of light shows round the paper");
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
        public void CrewsLift_ReadsJammed_AndCarriesItsRestoredParts()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            ModelNode platform = lander.GetDescendant("LiftPlatform");
            Assert.Greater(Quaternion.Angle(platform.LocalRotation, BaseModelBuilder.LiftPlatformLevel), 8f,
                "jammed, the platform hangs tilted on one cable");
            ModelNode left = lander.GetDescendant("LiftCable_L");
            Vector3 cableFoot = left.LocalPosition + Vector3.down * BaseModelBuilder.LiftCableBuiltLength;
            Vector3 shackle = platform.LocalPosition + platform.LocalRotation * new Vector3(-0.84f, 1f, -1.29f);
            Assert.Less(new Vector2(cableFoot.x - shackle.x, cableFoot.z - shackle.z).magnitude, 0.02f,
                "the left cable still hangs plumb to its shackle");
            Assert.IsFalse(lander.GetDescendant("LiftCable_R").Active, "the taut right cable waits for the repair");
            Assert.IsTrue(lander.GetDescendant(BaseModelBuilder.LiftSnappedCableName).Active,
                "the snapped one hangs and coils on the dust");
            Assert.IsTrue(platform.GetDescendant(BaseModelBuilder.LiftJamPropsName).Active,
                "the crew's out-of-order card and the cable's loose end say jammed at a glance");
            ModelNode lamp = lander.GetDescendant(BaseModelBuilder.WinchLampName);
            Assert.AreEqual(ModelMaterial.PaletteGlowOff, lamp.Material, "the winch lamp starts dark");
            Assert.That(lander.GetDescendant(BaseModelBuilder.LiftHopperMouthName).LocalPosition.y,
                Is.InRange(0.9f, 1.5f), "07's beam reaches the winch hopper");
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
            foreach (string target in new[] { "Home", "Bay", "Lift" })
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
        public void Anchors_LeaveRoomAroundTheLander()
        {
            ModelNode lander = BaseModelBuilder.CreateLander();
            Bounds hull = lander.Mesh.Geometry.Bounds;
            float shelfHalfWidth = BaseModelBuilder.ShelfWidth * 0.5f + 0.4f;

            Assert.Greater(BaseModelBuilder.ShelfAnchor.x - shelfHalfWidth, hull.max.x, "shelf clears the hull");
            Assert.Less(BaseModelBuilder.TowerAnchor.x + BaseModelBuilder.TowerPlinthSize, hull.min.x,
                "tower clears the hull");
        }

        [Test]
        public void Shelf_HasSixUprightSlots_OnItsTwoTiers()
        {
            ModelNode shelf = BaseModelBuilder.CreateShelf();
            var heights = new HashSet<float>();

            for (int i = 0; i < 6; i++)
            {
                ModelNode slot = AssertEmpty(shelf, $"Slot_{i}");
                Assert.AreEqual(Quaternion.identity, slot.LocalRotation, "slots are +Y up");
                heights.Add(slot.LocalPosition.y);
            }

            Assert.IsNull(shelf.GetDescendant("Slot_6"));
            Assert.AreEqual(2, heights.Count, "two tiers");
            Assert.AreEqual(0f, shelf.Mesh.Geometry.Bounds.min.y, 1e-3f);
            AssertGlowsOnly(shelf.GetDescendant("Lights"));
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
                previousHeight = bounds.max.y;
            }

            Assert.Throws<System.ArgumentOutOfRangeException>(() => BaseModelBuilder.CreateTower(4));
        }

        [Test]
        public void EveryBaseMesh_IsWellFormed_AndWithinBudget()
        {
            var models = new List<ModelNode> { BaseModelBuilder.CreateLander(), BaseModelBuilder.CreateShelf() };
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

        private static ModelNode AssertEmpty(ModelNode model, string name)
        {
            ModelNode node = model.GetDescendant(name);
            Assert.IsNotNull(node, $"{model.Name} needs {name}");
            Assert.IsNull(node.Mesh, $"{name} must be an empty");
            return node;
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

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
                foreach (ModelNode node in new[] { model }.Concat(model.Children))
                {
                    if (node.Mesh != null)
                    {
                        MeshChecks.AssertWellFormed(node.Mesh.Geometry);
                        triangles += node.Mesh.Geometry.TriangleCount;
                    }
                }

                Assert.That(triangles, Is.InRange(300, 6000), $"{model.Name} triangle budget");
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
                    Assert.LessOrEqual(Mathf.Abs(p.z), half);
                }
            }
        }
    }
}

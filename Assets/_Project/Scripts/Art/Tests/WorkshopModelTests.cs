using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// M3-03 art: the lander's WorkshopAnchor (Kenji's Rover Bay stands on it, clear of the lander's legs) and the
    /// Hover-Jump coils on 07.
    /// </summary>
    public sealed class WorkshopModelTests
    {
        private const string RoverPrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab";
        private const string HoverCoilsPrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/HoverCoils.prefab";

        [Test]
        public void Lander_HasTheWorkshopAnchor_WhereGameplayExpectsIt()
        {
            ModelNode anchor = BaseModelBuilder.CreateLander().GetDescendant("WorkshopAnchor");

            Assert.IsNull(anchor.Mesh);
            Assert.AreEqual(new Vector3(12.5f, 0f, -2f), anchor.LocalPosition);
            Assert.AreEqual(Quaternion.identity, anchor.LocalRotation);
        }

        [Test]
        public void RoverBay_OnItsAnchor_StandsClearOfTheLander()
        {
            float lander = BaseModelBuilder.CreateLander().Mesh.Geometry.Positions.Where(p => p.y > 0.3f)
                .Max(p => p.x);
            foreach (Vector3 p in MeshChecks.Points(BaseModelBuilder.CreateRoverBay(),
                Matrix4x4.Translate(BaseModelBuilder.WorkshopAnchor), false))
            {
                Assert.Greater(p.x, lander, $"the bay reaches the lander at {p}");
            }
        }

        [Test]
        public void HoverCoils_HaveSquashableSprings_AndGlowOffRings()
        {
            GameObject coils = MeshChecks.LoadGeneratedPrefab(HoverCoilsPrefabPath);
            string[] corners = { "FL", "FR", "RL", "RR" };
            string lampMatName = R1MaterialRecipes.Get(R1MaterialSlot.LampAmber).MaterialAssetName;

            Assert.AreEqual("HoverCoils", coils.name);
            var childNames = new List<string>();
            for (int i = 0; i < coils.transform.childCount; i++)
            {
                childNames.Add(coils.transform.GetChild(i).name);
            }

            CollectionAssert.AreEquivalent(corners.Select(c => "Coil_" + c).ToArray(), childNames);
            foreach (string corner in corners)
            {
                Transform spring = coils.transform.Find("Coil_" + corner);
                Assert.IsNotNull(spring, "Coil_" + corner);
                Mesh springMesh = spring.GetComponent<MeshFilter>()?.sharedMesh;
                Assert.IsNotNull(springMesh, $"Coil_{corner} mesh");
                Assert.LessOrEqual(springMesh.bounds.max.y, 1e-3f, "springs hang below their pivot");
                Transform glow = spring.Find("Glow_" + corner);
                Assert.IsNotNull(glow, "Glow_" + corner);
                Assert.AreEqual(lampMatName, glow.GetComponent<MeshRenderer>()?.sharedMaterial?.name);
            }
        }

        [Test]
        public void HoverCoils_OnTheCoilSocket_FitUnderTheBelly_AboveTheGround()
        {
            GameObject rover = MeshChecks.LoadGeneratedPrefab(RoverPrefabPath);
            Transform socket = rover.transform.Find("CoilSocket");
            Assert.IsNotNull(socket, "CoilSocket");
            Assert.IsNull(socket.GetComponent<Renderer>());
            Assert.AreEqual(Quaternion.identity, socket.localRotation);

            Bounds bounds = MeshChecks.PrefabBounds(HoverCoilsPrefabPath);
            Vector3 min = socket.localPosition + bounds.min;
            Vector3 max = socket.localPosition + bounds.max;
            Assert.Greater(min.x, -0.4f, "between the bogies");
            Assert.Less(max.x, 0.4f, "between the bogies");
            Assert.Greater(min.y, 0.1f, "clear of the ground");
            Assert.LessOrEqual(max.y, socket.localPosition.y + 1e-3f, "under the belly");
        }
    }
}

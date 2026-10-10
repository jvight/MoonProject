using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>M3-18 V1: grounded models get a dark foot and a dust skirt round each place they touch the ground.</summary>
    public sealed class GroundOcclusionTests
    {
        [Test]
        public void Foot_FadesTowardDustShadow_OnlyInItsLowestStretch()
        {
            Assert.AreEqual(GroundOcclusion.FootDarkness, GroundOcclusion.FootOcclusion(0f), 1e-5f);
            Assert.Greater(GroundOcclusion.FootOcclusion(GroundOcclusion.FootHeight * 0.5f), 0f);
            Assert.AreEqual(0f, GroundOcclusion.FootOcclusion(GroundOcclusion.FootHeight), 1e-5f);
            Assert.AreEqual(0f, GroundOcclusion.FootOcclusion(2f), 1e-5f);
        }

        [Test]
        public void Ground_DarkensEachMeshInRootSpace_AndRingsEveryContactWithASkirt()
        {
            var postGeometry = new LowPolyMeshBuilder();
            postGeometry.Box(Matrix4x4.Translate(new Vector3(0f, 1f, 0f)), new Vector3(0.2f, 2f, 0.2f),
                PaletteSwatch.Metal);
            var post = new ModelMesh("Post", postGeometry);
            var root = new ModelNode("Gate", Vector3.zero);
            root.Add(new ModelNode("PostL", new Vector3(-2f, 0f, 0f), post));
            var raised = new LowPolyMeshBuilder();
            raised.Box(Matrix4x4.identity, new Vector3(1f, 0.1f, 1f), PaletteSwatch.Metal);
            root.Add(new ModelNode("Beam", new Vector3(0f, 2f, 0f), new ModelMesh("Beam", raised)));
            root.Add(new ModelNode("PostR", new Vector3(2f, 0f, 0f), post));

            GroundOcclusion.Ground(root);

            for (int v = 0; v < postGeometry.VertexCount; v++)
            {
                float side = GroundOcclusion.FootOcclusion(postGeometry.Positions[v].y);
                float expected = postGeometry.Normals[v].y > 0.5f ? side * GroundOcclusion.UpFaceShare : side;
                Assert.AreEqual(expected, postGeometry.Occlusion[v].x, 1e-5f, "a post's foot darkens, its top stays");
            }

            for (int v = 0; v < raised.VertexCount; v++)
            {
                Assert.AreEqual(0f, raised.Occlusion[v].x, "nothing 2 m up is occluded by the ground");
            }

            ModelNode skirt = root.GetDescendant(GroundOcclusion.SkirtName);
            Assert.IsNotNull(skirt, "a grounded model gets its dust skirts");
            Bounds rings = skirt.Mesh.Geometry.Bounds;
            Assert.Less(rings.min.x, -2.1f, "the left post's foot is ringed");
            Assert.Greater(rings.max.x, 2.1f, "the right post's foot is ringed");
            Assert.Less(rings.size.y, 0.06f, "the skirt is a low drift, not a plinth");
            int ringsBetween = 0;
            foreach (Vector3 p in skirt.Mesh.Geometry.Positions)
            {
                ringsBetween += Mathf.Abs(p.x) < 1f ? 1 : 0;
            }

            Assert.AreEqual(0, ringsBetween, "two feet 4 m apart get two skirts, not one plinth under the gate");
        }
    }
}

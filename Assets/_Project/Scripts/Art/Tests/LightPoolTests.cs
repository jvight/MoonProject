using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Art.Tests
{
    /// <summary>M3-18 V3: the warm light-decal disc World lays under its lamps.</summary>
    public sealed class LightPoolTests
    {
        [Test]
        public void Disc_IsAFlatUnitFan_FacingUp_WithUvAcrossIt()
        {
            Mesh disc = LightPoolBuilder.CreateDisc();
            try
            {
                Assert.AreEqual(LightPoolBuilder.RimCorners * 3, disc.triangles.Length);
                Vector3[] vertices = disc.vertices;
                Vector2[] uvs = disc.uv;
                int[] triangles = disc.triangles;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = vertices[triangles[t]];
                    Vector3 normal = Vector3.Cross(vertices[triangles[t + 1]] - a, vertices[triangles[t + 2]] - a);
                    Assert.Greater(normal.y, 0f, "every triangle faces the sky");
                }

                for (int v = 0; v < vertices.Length; v++)
                {
                    Assert.AreEqual(0f, vertices[v].y, "the disc lies flat on the ground");
                    Assert.AreEqual(new Vector2(vertices[v].x, vertices[v].z), uvs[v], "uv runs -1..1 across it");
                    Assert.LessOrEqual(vertices[v].magnitude, 1f + 1e-5f, "radius 1: World scales it to the pool");
                }
            }
            finally
            {
                Object.DestroyImmediate(disc);
            }
        }

        [Test]
        public void Material_IsAFaintWarmFloor_FarUnderTheBloom()
        {
            Material material = LightPoolBuilder.CreateMaterial();
            try
            {
                Color pool = material.GetColor("_BaseColor").linear;
                Assert.Greater(pool.r, pool.b, "warm, not cool");
                Assert.Less(pool.maxColorComponent, Palette.BloomThreshold * 0.5f, "the lamp stays the bright point");
                Assert.AreEqual(Vector4.one, material.GetVector("_EmissionColor"),
                    "authored at strength 1 for World's MaterialPropertyBlock multiplier");
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}

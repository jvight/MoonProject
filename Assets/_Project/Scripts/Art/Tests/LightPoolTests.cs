using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Art.Tests
{
    /// <summary>M3-18 V3: the warm light pool World lays under its lamps, projected inside a unit prism.</summary>
    public sealed class LightPoolTests
    {
        [Test]
        public void Volume_IsAClosedPrism_RoundTheWholeUnitPool_FacingOut()
        {
            Mesh volume = LightPoolBuilder.CreateVolume();
            try
            {
                Assert.AreEqual(LightPoolBuilder.RimCorners * 12, volume.triangles.Length, "sides and both caps");
                Vector3[] vertices = volume.vertices;
                int[] triangles = volume.triangles;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = vertices[triangles[t]];
                    Vector3 b = vertices[triangles[t + 1]];
                    Vector3 c = vertices[triangles[t + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    Assert.Greater(Vector3.Dot(normal, (a + b + c) / 3f), 0f, "every face looks out of the prism");
                }

                Bounds bounds = volume.bounds;
                Assert.AreEqual(LightPoolBuilder.PoolBand, bounds.max.y, 1e-5f, "the band above the ground");
                Assert.AreEqual(-LightPoolBuilder.PoolBand, bounds.min.y, 1e-5f, "the band below the ground");
                for (int i = 0; i < LightPoolBuilder.RimCorners; i++)
                {
                    float angle = i * Mathf.PI * 2f / LightPoolBuilder.RimCorners;
                    var edge = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    float reach = 0f;
                    foreach (Vector3 vertex in vertices)
                    {
                        reach = Mathf.Max(reach, Vector3.Dot(vertex, edge));
                    }

                    Assert.GreaterOrEqual(reach, 1f - 1e-5f, "the unit pool fits inside: World scales it to a radius");
                }
            }
            finally
            {
                Object.DestroyImmediate(volume);
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
                Assert.AreEqual(LightPoolBuilder.PoolBand, material.GetFloat("_Band"), 1e-5f,
                    "the shader fades the pool over the prism's own height");
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}

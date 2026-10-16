using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies the <c>MoonProject/Art/LofiHologram</c> shader and <see cref="HologramMaterialBuilder"/> contract:
    /// cyan body (<c>#5FF3FF</c>), warm-amber Fresnel rim (<c>#FFB547</c>), invalid placement state, and bloom safety
    /// (<c>max(R, G, B) &lt; Palette.BloomThreshold</c>).
    /// </summary>
    public sealed class HologramMaterialTests
    {
        [Test]
        public void LofiHologramShader_CompilesWithoutErrors_AndStaysBelowBloomThreshold()
        {
            Shader shader = HologramMaterialBuilder.LoadShader();
            Assert.IsNotNull(shader, "LofiHologram shader must exist.");
            Assert.AreEqual(HologramMaterialBuilder.ShaderName, shader.name);
            Assert.IsTrue(shader.isSupported, "LofiHologram shader must be supported.");
            Assert.IsFalse(ShaderUtil.ShaderHasError(shader), "LofiHologram shader must compile without errors.");

            Material valid = HologramMaterialBuilder.CreateMaterial(false);
            Material invalid = HologramMaterialBuilder.CreateMaterial(true);
            try
            {
                Assert.IsTrue(valid.enableInstancing);
                AssertColorClose(Palette.Get(PaletteSwatch.TechGlow), valid.GetColor("_BaseColor"));
                AssertColorClose(Palette.Get(PaletteSwatch.WarmLamp), valid.GetColor("_RimColor"));
                AssertColorClose(Palette.Get(PaletteSwatch.AlertSoft), valid.GetColor("_InvalidColor"));
                Assert.AreEqual(0f, valid.GetFloat("_Invalid"), 1e-5f);
                Assert.AreEqual(1f, invalid.GetFloat("_Invalid"), 1e-5f);

                float ceiling = valid.GetFloat("_BloomCeiling");
                Assert.Less(ceiling, Palette.BloomThreshold,
                    "Hologram ceiling must stay strictly below Palette.BloomThreshold so the ghost never blows out bloom.");
                Assert.Less(valid.GetFloat("_BodyIntensity") * (1f + valid.GetFloat("_ScanContrast")),
                    Palette.BloomThreshold);
                Assert.Less(valid.GetFloat("_RimIntensity"), Palette.BloomThreshold);
            }
            finally
            {
                Object.DestroyImmediate(valid);
                Object.DestroyImmediate(invalid);
            }
        }

        private static void AssertColorClose(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-3f);
            Assert.AreEqual(expected.g, actual.g, 1e-3f);
            Assert.AreEqual(expected.b, actual.b, 1e-3f);
            Assert.AreEqual(expected.a, actual.a, 1e-3f);
        }
    }
}

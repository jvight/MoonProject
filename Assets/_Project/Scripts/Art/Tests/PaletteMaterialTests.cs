using System;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Art.Tests
{
    /// <summary>Emission colours of the palette and the two shared materials built from it.</summary>
    public sealed class PaletteMaterialTests
    {
        [Test]
        public void GlassSwatches_AreDarkUnlit_ButGlowTheirLampColour()
        {
            AssertDarkerThan(Palette.Get(PaletteSwatch.LampGlass), Palette.Get(PaletteSwatch.WarmLamp));
            AssertDarkerThan(Palette.Get(PaletteSwatch.EyeGlass), Palette.Get(PaletteSwatch.TechGlow));
            Assert.AreEqual(Palette.Get(PaletteSwatch.WarmLamp), Palette.GetEmission(PaletteSwatch.LampGlass));
            Assert.AreEqual(Palette.Get(PaletteSwatch.TechGlow), Palette.GetEmission(PaletteSwatch.EyeGlass));
        }

        [Test]
        public void OtherSwatches_GlowTheirOwnColour_OrNothing()
        {
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                if (swatch == PaletteSwatch.LampGlass || swatch == PaletteSwatch.EyeGlass)
                {
                    continue;
                }

                Color32 expected = Palette.IsEmissive(swatch) ? Palette.Get(swatch) : new Color32(0, 0, 0, 255);
                Assert.AreEqual(expected, Palette.GetEmission(swatch), swatch.ToString());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Materials_KeepEmissionEnabled_SoPropertyBlocksCanModulateIt(bool glowOff)
        {
            Material material = PaletteAssetBuilder.CreateMaterial(PaletteAssetBuilder.LoadShader(), null, null,
                glowOff);
            try
            {
                Assert.IsTrue(material.IsKeywordEnabled("_EMISSION"), "emission must stay compiled in");
                Assert.AreEqual(glowOff ? Color.black : Color.white, material.GetColor("_EmissionColor"));
                Assert.IsTrue(material.enableInstancing);
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        private static void AssertDarkerThan(Color32 dark, Color32 bright)
        {
            Assert.Less(dark.r + dark.g + dark.b, (bright.r + bright.g + bright.b) / 3);
        }
    }
}

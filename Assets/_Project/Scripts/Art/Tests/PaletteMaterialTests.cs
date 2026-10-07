using System;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Art.Tests
{
    /// <summary>Glow colours of the palette, the palette textures' cells and the two shared materials.</summary>
    public sealed class PaletteMaterialTests
    {
        [Test]
        public void GlassSwatches_AreDarkUnlit_ButGlowTheirLampColour()
        {
            AssertDarkerThan(Palette.Get(PaletteSwatch.LampGlass), Palette.Get(PaletteSwatch.WarmLamp));
            AssertDarkerThan(Palette.Get(PaletteSwatch.EyeGlass), Palette.Get(PaletteSwatch.TechGlow));
            AssertSameHue(Palette.GetGlow(PaletteSwatch.WarmLamp), Palette.GetGlow(PaletteSwatch.LampGlass));
            Assert.AreEqual(Palette.GetGlow(PaletteSwatch.TechGlow), Palette.GetGlow(PaletteSwatch.EyeGlass));
        }

        [Test]
        public void OtherSwatches_GlowTheirOwnColour_OrNothing()
        {
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                switch (swatch)
                {
                    case PaletteSwatch.WarmLamp:
                    case PaletteSwatch.PilotLight:
                    case PaletteSwatch.LampGlass:
                    case PaletteSwatch.TechGlow:
                    case PaletteSwatch.EyeGlass:
                        continue;
                }

                Color expected = Palette.IsEmissive(swatch) ? Palette.Linear(Palette.Get(swatch)) : Color.black;
                Assert.AreEqual(expected, Palette.GetGlow(swatch), swatch.ToString());
            }
        }

        [Test]
        public void PaletteTextures_HoldEachSwatch_InItsCell()
        {
            Color32[] surfaces = PaletteAssetBuilder.CreateBasePixels();
            Color[] glows = PaletteAssetBuilder.CreateGlowPixels();
            int width = Palette.Columns * PaletteAssetBuilder.CellPixels;
            int height = Palette.Rows * PaletteAssetBuilder.CellPixels;

            Assert.AreEqual(width * height, surfaces.Length);
            Assert.AreEqual(width * height, glows.Length);
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                Vector2 uv = Palette.Uv(swatch);
                int pixel = (int)(uv.y * height) * width + (int)(uv.x * width);
                Assert.AreEqual(Palette.GetSurface(swatch), surfaces[pixel], swatch.ToString());
                Assert.AreEqual(Palette.GetGlow(swatch), glows[pixel], swatch.ToString());
            }

            Assert.AreEqual(Color.black, glows[glows.Length - 1], "unused cells do not glow");
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
                Assert.AreEqual(glowOff ? Color.black : Color.white, material.GetColor("_EmissionColor"),
                    "authored white x 1 is the HDR glow of the emission map (the MaterialPropertyBlock contract)");
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

        private static void AssertSameHue(Color expected, Color actual)
        {
            var a = new Vector3(expected.r, expected.g, expected.b);
            var b = new Vector3(actual.r, actual.g, actual.b);
            Assert.Greater(Vector3.Dot(a.normalized, b.normalized), 0.9999f, $"{actual} is not the hue of {expected}");
        }
    }
}

using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Pillar 6, "warm points in a cold field": at intensity 1 only the living lights glow above the bloom threshold,
    /// the warm lamps above cyan and still amber once graded, while every lit surface (cream enamel first) stays under
    /// it, so from 150-300 m the base blooms into a small amber cluster and nothing else does.
    /// </summary>
    public sealed class PaletteBloomTests
    {
        /// <summary>The only swatches whose glow may cross the bloom threshold.</summary>
        private static readonly PaletteSwatch[] LivingLights =
        {
            PaletteSwatch.WarmLamp, PaletteSwatch.PilotLight, PaletteSwatch.LampGlass, PaletteSwatch.TechGlow,
            PaletteSwatch.EyeGlass,
        };

        private static readonly PaletteSwatch[] WarmLights =
        {
            PaletteSwatch.WarmLamp, PaletteSwatch.PilotLight, PaletteSwatch.LampGlass,
        };

        private static readonly PaletteSwatch[] CyanLights = { PaletteSwatch.TechGlow, PaletteSwatch.EyeGlass };

        // Lit dust sits at about 0.97: a warm lamp must clear it by a wide margin to bloom on its own.
        private const float MinWarmGlow = 1.6f;
        private const float MaxWarmGlow = 2.5f;
        private const float CyanNotch = 1.15f;

        // Per-channel tonemapping turns a glow lemon once its green climbs past this share of its red.
        private const float MaxAmberGreen = 0.25f;
        private const float SurfaceRounding = 0.01f;
        private const float HueTolerance = 0.995f;

        [Test]
        public void OnlyTheLivingLights_GlowAboveTheBloomThreshold()
        {
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                float glow = Brightness(Palette.GetGlow(swatch));
                if (LivingLights.Contains(swatch))
                {
                    Assert.Greater(glow, Palette.BloomThreshold, $"{swatch} is alive and must bloom");
                }
                else
                {
                    Assert.LessOrEqual(glow, Palette.BloomThreshold, $"{swatch} must not glow above the threshold");
                }
            }
        }

        [Test]
        public void WarmLights_GlowWellAboveLitDust_AndANotchAboveCyan()
        {
            float dimmestWarm = WarmLights.Min(swatch => Brightness(Palette.GetGlow(swatch)));
            float brightestCyan = CyanLights.Max(swatch => Brightness(Palette.GetGlow(swatch)));
            foreach (PaletteSwatch swatch in WarmLights)
            {
                float glow = Brightness(Palette.GetGlow(swatch));
                Assert.That(glow, Is.InRange(MinWarmGlow, MaxWarmGlow), swatch.ToString());
            }

            Assert.Greater(dimmestWarm, brightestCyan * CyanNotch, "warm reads as home, above cyan");
            Assert.AreEqual(WarmLights.Max(swatch => Brightness(Palette.GetGlow(swatch))),
                Brightness(Palette.GetGlow(PaletteSwatch.WarmLamp)), "the home lamps glow brightest");
        }

        [Test]
        public void WarmGlows_StayAmber_ThroughPerChannelTonemapping()
        {
            foreach (PaletteSwatch swatch in new[] { PaletteSwatch.WarmLamp, PaletteSwatch.LampGlass })
            {
                Color glow = Palette.GetGlow(swatch);
                Assert.Less(glow.g, glow.r * MaxAmberGreen, $"{swatch} would bleach to lemon");
                Assert.Less(glow.b, glow.g, $"{swatch} is a warm hue");
            }
        }

        [Test]
        public void LitSurfaces_StayUnderTheBloomThreshold()
        {
            Assert.Less(Palette.LitCeiling, Palette.BloomThreshold);
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                Color lit = Palette.Linear(Palette.GetSurface(swatch)) * Palette.ReferenceLight;
                Assert.LessOrEqual(Brightness(lit), Palette.LitCeiling + SurfaceRounding,
                    $"{swatch} lit square-on by the earthlight would bloom");
            }
        }

        [Test]
        public void Surfaces_DarkenOnlyTheBrightest_AndKeepTheirHue()
        {
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                Color albedo = Palette.Linear(Palette.Get(swatch));
                Color surface = Palette.Linear(Palette.GetSurface(swatch));
                if (Brightness(albedo * Palette.ReferenceLight) <= Palette.LitCeiling)
                {
                    Assert.AreEqual(Palette.Get(swatch), Palette.GetSurface(swatch), $"{swatch} needs no change");
                    continue;
                }

                Assert.Less(Brightness(surface), Brightness(albedo), $"{swatch} is darkened");
                var hue = new Vector3(albedo.r, albedo.g, albedo.b).normalized;
                var kept = new Vector3(surface.r, surface.g, surface.b).normalized;
                Assert.Greater(Vector3.Dot(hue, kept), HueTolerance, $"{swatch} keeps its hue");
            }

            foreach (PaletteSwatch cream in new[] { PaletteSwatch.Enamel, PaletteSwatch.Cream })
            {
                Assert.AreNotEqual(Palette.Get(cream), Palette.GetSurface(cream), $"lit {cream} would glow");
            }
        }

        private static float Brightness(Color colour)
        {
            return Mathf.Max(colour.r, Mathf.Max(colour.g, colour.b));
        }
    }
}

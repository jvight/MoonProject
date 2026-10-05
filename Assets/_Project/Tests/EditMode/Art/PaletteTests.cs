using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.Tests.EditMode.Art
{
    public sealed class PaletteTests
    {
        [Test]
        public void EverySwatch_HasAColour_AndFitsTheTexture()
        {
            int swatches = Enum.GetValues(typeof(PaletteSwatch)).Length;

            Assert.AreEqual(swatches, Palette.Count);
            Assert.LessOrEqual(swatches, Palette.Columns * Palette.Rows);
        }

        [Test]
        public void SwatchValues_AreContiguousFromZero()
        {
            var values = (int[])Enum.GetValues(typeof(PaletteSwatch));
            Array.Sort(values);

            for (int i = 0; i < values.Length; i++)
            {
                Assert.AreEqual(i, values[i]);
            }
        }

        [Test]
        public void Uvs_AreInsideTheTexture_AndUnique()
        {
            var seen = new HashSet<Vector2>();
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                Vector2 uv = Palette.Uv(swatch);
                Assert.That(uv.x, Is.InRange(0f, 1f));
                Assert.That(uv.y, Is.InRange(0f, 1f));
                Assert.IsTrue(seen.Add(uv), $"{swatch} shares a palette cell");
            }
        }
    }
}

using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Recipe of the glint materials (Glint shader): TechGlow over loose salvage, WarmLamp amber over friend parts. The
    /// Gameplay/Materials builder writes them as assets; tests build the same materials in memory.
    /// </summary>
    public static class GlintMaterials
    {
        public const string ShaderName = "MoonProject/Gameplay/Glint";

        private const int TransparentQueue = 3000;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>The cyan glint over loose salvage along a debris trail.</summary>
        public static Material CreateSalvage(Shader shader)
        {
            return Create(shader, "M_SalvageGlint", PaletteSwatch.TechGlow);
        }

        /// <summary>The amber glint over a friend's missing parts (not cyan like salvage).</summary>
        public static Material CreatePart(Shader shader)
        {
            return Create(shader, "M_PartGlint", PaletteSwatch.WarmLamp);
        }

        private static Material Create(Shader shader, string name, PaletteSwatch swatch)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            var material = new Material(shader)
            {
                name = name,
                renderQueue = TransparentQueue,
            };
            material.SetColor(ColorId, Palette.Get(swatch));
            return material;
        }
    }
}

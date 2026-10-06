using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Recipe of the scrap glint material (Glint shader, TechGlow). The Gameplay/Materials builder writes it
    /// as an asset; tests build the same material in memory.
    /// </summary>
    public static class GlintMaterials
    {
        public const string ShaderName = "MoonProject/Gameplay/Glint";

        private const int TransparentQueue = 3000;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static Material Create(Shader shader)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            var material = new Material(shader)
            {
                name = "M_ScrapGlint",
                renderQueue = TransparentQueue,
            };
            material.SetColor(ColorId, Palette.Get(PaletteSwatch.TechGlow));
            return material;
        }
    }
}

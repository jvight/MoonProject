using System;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Recipe of the glint material (Glint shader): WarmLamp amber over pickups on the ground. The Gameplay/Materials
    /// builder writes it as an asset; tests build the same material in memory.
    /// </summary>
    public static class GlintMaterials
    {
        public const string ShaderName = "MoonProject/Gameplay/Glint";

        private const int TransparentQueue = 3000;

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>The amber glint over a friend's missing parts and the other pickups on the ground.</summary>
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

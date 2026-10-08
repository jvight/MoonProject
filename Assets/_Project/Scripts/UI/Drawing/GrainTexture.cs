using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// A small tileable static-grain texture made in code: white specks of varied strength on clear, tinted and tiled
    /// by the stylesheet. Seeded, so every run draws the same grain. Allocates: make it once, destroy it with its
    /// owner.
    /// </summary>
    internal static class GrainTexture
    {
        private const uint Seed = 0x2545F491u;

        /// <param name="texels">Texels per side.</param>
        /// <param name="density">Share (0..1) of texels carrying a speck.</param>
        public static Texture2D Create(int texels, float density)
        {
            var texture = new Texture2D(texels, texels, TextureFormat.RGBA32, false, false)
            {
                name = "HopGrain",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[texels * texels];
            uint state = Seed;
            for (int i = 0; i < pixels.Length; i++)
            {
                float speck = Next01(ref state);
                float strength = Next01(ref state);
                byte alpha = speck < density ? (byte)(strength * byte.MaxValue) : (byte)0;
                pixels[i] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float Next01(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / (float)0x1000000;
        }
    }
}

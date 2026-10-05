using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Colour values of every <see cref="PaletteSwatch"/> (sRGB), whether the swatch glows, and where it lives in
    /// the generated palette texture: a <see cref="Columns"/> x <see cref="Rows"/> grid of flat cells, swatch i at
    /// column i % Columns, row i / Columns, row 0 at the bottom (UV v = 0).
    /// </summary>
    public static class Palette
    {
        public const int Columns = 8;
        public const int Rows = 4;
        private static readonly Color32[] Colors =
        {
            new Color32(0x0B, 0x0E, 0x2A, 0xFF), // SkyTop
            new Color32(0x3B, 0x2A, 0x6B, 0xFF), // SkyHorizon
            new Color32(0xA7, 0xA9, 0xCC, 0xFF), // DustLight
            new Color32(0x7C, 0x7F, 0xAE, 0xFF), // DustMid
            new Color32(0x45, 0x47, 0x7A, 0xFF), // DustShadow
            new Color32(0x6E, 0x68, 0x94, 0xFF), // RockLight
            new Color32(0x5A, 0x54, 0x80, 0xFF), // RockDark
            new Color32(0xFF, 0xB5, 0x47, 0xFF), // WarmLamp
            new Color32(0xFF, 0x8A, 0x5B, 0xFF), // WarmAccent
            new Color32(0xF4, 0xE6, 0xC8, 0xFF), // Cream
            new Color32(0x5F, 0xF3, 0xFF, 0xFF), // TechGlow
            new Color32(0x3F, 0xA7, 0xD6, 0xFF), // EarthOcean
            new Color32(0x7B, 0xD3, 0x89, 0xFF), // EarthLand
            new Color32(0x3C, 0xFF, 0xC2, 0xFF), // BiolumTeal
            new Color32(0xFF, 0x5F, 0xD2, 0xFF), // BiolumMagenta
            new Color32(0xFF, 0x5A, 0x6E, 0xFF), // AlertSoft
            new Color32(0x2B, 0x2A, 0x3A, 0xFF), // Charcoal
            new Color32(0x9A, 0x9C, 0xB0, 0xFF), // Metal
            new Color32(0x86, 0xAE, 0x9C, 0xFF), // Sage: mismatched replacement parts (old-appliance teal)
            new Color32(0xFF, 0xD2, 0x7A, 0xFF), // Honey: warm non-glowing yellow for relics and details
            new Color32(0xFF, 0xC9, 0x8A, 0xFF), // PilotLight: pale, soft warm glow for small indicator lamps
        };

        private static readonly bool[] Emissive =
        {
            false, false, false, false, false, false, false,
            true,  // WarmLamp
            false, false,
            true,  // TechGlow
            true,  // EarthOcean
            true,  // EarthLand
            true,  // BiolumTeal
            true,  // BiolumMagenta
            true,  // AlertSoft
            false, false, false, false,
            true,  // PilotLight
        };

        public static int Count => Colors.Length;

        public static Color32 Get(PaletteSwatch swatch)
        {
            return Colors[(int)swatch];
        }

        public static bool IsEmissive(PaletteSwatch swatch)
        {
            return Emissive[(int)swatch];
        }

        /// <summary>UV at the centre of the swatch's cell; every vertex of a face using the swatch gets this UV.</summary>
        public static Vector2 Uv(PaletteSwatch swatch)
        {
            int index = (int)swatch;
            return new Vector2((index % Columns + 0.5f) / Columns, (index / Columns + 0.5f) / Rows);
        }
    }
}

using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Colour values of every <see cref="PaletteSwatch"/> (sRGB), whether the swatch glows, and where it lives in
    /// the generated palette textures: a <see cref="Columns"/> x <see cref="Rows"/> grid of flat cells, swatch i at
    /// column i % Columns, row i / Columns, row 0 at the bottom (UV v = 0). Low-poly meshes paint with
    /// <see cref="GetSurface"/> and glow with <see cref="GetGlow"/> (pillar 6, "warm points in a cold field"): only
    /// the living lights cross the game's bloom threshold, warm above cyan, while every lit surface stays under it.
    /// </summary>
    public static class Palette
    {
        public const int Columns = 8;
        public const int Rows = 4;

        /// <summary>Linear brightness above which the game's bloom picks a pixel up (World's grade).</summary>
        public const float BloomThreshold = 1f;

        /// <summary>
        /// The brightest a lit surface may get under <see cref="ReferenceLight"/>: just under the bloom threshold,
        /// where the world keeps its lit dust, so cream panels and orange stripes never glow like lamps.
        /// </summary>
        public const float LitCeiling = 0.95f;

        /// <summary>
        /// Glow of the home lamps (windows, bulbs, 07's eye) at intensity 1: well above lit dust and cream, so the
        /// bloom picks out the base's lamps and nothing around them.
        /// </summary>
        public const float WarmLampGlow = 2.4f;

        /// <summary>
        /// Glow of lamp glass (part lamps, Bell's dial, valves, a tape's window): broad faces seen up close, so a
        /// little under the lamps, which keeps the dial amber instead of bleaching to lemon.
        /// </summary>
        public const float LampGlassGlow = 1.7f;

        /// <summary>Glow of the small pale pilot lamps (antenna tips, indicator rings, catchlights).</summary>
        public const float PilotLightGlow = 1.8f;

        /// <summary>Glow of the cyan tech swatches (scrap, sensor glass): a notch under the warm lamps.</summary>
        public const float CyanGlow = 1.45f;

        /// <summary>
        /// The most light a lit face receives in the game, in linear RGB: the low earthlight square-on plus the sky's
        /// ambient (World's AtmosphereSettings: colour (0.86, 0.85, 1) at intensity 1.75, ambient sky
        /// (0.17, 0.155, 0.33)). Surfaces are darkened against it, hue kept, until they sit under
        /// <see cref="LitCeiling"/>.
        /// </summary>
        public static readonly Color ReferenceLight = new Color(1.27f, 1.24f, 1.84f);

        // The game's Neutral tonemapper compresses each channel on its own, so a bright WarmLamp drifts towards lemon:
        // warm glows keep less green and blue than the swatch to still read amber once graded.
        private const float AmberGreen = 0.41f;
        private const float AmberBlue = 0.33f;
        private const float PaleGreen = 0.6f;
        private const float PaleBlue = 0.4f;

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
            new Color32(0xF6, 0xD7, 0xA7, 0xFF), // Enamel: warm cream for the rover's big panels (stays warm
                                                 // under cool moonlight, where Cream greys out)
            new Color32(0x3B, 0x30, 0x2E, 0xFF), // LampGlass: dark glass of an indicator lamp; glows WarmLamp
            new Color32(0x24, 0x2C, 0x3A, 0xFF), // EyeGlass: dark glass of a young sensor eye; glows TechGlow
            new Color32(0xC0, 0x7A, 0x4C, 0xFF), // Wood: warm caramel teak of an old radio cabinet (Bell); light
                                                 // enough to stay brown, not mauve, under moonlight
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
            false,
            true,  // LampGlass
            true,  // EyeGlass
            false, // Wood
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

        /// <summary>
        /// What the swatch glows with at intensity 1, in linear HDR RGB (its cell in the emission map; renderers scale
        /// it linearly with a MaterialPropertyBlock SetVector(_EmissionColor, (i, i, i, 1))). The living lights glow
        /// above <see cref="BloomThreshold"/>: the warm lamps strongest, then the pale pilot lamps, lamp glass and, a
        /// notch under them, cyan. The glass swatches are dark when unlit and glow another swatch's colour, so a
        /// renderer turned down to zero reads as an off lamp. The other glowing swatches (Earth, bioluminescence, the
        /// peak's alert) glow their own colour no brighter than 1; non-glowing swatches are black.
        /// </summary>
        public static Color GetGlow(PaletteSwatch swatch)
        {
            switch (swatch)
            {
                case PaletteSwatch.WarmLamp:
                    return Tint(PaletteSwatch.WarmLamp, AmberGreen, AmberBlue) * WarmLampGlow;
                case PaletteSwatch.LampGlass:
                    return Tint(PaletteSwatch.WarmLamp, AmberGreen, AmberBlue) * LampGlassGlow;
                case PaletteSwatch.PilotLight:
                    return Tint(PaletteSwatch.PilotLight, PaleGreen, PaleBlue) * PilotLightGlow;
                case PaletteSwatch.TechGlow:
                case PaletteSwatch.EyeGlass:
                    return Linear(Get(PaletteSwatch.TechGlow)) * CyanGlow;
                default:
                    return IsEmissive(swatch) ? Linear(Get(swatch)) : Color.black;
            }
        }

        /// <summary>
        /// The swatch's colour as painted on lit low-poly surfaces (the base map, sRGB): <see cref="Get"/>, darkened
        /// with its hue kept just enough that a face square-on to <see cref="ReferenceLight"/> stays under
        /// <see cref="LitCeiling"/>. Only the brightest swatches move (cream, enamel, honey, the orange accents, the
        /// lightest dust, lamp bodies); <see cref="Get"/> stays the art bible's colour for UI, sky and terrain.
        /// </summary>
        public static Color32 GetSurface(PaletteSwatch swatch)
        {
            Color albedo = Linear(Get(swatch));
            float lit = Mathf.Max(albedo.r * ReferenceLight.r,
                Mathf.Max(albedo.g * ReferenceLight.g, albedo.b * ReferenceLight.b));
            if (lit <= LitCeiling)
            {
                return Get(swatch);
            }

            float scale = LitCeiling / lit;
            return new Color32(ToSrgb(albedo.r * scale), ToSrgb(albedo.g * scale), ToSrgb(albedo.b * scale), 0xFF);
        }

        /// <summary>An sRGB swatch colour in linear RGB (the standard sRGB curve, computed in managed code).</summary>
        public static Color Linear(Color32 srgb)
        {
            return new Color(ToLinear(srgb.r), ToLinear(srgb.g), ToLinear(srgb.b));
        }

        /// <summary>UV at the centre of the swatch's cell; every vertex of a face using the swatch gets this UV.</summary>
        public static Vector2 Uv(PaletteSwatch swatch)
        {
            int index = (int)swatch;
            return new Vector2((index % Columns + 0.5f) / Columns, (index / Columns + 0.5f) / Rows);
        }

        private static Color Tint(PaletteSwatch swatch, float green, float blue)
        {
            Color colour = Linear(Get(swatch));
            return new Color(colour.r, colour.g * green, colour.b * blue);
        }

        private static float ToLinear(byte channel)
        {
            float c = channel / 255f;
            return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        private static byte ToSrgb(float linear)
        {
            float c = Mathf.Clamp01(linear);
            float srgb = c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1f / 2.4f) - 0.055f;
            return (byte)Mathf.RoundToInt(srgb * 255f);
        }
    }
}

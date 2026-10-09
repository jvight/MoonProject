namespace MoonProject.Art
{
    /// <summary>
    /// The art-bible palette (docs/VISION.md). Every low-poly face maps to exactly one swatch; the palette texture
    /// and material are generated from <see cref="Palette"/>. Append new swatches at the end only — the numeric
    /// value is the swatch's cell in the palette texture, so reordering would recolour every generated mesh.
    /// </summary>
    public enum PaletteSwatch
    {
        SkyTop = 0,
        SkyHorizon = 1,
        DustLight = 2,
        DustMid = 3,
        DustShadow = 4,
        RockLight = 5,
        RockDark = 6,
        WarmLamp = 7,
        WarmAccent = 8,
        Cream = 9,
        TechGlow = 10,
        EarthOcean = 11,
        EarthLand = 12,
        BiolumTeal = 13,
        BiolumMagenta = 14,
        AlertSoft = 15,
        Charcoal = 16,
        Metal = 17,
        Sage = 18,
        Honey = 19,
        PilotLight = 20,
        Enamel = 21,
        LampGlass = 22,
        EyeGlass = 23,
        Wood = 24,
        SignalGlass = 25,
        Rust = 26,
        FadedPaint = 27,
        CakedDust = 28,
        FadedAccent = 29,
    }
}

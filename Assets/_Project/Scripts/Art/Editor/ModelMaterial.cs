namespace MoonProject.Art.Editor
{
    /// <summary>Which shared palette material a <see cref="ModelNode"/> renders with.</summary>
    public enum ModelMaterial
    {
        /// <summary>M_LowPoly: glow as authored by the emission map.</summary>
        Palette = 0,

        /// <summary>M_LowPolyGlowOff: dark until lit with a MaterialPropertyBlock _EmissionColor.</summary>
        PaletteGlowOff = 1,

        /// <summary>M_LowPolyWeather: draws each vertex's colour (the removable weather skins), never glows.</summary>
        PaletteWeather = 2,

        /// <summary>M_ScanSolid: a scannable fragment's solid, lit like M_LowPoly, swept, paled and dissolved by its
        /// scan (0.5.5 §1).</summary>
        ScanSolid = 3,

        /// <summary>M_ScanLines: a scannable fragment's white line drawing, revealed and unthreaded by its scan.</summary>
        ScanLines = 4,
    }
}

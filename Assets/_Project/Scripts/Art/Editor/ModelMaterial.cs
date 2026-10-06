namespace MoonProject.Art.Editor
{
    /// <summary>Which shared palette material a <see cref="ModelNode"/> renders with.</summary>
    public enum ModelMaterial
    {
        /// <summary>M_LowPoly: glow as authored by the emission map.</summary>
        Palette = 0,

        /// <summary>M_LowPolyGlowOff: dark until lit with a MaterialPropertyBlock _EmissionColor.</summary>
        PaletteGlowOff = 1,
    }
}

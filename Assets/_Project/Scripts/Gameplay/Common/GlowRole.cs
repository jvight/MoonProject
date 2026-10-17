namespace MoonProject.Gameplay
{
    /// <summary>Every SoftGlow material gameplay uses (see <see cref="GlowMaterials"/> for each recipe).</summary>
    public enum GlowRole
    {
        /// <summary>07's weld beam on a structure (TechGlow, additive, flowing bands).</summary>
        WeldBeam = 4,

        /// <summary>The flash as a bundle reaches 07's hold.</summary>
        Flash = 5,

        /// <summary>A salvage piece's halo shell (TechGlow, additive, inverted hull with fresnel).</summary>
        PieceHalo = 6,

        /// <summary>A weld's sparks (WarmLamp, additive, soft round sprites).</summary>
        Spark = 11,
    }
}

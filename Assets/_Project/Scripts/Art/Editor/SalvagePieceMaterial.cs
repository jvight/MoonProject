namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The three salvage materials (docs/features/M3-13-salvage-sites.md) as Art names its nodes: what each salvage
    /// piece and bundle carries (Salvage_&lt;n&gt;_&lt;Material&gt;, Material_&lt;Material&gt;). Same names and values
    /// as Core's SalvageMaterial, which Art's editor assembly does not reference.
    /// </summary>
    public enum SalvagePieceMaterial
    {
        /// <summary>Plates, struts and hull pieces.</summary>
        Metal = 0,

        /// <summary>Cable bundles, circuit boards and junction boxes.</summary>
        Wiring = 1,

        /// <summary>Solar cells, lenses and dish panels.</summary>
        Optics = 2,
    }
}

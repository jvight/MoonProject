namespace MoonProject.Core
{
    /// <summary>What salvage yields (docs/features/M3-13). Values are saved: never renumber.</summary>
    public enum SalvageMaterial
    {
        /// <summary>Plates, struts, hull pieces.</summary>
        Metal = 0,

        /// <summary>Cable bundles, circuit boards, junction boxes.</summary>
        Wiring = 1,

        /// <summary>Solar cells, lenses, dish panels.</summary>
        Optics = 2,
    }
}

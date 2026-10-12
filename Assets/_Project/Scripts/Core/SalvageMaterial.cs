namespace MoonProject.Core
{
    /// <summary>
    /// Every material 07 can carry or home can store (docs/features/M3-13, M3-27 §8). The name stays for save
    /// compatibility; it now means "any material", not only salvage. Values are saved: append only, never renumber
    /// (and raise <see cref="SalvageMaterials.Count"/>).
    /// </summary>
    public enum SalvageMaterial
    {
        /// <summary>Plates, struts, hull pieces.</summary>
        Metal = 0,

        /// <summary>Cable bundles, circuit boards, junction boxes.</summary>
        Wiring = 1,

        /// <summary>Solar cells, lenses, dish panels.</summary>
        Optics = 2,

        /// <summary>Sealed circuit chips from intact hulls (0.7).</summary>
        CircuitChips = 3,

        /// <summary>Orange glass beads from the basin's vent field (lunar; harvestable from 0.6).</summary>
        OrangeBeads = 4,

        /// <summary>Ilmenite from the terrace strata (lunar; 0.6).</summary>
        Ilmenite = 5,

        /// <summary>Water ice from shadowed crater floors (lunar; 0.7).</summary>
        WaterIce = 6,

        /// <summary>Rare earths from deep veins (lunar; 0.8).</summary>
        RareEarths = 7,

        /// <summary>Glass refined from orange beads (refined; 0.6).</summary>
        Glass = 8,

        /// <summary>Alloy refined from ilmenite and metal (refined; 0.6).</summary>
        Alloy = 9,

        /// <summary>Coolant refined from water ice (refined; 0.7).</summary>
        Coolant = 10,

        /// <summary>Crystal grown in the Biodome (refined; 0.7).</summary>
        Crystal = 11,

        /// <summary>Superconductor refined from rare earths (refined; 0.8).</summary>
        Superconductor = 12,
    }
}

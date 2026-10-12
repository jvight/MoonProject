namespace MoonProject.Core
{
    /// <summary>
    /// A node of the tech tree that 0.5 holds (docs/design/gdd-v3.md §4.3, docs/features/M3-21): the station's nodes,
    /// built at their own ports and pads, and 07's kits, decoded in the lab and fitted in the bay. Later versions add
    /// their nodes here.
    /// </summary>
    public enum TechNodeId
    {
        /// <summary>Radio tower level 1: power to home and the lab.</summary>
        TowerL1 = 0,

        /// <summary>Radio tower level 2: power to Kenji's Rover Bay.</summary>
        TowerL2 = 1,

        /// <summary>Radio tower level 3: power to the lab's survey chart.</summary>
        TowerL3 = 2,

        /// <summary>The relay network: built with the first lit mast.</summary>
        RelayNetwork = 3,

        HoverJump = 4,

        CargoCradle = 5,

        WarmHeadlamp = 6,

        /// <summary>The Boost Coils' capacitor drums, which also buffer charge (docs/features/M3-20).</summary>
        CapacitorDrums = 7,

        /// <summary>Four cells become six; the battery spine on 07's back (docs/features/M3-20).</summary>
        CellPackII = 8,

        /// <summary>The Focus Lens: cutter tier 1 (docs/features/M3-26).</summary>
        FocusLens = 9,

        /// <summary>The Side Bins: four more slots in 07's hold (docs/features/M3-25).</summary>
        SideBins = 10,
    }
}

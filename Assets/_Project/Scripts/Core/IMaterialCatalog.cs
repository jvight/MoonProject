namespace MoonProject.Core
{
    /// <summary>
    /// What each material is (docs/features/M3-27 §8), registered in the GameContext by Gameplay from its
    /// MaterialCatalog asset: its family, how many units share one slot, and the keys for its name, its job and where
    /// it is found. Allocation-free; the keys are built once.
    /// </summary>
    public interface IMaterialCatalog
    {
        /// <summary>True when the catalog describes <paramref name="material"/> in this version.</summary>
        bool Contains(SalvageMaterial material);

        MaterialFamily FamilyOf(SalvageMaterial material);

        /// <summary>Units of <paramref name="material"/> one slot holds (4 for salvage bundles, 6 for ores).</summary>
        int StackSize(SalvageMaterial material);

        /// <summary>The localization key of its name ("material.metal.name").</summary>
        string NameKey(SalvageMaterial material);

        /// <summary>The localization key of what it is for, one plain line.</summary>
        string JobKey(SalvageMaterial material);

        /// <summary>The localization key of where it is found, for recipe rows.</summary>
        string SourceKey(SalvageMaterial material);
    }
}

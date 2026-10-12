namespace MoonProject.Core
{
    /// <summary>How units of a material were gained (docs/features/M3-27 §8). Append only.</summary>
    public enum MaterialSource
    {
        /// <summary>The beam cut a salvage piece loose.</summary>
        Cut = 0,

        /// <summary>The beam dug it out of the dust (bead patches, 0.6).</summary>
        Dig = 1,

        /// <summary>The Core Drill worked an ore vein (0.6).</summary>
        Drill = 2,

        /// <summary>A home building refined it from other materials (0.6).</summary>
        Refine = 3,

        /// <summary>A crew supply crate held it (M3-25 T4).</summary>
        Crate = 4,
    }
}

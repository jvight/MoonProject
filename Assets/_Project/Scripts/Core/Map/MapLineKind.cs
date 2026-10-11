namespace MoonProject.Core.Map
{
    /// <summary>What a <see cref="MapLine"/> of the <see cref="MapSheet"/> traces: how Ines's pencil draws it.</summary>
    public enum MapLineKind
    {
        /// <summary>A contour line at <see cref="MapLine.Height"/>.</summary>
        Contour = 0,

        /// <summary>The basin's rim crest, closed.</summary>
        RimCrest = 1,

        /// <summary>One wall of Whispering Canyon or its way out.</summary>
        CanyonWall = 2,

        /// <summary>An edge of the chasm across the canyon.</summary>
        Chasm = 3,

        /// <summary>A crater's rim, closed.</summary>
        CraterRim = 4,
    }
}

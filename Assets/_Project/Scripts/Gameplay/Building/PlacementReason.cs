namespace MoonProject.Gameplay
{
    /// <summary>
    /// Outcome of <see cref="PlacementRules"/> when checking whether 07 can place a structure at a candidate pose.
    /// </summary>
    public enum PlacementReason
    {
        /// <summary>The pose satisfies all terrain, distance, slope, and clearance rules.</summary>
        Valid = 0,

        /// <summary>Part of the footprint lies outside the drivable play area.</summary>
        OutsidePlayArea = 1,

        /// <summary>The candidate is too close to 07 (or overlaps 07's position).</summary>
        TooCloseTo07 = 2,

        /// <summary>The candidate is beyond 07's placement reach.</summary>
        TooFarFrom07 = 3,

        /// <summary>The terrain beneath the footprint is steeper than the structure's <c>MaxGroundSlope</c>.</summary>
        TooSteep = 4,

        /// <summary>The footprint + clearance overlaps an already-placed structure's footprint + clearance.</summary>
        Overlaps = 5,

        /// <summary>A world prop or collider on <c>Layers.Prop</c> blocks the structure's footprint.</summary>
        Blocked = 6,

        /// <summary>
        /// The structure is built on a towable load (<see cref="StructureDefinition.FrameLoad"/>) that does not stand
        /// on the apron, off the cable: tow it home first.
        /// </summary>
        NeedsFrame = 7,
    }
}

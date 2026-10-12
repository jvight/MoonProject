namespace MoonProject.Gameplay
{
    /// <summary>
    /// How big a salvage piece is (docs/features/M3-26 §4): it sets the piece's segments, each segment's cut time and
    /// the units it yields (<see cref="SalvageTuning"/>). Values are serialized: append only.
    /// </summary>
    public enum PieceSize
    {
        /// <summary>One short segment: the depot's first, snappy cuts.</summary>
        Small = 0,

        /// <summary>One segment: most pieces.</summary>
        Medium = 1,

        /// <summary>Two segments: a large plate.</summary>
        Large = 2,

        /// <summary>Three long segments: a hull plate, the lander's.</summary>
        Hull = 3,
    }
}

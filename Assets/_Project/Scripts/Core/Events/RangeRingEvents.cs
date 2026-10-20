namespace MoonProject.Core.Events
{
    /// <summary>
    /// The range ring was shown (<see cref="IRangeRing"/>): faint at the first charge, in full at the release. UI may
    /// draw it on 07's face for <see cref="Seconds"/>; Audio may soften the moment.
    /// </summary>
    public readonly struct RangeRingShown
    {
        public RangeRingShown(RangeRingMode mode, float seconds)
        {
            Mode = mode;
            Seconds = seconds;
        }

        public RangeRingMode Mode { get; }

        /// <summary>Seconds the ring stays on show.</summary>
        public float Seconds { get; }
    }
}

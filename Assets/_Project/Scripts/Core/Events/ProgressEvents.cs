namespace MoonProject.Core.Events
{
    /// <summary>
    /// A lamp lit in a recovery target's row (docs/features/M3-21), or the row's state was restored on load
    /// (<see cref="Restored"/> true, once per target). The recovery panel lights its lamp, the survey chart extends
    /// the Link's line, and a completed target plays its moment.
    /// </summary>
    public readonly struct RecoveryProgressChanged
    {
        public RecoveryProgressChanged(RecoveryTarget target, int litLamps, int lamps, bool restored)
        {
            Target = target;
            LitLamps = litLamps;
            Lamps = lamps;
            Restored = restored;
        }

        public RecoveryTarget Target { get; }

        public int LitLamps { get; }

        public int Lamps { get; }

        public bool Restored { get; }
    }
}

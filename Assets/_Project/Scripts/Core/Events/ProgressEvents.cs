namespace MoonProject.Core.Events
{
    /// <summary>
    /// A tech node changed state (docs/features/M3-21): a kit's blueprint decoded at the lab's reader, a kit fitted or a
    /// station node built. Published by Gameplay as it happens, and once per known node after a save is applied
    /// (<see cref="Restored"/> true: show it at once, with no moment). Art lights the board's sheet and the porthole's
    /// next lamp; Audio plays the decode chime.
    /// </summary>
    public readonly struct TechNodeChanged
    {
        public TechNodeChanged(TechNodeId node, TechNodeState state, bool restored)
        {
            Node = node;
            State = state;
            Restored = restored;
        }

        public TechNodeId Node { get; }

        public TechNodeState State { get; }

        public bool Restored { get; }
    }

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

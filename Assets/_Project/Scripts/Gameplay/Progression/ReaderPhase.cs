namespace MoonProject.Gameplay
{
    /// <summary>What the lab's reader is doing (docs/features/M3-19 §5).</summary>
    public enum ReaderPhase
    {
        /// <summary>Waiting for a tube.</summary>
        Idle = 0,

        /// <summary>07's beam feeds a tube into the slot.</summary>
        Feeding = 1,

        /// <summary>The reel spins and the tape runs: the tube is decoding.</summary>
        Decoding = 2,
    }
}

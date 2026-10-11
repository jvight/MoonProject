namespace MoonProject.Gameplay
{
    /// <summary>What one step of the lab's reader finished (docs/features/M3-19 §5).</summary>
    public enum ReaderStep
    {
        None = 0,

        /// <summary>The tube is in the slot: the reader has taken it and starts decoding.</summary>
        Taken = 1,

        /// <summary>The decode finished.</summary>
        Decoded = 2,
    }
}

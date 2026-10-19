namespace MoonProject.Core.Events
{
    /// <summary>
    /// How clearly 07 hears the call from the sky (docs/design/opening-v3-purpose.md §2): broken on reserve, locked to a
    /// bearing after the first charge, clear once the base holds power through the night, and answered once the mast
    /// has transmitted and a relay blinked the pattern back.
    /// </summary>
    public enum CallState
    {
        /// <summary>07 runs on reserve; the carrier breaks up.</summary>
        Broken = 0,

        /// <summary>The receiver has power; the carrier holds a bearing.</summary>
        Locked = 1,

        /// <summary>The base keeps 07 powered through the night; the carrier is steady.</summary>
        Clear = 2,

        /// <summary>07 transmitted from the mast and relay.1 answered.</summary>
        Answered = 3,
    }
}

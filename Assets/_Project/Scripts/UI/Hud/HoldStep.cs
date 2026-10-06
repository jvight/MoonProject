namespace MoonProject.UI
{
    /// <summary>What one step of <see cref="HoldToConfirm"/> did, for sounds and the purchase.</summary>
    internal enum HoldStep
    {
        /// <summary>Nothing new: idle, still filling, draining or holding a full ring.</summary>
        None = 0,

        /// <summary>The ring started filling (a fresh, armed press).</summary>
        Started = 1,

        /// <summary>The ring just filled: confirm now (exactly once per hold).</summary>
        Confirmed = 2,
    }
}

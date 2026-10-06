namespace MoonProject.Rover
{
    /// <summary>What a <see cref="HoverJump"/> step produced.</summary>
    public enum HoverJumpEvent
    {
        None = 0,

        /// <summary>A charge started (strength 0) or grew by one step (up to 1 at full charge).</summary>
        ChargeProgress = 1,

        /// <summary>Jump was released: leap with the eased strength.</summary>
        Leap = 2,

        /// <summary>A charge ended without a leap: 07 left the ground, the ability went away or 07 was held still.</summary>
        Cancelled = 3,
    }
}

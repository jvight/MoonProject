namespace MoonProject.Core
{
    /// <summary>What one bay of the socket rack shows (<see cref="IRackStatus"/>).</summary>
    public enum RackBayState
    {
        /// <summary>Nothing: the rack is not out yet, or 07 has not seen this part.</summary>
        Hidden = 0,

        /// <summary>The part's outline: seen, not yet earned.</summary>
        Outline = 1,

        /// <summary>The outline filled: the part is fitted on 07.</summary>
        Filled = 2,
    }
}

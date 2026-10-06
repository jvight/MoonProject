namespace MoonProject.Rover
{
    /// <summary>
    /// A drive source (<see cref="IRoverDriveSource"/>) that also holds the Hover-Jump button. While a drive source is
    /// set, 07 jumps only if it implements this; the player's Jump button counts when no drive source is set.
    /// </summary>
    public interface IRoverJumpSource
    {
        /// <summary>Holding the Hover-Jump button (same meaning as <c>InputReader.JumpHeld</c>).</summary>
        bool JumpHeld { get; }
    }
}

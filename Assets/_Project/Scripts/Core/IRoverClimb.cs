namespace MoonProject.Core
{
    /// <summary>
    /// How steep a slope 07 can climb, registered in the <see cref="GameContext"/> by the Rover domain so other
    /// domains (the range ring) read the one real limit instead of duplicating it.
    /// </summary>
    public interface IRoverClimb
    {
        /// <summary>Steepest ground (deg) 07 can drive up: past it the slope assist has faded out and it slides back.</summary>
        float ClimbLimitDegrees { get; }
    }
}

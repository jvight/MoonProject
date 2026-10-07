namespace MoonProject.Core
{
    /// <summary>
    /// How long 07 has been resting, registered in the <see cref="GameContext"/> by the Rover domain so the camera's
    /// wide shot, the soundscape and anything else that rewards stillness (VISION pillar 6) agree on one signal.
    /// Each consumer eases its own response.
    /// </summary>
    public interface IRoverStillness
    {
        /// <summary>
        /// Seconds since 07 last moved or the player last gave drive or look input; 0 while either is happening.
        /// </summary>
        float StillSeconds { get; }
    }
}

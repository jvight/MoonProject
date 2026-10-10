namespace MoonProject.Core
{
    /// <summary>Where the session is (docs/features/M3-09): the title over the live world, or play.</summary>
    public enum GameFlowState
    {
        /// <summary>The world is loaded and framed by a still camera; the rover's controls are held.</summary>
        Title = 0,

        /// <summary>The player drives 07.</summary>
        Playing = 1,
    }
}

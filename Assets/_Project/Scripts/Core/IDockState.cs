namespace MoonProject.Core
{
    /// <summary>
    /// Whether 07 rests on home's charging dock (docs/features/M3-14), registered in the GameContext by Gameplay:
    /// UI polls it to keep the docked transfer view open only while the walls are locked (0.5.5 §3, ruling B3).
    /// <see cref="Events.RoverDockChanged"/> announces each change.
    /// </summary>
    public interface IDockState
    {
        /// <summary>True while 07 rests on the dock.</summary>
        bool IsDocked { get; }
    }
}

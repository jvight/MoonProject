namespace MoonProject.Gameplay
{
    /// <summary>
    /// Whether 07's beam is at work (cut, dig, stitch, feed), registered in the GameContext for the UI: the HUD's
    /// battery arc hides meanwhile and the flank lamps carry the charge (docs/features/M3-20). Allocation-free.
    /// </summary>
    public interface IBeamStatus
    {
        /// <summary>A beam verb worked during the last frame.</summary>
        bool Working { get; }
    }
}

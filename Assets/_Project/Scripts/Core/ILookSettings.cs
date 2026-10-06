namespace MoonProject.Core
{
    /// <summary>
    /// Player-facing camera look preferences, registered in the <see cref="GameContext"/> by the Rover domain's camera
    /// rig so UI can drive them without referencing Rover. UI owns persistence: it applies saved values on load and
    /// whenever the player changes them in the pause menu.
    /// </summary>
    public interface ILookSettings
    {
        /// <summary>
        /// Multiplier on the tuned mouse sensitivity and gamepad stick rate (1 = as tuned). Takes effect immediately.
        /// </summary>
        float Sensitivity { get; set; }

        /// <summary>True when pushing up tilts the view down. Takes effect immediately.</summary>
        bool InvertY { get; set; }
    }
}

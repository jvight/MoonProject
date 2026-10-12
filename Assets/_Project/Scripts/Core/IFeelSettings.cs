namespace MoonProject.Core
{
    /// <summary>
    /// Player-facing feel preferences (docs/design/feel-pass.md §4), registered in the <see cref="GameContext"/> by
    /// the Rover domain, which plays the gamepad rumble. UI owns persistence: it applies saved values on load and
    /// whenever the player changes them in the settings, as with <see cref="ILookSettings"/> (which keeps the Gentle
    /// camera).
    /// </summary>
    public interface IFeelSettings
    {
        /// <summary>
        /// True to let beats rumble the gamepad. On by default (owner, 2026-10-12). Turning it off stops any rumble
        /// at once.
        /// </summary>
        bool RumbleEnabled { get; set; }

        /// <summary>
        /// Multiplier 0..1 on every rumble profile (1 = as tuned), applied below the hard motor cap of 0.35. Takes
        /// effect immediately.
        /// </summary>
        float RumbleStrength { get; set; }
    }
}

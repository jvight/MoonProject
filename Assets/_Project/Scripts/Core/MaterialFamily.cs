namespace MoonProject.Core
{
    /// <summary>Where a material comes from (docs/features/M3-27 §8). Values are saved: append only.</summary>
    public enum MaterialFamily
    {
        /// <summary>Cut from the crew's wrecks and stations (Metal, Wiring, Optics).</summary>
        Salvage = 0,

        /// <summary>Gathered from the Moon itself: dug, drilled or melted out.</summary>
        Lunar = 1,

        /// <summary>Made at home from other materials, with no input from the player.</summary>
        Refined = 2,
    }
}

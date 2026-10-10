namespace MoonProject.Gameplay
{
    /// <summary>Where a tower story part waits for 07 (docs/features/M3-16 P1).</summary>
    public enum TowerPartSource
    {
        /// <summary>In a salvage site's heart, once the heart is revealed.</summary>
        SiteHeart = 0,

        /// <summary>At the first relay mast 07 restores, on its pad.</summary>
        FirstRestoredMast = 1,
    }
}

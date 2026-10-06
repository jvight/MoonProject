namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The six lost memories of the vertical slice (ids from the M2 content contract). Order is the catalog order.
    /// The walkman and the bath duck are buried close to home so the first pings answer; the golden record waits at
    /// the rim with a view of The Peak, where the last broadcast will one day be sent from. Answer notes climb the
    /// D major pentatonic ladder from D5 (0 = D5, 1 = E5, 2 = F#5, 3 = A5, 4 = B5, 5 = D6). Names and memory texts
    /// live in the localization tables (relic.&lt;id&gt;.name, relic.&lt;id&gt;.memory).
    /// </summary>
    internal static class RelicRecipes
    {
        public static readonly RelicRecipe[] All =
        {
            new RelicRecipe("cassette_player", 6f, 3, RelicPlacementBand.Onboarding),
            new RelicRecipe("rubber_duck", 3f, 5, RelicPlacementBand.Onboarding),
            new RelicRecipe("golden_record", 5f, 0, RelicPlacementBand.RimView),
            new RelicRecipe("astronaut_boot", 9f, 1, RelicPlacementBand.Wanderer),
            new RelicRecipe("teapot", 7f, 4, RelicPlacementBand.Wanderer),
            new RelicRecipe("garden_gnome", 14f, 2, RelicPlacementBand.Wanderer),
        };
    }
}

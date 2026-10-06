namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The six lost memories of the vertical slice (ids from the M2 content contract). Order is the catalog order.
    /// The walkman and the bath duck are buried close to home so the first pings answer; the golden record waits at
    /// the rim with a view of The Peak, where the last broadcast will one day be sent from. Answer notes climb the
    /// D major pentatonic ladder from D5 (0 = D5, 1 = E5, 2 = F#5, 3 = A5, 4 = B5, 5 = D6).
    /// </summary>
    internal static class RelicRecipes
    {
        public static readonly RelicRecipe[] All =
        {
            new RelicRecipe("cassette_player", "Mixtape Walkman",
                "A walkman with a mixtape still inside, labelled \"for the long drive\" in careful pen. " +
                "Someone made this for someone they loved.",
                6f, 3, RelicPlacementBand.Onboarding),
            new RelicRecipe("rubber_duck", "Bath Duck",
                "A yellow bath duck, sun-faded on one side. It bobbed through a thousand bedtime baths " +
                "before it floated all the way up here.",
                3f, 5, RelicPlacementBand.Onboarding),
            new RelicRecipe("golden_record", "Golden Record",
                "A golden disc of greetings in many languages, sent outward to anyone listening. " +
                "It came to rest on the moon instead, still waiting to be heard.",
                5f, 0, RelicPlacementBand.RimView),
            new RelicRecipe("astronaut_boot", "Moonwalker's Boot",
                "One boot, left behind, its tread still sharp with dust. Whoever wore it stood here once " +
                "and looked up at home, just like you.",
                9f, 1, RelicPlacementBand.Wanderer),
            new RelicRecipe("teapot", "Enamel Teapot",
                "A dented enamel teapot that still smells faintly of bergamot. Somewhere, a kitchen " +
                "is missing its slow Sunday afternoons.",
                7f, 4, RelicPlacementBand.Wanderer),
            new RelicRecipe("garden_gnome", "Wandering Gnome",
                "A garden gnome with a chipped red hat, once borrowed as a joke and photographed all " +
                "over the world. This is the farthest anyone ever took him.",
                14f, 2, RelicPlacementBand.Wanderer),
        };
    }
}

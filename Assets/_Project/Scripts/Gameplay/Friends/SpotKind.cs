namespace MoonProject.Gameplay
{
    /// <summary>What a spotter friend can notice, most interesting first.</summary>
    public enum SpotKind
    {
        /// <summary>A relic still in the ground that has not answered yet.</summary>
        Relic = 0,

        /// <summary>A friend's missing part, not yet picked up.</summary>
        Part = 1,

        /// <summary>A cluster of scrap.</summary>
        Scrap = 2,
    }
}

namespace MoonProject.Gameplay
{
    /// <summary>What a spotter friend can notice, most interesting first.</summary>
    public enum SpotKind
    {
        /// <summary>A salvage site with something left to find that has not answered yet.</summary>
        Site = 0,

        /// <summary>A friend's missing part, not yet picked up.</summary>
        Part = 1,
    }
}

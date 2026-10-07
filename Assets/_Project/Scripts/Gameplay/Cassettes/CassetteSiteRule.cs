namespace MoonProject.Gameplay
{
    /// <summary>How a cassette's spot is chosen. Serialized in content assets: append, never renumber.</summary>
    public enum CassetteSiteRule
    {
        /// <summary>At a world anchor (a canyon ledge, beside the crew log cache).</summary>
        Anchor = 0,

        /// <summary>On the basin floor, at a spot the <see cref="CassetteSitePlanner"/> picks.</summary>
        BasinPlanner = 1,
    }
}

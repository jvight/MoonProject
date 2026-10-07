namespace MoonProject.Gameplay
{
    /// <summary>How a friend's site and parts are chosen. Serialized in content: append, never renumber.</summary>
    public enum FriendSiteRule
    {
        /// <summary>A spot the <see cref="FriendSitePlanner"/> picks on the basin floor (Tilly's crater).</summary>
        Planner = 0,

        /// <summary>At the World's anchors (Bell at the canyon terminus, her parts in its alcoves).</summary>
        Anchors = 1,
    }
}

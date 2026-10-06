namespace MoonProject.Gameplay
{
    /// <summary>Life of a friend. Saved as an int: append new states, never renumber.</summary>
    public enum FriendState
    {
        /// <summary>
        /// Lying broken at its site, none of its parts found yet; answers the sonar with a broken chirp.
        /// </summary>
        Dormant = 0,

        /// <summary>Some (or all) of its missing parts are gathered; still broken, still answering.</summary>
        PartsGathering = 1,

        /// <summary>07's beam is stitching it back together and it is booting up.</summary>
        Repairing = 2,

        /// <summary>Repaired: it follows 07 on trips and lives at the base.</summary>
        Awake = 3,
    }
}

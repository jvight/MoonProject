namespace MoonProject.Gameplay
{
    /// <summary>Which body a friend has (its rig, repair beat and life). Serialized: append, never renumber.</summary>
    public enum FriendBodyKind
    {
        /// <summary>A small hover-drone (Tilly): flies, follows 07, perches ("Contract: friend Tilly").</summary>
        Drone = 0,

        /// <summary>A radio cabinet on legs (Bell): stands, waddles, dances ("Contract: friend Bell").</summary>
        RadioCabinet = 1,
    }
}

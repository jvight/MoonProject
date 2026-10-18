namespace MoonProject.Gameplay
{
    /// <summary>Why a held E does nothing on the target it is on (<see cref="InteractionBlocked"/>).</summary>
    public enum BlockReason
    {
        None = 0,

        /// <summary>07 is stopped and aimed, but its beam cannot see the target: something stands in the way.</summary>
        NoLineOfSight = 1,
    }
}

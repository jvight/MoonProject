namespace MoonProject.Gameplay
{
    /// <summary>Where a tower component is in its journey (docs/features/M3-16 P1).</summary>
    public enum TowerPartState
    {
        /// <summary>Where it lies, not yet picked up.</summary>
        Waiting = 0,

        /// <summary>Riding on 07's back.</summary>
        Carried = 1,

        /// <summary>Fed to the tower's port: its level can be built.</summary>
        Delivered = 2,
    }
}

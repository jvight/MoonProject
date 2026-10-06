namespace MoonProject.Gameplay
{
    /// <summary>A place in the world where an upgrade is sold (the radio tower's pad).</summary>
    public interface IUpgradeStation
    {
        UpgradeDefinition Definition { get; }

        /// <summary>True while 07 is parked there.</summary>
        bool Occupied { get; }
    }
}

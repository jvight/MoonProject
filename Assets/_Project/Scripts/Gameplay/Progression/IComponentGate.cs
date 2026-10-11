namespace MoonProject.Gameplay
{
    /// <summary>The tower's components as the shop sees them (docs/features/M3-16 P1, M3-21 Â§6).</summary>
    public interface IComponentGate
    {
        /// <summary>
        /// True when level <paramref name="level"/> of <paramref name="upgradeId"/> waits for a component not yet fed
        /// to the tower's port.
        /// </summary>
        bool NeedsPart(string upgradeId, int level);

        /// <summary>
        /// The component (Art's model id, e.g. Part_TowerCoil) level <paramref name="level"/> of
        /// <paramref name="upgradeId"/> still waits for at the tower's port, or null when it waits for none.
        /// </summary>
        string NeededComponent(string upgradeId, int level);
    }
}

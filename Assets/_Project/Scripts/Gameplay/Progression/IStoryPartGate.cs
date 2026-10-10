namespace MoonProject.Gameplay
{
    /// <summary>The tower's story parts as the shop sees them (docs/features/M3-16 P1).</summary>
    public interface IStoryPartGate
    {
        /// <summary>
        /// True when level <paramref name="level"/> of <paramref name="upgradeId"/> waits for a story part not yet fed
        /// to the tower's port.
        /// </summary>
        bool NeedsPart(string upgradeId, int level);
    }
}

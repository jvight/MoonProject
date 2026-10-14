namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's blueprint board (docs/features/M3-16): one sheet per kit the bay can build, pinned once found. UI lists
    /// the bay's kits with it; <see cref="Core.Events.BlueprintCompleted"/> tells when a sheet is pinned.
    /// </summary>
    public interface IBlueprintBoard
    {
        /// <summary>True when <paramref name="upgradeId"/> may be built (pinned, or not a kit of the board).</summary>
        bool IsPinned(string upgradeId);
    }
}

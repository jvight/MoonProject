namespace MoonProject.Gameplay
{
    /// <summary>
    /// What each of the opening's fixed sources is for (opening-v3 §4, E3): the structure its items are spent on, so
    /// the pickup notice can say "Metal 4 · for the charging pad". Registered in the GameContext by Gameplay.
    /// </summary>
    public interface ISupplyPurposes
    {
        /// <summary>
        /// The structure source <paramref name="sourceId"/> (a <c>MaterialSalvaged.SiteId</c> from a crate) is for;
        /// false when it is no source of the opening.
        /// </summary>
        bool TryPurposeOf(string sourceId, out string structureId);
    }
}

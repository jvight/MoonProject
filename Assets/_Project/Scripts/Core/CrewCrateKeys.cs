using System;

namespace MoonProject.Core
{
    /// <summary>
    /// Localization keys of the crew supply crates (docs/features/M3-25 T4). A crate is keyed by its site, not its
    /// crew member (Kenji packed two): "crate.depot.note" for site.depot.
    /// </summary>
    public static class CrewCrateKeys
    {
        private const string Prefix = "crate.";
        private const string NoteSuffix = ".note";

        /// <summary>
        /// The packing note's key for the crate at <paramref name="siteId"/> ("site.depot" or "depot"). Builds a string:
        /// call it when the crate opens, not every frame.
        /// </summary>
        public static string Note(string siteId)
        {
            if (string.IsNullOrEmpty(siteId))
            {
                throw new ArgumentException("A crate's note needs its site id.", nameof(siteId));
            }

            string site = siteId.StartsWith(WorldAnchorIds.SitePrefix, StringComparison.Ordinal)
                ? siteId.Substring(WorldAnchorIds.SitePrefix.Length)
                : siteId;
            return Prefix + site + NoteSuffix;
        }
    }
}

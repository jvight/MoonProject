using System;

namespace MoonProject.UI
{
    /// <summary>
    /// The credits (Assets/_Project/UI/Credits/credits.json): the one source of truth for what the credits screen
    /// shows. Prose is a localization key; names, licences, copyright lines and links are plain text.
    /// </summary>
    [Serializable]
    public sealed class CreditsData
    {
        public CreditsSection[] sections = Array.Empty<CreditsSection>();
    }

    /// <summary>A heading and its entries.</summary>
    [Serializable]
    public sealed class CreditsSection
    {
        /// <summary>The localization key of the section's heading.</summary>
        public string headingKey = string.Empty;

        public CreditsEntry[] entries = Array.Empty<CreditsEntry>();
    }

    /// <summary>One credited thing: a name (or a localized line), what it is licensed under, and a note.</summary>
    [Serializable]
    public sealed class CreditsEntry
    {
        /// <summary>The name as it is written, never translated ("Nunito Sans"); empty when <see cref="nameKey"/> is set.</summary>
        public string name = string.Empty;

        /// <summary>A localized line standing in for a name ("Made by ..."); empty when <see cref="name"/> is set.</summary>
        public string nameKey = string.Empty;

        /// <summary>The licence as it is written ("SIL Open Font License 1.1", "CC BY 4.0").</summary>
        public string detail = string.Empty;

        /// <summary>A localized licence line ("public domain"); used when <see cref="detail"/> is empty.</summary>
        public string detailKey = string.Empty;

        /// <summary>A copyright line or a link, as written.</summary>
        public string note = string.Empty;

        /// <summary>For a font: the file prefix in UI/Fonts ("NunitoSans" for NunitoSans-Regular.ttf).</summary>
        public string family = string.Empty;
    }
}

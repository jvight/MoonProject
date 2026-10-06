using System.Collections.Generic;

namespace MoonProject.Core
{
    /// <summary>
    /// Every player-facing string, by stable dotted key ("ui.pause.resume", "hint.excavate", "relic.&lt;id&gt;.name"),
    /// in the language the player chose. Registered in the <see cref="GameContext"/> by the UI domain, which owns the
    /// string tables (Assets/_Project/Data/Localization) and saves the choice. English ("en") is the primary language:
    /// the source of every key and the default. A change of language is published as
    /// <see cref="Events.LanguageChanged"/>.
    /// </summary>
    public interface ILocalization
    {
        /// <summary>Code of the language in use, e.g. "en" or "vi".</summary>
        string Language { get; }

        /// <summary>Codes of every language the player can choose, the primary language first.</summary>
        IReadOnlyList<string> Languages { get; }

        /// <summary>The name of <paramref name="language"/> in that language ("English", "Tiếng Việt").</summary>
        string GetLanguageName(string language);

        /// <summary>
        /// The text of <paramref name="key"/> in the current language; never allocates. A key missing from the tables
        /// is a content bug: it is logged once and the key itself is returned so it shows up on screen.
        /// </summary>
        string Get(string key);

        /// <summary>Switches every string to <paramref name="language"/> (one of <see cref="Languages"/>).</summary>
        void SetLanguage(string language);
    }
}

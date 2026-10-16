using MoonProject.Editor.Builders;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Where the UI builders read and write: hand-written UXML/USS and fonts under Assets/_Project/UI, tuning
    /// (create-if-missing) and string tables under Data, generated theme, palette, font assets and PanelSettings under
    /// Generated/UI.
    /// </summary>
    internal static class UiAssetPaths
    {
        public const string SourceFolder = "Assets/_Project/UI";
        public const string Uxml = SourceFolder + "/GameUI.uxml";

        /// <summary>The credits (one source of truth) and the full licence texts beside it.</summary>
        public const string Credits = SourceFolder + "/Credits/credits.json";
        public const string FontFolder = SourceFolder + "/Fonts";

        public const string TuningFolder = "Assets/_Project/Data/Tuning/UI";
        public const string Tuning = TuningFolder + "/UiTuning.asset";
        public const string MapViewTuning = TuningFolder + "/MapViewTuning.asset";

        public const string LocalizationFolder = "Assets/_Project/Data/Localization";
        public const string PrimaryLanguage = "en";

        public const string RelicCatalog = "Assets/_Project/Data/Content/RelicCatalog.asset";

        public const string GeneratedFolder = GeneratedAssets.Root + "/UI";
        public const string PanelSettings = GeneratedFolder + "/LofiLunarPanel.asset";

        /// <summary>The approved Lunarion wordmark (v2, cream-on-night variant), the source of the title's mark.</summary>
        public const string WordmarkSource = SourceFolder + "/Wordmark/LunarionWordmark.svg";

        /// <summary>The wordmark's path commands, parsed from <see cref="WordmarkSource"/> by UI/Wordmark.</summary>
        public const string Wordmark = GeneratedFolder + "/Wordmark.asset";
        public const string Theme = GeneratedFolder + "/LofiLunarTheme.tss";
        public const string Palette = GeneratedFolder + "/Palette.uss";
        public const string GeneratedFontFolder = GeneratedFolder + "/Fonts";

        /// <summary>The Moonlight Glass textures (UI/Glass builder), drawn at 2x.</summary>
        public const string GlassFolder = GeneratedFolder + "/Glass";
        public const string GlassPanel = GlassFolder + "/GlassPanel.png";
        public const string GlassPill = GlassFolder + "/GlassPill.png";
        public const string GlassBubble = GlassFolder + "/GlassBubble.png";
        public const string GlassDial = GlassFolder + "/GlassDial.png";

        /// <summary>Text: body, buttons, toasts (Moonlight Glass; file names NunitoSans-&lt;style&gt;.ttf).</summary>
        public const string TextFamily = "NunitoSans";

        /// <summary>Numbers and titles: dials, the meter, names, headings (file names Exo2-&lt;style&gt;.ttf).</summary>
        public const string NumberFamily = "Exo2";

        /// <summary>Every font the UI builds and uses, as family and style.</summary>
        public static readonly string[][] Fonts =
        {
            new[] { TextFamily, "Regular" },
            new[] { TextFamily, "SemiBold" },
            new[] { TextFamily, "Bold" },
            new[] { NumberFamily, "Medium" },
            new[] { NumberFamily, "SemiBold" },
            new[] { NumberFamily, "Bold" },
        };

        public static string SourceFont(string family, string style)
        {
            return FontFolder + "/" + family + "-" + style + ".ttf";
        }

        public static string FontAsset(string family, string style)
        {
            return GeneratedFontFolder + "/" + family + "-" + style + ".asset";
        }

        public static string StringTable(string language)
        {
            return LocalizationFolder + "/" + language + ".json";
        }
    }
}

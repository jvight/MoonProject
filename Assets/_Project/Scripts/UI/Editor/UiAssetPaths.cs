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
        public const string FontFolder = SourceFolder + "/Fonts";

        public const string TuningFolder = "Assets/_Project/Data/Tuning/UI";
        public const string Tuning = TuningFolder + "/UiTuning.asset";

        public const string LocalizationFolder = "Assets/_Project/Data/Localization";
        public const string PrimaryLanguage = "en";

        public const string RelicCatalog = "Assets/_Project/Data/Content/RelicCatalog.asset";

        public const string GeneratedFolder = GeneratedAssets.Root + "/UI";
        public const string PanelSettings = GeneratedFolder + "/LofiLunarPanel.asset";
        public const string Theme = GeneratedFolder + "/LofiLunarTheme.tss";
        public const string Palette = GeneratedFolder + "/Palette.uss";
        public const string GeneratedFontFolder = GeneratedFolder + "/Fonts";

        /// <summary>Body text weight (file names BeVietnamPro-&lt;style&gt;.ttf).</summary>
        public const string RegularStyle = "Regular";

        /// <summary>Buttons, chips and prompt words.</summary>
        public const string MediumStyle = "Medium";

        /// <summary>Headings, names and the wordmark.</summary>
        public const string SemiBoldStyle = "SemiBold";

        public static string SourceFont(string style)
        {
            return FontFolder + "/BeVietnamPro-" + style + ".ttf";
        }

        public static string FontAsset(string style)
        {
            return GeneratedFontFolder + "/BeVietnamPro-" + style + ".asset";
        }

        public static string StringTable(string language)
        {
            return LocalizationFolder + "/" + language + ".json";
        }
    }
}

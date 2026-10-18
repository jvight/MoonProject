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

        public const string LocalizationFolder = "Assets/_Project/Data/Localization";
        public const string PrimaryLanguage = "en";

        public const string StructureCatalog = "Assets/_Project/Data/Content/StructureCatalog.asset";

        public const string GeneratedFolder = GeneratedAssets.Root + "/UI";
        public const string PanelSettings = GeneratedFolder + "/LofiLunarPanel.asset";
        public const string FacePanelSettings = GeneratedFolder + "/LofiLunarFacePanel.asset";
        public const string FaceRenderTexture = GeneratedFolder + "/LofiLunarFaceTarget.asset";

        /// <summary>The approved Lunarion wordmark (v2, cream-on-night variant), the source of the title's mark.</summary>
        public const string WordmarkSource = SourceFolder + "/Wordmark/LunarionWordmark.svg";

        /// <summary>The wordmark's path commands, parsed from <see cref="WordmarkSource"/> by UI/Wordmark.</summary>
        public const string Wordmark = GeneratedFolder + "/Wordmark.asset";

        /// <summary>The 25 production vector icons (item_*.svg and kit_*.svg), parsed by UI/Icons.</summary>
        public const string IconsFolder = SourceFolder + "/Icons";

        /// <summary>The parsed 64x64 UI vector icons asset, built by UI/Icons.</summary>
        public const string Icons = GeneratedFolder + "/UiIcons.asset";

        /// <summary>The generated 07 v3 rover model prefab whose socket transforms feed <see cref="RoverDiagram"/>.</summary>
        public const string RoverModelPrefab = GeneratedAssets.Root + "/Art/R1/Models/Rover/RoverModel.prefab";

        /// <summary>Argon's art for 07's face screen: the line drawing, its leader anchors and the face icons.</summary>
        public const string FaceArtFolder = "Assets/_Project/Art/UI/Face";

        /// <summary>07's three-quarter line drawing (2048x2048, clear around cream lines).</summary>
        public const string FaceArt = FaceArtFolder + "/rover_07_line_3q.png";

        /// <summary>The leader end points on the drawings (u, v with a top-left origin).</summary>
        public const string FaceAnchors = FaceArtFolder + "/rover_07_anchors.json";

        /// <summary>07 v3's socket positions and icons for the Tab screen, built by UI/Rover Diagram.</summary>
        public const string RoverDiagram = GeneratedFolder + "/RoverDiagram.asset";
        public const string Theme = GeneratedFolder + "/LofiLunarTheme.tss";
        public const string Palette = GeneratedFolder + "/Palette.uss";
        public const string GeneratedFontFolder = GeneratedFolder + "/Fonts";

        /// <summary>The CRT panel and scanline textures (UI/Glass builder), drawn at 2x.</summary>
        public const string GlassFolder = GeneratedFolder + "/Glass";
        public const string GlassPanel = GlassFolder + "/GlassPanel.png";
        public const string GlassPill = GlassFolder + "/GlassPill.png";
        public const string GlassBubble = GlassFolder + "/GlassBubble.png";
        public const string GlassDial = GlassFolder + "/GlassDial.png";

        /// <summary>256x256 three-quarter structure icons rendered by UI/StructureIcons.</summary>
        public const string StructureIconFolder = GeneratedFolder + "/StructureIcons";

        public static string StructureIcon(string id)
        {
            return StructureIconFolder + "/" + id + ".png";
        }

        /// <summary>Text: body, buttons, toasts, terminal readouts (file names IBMPlexMono-&lt;style&gt;.ttf).</summary>
        public const string TextFamily = "IBMPlexMono";

        /// <summary>Numbers and titles: dials, the meter, names, headings (file names ChakraPetch-&lt;style&gt;.ttf).</summary>
        public const string NumberFamily = "ChakraPetch";

        /// <summary>Crew documents and paper notes (file names BeVietnamPro-&lt;style&gt;.ttf).</summary>
        public const string PaperFamily = "BeVietnamPro";

        /// <summary>Every font the UI builds and uses, as family and style.</summary>
        public static readonly string[][] Fonts =
        {
            new[] { TextFamily, "Regular" },
            new[] { TextFamily, "Medium" },
            new[] { TextFamily, "SemiBold" },
            new[] { TextFamily, "Bold" },
            new[] { NumberFamily, "Medium" },
            new[] { NumberFamily, "SemiBold" },
            new[] { NumberFamily, "Bold" },
            new[] { PaperFamily, "Regular" },
            new[] { PaperFamily, "Medium" },
            new[] { PaperFamily, "SemiBold" },
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

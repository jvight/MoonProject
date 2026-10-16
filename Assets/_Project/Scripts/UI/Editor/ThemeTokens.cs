using System.Globalization;
using System.Text;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// The UI colour tokens, written as USS custom properties on :root so every stylesheet reads colours from here and
    /// never from a hex literal: 07's two-colour amber CRT terminal ("Terminal hai màu", Variant C) paired with a calm
    /// cyan telemetry accent (PaletteSwatch.TechGlow), warm coral shortages (PaletteSwatch.WarmAccent), and a warm
    /// off-white paper surface for crew documents.
    /// </summary>
    internal static class ThemeTokens
    {
        // Declared before Tokens: static fields initialise in order, so the table must see these set.
        private static readonly Color32 CrtBright = new Color32(255, 214, 128, 255);
        private static readonly Color32 CrtText = new Color32(255, 194, 92, 255);
        private static readonly Color32 CrtTextDim = new Color32(232, 174, 82, 255);
        private static readonly Color32 CrtTextFaint = new Color32(204, 150, 68, 255);
        private static readonly Color32 CrtFill = new Color32(20, 14, 8, 255);
        private static readonly Color32 CrtPlate = new Color32(16, 11, 6, 255);
        private static readonly Color32 CrtDeep = new Color32(14, 9, 5, 255);
        private static readonly Color32 CrtBar = new Color32(30, 20, 11, 255);
        private static readonly Color32 CrtButton = new Color32(28, 19, 10, 255);
        private static readonly Color32 CrtBubble = new Color32(34, 23, 12, 255);
        private static readonly Color32 CrtVeil = new Color32(12, 8, 4, 255);
        private static readonly Color32 CrtShadow = new Color32(10, 6, 3, 255);
        private static readonly Color32 CrtSelectionText = new Color32(26, 16, 5, 255);
        private static readonly Color32 CrtCyanDim = new Color32(146, 240, 252, 255);
        private static readonly Color32 CrtCyanFill = new Color32(10, 26, 32, 255);
        private static readonly Color32 CrtCoralFill = new Color32(42, 18, 12, 255);
        private static readonly Color32 PaperFill = new Color32(245, 232, 206, 255);
        private static readonly Color32 PaperCard = new Color32(238, 222, 192, 255);
        private static readonly Color32 PaperBorder = new Color32(186, 156, 108, 255);
        private static readonly Color32 PaperRule = new Color32(160, 130, 86, 255);
        private static readonly Color32 PaperInk = new Color32(38, 28, 18, 255);
        private static readonly Color32 PaperInkDim = new Color32(74, 56, 36, 255);
        private static readonly Color32 PaperStamp = new Color32(124, 72, 28, 255);

        private static readonly Token[] Tokens =
        {
            new Token("--moon-sky-top", PaletteSwatch.SkyTop, 1f),
            new Token("--moon-sky-horizon", PaletteSwatch.SkyHorizon, 1f),
            new Token("--moon-warm-lamp", PaletteSwatch.WarmLamp, 1f),
            new Token("--moon-warm-accent", PaletteSwatch.WarmAccent, 1f),
            new Token("--moon-cream", PaletteSwatch.Cream, 1f),
            new Token("--moon-honey", PaletteSwatch.Honey, 1f),
            new Token("--moon-tech-glow", PaletteSwatch.TechGlow, 1f),
            new Token("--moon-metal", PaletteSwatch.Metal, 1f),
            new Token("--moon-text", CrtText, 1f),
            new Token("--moon-text-dim", CrtTextDim, 1f),
            new Token("--moon-text-faint", CrtTextFaint, 1f),
            new Token("--moon-glass", CrtFill, 0.95f),
            new Token("--moon-plate", CrtPlate, 0.95f),
            new Token("--moon-glass-edge", PaletteSwatch.WarmLamp, 0.78f),
            new Token("--moon-veil", CrtVeil, 0.62f),
            new Token("--moon-shadow", CrtShadow, 0.75f),
            new Token("--moon-track", PaletteSwatch.WarmLamp, 0.28f),
            new Token("--moon-cream-glow", PaletteSwatch.WarmLamp, 0.12f),
            new Token("--moon-amber-soft", PaletteSwatch.WarmLamp, 0.18f),
            new Token("--moon-amber-edge", PaletteSwatch.WarmLamp, 0.75f),
            new Token("--moon-glow-soft", PaletteSwatch.TechGlow, 0.22f),
            new Token("--glass-hi", CrtBright, 1f),
            new Token("--glass-rim", PaletteSwatch.WarmLamp, 0.78f),
            new Token("--glass-rim-glow", PaletteSwatch.WarmLamp, 0.42f),
            new Token("--glass-ring", PaletteSwatch.WarmLamp, 1f),
            new Token("--glass-sel", PaletteSwatch.WarmLamp, 1f),
            new Token("--glass-sel-text", CrtSelectionText, 1f),
            new Token("--glass-sel-glow", PaletteSwatch.WarmLamp, 0.55f),
            new Token("--glass-bar", CrtBar, 0.95f),
            new Token("--glass-deep", CrtDeep, 0.96f),
            new Token("--glass-btn", CrtButton, 0.95f),
            new Token("--glass-bubble", CrtBubble, 0.95f),
            new Token("--crt-cyan-dim", CrtCyanDim, 1f),
            new Token("--crt-cyan-edge", PaletteSwatch.TechGlow, 0.78f),
            new Token("--crt-cyan-glow", PaletteSwatch.TechGlow, 0.38f),
            new Token("--crt-cyan-fill", CrtCyanFill, 0.95f),
            new Token("--crt-coral-edge", PaletteSwatch.WarmAccent, 0.85f),
            new Token("--crt-coral-fill", CrtCoralFill, 0.95f),
            new Token("--paper-fill", PaperFill, 0.98f),
            new Token("--paper-card", PaperCard, 0.98f),
            new Token("--paper-tint", PaperFill, 0.06f),
            new Token("--paper-border", PaperBorder, 0.95f),
            new Token("--paper-rule", PaperRule, 0.55f),
            new Token("--paper-ink", PaperInk, 1f),
            new Token("--paper-ink-dim", PaperInkDim, 1f),
            new Token("--paper-stamp", PaperStamp, 1f),
        };

        /// <summary>The token stylesheet (deterministic text).</summary>
        public static string BuildStyleSheet()
        {
            var builder = new StringBuilder();
            builder.Append("/* Generated by the UI/Theme builder from MoonProject.Art.Palette and the Variant C CRT set. ")
                .Append("Do not edit. */\n\n");
            builder.Append(":root {\n");
            foreach (Token token in Tokens)
            {
                Color32 color = token.IsRaw ? token.Raw : Palette.Get(token.Swatch);
                builder.Append("    ").Append(token.Name).Append(": rgba(")
                    .Append(color.r.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(color.g.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(color.b.ToString(CultureInfo.InvariantCulture)).Append(", ")
                    .Append(token.Alpha.ToString("0.###", CultureInfo.InvariantCulture)).Append(");\n");
            }

            builder.Append("}\n");
            return builder.ToString();
        }

        private readonly struct Token
        {
            public Token(string name, PaletteSwatch swatch, float alpha)
            {
                Name = name;
                Swatch = swatch;
                Raw = default;
                IsRaw = false;
                Alpha = alpha;
            }

            /// <summary>A Moonlight Glass colour, not a palette swatch.</summary>
            public Token(string name, Color32 raw, float alpha)
            {
                Name = name;
                Swatch = default;
                Raw = raw;
                IsRaw = true;
                Alpha = alpha;
            }

            public Color32 Raw { get; }

            public bool IsRaw { get; }

            public string Name { get; }

            public PaletteSwatch Swatch { get; }

            public float Alpha { get; }
        }
    }
}

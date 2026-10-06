using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using MoonProject.UI.Editor;
using TextAsset = UnityEngine.TextAsset;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The look is single-sourced: every colour the stylesheet uses is a palette token, and the fonts carry every
    /// character the string tables need (so Vietnamese diacritics render without runtime atlas growth).
    /// </summary>
    public sealed class ThemeTests
    {
        private const string StyleSheet = UiAssetPaths.SourceFolder + "/GameUI.uss";
        private static readonly Regex Variable = new Regex(@"var\((--[a-z0-9-]+)\)");
        private static readonly Regex HexColour = new Regex(@"#[0-9a-fA-F]{3,8}\b");

        [Test]
        public void EveryVariableTheStylesheetReads_IsAGeneratedPaletteToken()
        {
            string tokens = ThemeTokens.BuildStyleSheet();
            string sheet = File.ReadAllText(StyleSheet);
            var used = new HashSet<string>();
            foreach (Match match in Variable.Matches(sheet))
            {
                used.Add(match.Groups[1].Value);
            }

            Assert.Greater(used.Count, 10);
            foreach (string name in used)
            {
                StringAssert.Contains(name + ":", tokens, $"{name} is not a palette token");
            }

            Assert.IsFalse(HexColour.IsMatch(sheet), "colours come from the palette tokens, never hex literals");
        }

        [Test]
        public void ThePaletteSheet_IsDeterministic_AndUsesThePaletteValues()
        {
            Assert.AreEqual(ThemeTokens.BuildStyleSheet(), ThemeTokens.BuildStyleSheet());
            StringAssert.Contains("--moon-warm-lamp: rgba(255, 181, 71, 1);", ThemeTokens.BuildStyleSheet());
        }

        [Test]
        public void FontCharacters_CoverVietnamese_AndEveryTableCharacter()
        {
            var texts = new List<string>();
            foreach (TextAsset table in StringTableAssets.Load())
            {
                texts.Add(table.text);
            }

            string characters = FontCharacters.Collect(texts);
            foreach (char c in "ẠạẶặỆệỘộỢợỰựĐđƠơƯư ×")
            {
                StringAssert.Contains(c.ToString(), characters);
            }

            Assert.AreEqual(characters, FontCharacters.Collect(texts), "deterministic");
        }

        [Test]
        public void TheGeneratedFonts_HoldEveryCharacterTheTablesUse()
        {
            var texts = new List<string>();
            foreach (TextAsset table in StringTableAssets.Load())
            {
                texts.Add(table.text);
            }

            string characters = FontCharacters.Collect(texts);
            foreach (string style in new[]
                     {
                         UiAssetPaths.RegularStyle, UiAssetPaths.MediumStyle, UiAssetPaths.SemiBoldStyle,
                     })
            {
                string path = UiAssetPaths.FontAsset(style);
                var font = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                Assert.IsNotNull(font, $"{path} is missing: run the UI/Fonts builder");
                Assert.AreEqual(AtlasPopulationMode.Dynamic, font.atlasPopulationMode, path);
                Assert.IsTrue(font.HasCharacters(characters, out List<char> missing),
                    $"{path} lacks \"{new string(missing?.ToArray() ?? new char[0])}\": run the UI/Fonts builder");
            }
        }
    }
}

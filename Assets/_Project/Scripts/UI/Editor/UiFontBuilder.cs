using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using MoonProject.Editor.Builders;
using TextAsset = UnityEngine.TextAsset;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// Builds the UI Toolkit font assets of Be Vietnam Pro (Regular, Medium, SemiBold) from the TTFs in
    /// Assets/_Project/UI/Fonts: signed-distance-field atlases with dynamic population (so any Vietnamese diacritic
    /// renders), pre-filled with every character of the string tables (<see cref="FontCharacters"/>) so play never
    /// has to add glyphs. An existing font asset keeps its GUID and only gains missing characters.
    /// </summary>
    internal static class UiFontBuilder
    {
        public const string BuilderPath = "UI/Fonts";
        public const int BuilderOrder = 605;

        private const int SamplingPointSize = 64;
        private const int AtlasPadding = 7;
        private const int AtlasSize = 1024;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        private static void Build()
        {
            string characters = FontCharacters.Collect(TableTexts());
            BuildStyle(UiAssetPaths.RegularStyle, characters);
            BuildStyle(UiAssetPaths.MediumStyle, characters);
            BuildStyle(UiAssetPaths.SemiBoldStyle, characters);
            AssetDatabase.SaveAssets();
        }

        private static void BuildStyle(string style, string characters)
        {
            string sourcePath = UiAssetPaths.SourceFont(style);
            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogError($"{BuilderPath}: no font at {sourcePath}.");
                return;
            }

            string path = UiAssetPaths.FontAsset(style);
            FontAsset asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
            bool created = asset == null;
            if (created)
            {
                asset = FontAsset.CreateFontAsset(font, SamplingPointSize, AtlasPadding, GlyphRenderMode.SDFAA,
                    AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic);
                if (asset == null)
                {
                    Debug.LogError($"{BuilderPath}: TextCore could not create a font asset from {sourcePath}.");
                    return;
                }

                asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
                GeneratedAssets.EnsureFolder(UiAssetPaths.GeneratedFontFolder);
                AssetDatabase.CreateAsset(asset, path);
            }

            if (!asset.TryAddCharacters(characters, out string missing) && !string.IsNullOrEmpty(missing))
            {
                Debug.LogError($"{BuilderPath}: {font.name} has no glyph for \"{missing}\".");
            }

            PersistSubAssets(asset);
            EditorUtility.SetDirty(asset);
            Debug.Log($"{BuilderPath}: {(created ? "created" : "updated")} {path} ({characters.Length} characters)");
        }

        /// <summary>
        /// Stores the atlas textures and material inside the font asset file (TextCore makes them in memory).
        /// </summary>
        private static void PersistSubAssets(FontAsset asset)
        {
            Texture2D[] atlases = asset.atlasTextures;
            for (int i = 0; i < atlases.Length; i++)
            {
                if (atlases[i] != null && !AssetDatabase.Contains(atlases[i]))
                {
                    atlases[i].name = asset.name + " Atlas" + (i == 0 ? string.Empty : " " + i);
                    AssetDatabase.AddObjectToAsset(atlases[i], asset);
                }
            }

            if (asset.material != null && !AssetDatabase.Contains(asset.material))
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
        }

        private static IEnumerable<string> TableTexts()
        {
            foreach (TextAsset table in StringTableAssets.Load())
            {
                StringTable parsed = StringTable.Parse(table.text, table.name);
                foreach (string key in parsed.Keys)
                {
                    parsed.TryGet(key, out string text);
                    yield return text;
                }

                yield return parsed.Name;
            }
        }
    }
}

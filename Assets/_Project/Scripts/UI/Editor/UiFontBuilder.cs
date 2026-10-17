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
    /// Builds the UI Toolkit font assets of every UI font (<see cref="UiAssetPaths.Fonts"/>: IBM Plex Mono for CRT
    /// body text, Chakra Petch for numbers and titles, Be Vietnam Pro for crew documents) from the TTFs in
    /// Assets/_Project/UI/Fonts: signed-distance-field atlases pre-filled with every character of the string tables,
    /// credits, digits, and UI symbols (<see cref="FontCharacters"/>) and locked to <see cref="AtlasPopulationMode.Static"/>
    /// so play and tests never write to font assets at runtime. An existing font asset keeps its GUID.
    /// </summary>
    internal static class UiFontBuilder
    {
        public const string BuilderPath = "UI/Fonts";
        public const int BuilderOrder = 605;

        private const int SamplingPointSize = 64;
        private const int AtlasPadding = 7;
        private const int AtlasSize = 1024;

        [MoonBuilder(BuilderPath, BuilderOrder)]
        internal static void Build()
        {
            string characters = FontCharacters.Collect(TableTexts());
            foreach (string[] font in UiAssetPaths.Fonts)
            {
                BuildStyle(font[0], font[1], characters);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Locks every UI font asset to static population in memory before PlayMode runs so tests never write glyphs
        /// back to the asset on disk.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LockFontAssetsInMemory()
        {
            foreach (string[] font in UiAssetPaths.Fonts)
            {
                string path = UiAssetPaths.FontAsset(font[0], font[1]);
                var asset = AssetDatabase.LoadAssetAtPath<FontAsset>(path);
                if (asset != null && asset.atlasPopulationMode == AtlasPopulationMode.Dynamic)
                {
                    asset.atlasPopulationMode = AtlasPopulationMode.Static;
                }
            }
        }

        private static void BuildStyle(string family, string style, string characters)
        {
            string sourcePath = UiAssetPaths.SourceFont(family, style);
            var font = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (font == null)
            {
                Debug.LogError($"{BuilderPath}: no font at {sourcePath}.");
                return;
            }

            string path = UiAssetPaths.FontAsset(family, style);
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
            else
            {
                asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            }

            if (!asset.TryAddCharacters(characters, out string missing) && !string.IsNullOrEmpty(missing))
            {
                Debug.LogError($"{BuilderPath}: {font.name} has no glyph for \"{missing}\".");
            }

            asset.atlasPopulationMode = AtlasPopulationMode.Static;
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
                if (atlases[i] != null)
                {
                    if (!AssetDatabase.Contains(atlases[i]))
                    {
                        atlases[i].name = asset.name + " Atlas" + (i == 0 ? string.Empty : " " + i);
                        AssetDatabase.AddObjectToAsset(atlases[i], asset);
                    }

                    EditorUtility.SetDirty(atlases[i]);
                }
            }

            if (asset.material != null)
            {
                if (!AssetDatabase.Contains(asset.material))
                {
                    asset.material.name = asset.name + " Material";
                    AssetDatabase.AddObjectToAsset(asset.material, asset);
                }

                EditorUtility.SetDirty(asset.material);
            }
        }

        internal static IEnumerable<string> TableTexts()
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

            var credits = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.Credits);
            if (credits != null)
            {
                yield return credits.text;
            }
        }
    }
}

using System;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Generates the palette textures (one flat cell per <see cref="PaletteSwatch"/>, laid out as
    /// <see cref="Palette.Uv"/> expects) and the shared <c>M_LowPoly</c> material every low-poly mesh renders with.
    /// </summary>
    public static class PaletteAssetBuilder
    {
        /// <summary>Pixels per palette cell (cells are flat: any size works with point filtering, no mips).</summary>
        public const int CellPixels = 8;

        private static readonly Color32 UnusedCell = new Color32(0, 0, 0, 255);

        /// <summary>Writes both palette textures and the shared material (first Art builder: others use it).</summary>
        [MoonBuilder("Art/Palette", 100)]
        public static void Build()
        {
            Texture2D baseMap = WritePaletteTexture(ArtPaths.PaletteTexture, false);
            Texture2D emissionMap = WritePaletteTexture(ArtPaths.PaletteEmissionTexture, true);
            WriteMaterial(baseMap, emissionMap);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The shared material, or an exception telling the caller to build the palette first.</summary>
        public static Material LoadMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ArtPaths.LowPolyMaterial);
            if (material == null)
            {
                throw new InvalidOperationException(
                    $"{ArtPaths.LowPolyMaterial} is missing: run the Art/Palette builder first.");
            }

            return material;
        }

        /// <summary>Palette pixels, row 0 at the bottom; emission-only maps keep non-glowing swatches black.</summary>
        public static Color32[] CreatePixels(bool emissionOnly)
        {
            int width = Palette.Columns * CellPixels;
            int height = Palette.Rows * CellPixels;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = (y / CellPixels) * Palette.Columns + x / CellPixels;
                    Color32 colour = UnusedCell;
                    if (index < Palette.Count)
                    {
                        var swatch = (PaletteSwatch)index;
                        if (!emissionOnly || Palette.IsEmissive(swatch))
                        {
                            colour = Palette.Get(swatch);
                        }
                    }

                    pixels[y * width + x] = colour;
                }
            }

            return pixels;
        }

        private static Texture2D WritePaletteTexture(string path, bool emissionOnly)
        {
            var texture = new Texture2D(Palette.Columns * CellPixels, Palette.Rows * CellPixels, TextureFormat.RGBA32,
                false, false);
            try
            {
                texture.SetPixels32(CreatePixels(emissionOnly));
                texture.Apply(false);
                GeneratedAssets.WriteFileIfChanged(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            ApplyImportSettings(path);
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (imported == null)
            {
                throw new InvalidOperationException($"{path} did not import as a texture.");
            }

            return imported;
        }

        private static void ApplyImportSettings(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new InvalidOperationException($"No texture importer for {path}.");
            }

            bool settled = importer.textureType == TextureImporterType.Default
                && importer.sRGBTexture
                && !importer.mipmapEnabled
                && importer.filterMode == FilterMode.Point
                && importer.wrapMode == TextureWrapMode.Clamp
                && importer.textureCompression == TextureImporterCompression.Uncompressed
                && importer.npotScale == TextureImporterNPOTScale.None
                && importer.alphaSource == TextureImporterAlphaSource.None
                && !importer.isReadable;
            if (settled)
            {
                return;
            }

            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        private static Material WriteMaterial(Texture2D baseMap, Texture2D emissionMap)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArtPaths.LowPolyShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"URP Simple Lit shader not found at {ArtPaths.LowPolyShader}.");
            }

            var material = new Material(shader);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", Vector2.one);
            material.SetTextureOffset("_BaseMap", Vector2.zero);
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_EmissionMap", emissionMap);
            // Authored white: the emission map alone sets each swatch's glow, and renderers scale it at runtime with a
            // MaterialPropertyBlock _EmissionColor (ARCHITECTURE.md, "Glow modulation").
            material.SetColor("_EmissionColor", Color.white);
            material.SetFloat("_SpecularHighlights", (float)SimpleLitGUI.SpecularSource.NoSpecular);
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_AlphaClip", 0f);
            material.SetFloat("_ReceiveShadows", 1f);
            material.SetFloat("_Cull", 2f);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            material.enableInstancing = true;
            BaseShaderGUI.SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
            return GeneratedAssets.CreateOrReplace(material, ArtPaths.LowPolyMaterial);
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Builds the placement-ghost and un-welded stage hologram materials (<see cref="ArtPaths.HologramMaterial"/> and
    /// <see cref="ArtPaths.HologramInvalidMaterial"/>) on <c>MoonProject/Art/LofiHologram</c> (0.5.7 slice 6,
    /// BUILD-MODE): calm translucent cyan (<see cref="PaletteSwatch.TechGlow"/>, <c>#5FF3FF</c>) body with a warm amber
    /// (<see cref="PaletteSwatch.WarmLamp"/>, <c>#FFB547</c>) Fresnel edge, horizontal scan-line shimmer, and a coral
    /// (<see cref="PaletteSwatch.AlertSoft"/>, <c>#FF5A6E</c>) invalid state, strictly clamped below
    /// <see cref="Palette.BloomThreshold"/>.
    /// </summary>
    public static class HologramMaterialBuilder
    {
        public const string ShaderName = "MoonProject/Art/LofiHologram";
        public const float BodyIntensity = 0.52f;
        public const float RimIntensity = 0.92f;
        public const float RimPower = 2.1f;
        public const float ScanDensity = 24f;
        public const float ScanSpeed = 0.85f;
        public const float ScanContrast = 0.24f;
        public const float BaseAlpha = 0.44f;
        public const float RimAlpha = 0.84f;
        public const float BloomSafeCeiling = 1.10f;

        [MoonBuilder("Art/Hologram", 107)]
        public static void Build()
        {
            GeneratedAssets.CreateOrReplace(CreateMaterial(false), ArtPaths.HologramMaterial);
            GeneratedAssets.CreateOrReplace(CreateMaterial(true), ArtPaths.HologramInvalidMaterial);
            AssetDatabase.SaveAssets();
        }

        public static Shader LoadShader()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArtPaths.HologramShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"The hologram shader is missing at {ArtPaths.HologramShader}.");
            }

            return shader;
        }

        public static Material CreateMaterial(bool invalid = false)
        {
            Shader shader = LoadShader();
            var material = new Material(shader);
            Color cyan = Palette.Get(PaletteSwatch.TechGlow);
            Color amber = Palette.Get(PaletteSwatch.WarmLamp);
            Color alert = Palette.Get(PaletteSwatch.AlertSoft);

            material.SetColor("_BaseColor", cyan);
            material.SetColor("_RimColor", amber);
            material.SetColor("_InvalidColor", alert);
            material.SetFloat("_BodyIntensity", BodyIntensity);
            material.SetFloat("_RimIntensity", RimIntensity);
            material.SetFloat("_RimPower", RimPower);
            material.SetFloat("_ScanDensity", ScanDensity);
            material.SetFloat("_ScanSpeed", ScanSpeed);
            material.SetFloat("_ScanContrast", ScanContrast);
            material.SetFloat("_Alpha", BaseAlpha);
            material.SetFloat("_RimAlpha", RimAlpha);
            material.SetFloat("_Invalid", invalid ? 1f : 0f);
            material.SetFloat("_StageFade", 1f);
            material.SetFloat("_BloomCeiling", BloomSafeCeiling);
            material.enableInstancing = true;
            return material;
        }

        public static Material LoadMaterial(bool invalid = false)
        {
            string path = invalid ? ArtPaths.HologramInvalidMaterial : ArtPaths.HologramMaterial;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException(
                    $"{path} is missing: run the Art/Hologram builder first.");
            }

            return material;
        }
    }
}

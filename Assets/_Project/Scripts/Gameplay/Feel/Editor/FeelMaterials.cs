using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;
using MoonProject.Editor.Builders;

namespace MoonProject.Gameplay.Feel.Editor
{
    /// <summary>
    /// The cut feel kit's look (docs/design/feel-pass.md §6: art owns the look): one white soft sprite for every
    /// glowing particle (each sprite carries its own colour), the beam's hot core and cyan sheath on SoftGlow, and the
    /// heat seam on its own shader. <see cref="Build"/> runs after Gameplay/Materials: it writes the materials into
    /// GameplayVisuals and makes the tuning asset if it is missing.
    /// </summary>
    internal static class FeelMaterials
    {
        public const string Folder = GeneratedAssets.Root + "/Gameplay/Feel";
        public const string SpritePath = Folder + "/M_FeelSprite.mat";
        public const string CorePath = Folder + "/M_BeamCore.mat";
        public const string SheathPath = Folder + "/M_BeamSheath.mat";
        public const string SeamPath = Folder + "/M_HeatSeam.mat";
        public const string SoftGlowShader = "Assets/_Project/Shaders/Gameplay/SoftGlow.shader";
        public const string HeatSeamShader = "Assets/_Project/Shaders/Gameplay/HeatSeam.shader";
        public const string TuningPath = "Assets/_Project/Data/Tuning/Gameplay/FeelVfxTuning.asset";

        // The sprites' glow over the bloom threshold, so sparks, beads and motes bloom softly (each sprite's own
        // colour dims it); the core's white-hot glow; the sheath's cyan glow and its faint shimmering bands.
        private const float SpriteIntensity = 2.2f;
        private const float CoreIntensity = 2.6f;
        private const float SheathIntensity = 2f;
        private const float BeamEdge = 0.5f;
        private const float SheathEdge = 0.3f;
        private const float SheathBands = 6f;
        private const float SheathBandSpeed = 3f;
        private const float SheathBandStrength = 0.25f;
        private const float SoftRadialPower = 2f;
        private const int TransparentQueue = 3000;

        public const string VisualsPath = GeneratedAssets.Root + "/Gameplay/GameplayVisuals.asset";

        [MoonBuilder("Gameplay/FeelVfx", 506)]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<FeelVfxTuning>(TuningPath) == null)
            {
                GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<FeelVfxTuning>(), TuningPath);
            }

            var visuals = AssetDatabase.LoadAssetAtPath<GameplayVisuals>(VisualsPath);
            if (visuals == null)
            {
                throw new InvalidOperationException(
                    $"Gameplay/FeelVfx: {VisualsPath} is missing (run Gameplay/Materials).");
            }

            Populate(visuals);
            EditorUtility.SetDirty(visuals);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Writes the four materials and hands them to <paramref name="visuals"/>.</summary>
        public static void Populate(GameplayVisuals visuals)
        {
            Shader glow = Load(SoftGlowShader);
            GeneratedAssets.EnsureFolder(Folder);
            Material sprite = Glow(glow, Color.white, SpriteIntensity, 0f, 1f, 0f);
            Material core = Glow(glow, Color.white, CoreIntensity, BeamEdge, 0f, 0f);
            Color cyan = Palette.Get(PaletteSwatch.TechGlow);
            Material sheath = Glow(glow, cyan, SheathIntensity, SheathEdge, 0f, SheathBandStrength);
            sheath.SetFloat("_BandCount", SheathBands);
            sheath.SetFloat("_BandSpeed", SheathBandSpeed);
            var seam = new Material(Load(HeatSeamShader)) { enableInstancing = true };
            visuals.PopulateFeel(GeneratedAssets.CreateOrReplace(sprite, SpritePath),
                GeneratedAssets.CreateOrReplace(core, CorePath), GeneratedAssets.CreateOrReplace(sheath, SheathPath),
                GeneratedAssets.CreateOrReplace(seam, SeamPath));
        }

        private static Material Glow(Shader shader, Color color, float intensity, float edge, float radial,
            float bandStrength)
        {
            var material = new Material(shader) { enableInstancing = true };
            material.SetColor("_Color", color);
            material.SetFloat("_Intensity", intensity);
            material.SetFloat("_AcrossIn", edge);
            material.SetFloat("_AcrossOut", edge);
            material.SetFloat("_LengthFade", 0f);
            material.SetFloat("_BandCount", 0f);
            material.SetFloat("_BandSpeed", 0f);
            material.SetFloat("_BandStrength", bandStrength);
            material.SetFloat("_FresnelMix", 0f);
            material.SetFloat("_CoreMix", 0f);
            material.SetFloat("_RadialMask", radial);
            material.SetFloat("_RadialPower", SoftRadialPower);
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
            material.SetFloat("_DstBlend", (float)BlendMode.One);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.renderQueue = TransparentQueue;
            return material;
        }

        private static Shader Load(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            return shader != null ? shader
                : throw new InvalidOperationException($"Gameplay/Materials: shader missing at {path}.");
        }
    }
}

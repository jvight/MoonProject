using UnityEditor;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Editor.Builders;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Physics and effect assets the rover prefab uses: the frictionless sphere material (all traction is done in
    /// code), the multiply-blended track material, and the flat-shaded low-poly dust puff mesh + lit particle material
    /// (dust reacts to the sun and to 07's headlamp). Colours come from the palette.
    /// </summary>
    public static class RoverFxAssetBuilder
    {
        /// <summary>How far a fresh, fully opaque track darkens the ground toward the dust shadow swatch.</summary>
        private const float TrackTintStrength = 1f;

        private const float DustPuffRadius = 0.5f;

        [MoonBuilder("Rover/Materials", 305)]
        public static void Build()
        {
            BuildSphereMaterial();
            BuildTrackMaterial();
            BuildDustMesh();
            BuildDustMaterial();
            AssetDatabase.SaveAssets();
        }

        private static void BuildSphereMaterial()
        {
            var material = new PhysicsMaterial
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.SpherePhysicsMaterial);
        }

        private static void BuildTrackMaterial()
        {
            var shader = BuildWiring.Require<Shader>(RoverAssetPaths.TrackShader, "the Rover shaders folder");
            Color tint = Palette.Get(PaletteSwatch.DustShadow);
            tint.a = TrackTintStrength;
            var material = new Material(shader) { enableInstancing = false };
            material.SetColor("_Color", tint);
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.TrackMaterial);
        }

        private static void BuildDustMesh()
        {
            var builder = new LowPolyMeshBuilder();
            builder.Icosphere(Matrix4x4.identity, DustPuffRadius, 0, PaletteSwatch.DustLight);
            GeneratedAssets.CreateOrReplace(builder.ToMesh("DustPuff"), RoverAssetPaths.DustMesh);
        }

        private static void BuildDustMaterial()
        {
            var shader = BuildWiring.Require<Shader>(RoverAssetPaths.ParticlesSimpleLitShader, "the URP package");
            var material = new Material(shader) { enableInstancing = true };
            material.SetColor("_BaseColor", Color.white);
            material.SetColor("_SpecColor", Color.black);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
            material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Alpha);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.DustMaterial);
        }
    }
}

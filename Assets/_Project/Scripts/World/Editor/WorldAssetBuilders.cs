using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Builders;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// World content builders: the tuning asset (created once, never overwritten), the sky and Earth materials and
    /// the post-processing Volume profile generated from <see cref="PostProcessSettings"/>. Terrain and Earth meshes
    /// are not assets: <see cref="WorldSystem"/> generates them at boot.
    /// </summary>
    public static class WorldAssetBuilders
    {
        [MoonBuilder("World/Settings", 200)]
        public static void BuildSettings()
        {
            if (AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings) != null)
            {
                return;
            }

            GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<WorldSettings>(), WorldPaths.Settings);
            AssetDatabase.SaveAssets();
        }

        [MoonBuilder("World/Sky Materials", 210)]
        public static void BuildSkyMaterials()
        {
            GeneratedAssets.CreateOrReplace(new Material(FindShader(WorldPaths.SkyShader)), WorldPaths.SkyMaterial);
            GeneratedAssets.CreateOrReplace(new Material(FindShader(WorldPaths.EarthShader)), WorldPaths.EarthMaterial);
            AssetDatabase.SaveAssets();
        }

        [MoonBuilder("World/Post Processing", 220)]
        public static void BuildVolumeProfile()
        {
            PostProcessSettings post = LoadSettings().PostProcessing;
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(WorldPaths.VolumeProfile);
            if (profile == null)
            {
                GeneratedAssets.EnsureFolder(WorldPaths.GeneratedRoot);
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, WorldPaths.VolumeProfile);
            }

            // Overrides are updated in place (never re-created) so the asset's sub-object IDs stay stable.
            Override<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);

            var bloom = Override<Bloom>(profile);
            bloom.threshold.Override(post.BloomThreshold);
            bloom.intensity.Override(post.BloomIntensity);
            bloom.scatter.Override(post.BloomScatter);
            bloom.tint.Override(post.BloomTint);
            bloom.highQualityFiltering.Override(true);

            var colour = Override<ColorAdjustments>(profile);
            colour.postExposure.Override(post.PostExposure);
            colour.contrast.Override(post.Contrast);
            colour.saturation.Override(post.Saturation);

            var liftGammaGain = Override<LiftGammaGain>(profile);
            liftGammaGain.lift.Override(post.Lift);
            liftGammaGain.gamma.Override(post.Gamma);
            liftGammaGain.gain.Override(post.Gain);

            var vignette = Override<Vignette>(profile);
            vignette.intensity.Override(post.VignetteIntensity);
            vignette.smoothness.Override(post.VignetteSmoothness);
            vignette.color.Override(post.VignetteColor);

            var grain = Override<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(post.GrainIntensity);
            grain.response.Override(post.GrainResponse);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        private static T Override<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing))
            {
                existing.active = true;
                return existing;
            }

            T component = profile.Add<T>();
            component.name = typeof(T).Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static WorldSettings LoadSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            if (settings == null)
            {
                throw new InvalidOperationException($"No WorldSettings at {WorldPaths.Settings}; run World/Settings.");
            }

            return settings;
        }

        private static Shader FindShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader '{name}' not found (Assets/_Project/Shaders/World).");
            }

            return shader;
        }
    }
}

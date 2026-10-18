using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Art;
using MoonProject.Editor.Automation;
using MoonProject.Editor.Builders;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// World content builders: the tuning asset (created once, never overwritten), the sky and Earth materials, and
    /// from <see cref="PostProcessSettings"/> the post-processing Volume profile and the PC renderer's SSAO. Terrain and Earth meshes
    /// are not assets: <see cref="WorldSystem"/> generates them at boot.
    /// </summary>
    public static class WorldAssetBuilders
    {
        private const string OcclusionFeature = "ScreenSpaceAmbientOcclusion";

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

        /// <summary>
        /// Overwrites the tuning asset with the code defaults, keeping its GUID so scene references survive. Discards
        /// any hand tuning: use it only to pick up new defaults. Batch:
        /// <c>python tools/unity_batch.py exec --method MoonProject.World.Editor.WorldAssetBuilders.ResetSettings</c>
        /// </summary>
        public static void ResetSettings()
        {
            BatchRunner.Run(nameof(ResetSettings), args =>
            {
                GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<WorldSettings>(), WorldPaths.Settings);
                AssetDatabase.SaveAssets();
                return true;
            });
        }

        [MenuItem("MoonProject/World/Reset Settings To Code Defaults")]
        private static void ResetSettingsFromMenu()
        {
            if (EditorUtility.DisplayDialog("Reset World Settings",
                    "Overwrite WorldSettings.asset with the code defaults? Hand tuning in it is lost.", "Reset",
                    "Cancel"))
            {
                ResetSettings();
            }
        }

        /// <summary>The terrain's material: R1Terrain draws painted vertex colours with multi-scale regolith and triplanar rock detail.</summary>
        [MoonBuilder("World/Ground Material", 205)]
        public static void BuildGroundMaterial()
        {
            MoonProject.Art.Editor.R1MaterialLibrary.BuildMaterials();
            var material = new Material(LoadShader(WorldPaths.GroundShader));
            material.SetTexture("_BaseMap", LoadTexture(WorldPaths.RegolithAlbedoTexture));
            material.SetTexture("_BumpMap", LoadTexture(WorldPaths.RegolithNormalTexture));
            material.SetTexture("_MaskMap", LoadTexture(WorldPaths.RegolithMaskTexture));
            material.SetTexture("_RockMap", LoadTexture(WorldPaths.BasaltAlbedoTexture));
            material.SetTexture("_RockBumpMap", LoadTexture(WorldPaths.BasaltNormalTexture));
            material.SetTexture("_RockMaskMap", LoadTexture(WorldPaths.BasaltMaskTexture));
            material.SetFloat("_TerrainVariant", -1f);
            GeneratedAssets.CreateOrReplace(material, WorldPaths.GroundMaterial);
            AssetDatabase.SaveAssets();
        }

        /// <summary>The marks pressed into the dust (M3-18 V4): the rover's track shader with World's tint and fade.</summary>
        [MoonBuilder("World/Ground Mark Material", 206)]
        public static void BuildGroundMarkMaterial()
        {
            GroundMarkSettings marks = LoadSettings().GroundMarks;
            var material = new Material(LoadShader(WorldPaths.GroundMarkShader));
            material.SetColor("_Color", marks.Tint);
            material.SetFloat("_FadeNear", marks.FadeNear);
            material.SetFloat("_FadeFar", marks.FadeFar);
            GeneratedAssets.CreateOrReplace(material, WorldPaths.GroundMarkMaterial);
            AssetDatabase.SaveAssets();
        }

        [MoonBuilder("World/Sky Materials", 210)]
        public static void BuildSkyMaterials()
        {
            GeneratedAssets.CreateOrReplace(new Material(LoadShader(WorldPaths.SkyShader)), WorldPaths.SkyMaterial);
            Shader earthShader = LoadShader(WorldPaths.EarthShader);
            Texture2D earthAlbedo = LoadEarthTexture(WorldPaths.EarthAlbedoTexture);
            GeneratedAssets.CreateOrReplace(CreateEarthMaterial(earthShader, earthAlbedo, -1f), WorldPaths.EarthMaterial);
            GeneratedAssets.CreateOrReplace(CreateEarthMaterial(earthShader, earthAlbedo, 0f), WorldPaths.EarthMaterialA);
            GeneratedAssets.CreateOrReplace(CreateEarthMaterial(earthShader, earthAlbedo, 1f), WorldPaths.EarthMaterialA2);
            AssetDatabase.SaveAssets();
        }

        private static Material CreateEarthMaterial(Shader shader, Texture2D albedo, float variant)
        {
            var material = new Material(shader);
            material.SetTexture("_EarthAlbedoMap", albedo);
            material.SetFloat("_EarthVariant", variant);
            return material;
        }

        /// <summary>
        /// The landed lander's amber emergency beacon (opening v2 §4): a LofiBeaconHalo in its colour, the cable
        /// clips' small halo (its brightness set per clip by the beacon) and the tear's cold spark.
        /// </summary>
        [MoonBuilder("World/Lander Beacon", 240)]
        public static void BuildLanderBeacon()
        {
            LanderBeaconSettings beacon = LoadSettings().Landmarks.LanderBeacon;
            var material = new Material(LoadShader(WorldPaths.BeaconHaloShader));
            material.SetColor("_Color", beacon.Color);
            material.SetFloat("_Intensity", beacon.HaloIntensity);
            material.SetFloat("_Radius", beacon.HaloRadius);
            material.SetFloat("_MinAngleTan", Mathf.Tan(beacon.HaloMinAngle * Mathf.Deg2Rad));
            GeneratedAssets.CreateOrReplace(material, WorldPaths.LanderBeaconMaterial);
            var markerHalo = new Material(LoadShader(WorldPaths.BeaconHaloShader));
            markerHalo.SetColor("_Color", beacon.Color);
            markerHalo.SetFloat("_Intensity", 0f);
            markerHalo.SetFloat("_Radius", beacon.MarkerHaloRadius);
            markerHalo.SetFloat("_MinAngleTan", 0f);
            GeneratedAssets.CreateOrReplace(markerHalo, WorldPaths.CableMarkerHaloMaterial);
            var spark = new Material(LoadShader(WorldPaths.BeaconHaloShader));
            spark.SetColor("_Color", beacon.SparkColor);
            spark.SetFloat("_Intensity", 0f);
            spark.SetFloat("_Radius", beacon.SparkRadius);
            spark.SetFloat("_MinAngleTan", 0f);
            GeneratedAssets.CreateOrReplace(spark, WorldPaths.CableTearSparkMaterial);
            AssetDatabase.SaveAssets();
        }

        [MoonBuilder("World/Post Processing", 220)]
        public static void BuildVolumeProfile()
        {
            PostProcessSettings post = LoadSettings().PostProcessing;
            VolumeProfile profile = LoadOrCreateProfile(WorldPaths.VolumeProfile);

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

            var split = Override<SplitToning>(profile);
            split.shadows.Override(post.SplitShadows);
            split.highlights.Override(post.SplitHighlights);
            split.balance.Override(post.SplitBalance);

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

        /// <summary>
        /// The PC renderer's screen-space ambient occlusion (M3-18 V1) and the PC pipeline's colour grading mode (V9)
        /// from <see cref="PostProcessSettings"/>. URP keeps the SSAO feature and its settings internal, so they are
        /// written through their serialized fields.
        /// </summary>
        [MoonBuilder("World/Render Pipeline", 225)]
        public static void BuildRenderPipeline()
        {
            PostProcessSettings post = LoadSettings().PostProcessing;
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(WorldPaths.PcRenderer);
            if (renderer == null)
            {
                throw new InvalidOperationException($"No URP renderer at {WorldPaths.PcRenderer}.");
            }

            ScriptableRendererFeature occlusion = null;
            foreach (ScriptableRendererFeature feature in renderer.rendererFeatures)
            {
                occlusion = feature != null && feature.GetType().Name == OcclusionFeature ? feature : occlusion;
            }

            if (occlusion == null)
            {
                throw new InvalidOperationException($"{WorldPaths.PcRenderer} has no {OcclusionFeature} feature.");
            }

            var serialized = new SerializedObject(occlusion);
            SetFloat(serialized, "m_Settings.Intensity", post.OcclusionIntensity);
            SetFloat(serialized, "m_Settings.Radius", post.OcclusionRadius);
            SetFloat(serialized, "m_Settings.DirectLightingStrength", post.OcclusionDirectLighting);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            occlusion.SetActive(true);
            EditorUtility.SetDirty(occlusion);
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(WorldPaths.PcPipeline);
            if (pipeline == null)
            {
                throw new InvalidOperationException($"No URP pipeline asset at {WorldPaths.PcPipeline}.");
            }

            pipeline.colorGradingMode = post.GradeInHdr ? ColorGradingMode.HighDynamicRange
                : ColorGradingMode.LowDynamicRange;
            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
        }

        private static VolumeProfile LoadOrCreateProfile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                GeneratedAssets.EnsureFolder(WorldPaths.GeneratedRoot);
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            return profile;
        }

        private static void SetFloat(SerializedObject serialized, string path, float value)
        {
            SerializedProperty property = serialized.FindProperty(path);
            if (property == null)
            {
                throw new InvalidOperationException($"{serialized.targetObject.name} has no serialized '{path}'.");
            }

            property.floatValue = value;
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

        private static Shader LoadShader(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
            {
                throw new InvalidOperationException($"No shader at '{path}'.");
            }

            return shader;
        }

        private static Texture2D LoadEarthTexture(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                bool dirty = false;
                if (importer.wrapModeU != TextureWrapMode.Repeat)
                {
                    importer.wrapModeU = TextureWrapMode.Repeat;
                    dirty = true;
                }

                if (importer.wrapModeV != TextureWrapMode.Clamp)
                {
                    importer.wrapModeV = TextureWrapMode.Clamp;
                    dirty = true;
                }

                if (!importer.sRGBTexture)
                {
                    importer.sRGBTexture = true;
                    dirty = true;
                }

                if (importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = false;
                    dirty = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            return LoadTexture(path);
        }

        private static Texture2D LoadTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException($"Texture at '{path}' not found.");
            }

            return texture;
        }
    }
}

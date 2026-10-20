using System;
using System.Collections.Generic;
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

        // The sky point's quad is drawn round the camera by its shader, so its bounds must never be culled.
        private const float SkyPointNeverCulled = 1e7f;

        // The ruin whose fallen wings glint, and the material its cells wear.
        private const string FallenSolarArray = "FallenSolarArray";
        private const string SolarCellMaterial = "R1_SolarCell";

        // Art's amber lamp lens, lit for relay.1's service cabinet.
        private const string LampLensSlot = "R1_LampAmber";

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
            Shader groundShader = LoadShader(WorldPaths.GroundShader);
            Texture2D regolithAlbedo = LoadTexture(WorldPaths.RegolithAlbedoTexture);
            Texture2D regolithNormal = LoadTexture(WorldPaths.RegolithNormalTexture);
            Texture2D regolithMask = LoadTexture(WorldPaths.RegolithMaskTexture);
            Texture2D basaltAlbedo = LoadTexture(WorldPaths.BasaltAlbedoTexture);
            Texture2D basaltNormal = LoadTexture(WorldPaths.BasaltNormalTexture);
            Texture2D basaltMask = LoadTexture(WorldPaths.BasaltMaskTexture);
            var material = new Material(groundShader);
            material.SetTexture("_BaseMap", regolithAlbedo);
            material.SetTexture("_BumpMap", regolithNormal);
            material.SetTexture("_MaskMap", regolithMask);
            material.SetTexture("_RockMap", basaltAlbedo);
            material.SetTexture("_RockBumpMap", basaltNormal);
            material.SetTexture("_RockMaskMap", basaltMask);
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
            var earthMaterial = new Material(LoadShader(WorldPaths.EarthShader));
            earthMaterial.SetTexture("_EarthAlbedoMap", LoadTexture(WorldPaths.EarthAlbedoTexture));
            GeneratedAssets.CreateOrReplace(earthMaterial, WorldPaths.EarthMaterial);
            CallSkyPointSettings point = LoadSettings().Sky.CallPoint;
            var pointMaterial = new Material(LoadShader(WorldPaths.SkyPointShader));
            pointMaterial.SetColor("_Color", point.Color);
            pointMaterial.SetColor("_EmberColor", point.EmberColor);
            pointMaterial.SetFloat("_RingPixels", point.RingPixels);
            pointMaterial.SetFloat("_RingWidthPixels", point.RingWidthPixels);
            pointMaterial.SetFloat("_RingBrightness", point.RingBrightness);
            pointMaterial.SetFloat("_Peak", point.Peak);
            pointMaterial.SetFloat("_CorePixels", point.CorePixels);
            pointMaterial.SetFloat("_GlowPixels", point.GlowPixels);
            pointMaterial.SetFloat("_GlowShare", point.GlowShare);
            GeneratedAssets.CreateOrReplace(pointMaterial, WorldPaths.SkyPointMaterial);
            GeneratedAssets.CreateOrReplace(SkyPointQuad(), WorldPaths.SkyPointMesh);
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// The sheen for each of the fallen array's wings (opening v3 R4): a copy of the wing's solar-cell faces, in
        /// its own mesh space, each face floated off the cells along its normal so the panel never cuts it. The scene
        /// build lays each on its wing.
        /// </summary>
        [MoonBuilder("World/Array Glint Sheens", 241)]
        public static void BuildArrayGlintSheens()
        {
            float lift = LoadSettings().Landmarks.ArrayGlint.SheenLift;
            string path = WorldPaths.RuinPrefab(FallenSolarArray);
            var array = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (array == null)
            {
                throw new InvalidOperationException($"No fallen solar array at '{path}' to cut the glint sheens from.");
            }

            int sheens = 0;
            foreach (MeshRenderer wing in array.GetComponentsInChildren<MeshRenderer>(true))
            {
                Mesh cells = CellsOf(wing, lift);
                if (cells != null)
                {
                    GeneratedAssets.CreateOrReplace(cells, WorldPaths.ArrayGlintSheenMesh(wing.name));
                    sheens++;
                }
            }

            if (sheens == 0)
            {
                throw new InvalidOperationException($"'{path}' has no {SolarCellMaterial} cells to lay a sheen on.");
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>A copy of <paramref name="wing"/>'s solar-cell faces lifted off them; null when it has none.</summary>
        private static Mesh CellsOf(MeshRenderer wing, float lift)
        {
            var filter = wing.GetComponent<MeshFilter>();
            Mesh source = filter != null ? filter.sharedMesh : null;
            if (source == null)
            {
                return null;
            }

            Vector3[] vertices = source.vertices;
            Material[] materials = wing.sharedMaterials;
            var positions = new List<Vector3>();
            for (int slot = 0; slot < materials.Length && slot < source.subMeshCount; slot++)
            {
                if (materials[slot] == null || materials[slot].name != SolarCellMaterial)
                {
                    continue;
                }

                int[] triangles = source.GetTriangles(slot);
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = vertices[triangles[t]];
                    Vector3 b = vertices[triangles[t + 1]];
                    Vector3 c = vertices[triangles[t + 2]];
                    Vector3 up = Vector3.Cross(b - a, c - a).normalized * lift;
                    positions.Add(a + up);
                    positions.Add(b + up);
                    positions.Add(c + up);
                }
            }

            if (positions.Count == 0)
            {
                return null;
            }

            var indices = new int[positions.Count];
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = i;
            }

            var mesh = new Mesh { name = "ArrayGlintSheen_" + wing.name };
            mesh.SetVertices(positions);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// The sky point's quad, corners at ±1 (the shader scales them to pixels round the point): it is drawn round
        /// whichever camera looks, so CPU culling must never reject it.
        /// </summary>
        private static Mesh SkyPointQuad()
        {
            var mesh = new Mesh { name = "CallSkyPointQuad" };
            mesh.SetVertices(new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f),
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * SkyPointNeverCulled);
            return mesh;
        }

        /// <summary>
        /// The landed lander's amber emergency beacon (opening v2 §4): a LofiBeaconHalo in its colour, the cable
        /// clips' small halo (its brightness set per clip by the beacon), the tear's cold spark, and the fallen solar
        /// array's sun-white sparkle and sheen and the habitat power module lamp's amber halo (opening v3 R4).
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
            ArrayGlintSettings glint = LoadSettings().Landmarks.ArrayGlint;
            var sparkle = new Material(LoadShader(WorldPaths.GlintStarShader));
            sparkle.SetColor("_Color", glint.Color);
            sparkle.SetFloat("_ArmPixels", glint.SparkleArmPixels);
            sparkle.SetFloat("_WidthPixels", glint.SparkleWidthPixels);
            sparkle.SetFloat("_CorePixels", glint.SparkleCorePixels);
            sparkle.SetFloat("_Intensity", 0f);
            GeneratedAssets.CreateOrReplace(sparkle, WorldPaths.ArrayGlintMaterial);
            var sheen = new Material(LoadShader(WorldPaths.GlintSheenShader));
            sheen.SetColor("_Color", glint.Color);
            sheen.SetFloat("_Intensity", 0f);
            GeneratedAssets.CreateOrReplace(sheen, WorldPaths.ArrayGlintSheenMaterial);
            ModuleLampSettings moduleLamp = LoadSettings().Landmarks.ModuleLamp;
            var moduleHalo = new Material(LoadShader(WorldPaths.BeaconHaloShader));
            moduleHalo.SetColor("_Color", moduleLamp.HaloColor);
            moduleHalo.SetFloat("_Intensity", 0f);
            moduleHalo.SetFloat("_Radius", moduleLamp.HaloRadius);
            moduleHalo.SetFloat("_MinAngleTan", Mathf.Tan(moduleLamp.HaloMinAngle * Mathf.Deg2Rad));
            GeneratedAssets.CreateOrReplace(moduleHalo, WorldPaths.ModuleLampHaloMaterial);
            var cabinetLamp = new Material(LoadMaterial(WorldPaths.R1Material(LampLensSlot)));
            cabinetLamp.SetFloat("_EmissionStrength", LoadSettings().Landmarks.Relay1Site.LampLensGlow);
            GeneratedAssets.CreateOrReplace(cabinetLamp, WorldPaths.Relay1CabinetLampMaterial);
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

        private static Texture2D LoadTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
            {
                throw new InvalidOperationException($"Texture at '{path}' not found.");
            }

            return texture;
        }

        private static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                throw new InvalidOperationException($"Material at '{path}' not found.");
            }

            return material;
        }
    }
}

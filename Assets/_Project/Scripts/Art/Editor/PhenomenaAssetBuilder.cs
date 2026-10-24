using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Builds the 0.6 lunar phenomena VFX tuning assets (<c>Assets/_Project/Config/*.asset</c>), shaders/materials,
    /// procedural meshes, and playable-preview prefabs (<c>Assets/_Project/Generated/Art/Phenomena/</c>):
    /// 1. <c>FX_LevitatingDustGlow.prefab</c> (<see cref="LevitatingDustGlowPreview"/>)
    /// 2. <c>FX_MicrometeoriteFlash.prefab</c> (<see cref="MicrometeoriteFlashPreview"/>)
    /// 3. <c>FX_MoonquakeDustSlide.prefab</c> (<see cref="MoonquakeDustSlidePreview"/>)
    /// 4. <c>FX_EarthriseLightPreset.prefab</c> (<see cref="EarthriseLightPresetPreview"/>)
    /// </summary>
    public static class PhenomenaAssetBuilder
    {
        public const string HorizonBandMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_DustGlowHorizonBand.asset";
        public const string MoteColumnsMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_DustGlowMoteColumns.asset";
        public const string FacetedMoteShardMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_FacetedMoteShard.asset";
        public const string ImpactCraterMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_ImpactCraterAndGlass.asset";
        public const string ImpactFlashStarMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_ImpactFlashStar.asset";
        public const string SlopeSheetMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_DustSlideSheet.asset";
        public const string ToeSettleMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_DustSlideToeSettle.asset";
        public const string EarthriseDiscMeshPath = ArtPaths.PhenomenaFolder + "/Mesh_EarthriseDisc.asset";

        [MoonBuilder("Art/Phenomena", 108)]
        public static void Build()
        {
            GeneratedAssets.EnsureFolder(ArtPaths.PhenomenaConfigFolder);
            GeneratedAssets.EnsureFolder(ArtPaths.PhenomenaFolder);

            LevitatingDustGlowTuning dustGlowTuning = EnsureTuningAsset<LevitatingDustGlowTuning>(ArtPaths.DustGlowTuningAsset);
            MicrometeoriteFlashTuning flashTuning = EnsureTuningAsset<MicrometeoriteFlashTuning>(ArtPaths.MicrometeoriteTuningAsset);
            MoonquakeDustSlideTuning slideTuning = EnsureTuningAsset<MoonquakeDustSlideTuning>(ArtPaths.MoonquakeSlideTuningAsset);
            EarthriseLightPresetTuning earthriseTuning = EnsureTuningAsset<EarthriseLightPresetTuning>(ArtPaths.EarthrisePresetTuningAsset);

            Material bandMat = GeneratedAssets.CreateOrReplace(CreateDustGlowMaterial(dustGlowTuning, false), ArtPaths.DustGlowBandMaterial);
            Material motesMat = GeneratedAssets.CreateOrReplace(CreateDustGlowMaterial(dustGlowTuning, true), ArtPaths.DustGlowMotesMaterial);
            Material impactGlassMat = GeneratedAssets.CreateOrReplace(CreateImpactGlassMaterial(flashTuning), ArtPaths.ImpactGlassMaterial);
            Material impactFlashMat = GeneratedAssets.CreateOrReplace(CreateImpactFlashMaterial(flashTuning), ArtPaths.ImpactFlashMaterial);
            Material slideSheetMat = GeneratedAssets.CreateOrReplace(CreateDustSlideMaterial(slideTuning, 0f), ArtPaths.DustSlideSheetMaterial);
            Material slideSettleMat = GeneratedAssets.CreateOrReplace(CreateDustSlideMaterial(slideTuning, 1f), ArtPaths.DustSlideSettleMaterial);
            Material slideGrainMat = GeneratedAssets.CreateOrReplace(CreateDustSlideMaterial(slideTuning, 2f), ArtPaths.DustSlideGrainMaterial);
            Material earthDiscMat = GeneratedAssets.CreateOrReplace(CreateEarthriseDiscMaterial(earthriseTuning), ArtPaths.EarthriseDiscMaterial);

            Mesh bandMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateHorizonHazeBandMesh(dustGlowTuning.BandWidthMeters, dustGlowTuning.BandHeightMeters),
                HorizonBandMeshPath);
            Mesh columnsMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateMoteColumnsMesh(
                    dustGlowTuning.ColumnCount,
                    dustGlowTuning.BandWidthMeters,
                    dustGlowTuning.ColumnRadiusMeters,
                    dustGlowTuning.ColumnHeightMeters),
                MoteColumnsMeshPath);
            Mesh shardMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateFacetedMoteShardMesh(),
                FacetedMoteShardMeshPath);
            Mesh craterMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateImpactCraterAndGlassMesh(flashTuning.CraterRadiusMeters),
                ImpactCraterMeshPath);
            Mesh flashStarMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateImpactFlashStarMesh(),
                ImpactFlashStarMeshPath);
            Mesh slopeSheetMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateSlopeSheetMesh(
                    slideTuning.SlopeWidthMeters,
                    slideTuning.SlopeLengthMeters,
                    slideTuning.SlopeAngleDegrees),
                SlopeSheetMeshPath);
            Mesh toeSettleMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateToeSettleApronMesh(
                    slideTuning.SlopeWidthMeters,
                    slideTuning.SlopeLengthMeters,
                    slideTuning.SlopeAngleDegrees,
                    slideTuning.SettleCloudMaxHeightMeters),
                ToeSettleMeshPath);
            Mesh earthDiscMesh = GeneratedAssets.CreateOrReplace(
                PhenomenaMeshes.CreateEarthriseDiscMesh(),
                EarthriseDiscMeshPath);

            BuildDustGlowPrefab(dustGlowTuning, bandMesh, columnsMesh, shardMesh, bandMat, motesMat);
            BuildMicrometeoritePrefab(flashTuning, craterMesh, flashStarMesh, shardMesh, impactGlassMat, impactFlashMat);
            BuildMoonquakeSlidePrefab(slideTuning, slopeSheetMesh, toeSettleMesh, shardMesh, slideSheetMat, slideSettleMat, slideGrainMat);
            BuildEarthrisePresetPrefab(earthriseTuning, earthDiscMesh, earthDiscMat);

            AssetDatabase.SaveAssets();
        }

        public static Shader LoadShader(string path)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
            {
                throw new InvalidOperationException($"PhenomenaAssetBuilder: shader missing at {path}.");
            }

            return shader;
        }

        public static Material CreateDustGlowMaterial(LevitatingDustGlowTuning tuning, bool columnMode)
        {
            Shader shader = LoadShader(ArtPaths.DustGlowShader);
            var mat = new Material(shader) { enableInstancing = true };
            mat.SetColor("_BaseColor", tuning.DayBandColor);
            mat.SetColor("_NightColor", tuning.NightBandColor);
            mat.SetColor("_RimColor", tuning.ShimmerRimColor);
            mat.SetFloat("_Intensity", columnMode ? tuning.MoteMaxIntensity : tuning.BandMaxIntensity);
            mat.SetFloat("_NightBlend", 0f);
            mat.SetFloat("_Phase", 0f);
            mat.SetFloat("_ColumnMode", columnMode ? 1f : 0f);
            mat.SetFloat("_BandSoftness", columnMode ? 1.4f : 1.65f);
            mat.SetFloat("_StreakScale", columnMode ? tuning.ColumnCount * 2f : 14f);
            mat.SetFloat("_BloomCeiling", tuning.BloomCeiling);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        public static Material CreateImpactGlassMaterial(MicrometeoriteFlashTuning tuning)
        {
            Shader shader = LoadShader(ArtPaths.ImpactGlassShader);
            var mat = new Material(shader) { enableInstancing = true };
            mat.SetColor("_BaseColor", tuning.GlassAlbedoColor);
            mat.SetColor("_CraterRimColor", tuning.CraterRimAlbedoColor);
            mat.SetColor("_HotCoreColor", tuning.HotCoreColor);
            mat.SetColor("_WarmAmberColor", tuning.WarmAmberColor);
            mat.SetColor("_DeepEmberColor", tuning.DeepEmberColor);
            mat.SetColor("_PermanentGlintColor", tuning.PermanentGlintColor);
            mat.SetFloat("_Heat", 1f);
            mat.SetFloat("_HeatIntensity", tuning.InitialHeatIntensity);
            mat.SetFloat("_GlintFloor", tuning.PermanentGlintFloor);
            mat.SetFloat("_FlashIntensity", 0f);
            mat.SetFloat("_FlashMode", 0f);
            mat.SetFloat("_SpecPower", 24f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_Cull", (float)CullMode.Back);
            mat.SetFloat("_ZWrite", 1f);
            mat.renderQueue = (int)RenderQueue.Transparent - 10;
            return mat;
        }

        public static Material CreateImpactFlashMaterial(MicrometeoriteFlashTuning tuning)
        {
            Shader shader = LoadShader(ArtPaths.ImpactGlassShader);
            var mat = new Material(shader) { enableInstancing = true };
            mat.SetColor("_BaseColor", tuning.FlashColor);
            mat.SetColor("_HotCoreColor", tuning.FlashColor);
            mat.SetColor("_WarmAmberColor", tuning.WarmAmberColor);
            mat.SetFloat("_FlashIntensity", tuning.FlashPeakIntensity);
            mat.SetFloat("_FlashMode", 1f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.One);
            mat.SetFloat("_Cull", (float)CullMode.Off);
            mat.SetFloat("_ZWrite", 0f);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        public static Material CreateDustSlideMaterial(MoonquakeDustSlideTuning tuning, float slideMode)
        {
            Shader shader = LoadShader(ArtPaths.DustSlideShader);
            var mat = new Material(shader) { enableInstancing = true };
            mat.SetColor("_BaseColor", tuning.DayDustColor);
            mat.SetColor("_NightColor", tuning.NightDustColor);
            mat.SetColor("_ShadowColor", tuning.ShadowDustColor);
            mat.SetFloat("_Opacity", tuning.MaxOpacity);
            mat.SetFloat("_ScrollOffset", 0f);
            mat.SetFloat("_NightBlend", 0f);
            mat.SetFloat("_SlideMode", slideMode);
            mat.SetFloat("_StreakDensity", 14f);
            mat.SetFloat("_BloomCeiling", tuning.BloomCeiling);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        public static Material CreateEarthriseDiscMaterial(EarthriseLightPresetTuning tuning)
        {
            Shader shader = LoadShader(ArtPaths.DustGlowShader);
            var mat = new Material(shader) { enableInstancing = true };
            mat.SetColor("_BaseColor", tuning.EarthKeyColor);
            mat.SetColor("_NightColor", tuning.EarthKeyColor);
            mat.SetColor("_RimColor", tuning.WarmRimColor);
            mat.SetFloat("_Intensity", 1.05f);
            mat.SetFloat("_NightBlend", 0f);
            mat.SetFloat("_ColumnMode", 2f);
            mat.SetFloat("_BandSoftness", 1.2f);
            mat.SetFloat("_StreakScale", 6f);
            mat.SetFloat("_BloomCeiling", 1.15f);
            mat.renderQueue = (int)RenderQueue.Transparent;
            return mat;
        }

        private static T EnsureTuningAsset<T>(string path) where T : ScriptableObject
        {
            return GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<T>(), path);
        }

        private static void BuildDustGlowPrefab(
            LevitatingDustGlowTuning tuning,
            Mesh bandMesh,
            Mesh columnsMesh,
            Mesh shardMesh,
            Material bandMat,
            Material motesMat)
        {
            var root = new GameObject("FX_LevitatingDustGlow");

            MeshRenderer bandRenderer = CreateChildMesh("HorizonBand", root.transform, bandMesh, bandMat);
            MeshRenderer columnsRenderer = CreateChildMesh("MoteColumns", root.transform, columnsMesh, motesMat);
            ParticleSystem moteParticles = CreateChildParticleSystem("RisingMotes", root.transform, shardMesh, motesMat, tuning.TotalMoteCount);

            var preview = root.AddComponent<LevitatingDustGlowPreview>();
            preview.Configure(tuning, bandRenderer, columnsRenderer, moteParticles);
            GeneratedAssets.SavePrefab(root, ArtPaths.DustGlowPrefab);
        }

        private static void BuildMicrometeoritePrefab(
            MicrometeoriteFlashTuning tuning,
            Mesh craterMesh,
            Mesh flashStarMesh,
            Mesh shardMesh,
            Material glassMat,
            Material flashMat)
        {
            var root = new GameObject("FX_MicrometeoriteFlash");

            MeshRenderer craterRenderer = CreateChildMesh("CraterAndGlass", root.transform, craterMesh, glassMat);
            MeshRenderer flashRenderer = CreateChildMesh("FlashBurst", root.transform, flashStarMesh, flashMat);
            flashRenderer.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            ParticleSystem ejectaParticles = CreateChildParticleSystem("EjectaParticles", root.transform, shardMesh, flashMat, tuning.EjectaCount);

            var lightObj = new GameObject("ImpactLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            var impactLight = lightObj.AddComponent<Light>();
            impactLight.type = LightType.Point;
            impactLight.color = tuning.WarmAmberColor;
            impactLight.intensity = tuning.InitialHeatIntensity * 0.85f;
            impactLight.range = 3.2f;
            impactLight.shadows = LightShadows.None;

            var preview = root.AddComponent<MicrometeoriteFlashPreview>();
            preview.Configure(tuning, craterRenderer, flashRenderer, ejectaParticles, impactLight);
            GeneratedAssets.SavePrefab(root, ArtPaths.MicrometeoriteFlashPrefab);
        }

        private static void BuildMoonquakeSlidePrefab(
            MoonquakeDustSlideTuning tuning,
            Mesh slopeSheetMesh,
            Mesh toeSettleMesh,
            Mesh shardMesh,
            Material sheetMat,
            Material settleMat,
            Material grainMat)
        {
            var root = new GameObject("FX_MoonquakeDustSlide");

            MeshRenderer sheetRenderer = CreateChildMesh("SheetStreams", root.transform, slopeSheetMesh, sheetMat);
            MeshRenderer settleRenderer = CreateChildMesh("ToeSettle", root.transform, toeSettleMesh, settleMat);
            ParticleSystem grainParticles = CreateChildParticleSystem("BallisticGrains", root.transform, shardMesh, grainMat, tuning.GrainCount);

            var preview = root.AddComponent<MoonquakeDustSlidePreview>();
            preview.Configure(tuning, sheetRenderer, settleRenderer, grainParticles);
            GeneratedAssets.SavePrefab(root, ArtPaths.MoonquakeDustSlidePrefab);
        }

        private static void BuildEarthrisePresetPrefab(
            EarthriseLightPresetTuning tuning,
            Mesh earthDiscMesh,
            Material earthDiscMat)
        {
            var root = new GameObject("FX_EarthriseLightPreset");

            var keyObj = new GameObject("EarthKeyLight");
            keyObj.transform.SetParent(root.transform, false);
            keyObj.transform.localRotation = Quaternion.Euler(tuning.PeakElevationDegrees, 142f, 0f);
            var keyLight = keyObj.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = tuning.EarthKeyColor;
            keyLight.intensity = tuning.EarthKeyIntensity;
            keyLight.shadows = LightShadows.Soft;

            var rimObj = new GameObject("WarmRimLight");
            rimObj.transform.SetParent(root.transform, false);
            rimObj.transform.localRotation = Quaternion.Euler(18f, -23f, 0f);
            var rimLight = rimObj.AddComponent<Light>();
            rimLight.type = LightType.Directional;
            rimLight.color = tuning.WarmRimColor;
            rimLight.intensity = tuning.WarmRimIntensity;
            rimLight.shadows = LightShadows.None;

            MeshRenderer discRenderer = CreateChildMesh("EarthDisc", root.transform, earthDiscMesh, earthDiscMat);
            discRenderer.transform.localPosition = new Vector3(-3.2f, 5.4f, 30f);

            var preview = root.AddComponent<EarthriseLightPresetPreview>();
            preview.Configure(tuning, keyLight, rimLight, discRenderer);
            GeneratedAssets.SavePrefab(root, ArtPaths.EarthriseLightPresetPrefab);
        }

        private static MeshRenderer CreateChildMesh(string name, Transform parent, Mesh mesh, Material material)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return renderer;
        }

        private static ParticleSystem CreateChildParticleSystem(
            string name,
            Transform parent,
            Mesh shardMesh,
            Material material,
            int maxParticles)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            var ps = child.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = Mathf.Max(8, maxParticles);
            main.startSpeed = 0f;
            main.startLifetime = 10f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.enabled = false;

            var psRenderer = child.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            psRenderer.mesh = shardMesh;
            psRenderer.sharedMaterial = material;
            psRenderer.shadowCastingMode = ShadowCastingMode.Off;
            psRenderer.receiveShadows = false;
            return ps;
        }
    }
}

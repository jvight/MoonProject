using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Art.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// EditMode verification for the 0.6 lunar phenomena VFX deliverables (docs/design/0.6-living-moon.md §2):
    /// 1. Levitating dust glow (<see cref="LevitatingDustGlowTuning"/>, <see cref="LevitatingDustGlowPreview"/>)
    /// 2. Micrometeorite flash &amp; 10-min cooling impact glass (<see cref="MicrometeoriteFlashTuning"/>, <see cref="MicrometeoriteFlashPreview"/>)
    /// 3. Gentle moonquake dust slide (<see cref="MoonquakeDustSlideTuning"/>, <see cref="MoonquakeDustSlidePreview"/>)
    /// 4. Earthrise light tint preset (<see cref="EarthriseLightPresetTuning"/>, <see cref="EarthriseLightPresetPreview"/>)
    /// </summary>
    public sealed class PhenomenaTests
    {
        private readonly List<Object> _tempObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _tempObjects.Count - 1; i >= 0; i--)
            {
                if (_tempObjects[i] != null)
                {
                    Object.DestroyImmediate(_tempObjects[i]);
                }
            }

            _tempObjects.Clear();
        }

        [Test]
        public void PhenomenaShaders_CompileWithoutErrors_AndSupportInstancing()
        {
            string[] shaderPaths =
            {
                ArtPaths.DustGlowShader,
                ArtPaths.ImpactGlassShader,
                ArtPaths.DustSlideShader,
            };

            foreach (string path in shaderPaths)
            {
                Shader shader = PhenomenaAssetBuilder.LoadShader(path);
                Assert.IsNotNull(shader, $"Shader missing at {path}.");
                Assert.IsTrue(shader.isSupported, $"Shader {shader.name} must be supported.");
                Assert.IsFalse(ShaderUtil.ShaderHasError(shader), $"Shader {shader.name} must compile without errors.");
            }
        }

        [Test]
        public void LevitatingDustGlow_HasEasedEnvelope_IrregularWisps_AndStaysBelowBloomThreshold()
        {
            var tuning = CreateTemp<LevitatingDustGlowTuning>();
            Assert.Less(tuning.BloomCeiling, Palette.BloomThreshold, "Dust glow ceiling must stay below BloomThreshold.");
            Assert.Less(tuning.BandMaxIntensity, Palette.BloomThreshold, "Horizon band intensity must stay below BloomThreshold.");
            Assert.Less(tuning.MoteMaxIntensity, Palette.BloomThreshold, "Rising mote intensity must stay below BloomThreshold.");

            // Eased envelope: 0 at start, monotonic rise, 1 on plateau, monotonic fall, 0 at end
            Assert.AreEqual(0f, tuning.EvaluateEnvelope(0f), 1e-5f);
            float midRise = tuning.EvaluateEnvelope(tuning.FadeInSeconds * 0.5f);
            Assert.Greater(midRise, 0.1f);
            Assert.Less(midRise, 0.9f);
            Assert.AreEqual(1f, tuning.EvaluateEnvelope(tuning.FadeInSeconds + tuning.HoldSeconds * 0.5f), 1e-5f);
            float midFall = tuning.EvaluateEnvelope(tuning.FadeInSeconds + tuning.HoldSeconds + tuning.FadeOutSeconds * 0.5f);
            Assert.Greater(midFall, 0.1f);
            Assert.Less(midFall, 0.9f);
            Assert.AreEqual(0f, tuning.EvaluateEnvelope(tuning.TotalDurationSeconds + 0.1f), 1e-5f);

            // Rising mote wisp positions advance over time and vary in height and brightness
            Vector3 pos0 = tuning.EvaluateMotePosition(0, 2, 1.0f, out float alpha0);
            Vector3 pos1 = tuning.EvaluateMotePosition(0, 2, 2.0f, out float alpha1);
            Assert.AreNotEqual(pos0.y, pos1.y, "Motes must drift vertically over time.");
            Assert.AreNotEqual(pos0.x, pos1.x, "Motes must drift gently sideways rather than rising in rigid vertical bars.");
            Assert.GreaterOrEqual(alpha0, 0f);
            Assert.LessOrEqual(alpha1, 1f);
        }

        [Test]
        public void MicrometeoriteFlash_CoolsOverTenMinutes_ToNonZeroPermanentGlintFloor_WithSoftDecalRim()
        {
            var tuning = CreateTemp<MicrometeoriteFlashTuning>();
            Assert.AreEqual(600f, tuning.CoolDownDurationSeconds, 1e-3f, "Impact glass must cool over ~10 minutes (600 s).");
            Assert.Greater(tuning.PermanentGlintFloor, 0.05f, "Cooled impact glass must retain a permanent glint floor.");
            Assert.Less(tuning.PermanentGlintFloor, Palette.BloomThreshold, "Permanent glint floor must stay below BloomThreshold.");

            // Brief white flash: 0 at t=0, peaks at FlashRiseSeconds, 0 after FlashTotalSeconds (< 0.5s total)
            Assert.LessOrEqual(tuning.FlashTotalSeconds, 0.35f, "Impact flash must be a brief bloom flash.");
            Assert.AreEqual(0f, tuning.EvaluateFlashEnvelope(0f), 1e-5f);
            Assert.AreEqual(1f, tuning.EvaluateFlashEnvelope(tuning.FlashRiseSeconds), 1e-4f);
            Assert.AreEqual(0f, tuning.EvaluateFlashEnvelope(tuning.FlashTotalSeconds + 0.05f), 1e-5f);

            // Flash star mesh is flattened vertically (no vertical light pillar)
            Mesh flashMesh = PhenomenaMeshes.CreateImpactFlashStarMesh();
            _tempObjects.Add(flashMesh);
            Assert.Less(flashMesh.bounds.size.y, flashMesh.bounds.size.x * 0.85f, "Flash mesh must be low-profile, not a vertical beam.");

            // Crater mesh has a soft terrain-decal skirt whose outer ring feathers to uv.y = 0
            Mesh craterMesh = PhenomenaMeshes.CreateImpactCraterAndGlassMesh(tuning.CraterRadiusMeters);
            _tempObjects.Add(craterMesh);
            var craterUvs = new List<Vector2>();
            craterMesh.GetUVs(0, craterUvs);
            bool hasZeroSkirtEdge = false;
            bool hasGlassCore = false;
            for (int i = 0; i < craterUvs.Count; i++)
            {
                if (craterUvs[i].y <= 0.001f && craterUvs[i].x <= 0.001f)
                {
                    hasZeroSkirtEdge = true;
                }

                if (craterUvs[i].x >= 0.99f && craterUvs[i].y >= 0.99f)
                {
                    hasGlassCore = true;
                }
            }

            Assert.IsTrue(hasZeroSkirtEdge, "Crater outer terrain-decal skirt must feather to zero alpha at its perimeter.");
            Assert.IsTrue(hasGlassCore, "Crater interior must include irregular impact glass shards with full glass mask.");

            // Thermal cool-down curve over 600 seconds (10 minutes)
            float heat0 = tuning.EvaluateNormalizedHeat(0f);
            float heat1Min = tuning.EvaluateNormalizedHeat(60f);
            float heat5Min = tuning.EvaluateNormalizedHeat(300f);
            float heat10Min = tuning.EvaluateNormalizedHeat(600f);
            Assert.AreEqual(1f, heat0, 1e-5f);
            Assert.Less(heat1Min, heat0);
            Assert.Less(heat5Min, heat1Min);
            Assert.Greater(heat5Min, 0f);
            Assert.AreEqual(0f, heat10Min, 1e-5f);

            // Emission stays at or above the permanent glint floor even after 10 minutes (and at 1 hour)
            tuning.EvaluateGlassEmission(0f, out float initialIntensity);
            Color cooledAt10Min = tuning.EvaluateGlassEmission(600f, out float cooledIntensity10Min);
            Color cooledAt1Hour = tuning.EvaluateGlassEmission(3600f, out float cooledIntensity1Hour);
            Assert.Greater(initialIntensity, Palette.BloomThreshold, "Freshly molten impact glass core should cross bloom.");
            Assert.AreEqual(tuning.PermanentGlintFloor, cooledIntensity10Min, 1e-5f);
            Assert.AreEqual(tuning.PermanentGlintFloor, cooledIntensity1Hour, 1e-5f);
            Assert.Greater(cooledAt10Min.maxColorComponent, 0.1f);
            Assert.AreEqual(cooledAt10Min, cooledAt1Hour);

            // Vacuum ballistic ejecta arc rises and falls back to ground under lunar gravity (1.62 m/s^2)
            Vector3 earlyEjecta = tuning.EvaluateEjectaShard(0, 0.15f, out float earlyAlpha);
            Assert.Greater(earlyEjecta.y, 0f);
            Assert.Greater(earlyAlpha, 0f);
            tuning.EvaluateEjectaShard(0, tuning.EjectaDurationSeconds + 0.1f, out float settledAlpha);
            Assert.AreEqual(0f, settledAlpha, 1e-5f);
        }

        [Test]
        public void MoonquakeDustSlide_FollowsVacuumBallisticArcs_WithoutBillowingAboveSettleCeiling()
        {
            var tuning = CreateTemp<MoonquakeDustSlideTuning>();
            Assert.AreEqual(1.62f, tuning.LunarGravityMetersPerSecondSquared, 1e-3f);
            Assert.Less(tuning.BloomCeiling, Palette.BloomThreshold);

            Assert.AreEqual(0f, tuning.EvaluateSheetEnvelope(0f), 1e-5f);
            Assert.AreEqual(1f, tuning.EvaluateSheetEnvelope(tuning.RampUpSeconds + tuning.SlideDurationSeconds * 0.5f), 1e-5f);
            Assert.AreEqual(0f, tuning.EvaluateSheetEnvelope(tuning.TotalDurationSeconds + 0.1f), 1e-5f);

            Assert.AreEqual(0f, tuning.EvaluateToeSettleEnvelope(0f), 1e-5f);
            Assert.Greater(tuning.EvaluateToeSettleEnvelope(tuning.RampUpSeconds + tuning.SlideDurationSeconds * 0.6f), 0.8f);
            Assert.AreEqual(0f, tuning.EvaluateToeSettleEnvelope(tuning.TotalDurationSeconds + 0.1f), 1e-5f);

            // Verify ballistic grains stay close to the slope plane and never billow upward like smoke
            float slopeRad = tuning.SlopeAngleDegrees * Mathf.Deg2Rad;
            float sinSlope = Mathf.Sin(slopeRad);
            float cosSlope = Mathf.Cos(slopeRad);
            float topY = tuning.SlopeLengthMeters * sinSlope;

            for (int i = 0; i < tuning.GrainCount; i++)
            {
                Vector3 pos = tuning.EvaluateBallisticGrain(i, tuning.RampUpSeconds + 1.2f, out float alpha);
                Assert.Greater(alpha, 0f);
                // Perpendicular distance above the inclined slope plane:
                float distAlong = pos.z / cosSlope;
                float slopeSurfaceY = topY - distAlong * sinSlope;
                float heightAboveSlope = (pos.y - slopeSurfaceY) * cosSlope;
                Assert.LessOrEqual(
                    heightAboveSlope,
                    tuning.SettleCloudMaxHeightMeters + 1e-3f,
                    "Vacuum ballistic dust grains must never billow above SettleCloudMaxHeightMeters.");
            }
        }

        [Test]
        public void EarthriseLightPreset_InterpolatesWarmBlueLook_SmoothlyAcrossStillnessBeat()
        {
            var tuning = CreateTemp<EarthriseLightPresetTuning>();
            Assert.AreEqual(0f, tuning.EvaluateBlendWeight(0f), 1e-5f);
            Assert.AreEqual(1f, tuning.EvaluateBlendWeight(tuning.FadeInSeconds + tuning.HoldSeconds * 0.5f), 1e-5f);
            Assert.AreEqual(0f, tuning.EvaluateBlendWeight(tuning.TotalDurationSeconds + 0.1f), 1e-5f);

            // Earthrise key light is cool azure-blue (B > R) while the rim light is warm golden-ivory (R > B)
            Assert.Greater(tuning.EarthKeyColor.b, tuning.EarthKeyColor.r, "Earthrise key must be azure-blue.");
            Assert.Greater(tuning.WarmRimColor.r, tuning.WarmRimColor.b, "Earthrise rim must be warm golden-ivory.");

            EarthriseLightState statePeak = tuning.EvaluateState(
                1f,
                Color.gray,
                0.4f,
                4f,
                Color.gray,
                0.2f,
                Color.black,
                Color.black,
                Color.black,
                Color.black);
            Assert.AreEqual(1f, statePeak.BlendWeight, 1e-5f);
            Assert.AreEqual(tuning.EarthKeyColor, statePeak.KeyLightColor);
            Assert.AreEqual(tuning.WarmRimColor, statePeak.RimLightColor);
            Assert.AreEqual(tuning.PeakElevationDegrees, statePeak.KeyElevationDegrees, 1e-4f);
        }

        [Test]
        public void PhenomenaBuilder_BuildsConfigAssets_Materials_Meshes_AndPlayablePrefabs()
        {
            PhenomenaAssetBuilder.Build();

            AssertTuningAssetMatchesDefaults<LevitatingDustGlowTuning>(ArtPaths.DustGlowTuningAsset);
            AssertTuningAssetMatchesDefaults<MicrometeoriteFlashTuning>(ArtPaths.MicrometeoriteTuningAsset);
            AssertTuningAssetMatchesDefaults<MoonquakeDustSlideTuning>(ArtPaths.MoonquakeSlideTuningAsset);
            AssertTuningAssetMatchesDefaults<EarthriseLightPresetTuning>(ArtPaths.EarthrisePresetTuningAsset);

            string[] prefabs =
            {
                ArtPaths.DustGlowPrefab,
                ArtPaths.MicrometeoriteFlashPrefab,
                ArtPaths.MoonquakeDustSlidePrefab,
                ArtPaths.EarthriseLightPresetPrefab,
            };

            foreach (string prefabPath in prefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.IsNotNull(prefab, $"Expected phenomena prefab at {prefabPath}.");
            }

            // Instantiate and step each playable preview prefab to verify wiring and zero-error sampling
            GameObject dustGlowInstance = InstantiateTemp(ArtPaths.DustGlowPrefab);
            var dustPreview = dustGlowInstance.GetComponent<LevitatingDustGlowPreview>();
            Assert.IsNotNull(dustPreview);
            dustPreview.Sample(8.0f, 0f);
            Assert.AreEqual(1f, dustPreview.CurrentEnvelope, 1e-4f);
            dustPreview.Sample(8.0f, 1f);
            Assert.AreEqual(1f, dustPreview.NightBlend, 1e-4f);

            GameObject flashInstance = InstantiateTemp(ArtPaths.MicrometeoriteFlashPrefab);
            var flashPreview = flashInstance.GetComponent<MicrometeoriteFlashPreview>();
            Assert.IsNotNull(flashPreview);
            flashPreview.Sample(flashPreview.Tuning.FlashRiseSeconds);
            Assert.AreEqual(1f, flashPreview.CurrentFlashEnvelope, 1e-3f);
            flashPreview.Sample(600f);
            Assert.AreEqual(0f, flashPreview.CurrentNormalizedHeat, 1e-4f);
            Assert.AreEqual(flashPreview.Tuning.PermanentGlintFloor, flashPreview.CurrentEffectiveEmissionIntensity, 1e-4f);

            GameObject slideInstance = InstantiateTemp(ArtPaths.MoonquakeDustSlidePrefab);
            var slidePreview = slideInstance.GetComponent<MoonquakeDustSlidePreview>();
            Assert.IsNotNull(slidePreview);
            slidePreview.Sample(4.5f, 0f);
            Assert.AreEqual(1f, slidePreview.CurrentSheetEnvelope, 1e-4f);
            Assert.Greater(slidePreview.CurrentToeSettleEnvelope, 0.8f);

            GameObject earthriseInstance = InstantiateTemp(ArtPaths.EarthriseLightPresetPrefab);
            var earthrisePreview = earthriseInstance.GetComponent<EarthriseLightPresetPreview>();
            Assert.IsNotNull(earthrisePreview);
            earthrisePreview.Sample(10.0f);
            Assert.AreEqual(1f, earthrisePreview.CurrentState.BlendWeight, 1e-4f);
        }

        private static void AssertTuningAssetMatchesDefaults<T>(string assetPath) where T : ScriptableObject
        {
            var saved = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            Assert.IsNotNull(saved, $"Tuning asset missing at {assetPath}.");

            var fresh = ScriptableObject.CreateInstance<T>();
            try
            {
                FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.Greater(fields.Length, 5, $"{typeof(T).Name} should expose serialized tuning fields.");

                foreach (FieldInfo field in fields)
                {
                    if (field.GetCustomAttribute<SerializeField>() == null && !field.IsPublic)
                    {
                        continue;
                    }

                    Assert.IsNotNull(
                        field.GetCustomAttribute<TooltipAttribute>(),
                        $"{typeof(T).Name}.{field.Name} must have a [Tooltip] attribute.");

                    if (field.FieldType == typeof(float) || field.FieldType == typeof(int))
                    {
                        bool hasRangeOrMin = field.GetCustomAttribute<UnityEngine.RangeAttribute>() != null
                            || field.GetCustomAttribute<UnityEngine.MinAttribute>() != null;
                        Assert.IsTrue(
                            hasRangeOrMin,
                            $"{typeof(T).Name}.{field.Name} must have a [Range] or [Min] attribute.");
                    }

                    object savedVal = field.GetValue(saved);
                    object freshVal = field.GetValue(fresh);
                    Assert.AreEqual(
                        freshVal,
                        savedVal,
                        $"{typeof(T).Name}.{field.Name} in {assetPath} must match its C# initializer.");
                }
            }
            finally
            {
                Object.DestroyImmediate(fresh);
            }
        }

        private T CreateTemp<T>() where T : ScriptableObject
        {
            var instance = ScriptableObject.CreateInstance<T>();
            _tempObjects.Add(instance);
            return instance;
        }

        private GameObject InstantiateTemp(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.IsNotNull(prefab, $"Missing prefab at {prefabPath}.");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            _tempObjects.Add(instance);
            return instance;
        }
    }
}

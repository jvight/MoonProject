using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Art;
using MoonProject.Editor.Automation;
using MoonProject.Rover;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Batch Editor capture runner for the crater-rim slope material (<c>R1_RimSlope.mat</c> / <c>R1Terrain.shader</c>)
    /// and wall-base talus scatter kit (<c>RimTalus_00..07</c> with <c>R1_RimTalus.mat</c>), rendering a 2x2 proof
    /// sheet to <c>docs/art/concepts/rim/sheet.png</c> comparing BEFORE (legacy horizontal stripe-banded slope material)
    /// vs AFTER (triplanar world-space regolith-mantled slope material + darker steep fresh-rock exposures + talus kit)
    /// on both a 25-35 deg test slope and a near-vertical crater-rim face under Day C low sun and Night A earthlight.
    /// </summary>
    public static class R1RimMaterialCapture
    {
        private const int PanelWidth = 1280;
        private const int PanelHeight = 720;
        private const int DividerPx = 8;
        private const float StageZOffset = 80.0f;

        private const string LofiEarthShaderPath = "Assets/_Project/Shaders/World/LofiEarth.shader";
        private const string EarthAlbedoTexturePath = "Assets/_Project/Art/R1/Textures/T_R1_Earth_Albedo.png";

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");
        private static readonly int MoonSunlitGroundId = Shader.PropertyToID("_MoonSunlitGround");
        private static readonly int MoonBlastZoneId = Shader.PropertyToID("_MoonBlastZone");
        private static readonly int MoonBlastHaloId = Shader.PropertyToID("_MoonBlastHalo");
        private static readonly int MoonBlastRaysId = Shader.PropertyToID("_MoonBlastRays");
        private static readonly int MoonReachRingId = Shader.PropertyToID("_MoonReachRing");
        private static readonly int LegacyBandedRimId = Shader.PropertyToID("_LegacyBandedRim");

        private static readonly int MoonEarthDirectionId = Shader.PropertyToID("_MoonEarthDirection");
        private static readonly int MoonEarthAxisXId = Shader.PropertyToID("_MoonEarthAxisX");
        private static readonly int MoonEarthAxisYId = Shader.PropertyToID("_MoonEarthAxisY");
        private static readonly int MoonEarthAxisZId = Shader.PropertyToID("_MoonEarthAxisZ");
        private static readonly int MoonEarthSunId = Shader.PropertyToID("_MoonEarthSun");
        private static readonly int MoonEarthGlowId = Shader.PropertyToID("_MoonEarthGlow");
        private static readonly int MoonEarthAtmosphereId = Shader.PropertyToID("_MoonEarthAtmosphere");
        private static readonly int MoonEarthOceanId = Shader.PropertyToID("_MoonEarthOcean");
        private static readonly int MoonEarthLandId = Shader.PropertyToID("_MoonEarthLand");
        private static readonly int MoonEarthCloudId = Shader.PropertyToID("_MoonEarthCloud");
        private static readonly int MoonEarthAuroraId = Shader.PropertyToID("_MoonEarthAurora");
        private static readonly int MoonEarthAuroraBandId = Shader.PropertyToID("_MoonEarthAuroraBand");
        private static readonly int MoonEclipseRingId = Shader.PropertyToID("_MoonEclipseRing");

        public static readonly string[] RimTalusPrefabPaths =
        {
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_00.prefab",
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_01.prefab",
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_02.prefab",
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_03.prefab",
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_04.prefab",
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_05.prefab",
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_06.prefab",
            "Assets/_Project/Generated/Art/R1/Models/Rocks/RimTalus_07.prefab",
        };

        /// <summary>
        /// Batch entry point invoked via:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1RimMaterialCapture.BuildAndCapture</c>
        /// </summary>
        public static void BuildAndCapture()
        {
            BatchRunner.Run(nameof(R1RimMaterialCapture), args =>
            {
                AssetDatabase.Refresh();
                Shader litShader = R1MaterialLibrary.LoadShader();
                R1MaterialLibrary.BuildMaterials();
                R1MaterialLibrary.BuildRimMaterials(litShader);
                R1ModelBuilder.Build();
                AssetDatabase.SaveAssets();

                string capturesDir = Path.GetFullPath("Logs/captures");
                Directory.CreateDirectory(capturesDir);

                string rimDocsDir = Path.GetFullPath("docs/art/concepts/rim");
                Directory.CreateDirectory(rimDocsDir);

                string beforeDayPath = Path.Combine(capturesDir, "rim_before_dayC.png");
                string afterDayPath = Path.Combine(capturesDir, "rim_after_dayC.png");
                string beforeNightPath = Path.Combine(capturesDir, "rim_before_nightA.png");
                string afterNightPath = Path.Combine(capturesDir, "rim_after_nightA.png");

                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    CaptureRimStage(beforeDayPath, nightMode: false, legacyBeforeMode: true);
                    CaptureRimStage(afterDayPath, nightMode: false, legacyBeforeMode: false);
                    CaptureRimStage(beforeNightPath, nightMode: true, legacyBeforeMode: true);
                    CaptureRimStage(afterNightPath, nightMode: true, legacyBeforeMode: false);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                string sheetOutPath = Path.Combine(rimDocsDir, "sheet.png");
                ComposeTwoByTwoSheet(
                    beforeDayPath,
                    afterDayPath,
                    beforeNightPath,
                    afterNightPath,
                    sheetOutPath);

                string logSheetCopy = Path.Combine(capturesDir, "rim_sheet.png");
                File.Copy(sheetOutPath, logSheetCopy, true);
                BatchRunner.Log($"capture {sheetOutPath}");
                return true;
            });
        }

        private static void CaptureRimStage(string outputPath, bool nightMode, bool legacyBeforeMode)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupRimEnvironment(temp, nightMode);
                SetupLowEarth(temp, nightMode);

                Material rimSlopeAsset = R1MaterialLibrary.LoadRimSlopeMaterial();
                if (rimSlopeAsset == null)
                {
                    throw new InvalidOperationException(
                        $"R1RimMaterialCapture: Missing rim slope material at '{R1MaterialLibrary.RimSlopeMaterialPath}'.");
                }

                Material activeRimMat = UnityEngine.Object.Instantiate(rimSlopeAsset);
                activeRimMat.name = legacyBeforeMode ? "R1_RimSlope_LegacyBefore" : "R1_RimSlope_TriplanarAfter";
                activeRimMat.SetFloat(LegacyBandedRimId, legacyBeforeMode ? 1.0f : 0.0f);
                temp.Add(activeRimMat);

                // Build deterministic test terrain featuring:
                // - Left side (x in -24..-0.5 m): 25-35 deg test slope (30 deg mean inclination, rounded shoulder)
                // - Right side (x in +0.5..+24 m): 28 deg lower apron + 76-82 deg near-vertical crater-rim headwall + rounded crest
                Mesh rimStageMesh = BuildTestRimSlopeAndVerticalFaceMesh();
                temp.Add(rimStageMesh);

                var stageObj = new GameObject("RimSlopeAndVerticalWallStage");
                stageObj.transform.position = new Vector3(0f, 0f, StageZOffset);
                var mf = stageObj.AddComponent<MeshFilter>();
                mf.sharedMesh = rimStageMesh;
                var mr = stageObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = activeRimMat;
                mr.shadowCastingMode = ShadowCastingMode.On;
                mr.receiveShadows = true;
                temp.Add(stageObj);

                if (!legacyBeforeMode)
                {
                    ScatterWallBaseTalusKit(temp);
                }

                // Rover 07 (1.59 m tall scale reference) on the flat regolith floor in front of the slope & wall base
                float roverY = EvaluateRimStageHeight(-3.4f, -0.8f);
                SpawnRover07WithHeadlamp(temp, new Vector3(-3.4f, roverY, -0.8f + StageZOffset), 32f, nightMode);

                RenderShot(
                    new Vector3(0.0f, 3.35f, -10.60f + StageZOffset),
                    new Vector3(0.35f, 5.40f, 11.50f + StageZOffset),
                    50f,
                    bg,
                    outputPath,
                    legacyBeforeMode
                        ? (nightMode ? "BEFORE: NIGHT A EARTHLIGHT (BANDED 25-35 DEG SLOPE + VERTICAL FACE)"
                                     : "BEFORE: DAY C LOW SUN (BANDED 25-35 DEG SLOPE + VERTICAL FACE)")
                        : (nightMode ? "AFTER: NIGHT A EARTHLIGHT (TRIPLANAR R1_RIMSLOPE + RIMTALUS_00..07)"
                                     : "AFTER: DAY C LOW SUN (TRIPLANAR R1_RIMSLOPE + RIMTALUS_00..07)"));
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void ScatterWallBaseTalusKit(ArtLightingPreview.TemporaryObjects temp)
        {
            // Place all 8 RimTalus_00..07 pieces along the base of the near-vertical face and the foot of the 25-35 deg slope,
            // plus additional varied instances so the wall base reads as a weathered lunar talus apron.
            (int idx, Vector3 pos, float yaw, float sink, float scale)[] placements =
            {
                // Anchor boulders (3.0 - 3.9 m) tucked right against the break-in-slope at the base of the near-vertical wall
                (7, new Vector3(5.40f, 0f, 5.45f), -28f, 0.24f, 1.00f),
                (6, new Vector3(2.05f, 0f, 5.30f), 42f, 0.22f, 1.00f),
                (5, new Vector3(8.35f, 0f, 5.50f), -64f, 0.20f, 1.00f),

                // Medium slump & hummock blocks (1.2 - 2.2 m) along the mid-apron and slope transition
                (4, new Vector3(0.35f, 0f, 4.35f), 18f, 0.16f, 1.00f),
                (3, new Vector3(3.75f, 0f, 3.70f), -46f, 0.14f, 1.00f),
                (2, new Vector3(6.55f, 0f, 3.55f), 74f, 0.12f, 1.00f),
                (4, new Vector3(-2.15f, 0f, 4.10f), -32f, 0.16f, 0.92f),
                (3, new Vector3(-5.10f, 0f, 3.35f), 54f, 0.14f, 0.95f),

                // Smaller rounded scree wedges & cobbles (0.55 - 1.2 m) feathering out onto the regolith floor
                (1, new Vector3(1.65f, 0f, 2.25f), -18f, 0.06f, 1.00f),
                (0, new Vector3(0.30f, 0f, 1.45f), 36f, 0.05f, 1.00f),
                (2, new Vector3(4.70f, 0f, 2.10f), -82f, 0.08f, 0.88f),
                (1, new Vector3(2.95f, 0f, 1.30f), 112f, 0.06f, 0.94f),
                (0, new Vector3(5.90f, 0f, 1.65f), -44f, 0.05f, 1.05f),
                (1, new Vector3(-0.95f, 0f, 2.40f), 64f, 0.07f, 0.96f),
                (0, new Vector3(-1.80f, 0f, 1.35f), -52f, 0.05f, 1.00f),
            };

            Material talusMat = R1MaterialLibrary.LoadRimTalusMaterial();

            for (int i = 0; i < placements.Length; i++)
            {
                var p = placements[i];
                string prefabPath = RimTalusPrefabPaths[p.idx];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        $"R1RimMaterialCapture: Missing generated talus prefab at '{prefabPath}'.");
                }

                float groundY = EvaluateRimStageHeight(p.pos.x, p.pos.z) - p.sink;
                Vector3 groundNormal = EvaluateRimStageNormal(p.pos.x, p.pos.z);
                Quaternion slopeAlign = Quaternion.FromToRotation(Vector3.up, Vector3.Slerp(Vector3.up, groundNormal, 0.72f));
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.transform.position = new Vector3(p.pos.x, groundY, p.pos.z + StageZOffset);
                instance.transform.rotation = slopeAlign * Quaternion.Euler(0f, p.yaw, 0f) * prefab.transform.localRotation;
                instance.transform.localScale = prefab.transform.localScale * p.scale;

                if (talusMat != null)
                {
                    foreach (Renderer r in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        r.sharedMaterial = talusMat;
                    }
                }

                temp.Add(instance);
            }
        }

        private static Vector3 EvaluateRimStageNormal(float x, float z)
        {
            const float eps = 0.15f;
            float hx1 = EvaluateRimStageHeight(x + eps, z);
            float hx0 = EvaluateRimStageHeight(x - eps, z);
            float hz1 = EvaluateRimStageHeight(x, z + eps);
            float hz0 = EvaluateRimStageHeight(x, z - eps);
            return new Vector3(-(hx1 - hx0) / (2f * eps), 1f, -(hz1 - hz0) / (2f * eps)).normalized;
        }

        private static Mesh BuildTestRimSlopeAndVerticalFaceMesh()
        {
            const int nx = 180;
            const int nz = 140;
            const float xMin = -26.0f;
            const float xMax = 26.0f;
            const float zMin = -8.0f;
            const float zMax = 30.0f;

            int vCount = (nx + 1) * (nz + 1);
            var vertices = new Vector3[vCount];
            var normals = new Vector3[vCount];
            var colors = new Color32[vCount];

            Color dustLight = Palette.Get(PaletteSwatch.DustLight);
            Color dustMid = Palette.Get(PaletteSwatch.DustMid);
            Color rockLight = Palette.Get(PaletteSwatch.RockLight);
            Color rockDark = Palette.Get(PaletteSwatch.RockDark);
            Color charcoal = Palette.Get(PaletteSwatch.Charcoal);

            for (int iz = 0; iz <= nz; iz++)
            {
                float tz = (float)iz / nz;
                float z = Mathf.Lerp(zMin, zMax, tz);
                for (int ix = 0; ix <= nx; ix++)
                {
                    float tx = (float)ix / nx;
                    float x = Mathf.Lerp(xMin, xMax, tx);
                    int idx = iz * (nx + 1) + ix;

                    float y = EvaluateRimStageHeight(x, z);
                    vertices[idx] = new Vector3(x, y, z);

                    // Central-difference analytic normal
                    const float eps = 0.12f;
                    float hx1 = EvaluateRimStageHeight(x + eps, z);
                    float hx0 = EvaluateRimStageHeight(x - eps, z);
                    float hz1 = EvaluateRimStageHeight(x, z + eps);
                    float hz0 = EvaluateRimStageHeight(x, z - eps);
                    Vector3 n = new Vector3(-(hx1 - hx0) / (2f * eps), 1f, -(hz1 - hz0) / (2f * eps)).normalized;
                    normals[idx] = n;

                    // Compute vertex colors matching TerrainPainter.PaintVertex's periodic elevation terrace bands
                    // so the BEFORE panel reproduces the legacy horizontal stripe banding and the AFTER panel proves
                    // that vertex-color and shader banding are completely eliminated.
                    float slopeDeg = Mathf.Acos(Mathf.Clamp(n.y, -1f, 1f)) * Mathf.Rad2Deg;
                    float slopeWeight = Mathf.SmoothStep(14f, 34f, slopeDeg);
                    float patch = Mathf.Sin(x * 0.08f + z * 0.06f) * 0.35f + Mathf.Cos(x * 0.05f - z * 0.09f) * 0.25f;
                    float terracePhase = y / 3.2f + 0.35f * patch;
                    float terraceFrac = terracePhase - Mathf.Floor(terracePhase);
                    float terraceRiser = Mathf.SmoothStep(0.38f, 0.62f, terraceFrac)
                        * (1f - Mathf.SmoothStep(0.72f, 0.92f, terraceFrac));

                    Color baseDust = Color.Lerp(dustMid, dustLight, 0.35f + 0.25f * patch);
                    Color rockCol = Color.Lerp(rockLight, rockDark, terraceRiser * 0.85f);
                    rockCol = Color.Lerp(rockCol, charcoal, terraceRiser * 0.48f);
                    Color finalCol = Color.Lerp(baseDust, rockCol, slopeWeight * (0.45f + 0.55f * terraceRiser));
                    colors[idx] = finalCol;
                }
            }

            var triangles = new int[nx * nz * 6];
            int tIdx = 0;
            for (int iz = 0; iz < nz; iz++)
            {
                for (int ix = 0; ix < nx; ix++)
                {
                    int a = iz * (nx + 1) + ix;
                    int b = a + 1;
                    int c = a + (nx + 1);
                    int d = c + 1;

                    triangles[tIdx++] = a;
                    triangles[tIdx++] = c;
                    triangles[tIdx++] = d;
                    triangles[tIdx++] = a;
                    triangles[tIdx++] = d;
                    triangles[tIdx++] = b;
                }
            }

            var mesh = new Mesh
            {
                name = "RimSlopeAndVerticalFaceStageMesh",
                indexFormat = IndexFormat.UInt32,
            };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float EvaluateRimStageHeight(float x, float z)
        {
            // Foreground crater floor (z <= 1.5 m) is gently undulating flat regolith
            float floorUndulation = 0.06f * Mathf.Sin(x * 0.22f + z * 0.18f)
                                  + 0.04f * Mathf.Cos(x * 0.14f - z * 0.27f);
            if (z <= 1.2f)
            {
                return floorUndulation * Mathf.Clamp01((z + 6.0f) / 7.2f);
            }

            // Smooth left-to-right blend across x in [-1.8, +1.8]:
            //   rightBlend = 0 -> Left side: 25-35 deg regolith-mantled test slope (mean 30 deg = tan 0.577)
            //   rightBlend = 1 -> Right side: 28 deg lower apron + 78-82 deg near-vertical crater-rim face + rounded crest
            float tSide = Mathf.Clamp01((x + 1.8f) / 3.6f);
            float rightBlend = tSide * tSide * (3f - 2f * tSide);

            // --- LEFT SIDE: 25-35 deg Test Slope (mean 30 deg = tan(30 deg) = 0.5774) ---
            float dzSlope = Mathf.Max(0f, z - 1.5f);
            // Smooth toe transition over the first 2.2 m, then constant 30 deg slope with subtle 3D shoulder swells
            // that vary the local inclination continuously across 25..35 deg.
            float toeSmooth = dzSlope * dzSlope / (dzSlope + 1.4f);
            float shoulderSwell = 0.52f * Mathf.Sin(x * 0.28f + dzSlope * 0.16f)
                                + 0.34f * Mathf.Cos(x * 0.19f - dzSlope * 0.22f);
            // Soft upper shoulder rounding above z = 24 m
            float slopeRise = Mathf.Min(toeSmooth, 22.0f + ( Mathf.Max(0f, toeSmooth - 22.0f) * 0.35f ));
            float leftSlopeY = slopeRise * 0.585f + shoulderSwell * Mathf.Clamp01(dzSlope / 5.0f);

            // --- RIGHT SIDE: 28 deg Lower Talus Apron + Near-Vertical Crater-Rim Face (78-82 deg) + Rounded Crest ---
            // 1. Lower apron from z = 1.2 to z = 6.6 m at ~27 deg (tan 27 deg = 0.51)
            float dzApron = Mathf.Max(0f, z - 1.2f);
            float apronToe = dzApron * dzApron / (dzApron + 1.1f);
            float apronY = Mathf.Min(apronToe, 5.4f) * 0.48f;

            // 2. Near-vertical cliff scarp from z = 6.4 to z = 9.4 m (rising 11.8 m over 2.6 m -> atan(4.54) = 77.6 deg,
            //    with crag buttresses steepening local faces to 82 deg)
            float cragOffset = 0.38f * Mathf.Sin(x * 0.55f + 0.8f) + 0.22f * Mathf.Cos(x * 0.95f - 0.4f);
            float zCliffStart = 6.5f + cragOffset;
            float cliffT = Mathf.Clamp01((z - zCliffStart) / 2.65f);
            // Quintic S-curve for cliff profile with rounded upper shoulder
            float cliffSmooth = cliffT * cliffT * (3f - 2f * cliffT);
            float cragRelief = (0.42f * Mathf.Sin(x * 0.72f + z * 0.35f)
                              + 0.26f * Mathf.Cos(x * 1.15f - z * 0.45f)) * Mathf.Sin(cliffT * Mathf.PI);
            float cliffHeight = 11.6f * cliffSmooth + cragRelief;

            // 3. Upper regolith-mantled rim shoulder above the cliff crest (z > 9.1 m, ~26 deg gentle rise)
            float dzUpper = Mathf.Max(0f, z - (zCliffStart + 2.65f));
            float upperShoulderY = dzUpper * 0.42f + 0.28f * Mathf.Sin(x * 0.32f + dzUpper * 0.24f);

            float rightWallY = apronY + cliffHeight + upperShoulderY;

            return floorUndulation + Mathf.Lerp(leftSlopeY, rightWallY, rightBlend);
        }

        private static Color SetupRimEnvironment(ArtLightingPreview.TemporaryObjects temp, bool nightMode)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));
            Shader.SetGlobalVector(MoonBlastZoneId, Vector4.zero);
            Shader.SetGlobalVector(MoonBlastHaloId, Vector4.zero);
            Shader.SetGlobalVector(MoonBlastRaysId, Vector4.zero);
            Shader.SetGlobalVector(MoonReachRingId, Vector4.zero);
            Shader.SetGlobalFloat(MoonSunlitGroundId, nightMode ? 0.0f : 1.0f);

            Color bgColor;
            if (nightMode)
            {
                // Night A (Earthlight): deep indigo-black sky + cool blue Earthshine key
                RenderSettings.ambientSkyColor = new Color(0.22f, 0.30f, 0.48f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.16f, 0.24f, 0.38f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.10f, 0.15f, 0.25f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.015f, 0.025f, 0.055f, 1f);

                Light earthLight = ArtLightingPreview.NewLight(
                    "NightAEarthshine",
                    LightType.Directional,
                    new Color(0.64f, 0.78f, 0.98f, 1f),
                    1.55f);
                earthLight.transform.rotation = Quaternion.Euler(22f, -36f, 0f);
                earthLight.shadows = LightShadows.Soft;
                earthLight.shadowStrength = 0.72f;
                RenderSettings.sun = earthLight;
                temp.Add(earthLight.gameObject);

                Light rimFill = ArtLightingPreview.NewLight(
                    "NightARimFill",
                    LightType.Directional,
                    new Color(0.48f, 0.62f, 0.88f, 1f),
                    0.55f);
                rimFill.transform.rotation = Quaternion.Euler(26f, 44f, 0f);
                rimFill.shadows = LightShadows.None;
                temp.Add(rimFill.gameObject);
            }
            else
            {
                // Day C (Low Sun): oblique grazing polar sun casting raking light across the 25-35 deg slope and near-vertical cliff
                RenderSettings.ambientSkyColor = new Color(0.38f, 0.42f, 0.52f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.30f, 0.34f, 0.44f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.20f, 0.23f, 0.30f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.022f, 0.032f, 0.060f, 1f);

                Light lowSun = ArtLightingPreview.NewLight(
                    "DayCLowPolarSun",
                    LightType.Directional,
                    new Color(1.00f, 0.96f, 0.88f, 1f),
                    3.25f);
                lowSun.transform.rotation = Quaternion.Euler(14.0f, -48f, 0f);
                lowSun.shadows = LightShadows.Soft;
                lowSun.shadowStrength = 0.86f;
                RenderSettings.sun = lowSun;
                temp.Add(lowSun.gameObject);

                Light earthFill = ArtLightingPreview.NewLight(
                    "DayCEarthshineFill",
                    LightType.Directional,
                    new Color(0.66f, 0.76f, 0.94f, 1f),
                    0.68f);
                earthFill.transform.rotation = Quaternion.Euler(28f, 48f, 0f);
                earthFill.shadows = LightShadows.None;
                temp.Add(earthFill.gameObject);
            }

            DynamicGI.UpdateEnvironment();

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temp.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(nightMode ? 0.46f : 0.32f);
            bloom.scatter.Override(0.60f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("R1RimProofVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temp.Add(volumeObj);

            return bgColor;
        }

        private static void SetupLowEarth(ArtLightingPreview.TemporaryObjects temp, bool nightMode)
        {
            Shader earthShader = AssetDatabase.LoadAssetAtPath<Shader>(LofiEarthShaderPath);
            Texture2D earthTex = AssetDatabase.LoadAssetAtPath<Texture2D>(EarthAlbedoTexturePath);
            if (earthShader != null)
            {
                Vector3 earthDir = new Vector3(-0.18f, 0.34f, 0.92f).normalized;
                float angularRadius = 0.125f;
                Vector3 axisZ = -earthDir;
                Vector3 axisX = Vector3.Cross(Vector3.up, axisZ).normalized;
                Vector3 axisY = Vector3.Cross(axisZ, axisX).normalized;
                Vector3 sunDir = new Vector3(-0.74f, 0.28f, -0.61f).normalized;

                Shader.SetGlobalVector(MoonEarthDirectionId, new Vector4(earthDir.x, earthDir.y, earthDir.z, angularRadius));
                Shader.SetGlobalVector(MoonEarthAxisXId, new Vector4(axisX.x, axisX.y, axisX.z, Mathf.Tan(angularRadius)));
                Shader.SetGlobalVector(MoonEarthAxisYId, new Vector4(axisY.x, axisY.y, axisY.z, 0f));
                Shader.SetGlobalVector(MoonEarthAxisZId, new Vector4(axisZ.x, axisZ.y, axisZ.z, 0f));
                Shader.SetGlobalVector(MoonEarthSunId, new Vector4(sunDir.x, sunDir.y, sunDir.z, 0.035f));
                Shader.SetGlobalVector(MoonEarthGlowId, new Vector4(nightMode ? 1.05f : 0.92f, 0f, 2.68f, 0f));
                Shader.SetGlobalVector(MoonEarthAtmosphereId, new Vector4(0.18f, 0.54f, 0.98f, 3.4f));
                Shader.SetGlobalVector(MoonEarthOceanId, new Vector4(0.05f, 0.24f, 0.54f, 1f));
                Shader.SetGlobalVector(MoonEarthLandId, new Vector4(0.20f, 0.44f, 0.24f, 1f));
                Shader.SetGlobalVector(MoonEarthCloudId, new Vector4(0.90f, 0.94f, 0.98f, 1f));
                Shader.SetGlobalVector(MoonEarthAuroraId, new Vector4(0.06f, 0.34f, 0.20f, 0f));
                Shader.SetGlobalVector(MoonEarthAuroraBandId, new Vector4(0.82f, 0.06f, 0.5f, 0f));
                Shader.SetGlobalVector(MoonEclipseRingId, new Vector4(0.90f, 0.30f, 0.10f, 0.04f));

                var earthMat = new Material(earthShader) { name = "RimProofEarthMat" };
                if (earthTex != null)
                {
                    earthMat.SetTexture("_EarthAlbedoMap", earthTex);
                }

                temp.Add(earthMat);

                var quadMesh = new Mesh { name = "RimProofEarthQuad" };
                quadMesh.vertices = new[]
                {
                    new Vector3(-1.15f, -1.15f, 0f),
                    new Vector3(1.15f, -1.15f, 0f),
                    new Vector3(1.15f, 1.15f, 0f),
                    new Vector3(-1.15f, 1.15f, 0f),
                };
                quadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                quadMesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4000f);
                temp.Add(quadMesh);

                var earthObj = new GameObject("RimProofEarth");
                var mf = earthObj.AddComponent<MeshFilter>();
                mf.sharedMesh = quadMesh;
                var mr = earthObj.AddComponent<MeshRenderer>();
                mr.sharedMaterial = earthMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveShadows = false;
                temp.Add(earthObj);
            }
        }

        private static GameObject SpawnRover07WithHeadlamp(
            ArtLightingPreview.TemporaryObjects temp,
            Vector3 position,
            float yawDeg,
            bool nightMode)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(R1RoverCapture.RoverPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1RimMaterialCapture: Missing Rover prefab at '{R1RoverCapture.RoverPrefabPath}'.");
            }

            var rover = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rover.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDeg, 0f));
            temp.Add(rover);

            Transform screenNode = FindChildRecursive(rover.transform, "Screen");
            if (screenNode != null)
            {
                var renderer = screenNode.GetComponent<Renderer>();
                if (renderer != null)
                {
                    var tuning = ScriptableObject.CreateInstance<FaceTuning>();
                    temp.Add(tuning);
                    var display = screenNode.GetComponent<FaceDisplay>();
                    if (display == null)
                    {
                        display = screenNode.gameObject.AddComponent<FaceDisplay>();
                    }

                    display.Initialize(tuning, renderer, 42);
                    display.State.SnapToFeeling(FaceFeeling.Calm);
                    display.ApplyToRenderer();
                }
            }

            if (nightMode)
            {
                Light eyeFill = ArtLightingPreview.NewLight(
                    "Rover07EyeWarmFill",
                    LightType.Point,
                    new Color(1.0f, 0.72f, 0.34f, 1f),
                    1.35f);
                eyeFill.transform.position = position + Quaternion.Euler(0f, yawDeg, 0f) * new Vector3(0f, 1.22f, 1.05f);
                eyeFill.range = 4.5f;
                eyeFill.shadows = LightShadows.None;
                temp.Add(eyeFill.gameObject);

                Light headlamp = ArtLightingPreview.NewLight(
                    "Rover07HeadlampCone",
                    LightType.Spot,
                    new Color(1.0f, 0.90f, 0.72f, 1f),
                    4.8f);
                Vector3 lampOrigin = position + new Vector3(0f, 1.15f, 0.20f);
                headlamp.transform.position = lampOrigin;
                headlamp.transform.rotation = Quaternion.LookRotation(new Vector3(1.5f, 1.8f, 6.5f + StageZOffset) - lampOrigin, Vector3.up);
                headlamp.range = 16f;
                headlamp.spotAngle = 68f;
                headlamp.innerSpotAngle = 34f;
                headlamp.shadows = LightShadows.Soft;
                temp.Add(headlamp.gameObject);
            }

            return rover;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.name, name, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform hit = FindChildRecursive(root.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        private static void RenderShot(
            Vector3 camPos,
            Vector3 lookAt,
            float fov,
            Color bgColor,
            string outputPath,
            string labelText)
        {
            var camObj = new GameObject("[R1RimProofCamera]") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = bgColor;
                cam.nearClipPlane = 0.05f;
                cam.farClipPlane = 2000f;
                cam.fieldOfView = fov;
                cam.allowHDR = true;
                cam.transform.position = camPos;
                cam.transform.rotation = Quaternion.LookRotation(lookAt - camPos, Vector3.up);

                UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.renderShadows = true;

                var warmRt = RenderTexture.GetTemporary(64, 64, 24, RenderTextureFormat.DefaultHDR);
                try
                {
                    cam.targetTexture = warmRt;
                    cam.Render();
                    cam.targetTexture = null;
                }
                finally
                {
                    RenderTexture.ReleaseTemporary(warmRt);
                }

                var rt = RenderTexture.GetTemporary(PanelWidth, PanelHeight, 24, RenderTextureFormat.DefaultHDR);
                RenderTexture prevActive = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = null;

                    RenderTexture.active = rt;
                    var tex = new Texture2D(PanelWidth, PanelHeight, TextureFormat.RGB24, false, false);
                    try
                    {
                        tex.ReadPixels(new Rect(0, 0, PanelWidth, PanelHeight), 0, 0, false);
                        StampBannerText(tex, 18, PanelHeight - 36, labelText);
                        StampBannerText(tex, 24, 18, "LEFT: 25-35 DEG TEST SLOPE      RIGHT: NEAR-VERTICAL FACE (78-82 DEG)");
                        tex.Apply(false);
                        File.WriteAllBytes(outputPath, tex.EncodeToPNG());
                        BatchRunner.Log($"capture {outputPath}");
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(tex);
                    }
                }
                finally
                {
                    RenderTexture.active = prevActive;
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camObj);
            }
        }

        private static void StampBannerText(Texture2D tex, int x0, int y0, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            const int scale = 2;
            int charAdvance = 6 * scale;
            int bannerW = Mathf.Min(tex.width - x0 - 8, text.Length * charAdvance + 16);
            int bannerH = 7 * scale + 10;

            Color bgTint = new Color(0.03f, 0.04f, 0.07f, 0.76f);
            for (int dy = -5; dy < bannerH - 5; dy++)
            {
                int py = y0 + dy;
                if (py < 0 || py >= tex.height)
                {
                    continue;
                }

                for (int dx = -8; dx < bannerW - 8; dx++)
                {
                    int px = x0 + dx;
                    if (px < 0 || px >= tex.width)
                    {
                        continue;
                    }

                    Color cur = tex.GetPixel(px, py);
                    tex.SetPixel(px, py, Color.Lerp(cur, bgTint, bgTint.a));
                }
            }

            Color fg = new Color(0.92f, 0.94f, 0.98f, 1f);
            for (int i = 0; i < text.Length; i++)
            {
                ulong glyph = GetGlyph5x7(char.ToUpperInvariant(text[i]));
                int cx = x0 + i * charAdvance;
                for (int row = 0; row < 7; row++)
                {
                    for (int col = 0; col < 5; col++)
                    {
                        int bitIndex = row * 5 + (4 - col);
                        if (((glyph >> bitIndex) & 1ul) == 0ul)
                        {
                            continue;
                        }

                        for (int sy = 0; sy < scale; sy++)
                        {
                            for (int sx = 0; sx < scale; sx++)
                            {
                                int px = cx + col * scale + sx;
                                int py = y0 + row * scale + sy;
                                if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                                {
                                    tex.SetPixel(px, py, fg);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static ulong GetGlyph5x7(char c)
        {
            switch (c)
            {
                case 'A': return 0b01110_10001_10001_11111_10001_10001_10001ul;
                case 'B': return 0b11110_10001_10001_11110_10001_10001_11110ul;
                case 'C': return 0b01110_10001_10000_10000_10000_10001_01110ul;
                case 'D': return 0b11100_10010_10001_10001_10001_10010_11100ul;
                case 'E': return 0b11111_10000_10000_11110_10000_10000_11111ul;
                case 'F': return 0b11111_10000_10000_11110_10000_10000_10000ul;
                case 'G': return 0b01110_10001_10000_10111_10001_10001_01110ul;
                case 'H': return 0b10001_10001_10001_11111_10001_10001_10001ul;
                case 'I': return 0b01110_00100_00100_00100_00100_00100_01110ul;
                case 'K': return 0b10001_10010_10100_11000_10100_10010_10001ul;
                case 'L': return 0b10000_10000_10000_10000_10000_10000_11111ul;
                case 'M': return 0b10001_11011_10101_10101_10001_10001_10001ul;
                case 'N': return 0b10001_11001_10101_10011_10001_10001_10001ul;
                case 'O': return 0b01110_10001_10001_10001_10001_10001_01110ul;
                case 'P': return 0b11110_10001_10001_11110_10000_10000_10000ul;
                case 'R': return 0b11110_10001_10001_11110_10100_10010_10001ul;
                case 'S': return 0b01111_10000_10000_01110_00001_00001_11110ul;
                case 'T': return 0b11111_00100_00100_00100_00100_00100_00100ul;
                case 'U': return 0b10001_10001_10001_10001_10001_10001_01110ul;
                case 'V': return 0b10001_10001_10001_10001_10001_01010_00100ul;
                case 'W': return 0b10001_10001_10001_10101_10101_11011_10001ul;
                case 'Y': return 0b10001_10001_01010_00100_00100_00100_00100ul;
                case '0': return 0b01110_10001_10011_10101_11001_10001_01110ul;
                case '1': return 0b00100_01100_00100_00100_00100_00100_01110ul;
                case '2': return 0b01110_10001_00001_00110_01000_10000_11111ul;
                case '3': return 0b11110_00001_00001_01110_00001_00001_11110ul;
                case '5': return 0b11111_10000_10000_11110_00001_00001_11110ul;
                case '7': return 0b11111_00001_00010_00100_01000_01000_01000ul;
                case '8': return 0b01110_10001_10001_01110_10001_10001_01110ul;
                case ':': return 0b00000_00100_00100_00000_00100_00100_00000ul;
                case '-': return 0b00000_00000_00000_11111_00000_00000_00000ul;
                case '+': return 0b00000_00100_00100_11111_00100_00100_00000ul;
                case '.': return 0b00000_00000_00000_00000_00000_00100_00100ul;
                case '_': return 0b00000_00000_00000_00000_00000_00000_11111ul;
                case '(': return 0b00010_00100_01000_01000_01000_00100_00010ul;
                case ')': return 0b01000_00100_00010_00010_00010_00100_01000ul;
                default:  return 0ul;
            }
        }

        private static void ComposeTwoByTwoSheet(
            string topLeftPath,
            string topRightPath,
            string bottomLeftPath,
            string bottomRightPath,
            string outPath)
        {
            Texture2D tl = LoadPng(topLeftPath);
            Texture2D tr = LoadPng(topRightPath);
            Texture2D bl = LoadPng(bottomLeftPath);
            Texture2D br = LoadPng(bottomRightPath);
            try
            {
                int totalW = PanelWidth * 2 + DividerPx;
                int totalH = PanelHeight * 2 + DividerPx;
                var canvas = new Texture2D(totalW, totalH, TextureFormat.RGB24, false, false);
                try
                {
                    var bgPixels = new Color32[totalW * totalH];
                    var dividerColor = new Color32(12, 15, 24, 255);
                    for (int i = 0; i < bgPixels.Length; i++)
                    {
                        bgPixels[i] = dividerColor;
                    }

                    canvas.SetPixels32(bgPixels);

                    // Top row: Day C Low Sun (Left = BEFORE banded, Right = AFTER triplanar + talus kit)
                    canvas.SetPixels(0, PanelHeight + DividerPx, PanelWidth, PanelHeight, tl.GetPixels());
                    canvas.SetPixels(PanelWidth + DividerPx, PanelHeight + DividerPx, PanelWidth, PanelHeight, tr.GetPixels());

                    // Bottom row: Night A Earthlight (Left = BEFORE banded, Right = AFTER triplanar + talus kit)
                    canvas.SetPixels(0, 0, PanelWidth, PanelHeight, bl.GetPixels());
                    canvas.SetPixels(PanelWidth + DividerPx, 0, PanelWidth, PanelHeight, br.GetPixels());

                    canvas.Apply(false);
                    File.WriteAllBytes(outPath, canvas.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(canvas);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tl);
                UnityEngine.Object.DestroyImmediate(tr);
                UnityEngine.Object.DestroyImmediate(bl);
                UnityEngine.Object.DestroyImmediate(br);
            }
        }

        private static Texture2D LoadPng(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGB24, false, false);
            tex.LoadImage(bytes, false);
            return tex;
        }
    }
}

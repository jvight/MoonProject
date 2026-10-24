using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Automation;
using MoonProject.Rover;

namespace MoonProject.Art.Editor.Concept
{
    /// <summary>
    /// Batch Editor capture runner for the 0.6 Lunar Phenomena VFX deliverables (docs/design/0.6-living-moon.md §2):
    /// renders 4 time-progression frames for each of the 4 phenomena on a sculpted R1 Mare basalt stage alongside
    /// Rover 07 for scale, covering Night A ("Moon grey") and Day C ("Low sun"), and composites the 4x4 grid into
    /// <c>docs/art/concepts/0.6-phenomena/sheet.png</c>.
    /// <code>
    /// python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.Concept.PhenomenaConceptCapture.Capture
    /// </code>
    /// </summary>
    public static class PhenomenaConceptCapture
    {
        private const int CellWidth = 640;
        private const int CellHeight = 360;
        private const int Columns = 4;
        private const int Rows = 4;
        private const int Padding = 12;
        private const int RowHeaderHeight = 28;

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        public static void Capture()
        {
            BatchRunner.Run(nameof(PhenomenaConceptCapture), args =>
            {
                AssetDatabase.Refresh();
                PaletteAssetBuilder.Build();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();
                PhenomenaAssetBuilder.Build();

                string capturesDir = Path.GetFullPath("Logs/captures/phenomena");
                Directory.CreateDirectory(capturesDir);

                string docsDir = Path.GetFullPath("docs/art/concepts/0.6-phenomena");
                Directory.CreateDirectory(docsDir);

                var cells = new Texture2D[Rows, Columns];
                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    CaptureRow0_LevitatingDustGlow(capturesDir, cells);
                    CaptureRow1_MicrometeoriteFlash(capturesDir, cells);
                    CaptureRow2_MoonquakeDustSlide(capturesDir, cells);
                    CaptureRow3_EarthriseLightPreset(capturesDir, cells);

                    string sheetPath = Path.Combine(docsDir, "sheet.png");
                    WriteCompositeSheet(cells, sheetPath);
                    BatchRunner.Log($"phenomena sheet written to {sheetPath}");
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                    for (int r = 0; r < Rows; r++)
                    {
                        for (int c = 0; c < Columns; c++)
                        {
                            if (cells[r, c] != null)
                            {
                                UnityEngine.Object.DestroyImmediate(cells[r, c]);
                            }
                        }
                    }
                }

                return true;
            });
        }

        // Row 0: Levitating dust glow (Day C onset, Day C peak, Night A peak, Night A fade-out)
        private static void CaptureRow0_LevitatingDustGlow(string outDir, Texture2D[,] cells)
        {
            (string name, float time, bool night)[] frames =
            {
                ("01_dustglow_t0_onset_dayC", 1.8f, false),
                ("01_dustglow_t1_peak_dayC", 10.0f, false),
                ("01_dustglow_t2_peak_nightA", 12.0f, true),
                ("01_dustglow_t3_fade_nightA", 23.2f, true),
            };

            for (int c = 0; c < Columns; c++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temp = new ArtLightingPreview.TemporaryObjects();
                try
                {
                    (string name, float time, bool night) = frames[c];
                    Color bg = SetupMareStage(temp, night, true);
                    SpawnRover07(temp, new Vector3(-2.1f, 0f, -2.2f), 24f, night);

                    GameObject fx = SpawnPrefab(ArtPaths.DustGlowPrefab, new Vector3(0f, 0.05f, 8.5f), 0f, temp);
                    var preview = fx.GetComponent<LevitatingDustGlowPreview>();
                    preview.Sample(time, night ? 1f : 0f);

                    Vector3 camPos = new Vector3(-0.6f, 1.55f, -7.8f);
                    Vector3 lookAt = new Vector3(0.2f, 1.45f, 6.0f);
                    cells[0, c] = RenderCell(camPos, lookAt, 46f, bg, Path.Combine(outDir, name + ".png"));
                }
                finally
                {
                    temp.Dispose();
                }
            }
        }

        // Row 1: Micrometeorite flash & 10-min cooling impact glass
        private static void CaptureRow1_MicrometeoriteFlash(string outDir, Texture2D[,] cells)
        {
            (string name, float time, bool night)[] frames =
            {
                ("02_meteorite_t0_flash_nightA", 0.04f, true),
                ("02_meteorite_t1_molten_nightA", 18.0f, true),
                ("02_meteorite_t2_cooling3m_dayC", 180.0f, false),
                ("02_meteorite_t3_glint10m_nightA", 600.0f, true),
            };

            for (int c = 0; c < Columns; c++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temp = new ArtLightingPreview.TemporaryObjects();
                try
                {
                    (string name, float time, bool night) = frames[c];
                    Color bg = SetupMareStage(temp, night, false);
                    SpawnRover07(temp, new Vector3(-1.85f, 0f, 0.35f), 52f, night);

                    GameObject fx = SpawnPrefab(ArtPaths.MicrometeoriteFlashPrefab, new Vector3(0.45f, 0.02f, 0.85f), 0f, temp);
                    var preview = fx.GetComponent<MicrometeoriteFlashPreview>();
                    preview.Sample(time);

                    Vector3 camPos = new Vector3(-0.55f, 1.45f, -3.45f);
                    Vector3 lookAt = new Vector3(-0.15f, 0.48f, 0.65f);
                    cells[1, c] = RenderCell(camPos, lookAt, 44f, bg, Path.Combine(outDir, name + ".png"));
                }
                finally
                {
                    temp.Dispose();
                }
            }
        }

        // Row 2: Gentle moonquake dust slide (Day C onset, Day C active sheet, Night A active sheet, Day C ballistic toe settle)
        private static void CaptureRow2_MoonquakeDustSlide(string outDir, Texture2D[,] cells)
        {
            (string name, float time, bool night)[] frames =
            {
                ("03_moonquake_t0_onset_dayC", 0.85f, false),
                ("03_moonquake_t1_sheet_dayC", 5.0f, false),
                ("03_moonquake_t2_sheet_nightA", 5.5f, true),
                ("03_moonquake_t3_settle_dayC", 10.2f, false),
            };

            for (int c = 0; c < Columns; c++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temp = new ArtLightingPreview.TemporaryObjects();
                try
                {
                    (string name, float time, bool night) = frames[c];
                    Color bg = SetupMareStage(temp, night, false);
                    BuildMareSlopeBackdrop(temp);
                    SpawnRover07(temp, new Vector3(-2.55f, 0f, 3.85f), 62f, night);

                    GameObject fx = SpawnPrefab(ArtPaths.MoonquakeDustSlidePrefab, new Vector3(0.5f, 0.02f, -2.5f), 0f, temp);
                    var preview = fx.GetComponent<MoonquakeDustSlidePreview>();
                    preview.Sample(time, night ? 1f : 0f);

                    Vector3 camPos = new Vector3(-3.8f, 2.25f, 8.2f);
                    Vector3 lookAt = new Vector3(0.1f, 1.05f, 1.4f);
                    cells[2, c] = RenderCell(camPos, lookAt, 46f, bg, Path.Combine(outDir, name + ".png"));
                }
                finally
                {
                    temp.Dispose();
                }
            }
        }

        // Row 3: Earthrise light tint preset (baseline Night A, rising, full warm-blue stillness beat, receding)
        private static void CaptureRow3_EarthriseLightPreset(string outDir, Texture2D[,] cells)
        {
            (string name, float time)[] frames =
            {
                ("04_earthrise_t0_baseline_nightA", 0.0f),
                ("04_earthrise_t1_climbing", 2.5f),
                ("04_earthrise_t2_stillness_peak", 11.0f),
                ("04_earthrise_t3_receding", 23.0f),
            };

            for (int c = 0; c < Columns; c++)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var temp = new ArtLightingPreview.TemporaryObjects();
                try
                {
                    (string name, float time) = frames[c];
                    Color bg = SetupMareStage(temp, true, true);
                    BuildEarthriseForegroundRocks(temp);
                    SpawnRover07(temp, new Vector3(-1.35f, 0f, -1.8f), 18f, true);

                    // Disable the default stage directional lights so the Earthrise preset prefab drives the scene look
                    Light existingSun = RenderSettings.sun;
                    if (existingSun != null)
                    {
                        existingSun.enabled = false;
                    }

                    GameObject fx = SpawnPrefab(ArtPaths.EarthriseLightPresetPrefab, Vector3.zero, 0f, temp);
                    var preview = fx.GetComponent<EarthriseLightPresetPreview>();
                    preview.Sample(time);
                    bg = preview.CurrentState.SkyBackgroundColor;

                    Vector3 camPos = new Vector3(1.1f, 1.45f, -6.8f);
                    Vector3 lookAt = new Vector3(-1.2f, 1.45f, 4.0f);
                    cells[3, c] = RenderCell(camPos, lookAt, 46f, bg, Path.Combine(outDir, name + ".png"));
                }
                finally
                {
                    temp.Dispose();
                }
            }
        }

        private static Color SetupMareStage(ArtLightingPreview.TemporaryObjects temp, bool nightA, bool distantRidge)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));

            Color bgColor;
            if (nightA)
            {
                // Look A ("Moon grey", night earthshine)
                RenderSettings.ambientSkyColor = new Color(0.12f, 0.15f, 0.23f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.09f, 0.11f, 0.17f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.05f, 0.07f, 0.11f, 1f);

                Light earthKey = ArtLightingPreview.NewLight(
                    "NightAEarthlight",
                    LightType.Directional,
                    new Color(0.58f, 0.68f, 0.95f, 1f),
                    0.52f);
                earthKey.transform.rotation = Quaternion.Euler(24f, 145f, 0f);
                earthKey.shadows = LightShadows.Soft;
                earthKey.shadowStrength = 0.75f;
                RenderSettings.sun = earthKey;
                temp.Add(earthKey.gameObject);

                Light rimFill = ArtLightingPreview.NewLight(
                    "NightARimFill",
                    LightType.Directional,
                    new Color(0.42f, 0.52f, 0.78f, 1f),
                    0.24f);
                rimFill.transform.rotation = Quaternion.Euler(20f, -35f, 0f);
                rimFill.shadows = LightShadows.None;
                temp.Add(rimFill.gameObject);
            }
            else
            {
                // Look C ("Low sun", grazing warm golden sun at ~11 deg elevation)
                RenderSettings.ambientSkyColor = new Color(0.30f, 0.33f, 0.40f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.24f, 0.25f, 0.30f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.16f, 0.16f, 0.19f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.11f, 0.13f, 0.18f, 1f);

                Light sunLight = ArtLightingPreview.NewLight(
                    "DayCLowSun",
                    LightType.Directional,
                    new Color(1.00f, 0.91f, 0.76f, 1f),
                    1.38f);
                sunLight.transform.rotation = Quaternion.Euler(11f, 132f, 0f);
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = 0.82f;
                RenderSettings.sun = sunLight;
                temp.Add(sunLight.gameObject);

                Light fillLight = ArtLightingPreview.NewLight(
                    "DayCCoolFill",
                    LightType.Directional,
                    new Color(0.68f, 0.76f, 0.92f, 1f),
                    0.38f);
                fillLight.transform.rotation = Quaternion.Euler(32f, -48f, 0f);
                fillLight.shadows = LightShadows.None;
                temp.Add(fillLight.gameObject);
            }

            Material basaltMat = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "MareBasaltFloor";
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(24f, 1f, 24f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = basaltMat;
            temp.Add(floor);

            if (distantRidge)
            {
                BuildDistantMareRidge(temp, basaltMat);
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temp.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(nightA ? 0.65f : 0.45f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("PhenomenaPreviewVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temp.Add(volumeObj);

            return bgColor;
        }

        private static void BuildDistantMareRidge(ArtLightingPreview.TemporaryObjects temp, Material basaltMat)
        {
            LowPolyMeshBuilder b = ConceptRegionMeshes.Heightfield(
                new Vector3(0f, -0.2f, 28f),
                64f,
                16,
                (x, z) =>
                {
                    float ridgeT = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(12f, 38f, z));
                    float ridge = ridgeT * 2.4f;
                    float waveT = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(10f, 24f, z));
                    float wave = (Mathf.Sin(x * 0.14f) * 0.75f + Mathf.Cos(z * 0.19f + x * 0.08f) * 0.40f) * waveT;
                    return Mathf.Max(0f, ridge + wave);
                },
                (x, z, y) => y > 1.6f ? PaletteSwatch.RockLight : PaletteSwatch.RockDark);
            GameObject ridgeObj = ArtLightingPreview.MeshObject("MareHorizonRidge", b.ToMesh("MareHorizonRidge"), basaltMat, temp);
            temp.Add(ridgeObj);
        }

        private static void BuildMareSlopeBackdrop(ArtLightingPreview.TemporaryObjects temp)
        {
            Material basaltMat = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);
            // Heightfield slope aligned to FX_MoonquakeDustSlide at (0.5, 0.02, -2.5):
            // crest at z = -2.5 (height ~2.52m) sloping at 25 deg down to the toe at z = +3.12 (height 0.0m)
            // with natural geological ridge undulation along the crest and flanks.
            LowPolyMeshBuilder b = ConceptRegionMeshes.Heightfield(
                new Vector3(0.5f, 0.0f, 0.0f),
                15f,
                20,
                (x, z) =>
                {
                    float flankWave = 0.65f * Mathf.Sin(x * 0.55f + 0.4f) + 0.35f * Mathf.Cos(z * 0.48f - 0.7f);
                    float lateralMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6.9f, 3.1f, Mathf.Abs(x - 0.5f + flankWave * 0.35f)));
                    float slopeT = Mathf.Clamp01((3.12f - z) / 5.62f);
                    float crestUndulation = 1.0f + 0.08f * Mathf.Sin(x * 0.72f - 0.3f) + 0.05f * Mathf.Cos(x * 1.45f + 0.8f);
                    return slopeT * 2.52f * crestUndulation * lateralMask;
                },
                (x, z, y) => y > 1.2f ? PaletteSwatch.RockLight : PaletteSwatch.RockDark);
            RockGenerator.Build(b, 401, 1.15f, RockStyle.Slab, Place.At(new Vector3(-3.3f, 0.0f, 1.2f)));
            RockGenerator.Build(b, 409, 1.05f, RockStyle.Jagged, Place.At(new Vector3(3.6f, 0.0f, 2.5f)));
            GameObject slopeObj = ArtLightingPreview.MeshObject("MareCraterSlope", b.ToMesh("MareCraterSlope"), basaltMat, temp);
            temp.Add(slopeObj);
        }

        private static void BuildEarthriseForegroundRocks(ArtLightingPreview.TemporaryObjects temp)
        {
            Material basaltMat = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);
            var b = new LowPolyMeshBuilder(128);
            RockGenerator.Build(b, 503, 1.10f, RockStyle.Boulder, Place.At(new Vector3(1.6f, 0.0f, 1.2f)));
            RockGenerator.Build(b, 509, 0.85f, RockStyle.Slab, Place.At(new Vector3(-3.8f, 0.0f, 2.6f)));
            GameObject rocksObj = ArtLightingPreview.MeshObject("EarthriseForegroundRocks", b.ToMesh("EarthriseForegroundRocks"), basaltMat, temp);
            temp.Add(rocksObj);
        }

        private static GameObject SpawnRover07(
            ArtLightingPreview.TemporaryObjects temp,
            Vector3 position,
            float yawDeg,
            bool nightA)
        {
            GameObject rover = SpawnPrefab(R1RoverCapture.RoverPrefabPath, position, yawDeg, temp);
            Transform screenNode = FindChild(rover.transform, "Screen");
            if (screenNode != null && screenNode.TryGetComponent(out Renderer screenRenderer))
            {
                var tuning = ScriptableObject.CreateInstance<FaceTuning>();
                temp.Add(tuning);
                var display = screenNode.GetComponent<FaceDisplay>();
                if (display == null)
                {
                    display = screenNode.gameObject.AddComponent<FaceDisplay>();
                }

                display.Initialize(tuning, screenRenderer, 42);
                display.State.SnapToFeeling(FaceFeeling.Calm);
                display.ApplyToRenderer();
            }

            if (nightA)
            {
                Light eyeFill = ArtLightingPreview.NewLight(
                    "Rover07EyeFill",
                    LightType.Point,
                    new Color(1.0f, 0.68f, 0.30f, 1f),
                    1.25f);
                eyeFill.transform.position = position + Quaternion.Euler(0f, yawDeg, 0f) * new Vector3(0f, 1.25f, 1.15f);
                eyeFill.range = 4.2f;
                eyeFill.shadows = LightShadows.None;
                temp.Add(eyeFill.gameObject);
            }

            return rover;
        }

        private static GameObject SpawnPrefab(
            string prefabPath,
            Vector3 position,
            float yawDeg,
            ArtLightingPreview.TemporaryObjects temp)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"PhenomenaConceptCapture: Missing prefab at '{prefabPath}'.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDeg, 0f));
            temp.Add(instance);
            return instance;
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform hit = FindChild(root.GetChild(i), name);
                if (hit != null)
                {
                    return hit;
                }
            }

            return null;
        }

        private static Texture2D RenderCell(Vector3 camPos, Vector3 lookAt, float fov, Color bgColor, string savePath)
        {
            var camObj = new GameObject("[PhenomenaCaptureCamera]") { hideFlags = HideFlags.HideAndDontSave };
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

                var rt = RenderTexture.GetTemporary(CellWidth, CellHeight, 24, RenderTextureFormat.DefaultHDR);
                RenderTexture prevActive = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    // First render warms up synchronous shader variants so Frame 0 never renders uninitialized materials
                    cam.Render();
                    cam.Render();
                    cam.targetTexture = null;

                    RenderTexture.active = rt;
                    var tex = new Texture2D(CellWidth, CellHeight, TextureFormat.RGB24, false, false)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    tex.ReadPixels(new Rect(0, 0, CellWidth, CellHeight), 0, 0, false);
                    tex.Apply(false);
                    File.WriteAllBytes(savePath, tex.EncodeToPNG());
                    return tex;
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

        private static void WriteCompositeSheet(Texture2D[,] cells, string outputPath)
        {
            int totalWidth = Columns * CellWidth + (Columns + 1) * Padding;
            int rowBlockHeight = CellHeight + RowHeaderHeight + Padding;
            int totalHeight = Rows * rowBlockHeight + Padding;

            var sheet = new Texture2D(totalWidth, totalHeight, TextureFormat.RGB24, false, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            try
            {
                Color32 bg = new Color32(14, 18, 28, 255);
                var allPixels = new Color32[totalWidth * totalHeight];
                for (int i = 0; i < allPixels.Length; i++)
                {
                    allPixels[i] = bg;
                }

                sheet.SetPixels32(allPixels);

                Color32[] rowAccentColors =
                {
                    new Color32(228, 196, 148, 255), // Row 0: Levitating Dust Glow (warm silver-gold)
                    new Color32(255, 138, 52, 255),  // Row 1: Micrometeorite Flash & Cooling Impact Glass (molten amber)
                    new Color32(158, 165, 178, 255), // Row 2: Gentle Moonquake Dust Slide (regolith slate)
                    new Color32(118, 176, 250, 255), // Row 3: Earthrise Light Tint Preset (warm-blue cerulean)
                };

                for (int r = 0; r < Rows; r++)
                {
                    // Top-to-bottom row placement (row 0 at top of image)
                    int flippedRow = (Rows - 1) - r;
                    int yBase = Padding + flippedRow * rowBlockHeight;

                    // Draw progress accent bar above each cell to indicate time progression t0..t3
                    for (int c = 0; c < Columns; c++)
                    {
                        int xBase = Padding + c * (CellWidth + Padding);
                        Texture2D cell = cells[r, c];
                        if (cell != null)
                        {
                            sheet.SetPixels(xBase, yBase, CellWidth, CellHeight, cell.GetPixels());
                        }

                        // Draw subtle header strip and time-step progress bar for the cell
                        int barY = yBase + CellHeight + 6;
                        int fillWidth = ((c + 1) * CellWidth) / Columns;
                        Color32 accent = rowAccentColors[r];
                        Color32 dimTrack = new Color32(32, 40, 58, 255);
                        for (int py = barY; py < barY + 6; py++)
                        {
                            for (int px = xBase; px < xBase + CellWidth; px++)
                            {
                                sheet.SetPixel(px, py, (px - xBase) <= fillWidth ? accent : dimTrack);
                            }
                        }
                    }
                }

                sheet.Apply(false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }
    }
}

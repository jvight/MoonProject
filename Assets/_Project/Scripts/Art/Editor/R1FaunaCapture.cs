using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MoonProject.Editor.Automation;
using MoonProject.Rover;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Batch Editor capture runner for the four 0.6 machine fauna species (<c>PanelMite</c>, <c>Sweeper</c>,
    /// <c>SurveyDrone</c>, <c>Trencher</c>) beside Rover 07 (<c>RoverModel.prefab</c>) in Day C ("Low sun")
    /// lighting and Night A / Eclipse lighting with their lamps lit. Writes individual shots to
    /// <c>Logs/captures/fauna_*.png</c> and composites the 6-panel proof sheet to
    /// <c>docs/art/concepts/0.6-fauna/fauna_sheet.png</c>.
    /// </summary>
    public static class R1FaunaCapture
    {
        private const int PanelWidth = 1280;
        private const int PanelHeight = 720;

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        /// <summary>
        /// Batch entry point invoked via:
        /// <c>python tools/unity_batch.py exec --project . --method MoonProject.Art.Editor.R1FaunaCapture.Capture</c>
        /// </summary>
        public static void Capture()
        {
            BatchRunner.Run(nameof(R1FaunaCapture), args =>
            {
                AssetDatabase.Refresh();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();
                R1FaunaBuilder.Build();

                string capturesDir = Path.GetFullPath("Logs/captures");
                Directory.CreateDirectory(capturesDir);

                string docsDir = Path.GetFullPath("docs/art/concepts/0.6-fauna");
                Directory.CreateDirectory(docsDir);

                string miteDayPath = Path.Combine(capturesDir, "fauna_panel_mite_dayC.png");
                string sweeperDayPath = Path.Combine(capturesDir, "fauna_sweeper_dayC.png");
                string droneDayPath = Path.Combine(capturesDir, "fauna_survey_drone_dayC.png");
                string trencherDayPath = Path.Combine(capturesDir, "fauna_trencher_dayC.png");
                string lineupDayPath = Path.Combine(capturesDir, "fauna_all_lineup_dayC.png");
                string nightLampsPath = Path.Combine(capturesDir, "fauna_all_night_lamps.png");
                string sheetPath = Path.Combine(docsDir, "fauna_sheet.png");

                bool prevAsync = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = false;
                try
                {
                    CapturePanelMitesNextTo07(miteDayPath);
                    CaptureSweeperNextTo07(sweeperDayPath);
                    CaptureSurveyDronesNextTo07(droneDayPath);
                    CaptureTrencherNextTo07(trencherDayPath);
                    CaptureFullLineupWith07(lineupDayPath, nightEclipse: false);
                    CaptureFullLineupWith07(nightLampsPath, nightEclipse: true);

                    CompositeContactSheet(
                        new[]
                        {
                            miteDayPath,
                            sweeperDayPath,
                            droneDayPath,
                            trencherDayPath,
                            lineupDayPath,
                            nightLampsPath,
                        },
                        columns: 2,
                        rows: 3,
                        sheetPath);
                }
                finally
                {
                    ShaderUtil.allowAsyncCompilation = prevAsync;
                }

                return true;
            });
        }

        private static void CapturePanelMitesNextTo07(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse: false);

                // Rover 07 on the left, angled toward the line of 7 panel mites crawling in formation
                SpawnRover07(temp, new Vector3(-0.98f, 0f, 0.25f), 152f, nightEclipse: false);

                // Line of 7 PanelMites marching along a curved service path in the foreground right beside 07's wheel
                int miteCount = 7;
                for (int i = 0; i < miteCount; i++)
                {
                    float t = i / (float)(miteCount - 1);
                    float x = Mathf.Lerp(0.12f, 1.15f, t);
                    float z = Mathf.Lerp(-0.78f, 0.45f, t) + Mathf.Sin(t * Mathf.PI) * 0.12f;
                    float yaw = 148f + Mathf.Cos(t * Mathf.PI) * 14f;

                    GameObject mite = SpawnFauna(
                        R1FaunaBuilder.PanelMitePrefabPath,
                        new Vector3(x, 0f, z),
                        yaw,
                        speed: 0.32f,
                        phaseOffset: i * 0.37f,
                        stepTime: 0.85f,
                        temp);
                    AddLampPointLightIfNight(mite, nightEclipse: false, isCyan: false, temp);
                }

                RenderShot(
                    new Vector3(0.18f, 0.92f, -2.05f),
                    new Vector3(-0.12f, 0.38f, -0.05f),
                    40f,
                    bg,
                    outputPath);
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureSweeperNextTo07(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse: false);

                SpawnRover07(temp, new Vector3(-1.22f, 0f, 0.12f), 156f, nightEclipse: false);
                SpawnFauna(
                    R1FaunaBuilder.SweeperPrefabPath,
                    new Vector3(1.08f, 0f, -0.05f),
                    146f,
                    speed: 1.25f,
                    phaseOffset: 0.2f,
                    stepTime: 1.15f,
                    temp);

                RenderShot(
                    new Vector3(0.05f, 1.48f, -3.75f),
                    new Vector3(0.02f, 0.62f, 0.02f),
                    40f,
                    bg,
                    outputPath);
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureSurveyDronesNextTo07(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse: false);

                SpawnRover07(temp, new Vector3(-1.05f, 0f, 0.15f), 154f, nightEclipse: false);

                // Flock of 5 Tycho hex survey drones hovering at staggered heights + 1 landed in front for skid detail
                Vector3[] flockPositions =
                {
                    new Vector3(0.55f, 0.58f, -0.35f),
                    new Vector3(1.28f, 0.92f, 0.10f),
                    new Vector3(0.32f, 1.22f, 0.48f),
                    new Vector3(1.52f, 0.68f, 0.82f),
                    new Vector3(0.88f, 0.00f, -0.82f),
                };
                float[] flockYaws = { 142f, 158f, 134f, 165f, 148f };

                for (int i = 0; i < flockPositions.Length; i++)
                {
                    bool landed = i == flockPositions.Length - 1;
                    GameObject drone = SpawnFauna(
                        R1FaunaBuilder.SurveyDronePrefabPath,
                        flockPositions[i],
                        flockYaws[i],
                        speed: landed ? 0f : 1.35f,
                        phaseOffset: i * 0.53f,
                        stepTime: landed ? 0f : 0.95f,
                        temp);
                    if (landed)
                    {
                        var motion = drone.GetComponent<FaunaIdleMotion>();
                        var landedPose = new FaunaMotionPose { LampEmissionStrength = 1.0f };
                        motion.ApplyPose(in landedPose);
                    }
                }

                RenderShot(
                    new Vector3(0.12f, 1.28f, -3.15f),
                    new Vector3(0.10f, 0.72f, 0.05f),
                    42f,
                    bg,
                    outputPath);
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureTrencherNextTo07(string outputPath)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse: false);

                SpawnRover07(temp, new Vector3(-1.45f, 0f, 0.10f), 156f, nightEclipse: false);
                SpawnFauna(
                    R1FaunaBuilder.TrencherPrefabPath,
                    new Vector3(1.15f, 0f, 0.05f),
                    144f,
                    speed: 0.55f,
                    phaseOffset: 0.3f,
                    stepTime: 1.40f,
                    temp);

                // Lay two ground cable segments trailing out behind the trencher's rear cable spool
                var cablePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(R1StoryPropsBuilder.GroundCableSegmentPrefabPath);
                if (cablePrefab != null)
                {
                    var cable0 = (GameObject)PrefabUtility.InstantiatePrefab(cablePrefab);
                    cable0.transform.SetPositionAndRotation(
                        new Vector3(2.25f, 0f, 1.58f),
                        Quaternion.Euler(0f, 144f, 0f) * cablePrefab.transform.localRotation);
                    temp.Add(cable0);
                }

                RenderShot(
                    new Vector3(0.05f, 1.72f, -4.45f),
                    new Vector3(0.05f, 0.78f, 0.12f),
                    42f,
                    bg,
                    outputPath);
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static void CaptureFullLineupWith07(string outputPath, bool nightEclipse)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temp = new ArtLightingPreview.TemporaryObjects();
            try
            {
                Color bg = SetupEnvironment(temp, nightEclipse);

                // Center-left: Rover 07 for scale
                SpawnRover07(temp, new Vector3(-1.25f, 0f, 0.10f), 158f, nightEclipse);

                // Far left: Sweeper
                GameObject sweeper = SpawnFauna(
                    R1FaunaBuilder.SweeperPrefabPath,
                    new Vector3(-3.35f, 0f, 0.25f),
                    162f,
                    speed: 1.1f,
                    phaseOffset: 0.15f,
                    stepTime: 1.0f,
                    temp);
                AddLampPointLightIfNight(sweeper, nightEclipse, isCyan: false, temp);

                // Foreground center: Line of 6 PanelMites
                for (int i = 0; i < 6; i++)
                {
                    float t = i / 5f;
                    GameObject mite = SpawnFauna(
                        R1FaunaBuilder.PanelMitePrefabPath,
                        new Vector3(Mathf.Lerp(-0.55f, 0.95f, t), 0f, Mathf.Lerp(-1.35f, -1.05f, t)),
                        152f,
                        speed: 0.35f,
                        phaseOffset: i * 0.41f,
                        stepTime: 0.9f,
                        temp);
                    if (i % 2 == 0)
                    {
                        AddLampPointLightIfNight(mite, nightEclipse, isCyan: false, temp);
                    }
                }

                // Mid-right: Flock of 4 SurveyDrones hovering above regolith
                Vector3[] droneOffsets =
                {
                    new Vector3(0.55f, 0.65f, -0.25f),
                    new Vector3(1.15f, 1.05f, 0.25f),
                    new Vector3(0.25f, 1.32f, 0.55f),
                    new Vector3(0.95f, 0.48f, -0.75f),
                };
                for (int i = 0; i < droneOffsets.Length; i++)
                {
                    GameObject drone = SpawnFauna(
                        R1FaunaBuilder.SurveyDronePrefabPath,
                        droneOffsets[i],
                        148f + i * 6f,
                        speed: 1.5f,
                        phaseOffset: i * 0.6f,
                        stepTime: 1.1f,
                        temp);
                    AddLampPointLightIfNight(drone, nightEclipse, isCyan: true, temp);
                }

                // Right: Trencher
                GameObject trencher = SpawnFauna(
                    R1FaunaBuilder.TrencherPrefabPath,
                    new Vector3(2.85f, 0f, 0.30f),
                    142f,
                    speed: 0.60f,
                    phaseOffset: 0.4f,
                    stepTime: 1.25f,
                    temp);
                AddLampPointLightIfNight(trencher, nightEclipse, isCyan: false, temp);

                RenderShot(
                    new Vector3(-0.10f, 1.95f, -5.65f),
                    new Vector3(-0.05f, 0.72f, -0.05f),
                    48f,
                    bg,
                    outputPath);
            }
            finally
            {
                temp.Dispose();
            }
        }

        private static GameObject SpawnFauna(
            string prefabPath,
            Vector3 position,
            float yawDeg,
            float speed,
            float phaseOffset,
            float stepTime,
            ArtLightingPreview.TemporaryObjects temp)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1FaunaCapture: Missing fauna prefab at '{prefabPath}'.");
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetPositionAndRotation(
                position,
                Quaternion.Euler(0f, yawDeg, 0f) * prefab.transform.localRotation);
            temp.Add(instance);

            var motion = instance.GetComponent<FaunaIdleMotion>();
            if (motion != null)
            {
                motion.ValidateOrThrow();
                motion.CacheRestPose();
                motion.PhaseOffsetSeconds = phaseOffset;
                motion.Speed = speed;
                motion.Step(stepTime);
            }

            return instance;
        }

        private static void AddLampPointLightIfNight(
            GameObject faunaInstance,
            bool nightEclipse,
            bool isCyan,
            ArtLightingPreview.TemporaryObjects temp)
        {
            if (!nightEclipse || faunaInstance == null)
            {
                return;
            }

            var motion = faunaInstance.GetComponent<FaunaIdleMotion>();
            if (motion != null && motion.LampRenderer != null)
            {
                // Boost _EmissionStrength slightly at night via MaterialPropertyBlock (never overwriting _EmissionColor)
                var block = new MaterialPropertyBlock();
                motion.LampRenderer.GetPropertyBlock(block);
                block.SetFloat("_EmissionStrength", 1.85f);
                motion.LampRenderer.SetPropertyBlock(block);

                Color lightColor = isCyan
                    ? new Color(0.32f, 0.90f, 1.00f, 1f)
                    : new Color(1.00f, 0.72f, 0.30f, 1f);
                Light lampLight = ArtLightingPreview.NewLight(
                    faunaInstance.name + "_LampGlow",
                    LightType.Point,
                    lightColor,
                    isCyan ? 1.15f : 1.35f);
                lampLight.transform.position = motion.LampRenderer.bounds.center + Vector3.up * 0.08f;
                lampLight.range = isCyan ? 2.4f : 3.2f;
                lampLight.shadows = LightShadows.None;
                temp.Add(lampLight.gameObject);
            }
        }

        private static Color SetupEnvironment(ArtLightingPreview.TemporaryObjects temp, bool nightEclipse)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));

            Color bgColor;
            if (nightEclipse)
            {
                RenderSettings.ambientSkyColor = new Color(0.11f, 0.14f, 0.22f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.08f, 0.10f, 0.16f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.10f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.05f, 0.07f, 0.11f, 1f);

                Light earthLight = ArtLightingPreview.NewLight(
                    "NightAEarthlight",
                    LightType.Directional,
                    new Color(0.58f, 0.68f, 0.95f, 1f),
                    0.52f);
                earthLight.transform.rotation = Quaternion.Euler(26f, 38f, 0f);
                earthLight.shadows = LightShadows.Soft;
                earthLight.shadowStrength = 0.75f;
                RenderSettings.sun = earthLight;
                temp.Add(earthLight.gameObject);

                Light rimLight = ArtLightingPreview.NewLight(
                    "NightARimFill",
                    LightType.Directional,
                    new Color(0.42f, 0.52f, 0.78f, 1f),
                    0.26f);
                rimLight.transform.rotation = Quaternion.Euler(22f, -142f, 0f);
                rimLight.shadows = LightShadows.None;
                temp.Add(rimLight.gameObject);
            }
            else
            {
                // Day C ("Low sun"): 18 deg elevation golden sun + cool lunar sky fill
                RenderSettings.ambientSkyColor = new Color(0.32f, 0.35f, 0.42f, 1f);
                RenderSettings.ambientEquatorColor = new Color(0.25f, 0.26f, 0.30f, 1f);
                RenderSettings.ambientGroundColor = new Color(0.18f, 0.18f, 0.20f, 1f);
                RenderSettings.ambientIntensity = 1.0f;
                bgColor = new Color(0.14f, 0.16f, 0.20f, 1f);

                Light sunLight = ArtLightingPreview.NewLight(
                    "DayCLowSun",
                    LightType.Directional,
                    new Color(1.00f, 0.91f, 0.76f, 1f),
                    1.42f);
                sunLight.transform.rotation = Quaternion.Euler(18f, 38f, 0f);
                sunLight.shadows = LightShadows.Soft;
                sunLight.shadowStrength = 0.78f;
                RenderSettings.sun = sunLight;
                temp.Add(sunLight.gameObject);

                Light fillLight = ArtLightingPreview.NewLight(
                    "DayCCoolFill",
                    LightType.Directional,
                    new Color(0.70f, 0.78f, 0.94f, 1f),
                    0.44f);
                fillLight.transform.rotation = Quaternion.Euler(35f, -142f, 0f);
                fillLight.shadows = LightShadows.None;
                temp.Add(fillLight.gameObject);
            }

            DynamicGI.UpdateEnvironment();

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "R1RegolithFloor";
            UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(16f, 1f, 16f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = R1MaterialLibrary.LoadMaterial(R1MaterialSlot.Basalt);
            temp.Add(floor);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temp.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(Palette.BloomThreshold);
            bloom.intensity.Override(nightEclipse ? 0.65f : 0.45f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("R1FaunaCaptureVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temp.Add(volumeObj);

            return bgColor;
        }

        private static GameObject SpawnRover07(
            ArtLightingPreview.TemporaryObjects temp,
            Vector3 position,
            float yawDeg,
            bool nightEclipse)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(R1RoverCapture.RoverPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"R1FaunaCapture: Missing '{R1RoverCapture.RoverPrefabPath}'.");
            }

            var rover = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rover.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yawDeg, 0f));
            temp.Add(rover);

            Transform screenNode = FindChildRecursive(rover.transform, "Screen");
            if (screenNode != null && screenNode.TryGetComponent(out Renderer renderer))
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

            if (nightEclipse)
            {
                Light eyeWarmFill = ArtLightingPreview.NewLight(
                    "Rover07EyeFill",
                    LightType.Point,
                    new Color(1.0f, 0.68f, 0.30f, 1f),
                    1.35f);
                eyeWarmFill.transform.position = position + Quaternion.Euler(0f, yawDeg, 0f) * new Vector3(0f, 1.25f, 1.15f);
                eyeWarmFill.range = 4.2f;
                eyeWarmFill.shadows = LightShadows.None;
                temp.Add(eyeWarmFill.gameObject);
            }

            return rover;
        }

        private static void RenderShot(Vector3 camPos, Vector3 lookAt, float fov, Color bgColor, string outputPath)
        {
            var camObj = new GameObject("[R1FaunaCaptureCamera]") { hideFlags = HideFlags.HideAndDontSave };
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

        private static void CompositeContactSheet(string[] panelPaths, int columns, int rows, string outputPath)
        {
            const int Border = 8;
            int totalWidth = columns * PanelWidth + (columns + 1) * Border;
            int totalHeight = rows * PanelHeight + (rows + 1) * Border;

            var sheet = new Texture2D(totalWidth, totalHeight, TextureFormat.RGB24, false, false);
            try
            {
                var bgPixels = new Color32[totalWidth * totalHeight];
                var borderColor = new Color32(18, 21, 28, 255);
                for (int i = 0; i < bgPixels.Length; i++)
                {
                    bgPixels[i] = borderColor;
                }

                sheet.SetPixels32(bgPixels);

                for (int index = 0; index < panelPaths.Length; index++)
                {
                    int col = index % columns;
                    int rowFromTop = index / columns;
                    int dstY = (rows - 1 - rowFromTop) * (PanelHeight + Border) + Border;
                    int dstX = col * (PanelWidth + Border) + Border;

                    byte[] bytes = File.ReadAllBytes(panelPaths[index]);
                    var panelTex = new Texture2D(2, 2, TextureFormat.RGB24, false, false);
                    try
                    {
                        panelTex.LoadImage(bytes, false);
                        Color[] pixels = panelTex.GetPixels(0, 0, PanelWidth, PanelHeight);
                        sheet.SetPixels(dstX, dstY, PanelWidth, PanelHeight, pixels);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(panelTex);
                    }
                }

                sheet.Apply(false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                BatchRunner.Log($"composite {outputPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
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
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Automation;
using MoonProject.Rover;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Batch Editor capture helper for 07's expressive CRT face (<see cref="FaceDisplay"/> + <c>MoonProject/R1/CrtFace</c>).
    /// Renders a 5x5 contact sheet to <c>Logs/captures/feelings_sheet.png</c> covering all 9 feelings in studio light,
    /// all 9 feelings in <c>Main.unity</c> under earthlit night, 3 frames of the CRT boot sequence, 1 frame of mid-blink,
    /// low-power mode, and chase-camera-distance (6.0 m) readability shots in both lighting environments.
    /// </summary>
    public static class R1CrtFaceCapture
    {
        public const string RoverPrefabPath = "Assets/_Project/Generated/Art/R1/Models/Rover/RoverModel.prefab";
        public const string OutputSheetRelativePath = "Logs/captures/feelings_sheet.png";

        private const int TileSize = 384;
        private const int GridCols = 5;
        private const int GridRows = 5;
        private const float HeadCloseDistanceM = 1.05f;
        private const float HeadCloseFov = 34f;
        private const float ChaseDistanceM = 6.0f;
        private const float ChaseFov = 35f;

        private static readonly int MoonHazeColorId = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int MoonHazeId = Shader.PropertyToID("_MoonHaze");
        private static readonly int MoonDesaturateId = Shader.PropertyToID("_MoonDesaturate");

        /// <summary>
        /// Batch entry point invoked via:
        /// <c>python tools/unity_batch.py exec --method MoonProject.Art.Editor.R1CrtFaceCapture.Capture</c>
        /// </summary>
        public static void Capture()
        {
            BatchRunner.Run(nameof(R1CrtFaceCapture), args =>
            {
                AssetDatabase.Refresh();
                R1MaterialLibrary.BuildMaterials();
                R1ModelBuilder.Build();

                var roverPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoverPrefabPath);
                if (roverPrefab == null)
                {
                    Debug.LogError($"R1CrtFaceCapture: Missing generated prefab at '{RoverPrefabPath}'.");
                    return false;
                }

                var tiles = new List<CapturedTile>(GridCols * GridRows);
                try
                {
                    if (!CaptureStudioTiles(roverPrefab, tiles))
                    {
                        return false;
                    }

                    if (!CaptureEarthlitNightTiles(roverPrefab, tiles))
                    {
                        return false;
                    }

                    string outPath = Path.GetFullPath(OutputSheetRelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(outPath) ?? "Logs/captures");
                    CompositeSheet(tiles, outPath);

                    if (args != null && !string.IsNullOrEmpty(args.OutputDirectory))
                    {
                        string batchDir = Path.Combine(args.OutputDirectory, "r1_crt_face");
                        Directory.CreateDirectory(batchDir);
                        File.Copy(outPath, Path.Combine(batchDir, "feelings_sheet.png"), true);
                    }

                    return true;
                }
                finally
                {
                    for (int i = 0; i < tiles.Count; i++)
                    {
                        if (tiles[i].Texture != null)
                        {
                            UnityEngine.Object.DestroyImmediate(tiles[i].Texture);
                        }
                    }
                }
            });
        }

        private static bool CaptureStudioTiles(GameObject roverPrefab, List<CapturedTile> tiles)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var temporary = new ArtLightingPreview.TemporaryObjects();
            bool prevAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                SetupStudioScene(temporary);
                var roverInstance = (GameObject)PrefabUtility.InstantiatePrefab(roverPrefab);
                roverInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                temporary.Add(roverInstance);

                if (!TrySetupFaceDisplay(roverInstance, temporary, out FaceDisplay display, out Renderer screenRenderer))
                {
                    return false;
                }

                GetHeadCameraFrame(roverInstance.transform, screenRenderer, out Vector3 closeCamPos, out Vector3 closeLookAt);

                // 1. All 9 feelings in studio close-up
                FaceFeeling[] feelings =
                {
                    FaceFeeling.Calm,
                    FaceFeeling.Curious,
                    FaceFeeling.Happy,
                    FaceFeeling.Proud,
                    FaceFeeling.Worried,
                    FaceFeeling.Tired,
                    FaceFeeling.Sad,
                    FaceFeeling.Startled,
                    FaceFeeling.Asleep,
                };

                for (int i = 0; i < feelings.Length; i++)
                {
                    FaceFeeling feeling = feelings[i];
                    ConfigureSteadyFeeling(display, feeling);
                    Texture2D tex = RenderShot(closeCamPos, closeLookAt, HeadCloseFov, TileSize, TileSize);
                    tiles.Add(new CapturedTile($"STUDIO: {feeling.ToString().ToUpperInvariant()}", tex));
                }

                // 2. 1 frame of mid-blink (Calm feeling with Blink() stepped to closed plateau 0.075s in 5ms increments)
                ConfigureSteadyFeeling(display, FaceFeeling.Calm);
                display.Blink();
                for (int s = 0; s < 15; s++)
                {
                    display.Step(0.005f);
                }

                tiles.Add(new CapturedTile(
                    "STUDIO: MID-BLINK",
                    RenderShot(closeCamPos, closeLookAt, HeadCloseFov, TileSize, TileSize)));

                // 3. 3 frames of boot: horizontal line expand (0.22), full line (0.42), vertical open (0.60)
                float[] bootStages = { 0.22f, 0.42f, 0.60f };
                string[] bootLabels = { "BOOT 1: LINE EXPAND", "BOOT 2: FULL LINE", "BOOT 3: EYES OPEN" };
                for (int i = 0; i < bootStages.Length; i++)
                {
                    ConfigureSteadyFeeling(display, FaceFeeling.Calm);
                    display.Boot();
                    display.Step(bootStages[i] / Mathf.Max(display.Tuning.BootRate, 0.01f));
                    tiles.Add(new CapturedTile(
                        bootLabels[i],
                        RenderShot(closeCamPos, closeLookAt, HeadCloseFov, TileSize, TileSize)));
                }

                // 4. Low power mode (clamped height <= 0.58, 55% brightness, battery glyph, coarser scanlines)
                ConfigureSteadyFeeling(display, FaceFeeling.Calm);
                display.SetLowPower(1f);
                for (int s = 0; s < 10; s++)
                {
                    display.Step(0.025f);
                }

                tiles.Add(new CapturedTile(
                    "STUDIO: LOW POWER",
                    RenderShot(closeCamPos, closeLookAt, HeadCloseFov, TileSize, TileSize)));

                // 5. Chase-camera distance (6.0 m) shot in Studio
                display.SetLowPower(0f);
                ConfigureSteadyFeeling(display, FaceFeeling.Calm);
                GetChaseCameraFrame(roverInstance.transform, screenRenderer, out Vector3 chaseCamPos, out Vector3 chaseLookAt);
                Texture2D studioChase = RenderShot(chaseCamPos, chaseLookAt, ChaseFov, TileSize, TileSize);
                EmbedScreenPipZoom(studioChase, chaseCamPos, chaseLookAt, ChaseFov, screenRenderer.bounds.center);
                tiles.Add(new CapturedTile("CHASE 6.0M (STUDIO)", studioChase));

                return true;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
                temporary.Dispose();
            }
        }

        private static bool CaptureEarthlitNightTiles(GameObject roverPrefab, List<CapturedTile> tiles)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CaptureAutomation.MainScenePath) == null)
            {
                Debug.LogError($"R1CrtFaceCapture: Missing scene at '{CaptureAutomation.MainScenePath}'.");
                return false;
            }

            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            var temporary = new ArtLightingPreview.TemporaryObjects();
            var hiddenObjects = new List<GameObject>();
            bool prevAsync = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                GameObject existingRover = FindSceneObject("[Rover]");
                if (existingRover != null && existingRover.activeSelf)
                {
                    existingRover.SetActive(false);
                    hiddenObjects.Add(existingRover);
                }

                DynamicGI.UpdateEnvironment();

                Vector3 homePos = new Vector3(0.4f, 0.0f, 2.2f);
                Quaternion homeRot = Quaternion.Euler(0f, 185f, 0f);
                var roverInstance = (GameObject)PrefabUtility.InstantiatePrefab(roverPrefab);
                roverInstance.transform.SetPositionAndRotation(homePos, homeRot);
                temporary.Add(roverInstance);

                // Subtle earthlight & porch fill so head casing silhouette reads in lunar night without washing out CRT
                Light earthRim = ArtLightingPreview.NewLight(
                    "R1FaceEarthlightFill",
                    LightType.Point,
                    new Color(0.58f, 0.70f, 0.95f, 1f),
                    1.65f);
                earthRim.transform.position = homePos + homeRot * new Vector3(-1.4f, 1.8f, 1.5f);
                earthRim.range = 8.0f;
                earthRim.shadows = LightShadows.None;
                temporary.Add(earthRim.gameObject);

                if (!TrySetupFaceDisplay(roverInstance, temporary, out FaceDisplay display, out Renderer screenRenderer))
                {
                    return false;
                }

                GetHeadCameraFrame(roverInstance.transform, screenRenderer, out Vector3 closeCamPos, out Vector3 closeLookAt);

                FaceFeeling[] feelings =
                {
                    FaceFeeling.Calm,
                    FaceFeeling.Curious,
                    FaceFeeling.Happy,
                    FaceFeeling.Proud,
                    FaceFeeling.Worried,
                    FaceFeeling.Tired,
                    FaceFeeling.Sad,
                    FaceFeeling.Startled,
                    FaceFeeling.Asleep,
                };

                for (int i = 0; i < feelings.Length; i++)
                {
                    FaceFeeling feeling = feelings[i];
                    ConfigureSteadyFeeling(display, feeling);
                    Texture2D tex = RenderShot(closeCamPos, closeLookAt, HeadCloseFov, TileSize, TileSize);
                    tiles.Add(new CapturedTile($"NIGHT: {feeling.ToString().ToUpperInvariant()}", tex));
                }

                // Chase-camera distance (6.0 m) shot under Earthlit Night in Main.unity
                ConfigureSteadyFeeling(display, FaceFeeling.Calm);
                GetChaseCameraFrame(roverInstance.transform, screenRenderer, out Vector3 chaseCamPos, out Vector3 chaseLookAt);
                Texture2D nightChase = RenderShot(chaseCamPos, chaseLookAt, ChaseFov, TileSize, TileSize);
                EmbedScreenPipZoom(nightChase, chaseCamPos, chaseLookAt, ChaseFov, screenRenderer.bounds.center);
                tiles.Add(new CapturedTile("CHASE 6.0M (NIGHT)", nightChase));

                return true;
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = prevAsync;
                temporary.Dispose();
                for (int i = 0; i < hiddenObjects.Count; i++)
                {
                    if (hiddenObjects[i] != null)
                    {
                        hiddenObjects[i].SetActive(true);
                    }
                }
            }
        }

        private static void ConfigureSteadyFeeling(FaceDisplay display, FaceFeeling feeling)
        {
            display.SetLowPower(0f);
            display.ClearLookTarget();
            display.State.SnapToFeeling(feeling);
            display.Step(0.25f);
            display.State.SnapToFeeling(feeling);
            display.ApplyToRenderer();
        }

        private static bool TrySetupFaceDisplay(
            GameObject roverInstance,
            ArtLightingPreview.TemporaryObjects temporary,
            out FaceDisplay display,
            out Renderer screenRenderer)
        {
            display = null;
            screenRenderer = null;

            Transform screenNode = FindChildRecursive(roverInstance.transform, "Screen");
            if (screenNode == null)
            {
                Debug.LogError("R1CrtFaceCapture: Could not find 'Screen' child on RoverModel prefab.");
                return false;
            }

            screenRenderer = screenNode.GetComponent<Renderer>();
            if (screenRenderer == null)
            {
                Debug.LogError("R1CrtFaceCapture: 'Screen' child has no Renderer component.");
                return false;
            }

            var tuning = ScriptableObject.CreateInstance<FaceTuning>();
            temporary.Add(tuning);

            display = screenNode.GetComponent<FaceDisplay>();
            if (display == null)
            {
                display = screenNode.gameObject.AddComponent<FaceDisplay>();
            }

            display.Initialize(tuning, screenRenderer, 1337);
            return true;
        }

        private static void GetHeadCameraFrame(
            Transform roverTransform,
            Renderer screenRenderer,
            out Vector3 camPos,
            out Vector3 lookAt)
        {
            Bounds b = screenRenderer.bounds;
            lookAt = b.center - roverTransform.up * 0.015f;

            Vector3 outward = roverTransform.forward;
            MeshFilter mf = screenRenderer.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                Vector3[] normals = mf.sharedMesh.normals;
                if (normals != null && normals.Length > 0)
                {
                    Vector3 sum = Vector3.zero;
                    for (int i = 0; i < normals.Length; i++)
                    {
                        sum += normals[i];
                    }

                    if (sum.sqrMagnitude > 0.001f)
                    {
                        outward = screenRenderer.transform.TransformDirection(sum.normalized).normalized;
                    }
                }
            }

            camPos = lookAt + outward * HeadCloseDistanceM + roverTransform.up * 0.02f;
        }

        private static void GetChaseCameraFrame(
            Transform roverTransform,
            Renderer screenRenderer,
            out Vector3 camPos,
            out Vector3 lookAt)
        {
            Vector3 screenCenter = screenRenderer.bounds.center;
            lookAt = screenCenter - roverTransform.up * 0.22f;
            Vector3 offsetDir = (roverTransform.forward * 0.96f + roverTransform.up * 0.22f - roverTransform.right * 0.14f).normalized;
            camPos = lookAt + offsetDir * ChaseDistanceM;
        }

        private static void EmbedScreenPipZoom(
            Texture2D chaseTex,
            Vector3 camPos,
            Vector3 lookAt,
            float fov,
            Vector3 screenWorldCenter)
        {
            int w = chaseTex.width;
            int h = chaseTex.height;

            // Project screenWorldCenter into normalized viewport coordinates [0..1]
            Quaternion camRot = Quaternion.LookRotation(lookAt - camPos, Vector3.up);
            Vector3 localPt = Quaternion.Inverse(camRot) * (screenWorldCenter - camPos);
            if (localPt.z <= 0.1f)
            {
                return;
            }

            float halfTan = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float uCenter = 0.5f + (localPt.x / (localPt.z * halfTan)) * 0.5f;
            float vCenter = 0.5f + (localPt.y / (localPt.z * halfTan)) * 0.5f;

            int srcCropSize = 36;
            int dstPipSize = 116;
            int margin = 10;
            int dstLeft = w - dstPipSize - margin;
            int dstBottom = 30; // above bottom label bar

            Color32[] pixels = chaseTex.GetPixels32();
            var copy = (Color32[])pixels.Clone();

            int srcCx = Mathf.RoundToInt(uCenter * w);
            int srcCy = Mathf.RoundToInt(vCenter * h);
            int srcLeft = Mathf.Clamp(srcCx - srcCropSize / 2, 0, w - srcCropSize);
            int srcBottom = Mathf.Clamp(srcCy - srcCropSize / 2, 0, h - srcCropSize);

            Color32 borderCol = new Color32(255, 174, 56, 255);
            for (int py = -2; py <= dstPipSize + 1; py++)
            {
                for (int px = -2; px <= dstPipSize + 1; px++)
                {
                    int dx = dstLeft + px;
                    int dy = dstBottom + py;
                    if (dx < 0 || dx >= w || dy < 0 || dy >= h)
                    {
                        continue;
                    }

                    if (px < 0 || px >= dstPipSize || py < 0 || py >= dstPipSize)
                    {
                        pixels[dy * w + dx] = borderCol;
                        continue;
                    }

                    int sx = srcLeft + (px * srcCropSize) / dstPipSize;
                    int sy = srcBottom + (py * srcCropSize) / dstPipSize;
                    pixels[dy * w + dx] = copy[sy * w + sx];
                }
            }

            chaseTex.SetPixels32(pixels);
            chaseTex.Apply(false);
        }

        private static void SetupStudioScene(ArtLightingPreview.TemporaryObjects temporary)
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.33f, 0.36f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.24f, 0.25f, 0.28f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.16f, 0.18f, 1f);
            RenderSettings.ambientIntensity = 1.0f;

            Shader.SetGlobalVector(MoonHazeColorId, Vector4.zero);
            Shader.SetGlobalVector(MoonHazeId, Vector4.zero);
            Shader.SetGlobalVector(MoonDesaturateId, new Vector4(10000f, 1f, 0f, 0f));

            Light keyLight = ArtLightingPreview.NewLight(
                "R1FaceStudioKey",
                LightType.Directional,
                new Color(1.0f, 0.98f, 0.95f, 1f),
                1.10f);
            keyLight.transform.rotation = Quaternion.Euler(38f, 148f, 0f);
            keyLight.shadows = LightShadows.Soft;
            RenderSettings.sun = keyLight;
            temporary.Add(keyLight.gameObject);

            Light fillLight = ArtLightingPreview.NewLight(
                "R1FaceStudioFill",
                LightType.Directional,
                new Color(0.86f, 0.92f, 1.0f, 1f),
                0.45f);
            fillLight.transform.rotation = Quaternion.Euler(24f, -140f, 0f);
            fillLight.shadows = LightShadows.None;
            temporary.Add(fillLight.gameObject);

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                var floorMat = new Material(urpLit) { name = "R1FaceStudioFloorMat" };
                floorMat.SetColor("_BaseColor", new Color(0.16f, 0.18f, 0.22f, 1f));
                floorMat.SetFloat("_Smoothness", 0.08f);
                temporary.Add(floorMat);

                GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "R1FaceStudioFloor";
                UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.transform.localScale = new Vector3(25f, 1f, 25f);
                floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
                temporary.Add(floor);
            }

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            temporary.Add(profile);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.45f);
            bloom.scatter.Override(0.65f);
            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            var volumeObj = new GameObject("R1FaceStudioVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            temporary.Add(volumeObj);
        }

        private static Texture2D RenderShot(
            Vector3 camPos,
            Vector3 lookAt,
            float fov,
            int width,
            int height)
        {
            var camObj = new GameObject("[R1CrtFaceCamera]") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var cam = camObj.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.08f, 0.10f, 0.13f, 1f);
                cam.nearClipPlane = 0.04f;
                cam.farClipPlane = 1500f;
                cam.fieldOfView = fov;
                cam.allowHDR = true;
                cam.transform.position = camPos;
                cam.transform.rotation = Quaternion.LookRotation(lookAt - camPos, Vector3.up);

                UniversalAdditionalCameraData camData = cam.GetUniversalAdditionalCameraData();
                camData.renderPostProcessing = true;
                camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.renderShadows = true;

                var rt = RenderTexture.GetTemporary(
                    width,
                    height,
                    24,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                RenderTexture prevActive = RenderTexture.active;
                try
                {
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.Render();
                    cam.targetTexture = null;

                    RenderTexture.active = rt;
                    var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
                    {
                        hideFlags = HideFlags.HideAndDontSave,
                    };
                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    tex.Apply(false);
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

        private static void CompositeSheet(List<CapturedTile> tiles, string outputPath)
        {
            int sheetW = GridCols * TileSize;
            int sheetH = GridRows * TileSize;
            var sheet = new Texture2D(sheetW, sheetH, TextureFormat.RGBA32, false, false);
            var sheetPixels = new Color32[sheetW * sheetH];
            var bgCol = new Color32(16, 18, 22, 255);
            for (int i = 0; i < sheetPixels.Length; i++)
            {
                sheetPixels[i] = bgCol;
            }

            try
            {
                for (int idx = 0; idx < tiles.Count && idx < GridCols * GridRows; idx++)
                {
                    int col = idx % GridCols;
                    int rowFromTop = idx / GridCols;
                    int dstX0 = col * TileSize;
                    int dstY0 = (GridRows - 1 - rowFromTop) * TileSize;

                    Color32[] tilePixels = tiles[idx].Texture.GetPixels32();
                    for (int y = 0; y < TileSize; y++)
                    {
                        int srcRow = y * TileSize;
                        int dstRow = (dstY0 + y) * sheetW + dstX0;
                        for (int x = 0; x < TileSize; x++)
                        {
                            sheetPixels[dstRow + x] = tilePixels[srcRow + x];
                        }
                    }

                    // Draw dark bottom banner and label text
                    for (int y = 0; y < 26; y++)
                    {
                        int dstRow = (dstY0 + y) * sheetW + dstX0;
                        for (int x = 0; x < TileSize; x++)
                        {
                            Color32 c = sheetPixels[dstRow + x];
                            sheetPixels[dstRow + x] = new Color32(
                                (byte)(c.r / 4),
                                (byte)(c.g / 4),
                                (byte)(c.b / 4),
                                255);
                        }
                    }

                    DrawLabel(sheetPixels, sheetW, sheetH, dstX0 + 10, dstY0 + 7, tiles[idx].Label, new Color32(255, 196, 92, 255));

                    // 1px tile border
                    var borderCol = new Color32(48, 54, 66, 255);
                    for (int x = 0; x < TileSize; x++)
                    {
                        sheetPixels[dstY0 * sheetW + dstX0 + x] = borderCol;
                        sheetPixels[(dstY0 + TileSize - 1) * sheetW + dstX0 + x] = borderCol;
                    }

                    for (int y = 0; y < TileSize; y++)
                    {
                        sheetPixels[(dstY0 + y) * sheetW + dstX0] = borderCol;
                        sheetPixels[(dstY0 + y) * sheetW + dstX0 + TileSize - 1] = borderCol;
                    }
                }

                sheet.SetPixels32(sheetPixels);
                sheet.Apply(false);
                File.WriteAllBytes(outputPath, sheet.EncodeToPNG());
                BatchRunner.Log($"capture {outputPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sheet);
            }
        }

        private static void DrawLabel(
            Color32[] buffer,
            int width,
            int height,
            int startX,
            int startY,
            string text,
            Color32 color)
        {
            const int Scale = 2;
            int cursorX = startX;
            for (int i = 0; i < text.Length; i++)
            {
                uint glyph = GetGlyph3x5(char.ToUpperInvariant(text[i]));
                for (int gy = 0; gy < 5; gy++)
                {
                    for (int gx = 0; gx < 3; gx++)
                    {
                        int bit = gy * 3 + (2 - gx);
                        if (((glyph >> bit) & 1u) == 0u)
                        {
                            continue;
                        }

                        for (int sy = 0; sy < Scale; sy++)
                        {
                            for (int sx = 0; sx < Scale; sx++)
                            {
                                int px = cursorX + gx * Scale + sx;
                                int py = startY + gy * Scale + sy;
                                if (px >= 0 && px < width && py >= 0 && py < height)
                                {
                                    buffer[py * width + px] = color;
                                }
                            }
                        }
                    }
                }

                cursorX += 4 * Scale;
            }
        }

        private static uint GetGlyph3x5(char c)
        {
            switch (c)
            {
                case 'A': return 0b010_101_111_101_101u;
                case 'B': return 0b110_101_110_101_110u;
                case 'C': return 0b011_100_100_100_011u;
                case 'D': return 0b110_101_101_101_110u;
                case 'E': return 0b111_100_110_100_111u;
                case 'F': return 0b111_100_110_100_100u;
                case 'G': return 0b011_100_101_101_011u;
                case 'H': return 0b101_101_111_101_101u;
                case 'I': return 0b111_010_010_010_111u;
                case 'J': return 0b001_001_001_101_010u;
                case 'K': return 0b101_101_110_101_101u;
                case 'L': return 0b100_100_100_100_111u;
                case 'M': return 0b101_111_111_101_101u;
                case 'N': return 0b101_111_111_111_101u;
                case 'O': return 0b010_101_101_101_010u;
                case 'P': return 0b110_101_110_100_100u;
                case 'Q': return 0b010_101_101_011_001u;
                case 'R': return 0b110_101_110_101_101u;
                case 'S': return 0b011_100_010_001_110u;
                case 'T': return 0b111_010_010_010_010u;
                case 'U': return 0b101_101_101_101_111u;
                case 'V': return 0b101_101_101_101_010u;
                case 'W': return 0b101_101_111_111_101u;
                case 'X': return 0b101_101_010_101_101u;
                case 'Y': return 0b101_101_010_010_010u;
                case 'Z': return 0b111_001_010_100_111u;
                case '0': return 0b111_101_101_101_111u;
                case '1': return 0b010_110_010_010_111u;
                case '2': return 0b111_001_111_100_111u;
                case '3': return 0b111_001_011_001_111u;
                case '4': return 0b101_101_111_001_001u;
                case '5': return 0b111_100_111_001_111u;
                case '6': return 0b111_100_111_101_111u;
                case '7': return 0b111_001_010_010_010u;
                case '8': return 0b111_101_111_101_111u;
                case '9': return 0b111_101_111_001_111u;
                case ':': return 0b000_010_000_010_000u;
                case '-': return 0b000_000_111_000_000u;
                case '.': return 0b000_000_000_000_010u;
                case '(': return 0b001_010_010_010_001u;
                case ')': return 0b100_010_010_010_100u;
                default: return 0u;
            }
        }

        private static GameObject FindSceneObject(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root;
                }

                Transform child = FindChildRecursive(root.transform, name);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private readonly struct CapturedTile
        {
            public readonly string Label;
            public readonly Texture2D Texture;

            public CapturedTile(string label, Texture2D texture)
            {
                Label = label;
                Texture = texture;
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// Real-scene R1 moonlook capture session: boots the built Main scene and captures the three required
    /// views (<c>owner</c>, <c>slopes150</c>, <c>peak_golden</c>) into
    /// <c>Logs/captures/moonlook/&lt;view&gt;-final.png</c>.
    /// </summary>
    [Explicit("Real-scene R1 moonlook capture session; run on demand with --filter MoonlookCaptureSession.")]
    [Category("MoonlookCaptureSession")]
    public sealed class MoonlookCaptureSession
    {
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 1.5f;
        private const float EarthlitOwnerTimeSeconds = 4816f;
        private const float SlopesGrazingSunTimeSeconds = 2350f;
        private const float Plain20SunTimeSeconds = 2380f;
        private const float OwnerFov = 60f;
        private const float Plain20Fov = 54f;
        private const float Slopes150Fov = 42f;
        private const float PeakGoldenFov = 48f;
        private const string EarthAlbedoAssetPath = "Assets/_Project/Art/R1/Textures/T_R1_Earth_Albedo.png";

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "moonlook"));

        private readonly List<string> _problems = new List<string>();
        private IDisposable _awoke;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _awoke?.Dispose();

            Scene scene = SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(CanyonSessionScene))]
        [PostBuildCleanup(typeof(CanyonSessionScene))]
        public IEnumerator Moonlook_CapturesOwnerSlopes150AndPeakGolden_Final()
        {
            BootstrapHarness.DeleteSaveFiles(CanyonSessionScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(CanyonSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("MoonlookCaptureSession loads the scene through the editor.");
#endif
            Scene loadedScene = SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath);
            GameBootstrap bootstrap = null;
            SkyCycleSystem skyCycle = null;
            WorldSystem worldSystem = null;
            foreach (GameObject root in loadedScene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                if (skyCycle == null)
                {
                    skyCycle = root.GetComponentInChildren<SkyCycleSystem>(true);
                }

                if (worldSystem == null)
                {
                    worldSystem = root.GetComponentInChildren<WorldSystem>(true);
                }
            }

            Assert.IsNotNull(bootstrap, "Main scene must have GameBootstrap.");
            Assert.IsNotNull(skyCycle, "Main scene must have SkyCycleSystem.");
            Assert.IsNotNull(worldSystem, "Main scene must have WorldSystem.");

            GameContext context = bootstrap.Context;
            _awoke = context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return new WaitForSeconds(SettleSeconds);

#if UNITY_EDITOR
            Texture2D earthAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(EarthAlbedoAssetPath);
            Assert.IsNotNull(earthAlbedo, "T_R1_Earth_Albedo.png must exist.");
            BindEarthAlbedoOnSceneEarth(loadedScene, earthAlbedo);
#endif

            Directory.CreateDirectory(CaptureFolder);
            MoonSurface surface = worldSystem.Surface;
            IWorldAnchors anchors = context.Get<IWorldAnchors>();
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.PlaceApron, out WorldAnchor apronAnchor));
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.PlaceLander, out WorldAnchor landerAnchor));

            Vector3 apron = apronAnchor.Position;
            Vector3 lander = landerAnchor.Position;
            Vector3 peakSummit = surface.PeakSummit;
            IRoverPlacement roverPlacement = context.Get<IRoverPlacement>();

            // 1. Owner view: chase camera at the apron/lander looking toward The Peak with Earth up on the left.
            Vector3 roverSpot = apron + Vector3.ProjectOnPlane(lander - apron, Vector3.up).normalized * 2.5f;
            roverSpot.y = surface.SampleHeight(roverSpot.x, roverSpot.z);
            Vector3 roverForward = Vector3.ProjectOnPlane(lander - roverSpot, Vector3.up).normalized;
            roverPlacement.PlaceAt(roverSpot, Quaternion.LookRotation(roverForward, Vector3.up));
            yield return null;

            Vector3 ownerForward = Quaternion.Euler(0f, 2.0f, 0f) * Vector3.forward;
            Vector3 ownerRight = Vector3.Cross(Vector3.up, ownerForward).normalized;
            Vector3 ownerEye = roverSpot - ownerForward * 16.5f + ownerRight * 2.4f;
            ownerEye.y = surface.SampleHeight(ownerEye.x, ownerEye.z) + 2.65f;
            Vector3 ownerLook = ownerEye + (Quaternion.AngleAxis(-3.8f, ownerRight) * ownerForward) * 250f;

            // 2. Slopes150 view: close-up of the sunlit steep inner rim / mountain slopes at ~150 m distance under raking low sun.
            Vector3 slopeTarget = new Vector3(28f, surface.SampleHeight(28f, 388f) + 12f, 388f);
            float slopesGrazingClock = FindSlopesGrazingClockTime(skyCycle, surface);
            Vector3 slopeApproachDir = new Vector3(0.11f, 0f, 0.994f).normalized;
            Vector3 slopes150Eye = slopeTarget - slopeApproachDir * 150f;
            slopes150Eye.y = surface.SampleHeight(slopes150Eye.x, slopes150Eye.z) + 6.0f;
            Vector3 slopes150Look = slopeTarget + Vector3.up * 5.0f;

            // 3. Peak golden hour view: The Peak and its flanking rim ridges under low golden-hour sunlight.
            float goldenHourClock = FindGoldenHourClockTime(skyCycle, peakSummit);
            Vector3 peakGoldenEye = new Vector3(36f, surface.SampleHeight(36f, 165f) + 6.0f, 165f);
            Vector3 peakGoldenLook = new Vector3(peakSummit.x * 0.78f, peakSummit.y * 0.56f, peakSummit.z);

            // 4. Plain20 / Chase20 view: 07 chase camera on the open flat plain looking at the regolith & craters around 20 m.
            Vector3 plainRoverSpot = new Vector3(roverSpot.x + 42f, 0f, roverSpot.z + 18f);
            plainRoverSpot.y = surface.SampleHeight(plainRoverSpot.x, plainRoverSpot.z);
            Vector3 plainForward = new Vector3(-0.18f, 0f, 0.984f).normalized;
            Vector3 plainEye = plainRoverSpot - plainForward * 6.6f;
            plainEye.y = surface.SampleHeight(plainEye.x, plainEye.z) + 2.38f;
            Vector3 plainLook = plainRoverSpot + plainForward * 13.5f + Vector3.up * 0.30f;
            float plainGrazingClock = FindPlainGrazingClockTime(skyCycle, plainRoverSpot, plainLook);

            Camera gameViewCamera = context.Get<IViewCamera>().Camera;
            var host = new GameObject("MoonlookCaptureCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(gameViewCamera);
                camera.enabled = false;
                CopyPipelineSettings(gameViewCamera, camera);

                // View 1: owner
                yield return CaptureFinalView(camera, skyCycle, "owner", EarthlitOwnerTimeSeconds, ownerEye, ownerLook, OwnerFov);

                // View 2: slopes150
                yield return CaptureFinalView(camera, skyCycle, "slopes150", slopesGrazingClock, slopes150Eye, slopes150Look, Slopes150Fov);

                // View 3: peak_golden
                yield return CaptureFinalView(camera, skyCycle, "peak_golden", goldenHourClock, peakGoldenEye, peakGoldenLook, PeakGoldenFov);

                // View 4: 07 chase view on the plain at 20 m (plain20-final.png & chase20-final.png)
                roverPlacement.PlaceAt(plainRoverSpot, Quaternion.LookRotation(plainForward, Vector3.up));
                yield return null;
                yield return CaptureFinalView(camera, skyCycle, "plain20", plainGrazingClock, plainEye, plainLook, Plain20Fov);
                string plain20Path = Path.Combine(CaptureFolder, "plain20-final.png");
                string chase20Path = Path.Combine(CaptureFolder, "chase20-final.png");
                File.Copy(plain20Path, chase20Path, true);

                skyCycle.SetClockTime(0f, true);
            }
            finally
            {
                Object.Destroy(host);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

#if UNITY_EDITOR
        private static IEnumerator CaptureFinalView(
            Camera camera,
            SkyCycleSystem skyCycle,
            string viewName,
            float clockTime,
            Vector3 eye,
            Vector3 look,
            float fov)
        {
            skyCycle.SetClockTime(clockTime, false);
            PlaceLookAt(camera, eye, look, fov);
            yield return null;

            skyCycle.SetClockTime(clockTime, false);
            yield return null;

            string path = Path.Combine(CaptureFolder, $"{viewName}-final.png");
            FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, path);
            Assert.IsTrue(File.Exists(path), $"Expected capture at {path}");
        }

        private static void BindEarthAlbedoOnSceneEarth(Scene scene, Texture2D earthAlbedo)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MeshRenderer mr in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (mr.gameObject.name == "Earth" && mr.sharedMaterial != null)
                    {
                        var mpb = new MaterialPropertyBlock();
                        mr.GetPropertyBlock(mpb);
                        mpb.SetTexture("_EarthAlbedoMap", earthAlbedo);
                        mr.SetPropertyBlock(mpb);
                    }
                }
            }
        }
#endif

        private static float FindSlopesGrazingClockTime(SkyCycleSystem skyCycle, MoonSurface surface)
        {
            Vector3 pLeft = new Vector3(4f, surface.SampleHeight(4f, 385f) + 4f, 385f);
            Vector3 pMid = new Vector3(28f, surface.SampleHeight(28f, 382f) + 4f, 382f);
            Vector3 pRight = new Vector3(52f, surface.SampleHeight(52f, 385f) + 4f, 385f);

            float bestTime = 2050f;
            float bestScore = float.MaxValue;
            for (float t = 0f; t <= 12000f; t += 15f)
            {
                skyCycle.SetClockTime(t, false);
                float litMid = skyCycle.SunlightAt(pMid);
                float litLeft = skyCycle.SunlightAt(pLeft);
                float litRight = skyCycle.SunlightAt(pRight);
                if (litMid < 0.95f || litLeft < 0.95f || litRight < 0.95f)
                {
                    continue;
                }

                Vector3 sunDir = skyCycle.SunDirection;
                float elev = Mathf.Asin(Mathf.Clamp(sunDir.y, -1f, 1f)) * Mathf.Rad2Deg;
                float sideRakingPenalty = (1f - Mathf.Abs(sunDir.x)) * 16f;
                float score = Mathf.Abs(elev - 14.0f) + sideRakingPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTime = t;
                }
            }

            return bestTime;
        }

        private static float FindPlainGrazingClockTime(SkyCycleSystem skyCycle, Vector3 roverPos, Vector3 lookPos)
        {
            Vector3 checkA = roverPos + Vector3.up * 0.8f;
            Vector3 checkB = lookPos + Vector3.up * 0.8f;
            Vector3 checkC = lookPos + new Vector3(14f, 0.8f, 4f);
            Vector3 checkD = lookPos + new Vector3(-14f, 0.8f, 4f);

            float bestTime = 1950f;
            float bestScore = float.MaxValue;
            for (float t = 0f; t <= 12000f; t += 15f)
            {
                skyCycle.SetClockTime(t, false);
                if (skyCycle.SunlightAt(checkA) < 0.95f ||
                    skyCycle.SunlightAt(checkB) < 0.95f ||
                    skyCycle.SunlightAt(checkC) < 0.95f ||
                    skyCycle.SunlightAt(checkD) < 0.95f)
                {
                    continue;
                }

                Vector3 sunDir = skyCycle.SunDirection;
                float elev = Mathf.Asin(Mathf.Clamp(sunDir.y, -1f, 1f)) * Mathf.Rad2Deg;
                float sideRakingPenalty = (1f - Mathf.Abs(sunDir.x)) * 12f;
                float score = Mathf.Abs(elev - 12.0f) + sideRakingPenalty;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTime = t;
                }
            }

            return bestTime;
        }

        private static float FindGoldenHourClockTime(SkyCycleSystem skyCycle, Vector3 peakSummit)
        {
            // Find a golden-hour sunset moment where the low warm Sun (~5.5 deg elevation) rakes across The Peak.
            float bestTime = 2250f;
            float bestDiff = float.MaxValue;
            for (float t = 1600f; t <= 3100f; t += 10f)
            {
                skyCycle.SetClockTime(t, false);
                float elev = Mathf.Asin(Mathf.Clamp(skyCycle.SunDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
                float diff = Mathf.Abs(elev - 5.8f);
                if (skyCycle.SunlightAt(peakSummit) > 0.95f && diff < bestDiff)
                {
                    bestDiff = diff;
                    bestTime = t;
                }
            }

            return bestTime;
        }

        private static void PlaceLookAt(Camera camera, Vector3 eye, Vector3 look, float fov)
        {
            camera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye, Vector3.up));
            camera.fieldOfView = fov;
        }

        private static void CopyPipelineSettings(Camera from, Camera to)
        {
            UniversalAdditionalCameraData source = from.GetUniversalAdditionalCameraData();
            UniversalAdditionalCameraData target = to.GetUniversalAdditionalCameraData();
            target.renderPostProcessing = source.renderPostProcessing;
            target.antialiasing = source.antialiasing;
            target.antialiasingQuality = source.antialiasingQuality;
            target.renderShadows = source.renderShadows;
            target.dithering = source.dithering;
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }
    }
}

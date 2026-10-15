using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// Real-scene verification and capture session for the lunar sky cycle (0.5.7, slice 4).
    /// Boots the built Main scene on an isolated save slot, places a review camera behind 07 at home looking toward
    /// Earth, captures the 7 required sky states into <c>Logs/captures/</c>, and measures the CPU time budget of one
    /// <see cref="ISunlight.SunlightAt"/> call and one per-frame sky update (<see cref="SkyCycleSystem.StepFrame"/>).
    /// </summary>
    [Explicit("Real-scene sky cycle capture and budget session; run on demand with --category SkyCycleSession.")]
    [Category("SkyCycleSession")]
    public sealed class SkyCycleSession
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 1.5f;
        private const float StepDt = 0.05f;
        private const float ReleaseFiveSeconds = 5f;
        private const float ReleaseTwentySeconds = 20f;
        private const float SunlitTimeSeconds = 210f;
        private const float SunsetSearchStartSeconds = 1200f;
        private const float SunsetSearchEndSeconds = 3250f;
        private const float EarthlitTimeSeconds = 4816f;
        private const float CameraBackDistance = 6.2f;
        private const float CameraHeightOffset = 2.2f;
        private const float TowardEarthPitchDeg = -3.5f;
        private const float TowardEarthYawOffsetDeg = 5f;
        private const float AwayFromEarthPitchDeg = 6f;
        private const float CameraFov = 60f;
        private const int BenchmarkWarmupIterations = 200;
        private const int BenchmarkTimedIterations = 2000;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

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
        public IEnumerator SkyCycle_CapturesSevenViews_AndMeasuresBudget()
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
            throw new NotSupportedException("SkyCycleSession loads the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            SkyCycleSystem skyCycle = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(CanyonSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                if (skyCycle == null)
                {
                    skyCycle = root.GetComponentInChildren<SkyCycleSystem>(true);
                }
            }

            Assert.IsNotNull(bootstrap, "Main scene must have GameBootstrap.");
            Assert.IsNotNull(skyCycle, "Main scene must have SkyCycleSystem wired under World.");
            GameContext context = bootstrap.Context;
            Assert.IsTrue(context.TryGet(out ISunlight sunlight), "ISunlight must be registered in GameContext.");
            Assert.AreSame(skyCycle, sunlight);

            _awoke = context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return new WaitForSeconds(SettleSeconds);

            Camera view = context.Get<IViewCamera>().Camera;
            Vector3 roverPos = context.Get<IRoverState>().Position;
            Vector3 earthDir = context.Get<IWorldLayout>().EarthDirection;
            Vector3 toEarthFlat = Vector3.ProjectOnPlane(earthDir, Vector3.up).normalized;
            Vector3 towardEarthForward = Quaternion.AngleAxis(TowardEarthYawOffsetDeg, Vector3.up) * toEarthFlat;

            Directory.CreateDirectory(CaptureFolder);
            var host = new GameObject("SkyCycleReviewCamera");
            var report = new StringBuilder();
            report.AppendLine("# Sky Cycle Captures & Budget (Real Main Scene)");
            report.AppendLine();
            report.AppendLine("| Capture | Clock (s) | Hold | Phase | EclipseFactor | EarthPhase | SunlightAt(07) | Smoothed |");
            report.AppendLine("|---|---:|---|---|---:|---:|---:|---:|");

            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);

                PlaceBehindRover(camera, roverPos, towardEarthForward, TowardEarthPitchDeg);

                // 1. Opening eclipse hold (t = 0 s, holding = true).
                skyCycle.SetClockTime(0f, true);
                yield return null;
                CaptureState(camera, skyCycle, roverPos, "01-eclipse-hold", report);
                Assert.AreEqual(SkyPhase.Eclipse, skyCycle.Phase);
                Assert.AreEqual(1f, skyCycle.EclipseFactor, 1e-4f);

                // 2. 5 s after release (step smoothly from 0 s to 5 s so smoothed sunlight evolves naturally).
                skyCycle.SetClockTime(0f, true);
                skyCycle.ReleaseOpeningEclipse();
                StepSeconds(skyCycle, ReleaseFiveSeconds);
                yield return null;
                CaptureState(camera, skyCycle, roverPos, "02-release-5s", report);
                Assert.Greater(skyCycle.EclipseFactor, 0.5f, "At 5 s after release the Sun is emerging in partial eclipse.");
                Assert.Less(skyCycle.EclipseFactor, 1f, "At 5 s after release the diamond-ring bead breaks past Earth's limb.");

                // 3. 20 s after release (step from 5 s to 20 s).
                StepSeconds(skyCycle, ReleaseTwentySeconds - ReleaseFiveSeconds);
                yield return null;
                CaptureState(camera, skyCycle, roverPos, "03-release-20s", report);
                Assert.AreEqual(0f, skyCycle.EclipseFactor, 1e-4f, "By 20 s after release the Sun disc is clear of Earth.");

                // 4. Full sunlit (looking toward Earth & the low northern Sun).
                skyCycle.SetClockTime(SunlitTimeSeconds, false);
                yield return null;
                CaptureState(camera, skyCycle, roverPos, "04-full-sunlit", report);
                Assert.AreEqual(SkyPhase.Sunlit, skyCycle.Phase);
                Assert.Greater(skyCycle.SunlightAt(roverPos), 0.9f);

                // 5. Sunset (low descending Sun grazing the eastern/southeastern rim while Sunset phase is active).
                float sunsetTime = FindGrazingSunsetTime(skyCycle, roverPos);
                skyCycle.SetClockTime(sunsetTime, false);
                yield return null;
                CaptureState(camera, skyCycle, roverPos, "05-sunset", report);
                Assert.AreEqual(SkyPhase.Sunset, skyCycle.Phase);

                // 6. Earthlit night (Sun below the horizon, Earth near full in the northern sky).
                skyCycle.SetClockTime(EarthlitTimeSeconds, false);
                yield return null;
                CaptureState(camera, skyCycle, roverPos, "06-earthlit-night", report);
                Assert.AreEqual(SkyPhase.Earthlit, skyCycle.Phase);
                Assert.AreEqual(0f, skyCycle.SunlightAt(roverPos), 1e-4f);

                // 7. Full sunlit looking away from Earth across the basin.
                skyCycle.SetClockTime(SunlitTimeSeconds, false);
                PlaceBehindRover(camera, roverPos, -toEarthFlat, AwayFromEarthPitchDeg);
                yield return null;
                CaptureState(camera, skyCycle, roverPos, "07-sunlit-away-from-earth", report);

                // Measure CPU budgets for SunlightAt and StepFrame.
                skyCycle.SetClockTime(SunlitTimeSeconds, false);
                for (int i = 0; i < BenchmarkWarmupIterations; i++)
                {
                    skyCycle.SunlightAt(roverPos);
                }

                var sw = Stopwatch.StartNew();
                float sink = 0f;
                for (int i = 0; i < BenchmarkTimedIterations; i++)
                {
                    sink += skyCycle.SunlightAt(roverPos);
                }
                sw.Stop();
                double sunlightUs = (sw.Elapsed.TotalMilliseconds * 1000.0) / BenchmarkTimedIterations;

                for (int i = 0; i < BenchmarkWarmupIterations; i++)
                {
                    skyCycle.StepFrame(1f / 60f);
                }

                sw.Restart();
                for (int i = 0; i < BenchmarkTimedIterations; i++)
                {
                    skyCycle.StepFrame(1f / 60f);
                }
                sw.Stop();
                double stepFrameUs = (sw.Elapsed.TotalMilliseconds * 1000.0) / BenchmarkTimedIterations;

                // Restore eclipse hold before finishing so the scene ends cleanly.
                skyCycle.SetClockTime(0f, true);

                report.AppendLine();
                report.AppendLine("## CPU Time Budget");
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "- `SunlightAt(Vector3)` ({0} calls): **{1:F2} µs** ({2:F4} ms) per call (sink={3:F1})",
                    BenchmarkTimedIterations, sunlightUs, sunlightUs / 1000.0, sink));
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "- Per-frame sky update `SkyCycleSystem.StepFrame(dt)` ({0} frames): **{1:F2} µs** ({2:F4} ms) per frame",
                    BenchmarkTimedIterations, stepFrameUs, stepFrameUs / 1000.0));

                File.WriteAllText(Path.Combine(CaptureFolder, "sky-cycle-budget.md"), report.ToString());
                Debug.Log("[sky-cycle]\n" + report);
            }
            finally
            {
                Object.Destroy(host);
            }

            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private static float FindGrazingSunsetTime(SkyCycleSystem skyCycle, Vector3 roverPos)
        {
            // Find the latest moment in SkyPhase.Sunset before the Sun drops into the eastern rim's shadow ramp,
            // with ~0.8 degrees of clearance above the rim mesh so low sunset rays rake across 07 and the pad.
            float rimTouchTime = 1900f;
            for (float t = SunsetSearchStartSeconds; t <= SunsetSearchEndSeconds; t += 2f)
            {
                skyCycle.SetClockTime(t, false);
                if (skyCycle.Phase != SkyPhase.Sunset)
                {
                    continue;
                }

                if (skyCycle.SunlightAt(roverPos) >= 0.999f)
                {
                    rimTouchTime = t;
                }
                else
                {
                    break;
                }
            }

            return Mathf.Max(SunsetSearchStartSeconds, rimTouchTime - 110f);
        }

        private static void StepSeconds(SkyCycleSystem skyCycle, float seconds)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(seconds / StepDt));
            float dt = seconds / steps;
            for (int i = 0; i < steps; i++)
            {
                skyCycle.StepFrame(dt);
            }
        }

        private static void PlaceBehindRover(Camera camera, Vector3 roverPos, Vector3 horizontalForward, float pitchDeg)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(horizontalForward, Vector3.up).normalized;
            Vector3 eye = roverPos - forwardFlat * CameraBackDistance + Vector3.up * CameraHeightOffset;
            Vector3 right = Vector3.Cross(Vector3.up, forwardFlat).normalized;
            Quaternion pitchRot = Quaternion.AngleAxis(pitchDeg, right);
            Quaternion rotation = Quaternion.LookRotation(pitchRot * forwardFlat, Vector3.up);
            camera.transform.SetPositionAndRotation(eye, rotation);
            camera.fieldOfView = CameraFov;
        }

        private static void CaptureState(
            Camera camera,
            SkyCycleSystem skyCycle,
            Vector3 roverPos,
            string fileName,
            StringBuilder report)
        {
            string fullPath = Path.Combine(CaptureFolder, fileName + ".png");
            FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, fullPath);
            float rawSunlight = skyCycle.SunlightAt(roverPos);
            report.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "| `{0}.png` | {1:F1} | {2} | {3} | {4:F3} | {5:F3} | {6:F3} | {7:F3} |",
                fileName,
                skyCycle.TimeSeconds,
                skyCycle.IsHoldingEclipse ? "yes" : "no",
                skyCycle.Phase,
                skyCycle.EclipseFactor,
                skyCycle.EarthPhase,
                rawSunlight,
                skyCycle.SmoothedSunlight));
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
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
    /// Real-scene verification, before/after visual capture, and 1080p GPU-synchronised performance session for
    /// TERRAIN-R1 (0.5.7, slice 7). Boots the built Main scene on an isolated save slot, captures the five required
    /// views into <c>Logs/captures/</c>, and records terrain/backdrop/scatter vertex and triangle counts plus
    /// 1920x1080 frame times.
    /// </summary>
    [Explicit("Real-scene TERRAIN-R1 capture and performance session; run on demand with --filter TerrainR1Session.")]
    [Category("TerrainR1Session")]
    public sealed class TerrainR1Session
    {
        private const int CaptureWidth = 1920;
        private const int CaptureHeight = 1080;
        private const int WarmUpFrames = 24;
        private const int TimedFrames = 48;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 1.5f;
        private const float SunlitTimeSeconds = 210f;
        private const float EarthlitTimeSeconds = 4816f;
        private const float CameraBackDistance = 6.2f;
        private const float CameraHeightOffset = 2.2f;
        private const float TowardEarthPitchDeg = -3.5f;
        private const float TowardEarthYawOffsetDeg = 5f;
        private const float HomeFov = 60f;
        private const float CraterRimFov = 48f;
        private const float RockFieldFov = 42f;
        private const float WideVistaFov = 52f;

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
        public IEnumerator TerrainR1_CapturesFiveViews_AndMeasuresPerformance()
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
            throw new NotSupportedException("TerrainR1Session loads the scene through the editor.");
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

            Directory.CreateDirectory(CaptureFolder);
            string beforeMetricsPath = Path.Combine(CaptureFolder, "before_metrics.json");
            string stageEnv = Environment.GetEnvironmentVariable("TERRAIN_CAPTURE_STAGE");
            string prefix = !string.IsNullOrEmpty(stageEnv)
                ? stageEnv.Trim().ToLowerInvariant()
                : (!File.Exists(beforeMetricsPath) ? "before" : "after");

            GeometryCounts counts = CountSceneGeometry(loadedScene);
            Camera view = context.Get<IViewCamera>().Camera;
            Vector3 roverPos = context.Get<IRoverState>().Position;
            Vector3 earthDir = context.Get<IWorldLayout>().EarthDirection;
            Vector3 toEarthFlat = Vector3.ProjectOnPlane(earthDir, Vector3.up).normalized;
            Vector3 towardEarthForward = Quaternion.AngleAxis(TowardEarthYawOffsetDeg, Vector3.up) * toEarthFlat;

            MoonSurface surface = worldSystem.Surface;
            GetCraterRimPose(surface, toEarthFlat, out Vector3 craterEye, out Vector3 craterLook);
            GetRockFieldPose(surface, new ScatterSettings(), worldSystem.Anchors, toEarthFlat,
                out Vector3 rockEye, out Vector3 rockLook);
            Vector3 vistaEye = new Vector3(-58f, surface.SampleHeight(-58f, -115f) + 6.5f, -115f);
            Vector3 vistaLook = new Vector3(25f, surface.SampleHeight(25f, 160f) + 14f, 160f);

            var host = new GameObject("TerrainR1ReviewCamera");
            var frameTimes = new Dictionary<string, (double Median, double P90)>();
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                CopyPipelineSettings(view, camera);

                // 1a. Home looking toward Earth — earthlit night via SkyCycleSystem.
                skyCycle.SetClockTime(EarthlitTimeSeconds, false);
                PlaceBehindRover(camera, roverPos, towardEarthForward, TowardEarthPitchDeg, HomeFov);
                yield return null;
                frameTimes["01_home_earthlit"] = CaptureAndMeasure(camera, $"{prefix}_01_home_earthlit");

                // 1b. Home looking toward Earth — sunlit day via SkyCycleSystem.
                skyCycle.SetClockTime(SunlitTimeSeconds, false);
                PlaceBehindRover(camera, roverPos, towardEarthForward, TowardEarthPitchDeg, HomeFov);
                yield return null;
                frameTimes["02_home_sunlit"] = CaptureAndMeasure(camera, $"{prefix}_02_home_sunlit");

                // 2. Crater rim against the sky (sunlit grazing light on rim and distant mountains).
                skyCycle.SetClockTime(SunlitTimeSeconds, false);
                PlaceLookAt(camera, craterEye, craterLook, CraterRimFov);
                yield return null;
                frameTimes["03_crater_rim_sky"] = CaptureAndMeasure(camera, $"{prefix}_03_crater_rim_sky");

                // 3. Rock field close-up (sunlit raking light on boulders and pebbles).
                skyCycle.SetClockTime(SunlitTimeSeconds, false);
                PlaceLookAt(camera, rockEye, rockLook, RockFieldFov);
                yield return null;
                frameTimes["04_rock_field_closeup"] = CaptureAndMeasure(camera, $"{prefix}_04_rock_field_closeup");

                // 4. Wide vista across the basin, craters, rocks, and mountain rim.
                skyCycle.SetClockTime(SunlitTimeSeconds, false);
                PlaceLookAt(camera, vistaEye, vistaLook, WideVistaFov);
                yield return null;
                frameTimes["05_wide_vista"] = CaptureAndMeasure(camera, $"{prefix}_05_wide_vista");

                skyCycle.SetClockTime(0f, true);
            }
            finally
            {
                Object.Destroy(host);
            }

            WriteMetricsJson(Path.Combine(CaptureFolder, $"{prefix}_metrics.json"), prefix, counts, frameTimes);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private static (double Median, double P90) CaptureAndMeasure(Camera camera, string baseName)
        {
            string fullPath = Path.Combine(CaptureFolder, baseName + ".png");
            FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight, fullPath);
            return MeasureFrames(camera);
        }

        private static (double Median, double P90) MeasureFrames(Camera camera)
        {
            var target = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            var samples = new double[TimedFrames];
            try
            {
                camera.targetTexture = target;
                var watch = new Stopwatch();
                for (int i = -WarmUpFrames; i < TimedFrames; i++)
                {
                    watch.Restart();
                    camera.Render();
                    AsyncGPUReadback.Request(target, 0, 0, 1, 0, 1, 0, 1).WaitForCompletion();
                    if (i >= 0)
                    {
                        samples[i] = watch.Elapsed.TotalMilliseconds;
                    }
                }
            }
            finally
            {
                camera.targetTexture = null;
                Object.Destroy(target);
            }

            Array.Sort(samples);
            return (samples[TimedFrames / 2], samples[TimedFrames * 9 / 10]);
        }

        private static void GetCraterRimPose(MoonSurface surface, Vector3 toEarthFlat,
            out Vector3 eye, out Vector3 look)
        {
            // Select a large impact crater in the southern/western half of the basin so looking outward across its
            // raised rim crest toward the outer mountain rim puts three-quarter sunlight on both rims against the sky.
            Crater best = surface.Craters[0];
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < surface.Craters.Count; i++)
            {
                Crater c = surface.Craters[i];
                if (c.IsPlayBowl)
                {
                    continue;
                }

                Vector2 dir = c.Center.normalized;
                float alignSouthWest = Vector2.Dot(dir, new Vector2(-0.55f, -0.83f));
                float score = c.Radius * (1.1f + 0.65f * alignSouthWest) + c.RimHeight * 10f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }

            Vector2 outward2D = best.Center.sqrMagnitude > 1f ? best.Center.normalized : new Vector2(-0.6f, -0.8f);
            Vector2 eye2D = best.Center - outward2D * (best.Radius * 0.55f);
            Vector2 crest2D = best.Center + outward2D * best.Radius;
            float eyeY = surface.SampleHeight(eye2D.x, eye2D.y) + 1.35f;
            float crestY = surface.SampleHeight(crest2D.x, crest2D.y) + 3.2f;
            eye = new Vector3(eye2D.x, eyeY, eye2D.y);
            look = new Vector3(crest2D.x, crestY, crest2D.y);
        }

        private static void GetRockFieldPose(MoonSurface surface, ScatterSettings scatterSettings, IWorldAnchors anchors,
            Vector3 toEarthFlat, out Vector3 eye, out Vector3 look)
        {
            List<ScatterInstance> instances = new ScatterPlanner(surface, scatterSettings, anchors).Plan();
            ScatterInstance bestBoulder = default;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < instances.Count; i++)
            {
                ScatterInstance candidate = instances[i];
                if (candidate.Kind != ScatterKind.Boulder || candidate.Size < 1.25f)
                {
                    continue;
                }

                float distFromHome = new Vector2(candidate.Position.x, candidate.Position.z).magnitude;
                if (distFromHome < 40f || distFromHome > 240f)
                {
                    continue;
                }

                int tightBoulders = 0;
                int tightPebbles = 0;
                for (int j = 0; j < instances.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    float dSq = (instances[j].Position - candidate.Position).sqrMagnitude;
                    if (dSq < 42f)
                    {
                        if (instances[j].Kind == ScatterKind.Boulder)
                        {
                            tightBoulders++;
                        }
                        else
                        {
                            tightPebbles++;
                        }
                    }
                }

                float score = candidate.Size * 3.5f + tightBoulders * 6.0f + tightPebbles * 1.2f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestBoulder = candidate;
                }
            }

            // Look at 142 degrees from toEarthFlat so the northern Sun rakes across the front-left of the rock cluster.
            Vector3 viewDir = Quaternion.Euler(0f, 142f, 0f) * toEarthFlat;
            Vector3 target = bestBoulder.Position + viewDir * 0.65f + Vector3.up * (bestBoulder.Size * 0.18f);
            Vector3 camPos = bestBoulder.Position - viewDir * (bestBoulder.Size * 2.15f + 1.25f);
            camPos.y = surface.SampleHeight(camPos.x, camPos.z) + 1.15f;
            eye = camPos;
            look = target;
        }

        private static void PlaceBehindRover(Camera camera, Vector3 roverPos, Vector3 horizontalForward,
            float pitchDeg, float fov)
        {
            Vector3 forwardFlat = Vector3.ProjectOnPlane(horizontalForward, Vector3.up).normalized;
            Vector3 eye = roverPos - forwardFlat * CameraBackDistance + Vector3.up * CameraHeightOffset;
            Vector3 right = Vector3.Cross(Vector3.up, forwardFlat).normalized;
            Quaternion pitchRot = Quaternion.AngleAxis(pitchDeg, right);
            Quaternion rotation = Quaternion.LookRotation(pitchRot * forwardFlat, Vector3.up);
            camera.transform.SetPositionAndRotation(eye, rotation);
            camera.fieldOfView = fov;
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

        private static GeometryCounts CountSceneGeometry(Scene scene)
        {
            var counts = new GeometryCounts();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    Mesh m = mf.sharedMesh;
                    if (m == null)
                    {
                        continue;
                    }

                    string meshName = m.name;
                    int verts = m.vertexCount;
                    int tris = (int)m.GetIndexCount(0) / 3;
                    if (meshName.StartsWith("TerrainChunk_", StringComparison.Ordinal))
                    {
                        counts.ChunkCount++;
                        counts.TerrainChunkVertices += verts;
                        counts.TerrainChunkTriangles += tris;
                    }
                    else if (meshName.StartsWith("TerrainBackdrop", StringComparison.Ordinal))
                    {
                        counts.BackdropVertices += verts;
                        counts.BackdropTriangles += tris;
                    }
                    else if (meshName.StartsWith("Pebbles_", StringComparison.Ordinal))
                    {
                        counts.PebbleBatchCount++;
                        counts.PebbleVertices += verts;
                        counts.PebbleTriangles += tris;
                    }
                    else if (meshName.StartsWith("Boulders_", StringComparison.Ordinal))
                    {
                        counts.BoulderBatchCount++;
                        counts.BoulderVertices += verts;
                        counts.BoulderTriangles += tris;
                    }
                }
            }

            return counts;
        }

        private static void WriteMetricsJson(string path, string stage, GeometryCounts c,
            Dictionary<string, (double Median, double P90)> frameTimes)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"stage\": \"{0}\",", stage));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"gpu\": \"{0}\",",
                SystemInfo.graphicsDeviceName.Replace("\"", "\\\"")));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"chunkCount\": {0},", c.ChunkCount));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"terrainChunkVertices\": {0},", c.TerrainChunkVertices));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"terrainChunkTriangles\": {0},", c.TerrainChunkTriangles));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"backdropVertices\": {0},", c.BackdropVertices));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"backdropTriangles\": {0},", c.BackdropTriangles));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"totalTerrainVertices\": {0},",
                c.TerrainChunkVertices + c.BackdropVertices));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"totalTerrainTriangles\": {0},",
                c.TerrainChunkTriangles + c.BackdropTriangles));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"pebbleBatchCount\": {0},", c.PebbleBatchCount));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"pebbleVertices\": {0},", c.PebbleVertices));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"pebbleTriangles\": {0},", c.PebbleTriangles));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"boulderBatchCount\": {0},", c.BoulderBatchCount));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"boulderVertices\": {0},", c.BoulderVertices));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"boulderTriangles\": {0},", c.BoulderTriangles));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"totalRockVertices\": {0},",
                c.PebbleVertices + c.BoulderVertices));
            sb.AppendLine(string.Format(CultureInfo.InvariantCulture, "  \"totalRockTriangles\": {0},",
                c.PebbleTriangles + c.BoulderTriangles));
            sb.AppendLine("  \"frameTimes1080pMs\": {");

            int idx = 0;
            foreach (KeyValuePair<string, (double Median, double P90)> kv in frameTimes)
            {
                string comma = idx < frameTimes.Count - 1 ? "," : "";
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "    \"{0}\": {{ \"medianMs\": {1:F3}, \"p90Ms\": {2:F3} }}{3}",
                    kv.Key, kv.Value.Median, kv.Value.P90, comma));
                idx++;
            }

            sb.AppendLine("  }");
            sb.AppendLine("}");
            File.WriteAllText(path, sb.ToString());
            Debug.Log($"[TerrainR1Session] Wrote {path}:\n{sb}");
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {message}");
            }
        }

        private struct GeometryCounts
        {
            public int ChunkCount;
            public int TerrainChunkVertices;
            public int TerrainChunkTriangles;
            public int BackdropVertices;
            public int BackdropTriangles;
            public int PebbleBatchCount;
            public int PebbleVertices;
            public int PebbleTriangles;
            public int BoulderBatchCount;
            public int BoulderVertices;
            public int BoulderTriangles;
        }
    }
}

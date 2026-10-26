using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using MoonProject.World;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// The "Unity max" look spike (engine comparison): boots the built Main scene on a fresh save, wakes 07 and home,
    /// and shoots the comparison vista. Writes Logs/look-max/&lt;stamp&gt;/. Needs a GPU:
    /// <c>python tools/unity_batch.py tests --platform playmode --filter LookMaxSession</c>.
    /// </summary>
    [Explicit("Slow look-spike captures in the real Main scene.")]
    [Category("LookMax")]
    public sealed class LookMaxSession
    {
        private const float WakeTimeout = 20f;
        private const float HomeSettleSeconds = 6f;
        private const int ProbeWidth = 1720;
        private const int ProbeHeight = 720;
        private const int StageSettleFrames = 90;
        private const float SunScanEnd = 2800f;
        private const float SunScanStep = 100f;
        private const float GroundProbeHeight = 400f;
        private const string ArrayPrefab = "Assets/_Project/Generated/Art/R1/Models/Structures/SolarArray.prefab";

        private static readonly float[] ProbeClockTimes = { 0f, 40f, 200f, 600f, 900f, 1500f, 2200f, 2650f, 4800f };

        private static readonly string OutputRoot =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "look-max"));

        private readonly List<string> _problems = new List<string>();
        private GameBootstrap _bootstrap;
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
            Scene scene = SceneManager.GetSceneByPath(LookMaxScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(LookMaxScene.SaveSlot);
        }

        /// <summary>
        /// Logs the sun's elevation at a spread of clock times, the world's anchors and the named landmarks near home,
        /// and shoots a ring of wide frames round home, so the vista poses can be chosen.
        /// </summary>
        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(LookMaxScene))]
        [PostBuildCleanup(typeof(LookMaxScene))]
        public IEnumerator Probe_LogsTheVista()
        {
            string folder = Path.Combine(OutputRoot, "probe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            yield return Boot();
            GameContext context = _bootstrap.Context;
            var sky = (SkyCycleSystem)context.Get<ISunlight>();
            var report = new StringBuilder();
            foreach (float t in ProbeClockTimes)
            {
                sky.SetClockTime(t);
                Vector3 sun = sky.SunDirection;
                report.AppendLine($"clock {t,6:0}: sun elevation {Mathf.Asin(sun.y) * Mathf.Rad2Deg,6:0.0} deg, " +
                    $"bearing {Mathf.Atan2(sun.x, sun.z) * Mathf.Rad2Deg,6:0.0} deg, phase {sky.Phase}");
            }

            LookMaxVista vista = LookMaxVista.Load();
            Vector3[] marks =
            {
                vista.rover.Position(Ground), vista.array.Position(Ground), new Vector3(34.94f, 3.5f, 160.24f),
                new Vector3(73.08f, 3.3f, 161f),
            };
            for (float t = 0f; t <= SunScanEnd; t += SunScanStep)
            {
                sky.SetClockTime(t);
                Vector3 sun = sky.SunDirection;
                report.Append($"clock {t,6:0}: elevation {Mathf.Asin(sun.y) * Mathf.Rad2Deg,5:0.0}, " +
                    $"bearing {Mathf.Atan2(sun.x, sun.z) * Mathf.Rad2Deg,6:0.0}, sunlight at 07/array/lander/fallen");
                foreach (Vector3 mark in marks)
                {
                    report.Append($" {sky.SunlightAt(mark + Vector3.up * 0.5f):0.00}");
                }

                report.AppendLine();
            }

            IWorldAnchors anchors = context.Get<IWorldAnchors>();
            for (int i = 0; i < anchors.Count; i++)
            {
                WorldAnchor anchor = anchors.Get(i);
                report.AppendLine($"anchor {anchor.Id}: {anchor.Position} forward {anchor.Forward}");
            }

            IWorldLayout layout = context.Get<IWorldLayout>();
            report.AppendLine($"base {layout.BasePosition} peak {layout.PeakPosition} earth {layout.EarthDirection}");
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    string name = t.name;
                    if (name.Contains("Lander") || name.Contains("Solar") || name.Contains("Array") ||
                        name == "RoverModel" || name.Contains("Rover07"))
                    {
                        report.AppendLine($"object {name} at {t.position} (parent {t.parent?.name})");
                    }
                }
            }

            Camera view = context.Get<IViewCamera>().Camera;
            report.AppendLine($"view camera at {view.transform.position} fov {view.fieldOfView}");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "probe.txt"), report.ToString());
            Debug.Log("[look-max] probe\n" + report);

            sky.SetClockTime(600f);
            var host = new GameObject("LookMaxProbeCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                for (int i = 0; i < 8; i++)
                {
                    float bearing = i * 45f;
                    Vector3 eye = layout.BasePosition + new Vector3(0f, 12f, 0f);
                    camera.fieldOfView = 60f;
                    camera.transform.SetPositionAndRotation(eye, Quaternion.Euler(8f, bearing, 0f));
                    FrameCapture.SavePng(camera, ProbeWidth, ProbeHeight,
                        Path.Combine(folder, $"ring_{bearing:000}.png"));
                    yield return null;
                }
            }
            finally
            {
                Object.Destroy(host);
            }

            Debug.Log("[look-max] probe captures " + folder);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>
        /// Shoots the comparison set (every pose by day C, the night poses by night A) in every profile at the vista's
        /// size, measures each profile's render time at the first pose, and writes poses.json (Unity and Unreal
        /// coordinates) and timing.txt beside the stills.
        /// </summary>
        [UnityTest]
        [Timeout(3600000)]
        [PrebuildSetup(typeof(LookMaxScene))]
        [PostBuildCleanup(typeof(LookMaxScene))]
        public IEnumerator Stills_AreShot_InEveryProfile()
        {
            LookMaxVista vista = LookMaxVista.Load();
            string folder = Path.Combine(OutputRoot, "stills-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(folder);
            yield return Boot();
            GameContext context = _bootstrap.Context;
            var sky = (SkyCycleSystem)context.Get<ISunlight>();
            yield return StageVista(vista, context);
            Camera view = context.Get<IViewCamera>().Camera;
            File.WriteAllText(Path.Combine(folder, "poses.json"), vista.ResolvedPosesJson(sky, Ground));
            var timing = new StringBuilder();
            view.enabled = false;
            try
            {
                foreach (string profileName in new[] { LookMaxProfile.Shipped, LookMaxProfile.Max, LookMaxProfile.Stp })
                {
                    var host = new GameObject("LookMaxCamera " + profileName);
                    Camera camera = host.AddComponent<Camera>();
                    camera.CopyFrom(view);
                    camera.enabled = false;
                    LookMaxShots.CopyPipelineSettings(view, camera);
                    var profile = new LookMaxProfile(profileName, camera);
                    var shots = new LookMaxShots(camera, vista.width, vista.height);
                    try
                    {
                        sky.SetClockTime(vista.dayClock);
                        foreach (LookMaxVista.Pose pose in vista.poses)
                        {
                            vista.Aim(camera, pose, Ground);
                            yield return shots.Shoot(vista.settleFrames,
                                Path.Combine(folder, $"{pose.name}_day_{profileName}.png"));
                        }

                        vista.Aim(camera, vista.poses[0], Ground);
                        yield return shots.Time(vista.timingFrames);
                        timing.AppendLine($"{profileName}: {shots.TimingSummary}");

                        sky.SetClockTime(vista.nightClock);
                        foreach (LookMaxVista.Pose pose in vista.poses)
                        {
                            if (Array.IndexOf(vista.night, pose.name) < 0)
                            {
                                continue;
                            }

                            vista.Aim(camera, pose, Ground);
                            yield return shots.Shoot(vista.settleFrames,
                                Path.Combine(folder, $"{pose.name}_night_{profileName}.png"));
                        }
                    }
                    finally
                    {
                        shots.Dispose();
                        profile.Dispose();
                        Object.Destroy(host);
                    }
                }
            }
            finally
            {
                view.enabled = true;
            }

            File.WriteAllText(Path.Combine(folder, "timing.txt"), timing.ToString());
            Debug.Log("[look-max] stills " + folder + "\n" + timing);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        /// <summary>Puts 07 and a built solar array on their marks and lets the physics settle.</summary>
        private IEnumerator StageVista(LookMaxVista vista, GameContext context)
        {
            context.Get<IRoverPlacement>().PlaceAt(vista.rover.Position(Ground), Quaternion.Euler(0f, vista.rover.yaw, 0f));
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ArrayPrefab);
            Assert.IsNotNull(prefab, ArrayPrefab + " exists");
            Object.Instantiate(prefab, vista.array.Position(Ground), Quaternion.Euler(0f, vista.array.yaw, 0f))
                .name = "LookMaxSolarArray";
#endif
            for (int i = 0; i < StageSettleFrames; i++)
            {
                yield return null;
            }
        }

        /// <summary>Height of the ground under (x, z), from the terrain colliders.</summary>
        private static float Ground(float x, float z)
        {
            Assert.IsTrue(Physics.Raycast(new Vector3(x, GroundProbeHeight, z), Vector3.down, out RaycastHit hit,
                GroundProbeHeight * 2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore),
                $"ground under ({x}, {z})");
            return hit.point.y;
        }

        /// <summary>Loads the scene copy on a fresh save, waits for 07 to wake and powers home.</summary>
        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(LookMaxScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(LookMaxScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The look-max session loads the scene through the editor.");
#endif
            foreach (GameObject root in SceneManager.GetSceneByPath(LookMaxScene.ScenePath).GetRootGameObjects())
            {
                _bootstrap = _bootstrap != null ? _bootstrap : root.GetComponent<GameBootstrap>();
            }

            Assert.IsNotNull(_bootstrap, "the scene has its bootstrap");
            GameContext context = _bootstrap.Context;
            _awoke = context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            context.Events.Publish(new BasePowerChanged(BasePowerStage.Home, false));
            yield return new WaitForSeconds(HomeSettleSeconds);
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add($"{type}: {condition}");
            }
        }
    }
}

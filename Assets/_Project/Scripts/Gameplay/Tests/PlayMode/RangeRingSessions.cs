using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using UnityEngine;
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

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The range ring in the real Main scene (opening v3, plan 4.2): the first charge shows it faint round 07 for a
    /// few seconds, drawn on the ground, and working it out does not hitch the frame. A top-down capture is written to
    /// Logs/gameplay-captures/range-ring.png.
    /// </summary>
    [Category("RangeRing")]
    public sealed class RangeRingSessions
    {
        private const float WakeTimeout = 20f;
        private const float WorkedOutWithinMilliseconds = 250f;

        /// <summary>Timed workings-out whose median is held to the budget, after one untimed warm-up.</summary>
        private const int TimedRuns = 5;
        private const float CaptureHeight = 1000f;
        private const int CaptureWidth = 1280;
        private const int CaptureTall = 720;

        private GameContext _context;
        private GameplaySystem _gameplay;
        private bool _awake;

        [SetUp]
        public void SetUp()
        {
            _gameplay = null;
            _awake = false;
        }

        [TearDown]
        public void TearDown()
        {
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheFirstCharge_ShowsAFaintRingOnTheGround_ForAFewSeconds_WithoutAHitch()
        {
            yield return Boot();
            var ring = _context.Get<IRangeRing>();
            Assert.IsFalse(ring.IsShown, "a new game shows no ring");
            var rover = _context.Get<IRoverState>();

            // The first working-out pays for the code's first run (JIT, first allocations) on a cold editor; it is
            // not timed. The budget is for the steady-state work: the median of several further workings-out.
            _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            Assert.IsTrue(ring.IsShown, "the first charge shows the ring");
            Assert.AreEqual(RangeRingMode.Faint, ring.Mode);
            var timings = new long[TimedRuns];
            for (int run = 0; run < TimedRuns; run++)
            {
                var timer = Stopwatch.StartNew();
                _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
                timer.Stop();
                timings[run] = timer.ElapsedMilliseconds;
            }

            System.Array.Sort(timings);
            long median = timings[TimedRuns / 2];
            Assert.Less(median, WorkedOutWithinMilliseconds,
                $"working the ring out took {median} ms (median of {TimedRuns}): no hitch");
            Assert.IsTrue(ring.IsShown);
            Assert.AreEqual(_gameplay.RangeRing.Count, ring.Count);

            float nearest = float.MaxValue;
            float furthest = 0f;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 offset = ring.Point(i) - rover.Position;
                offset.y = 0f;
                nearest = Mathf.Min(nearest, offset.magnitude);
                furthest = Mathf.Max(furthest, offset.magnitude);
            }

            Assert.Greater(furthest, 20f, "07 has charge: the ring has a size");

            yield return RealSeconds(1.5f);
            Assert.IsTrue(_gameplay.RangeRingLine.IsDrawn, "the ring is drawn on the ground");
            Capture(rover.Position);
            yield return RealSeconds(ring.Mode == RangeRingMode.Faint ? 3.5f : 1f);
            Assert.IsFalse(ring.IsShown, "and gone after its few seconds");
            Assert.IsFalse(_gameplay.RangeRingLine.IsDrawn);
        }

        private static IEnumerator RealSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private void Capture(Vector3 centre)
        {
            var host = new GameObject("RangeRingLens");
            Camera camera = host.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 50f;
            camera.farClipPlane = 2000f;
            host.transform.position = centre + Vector3.up * CaptureHeight;
            host.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures"));
            Directory.CreateDirectory(folder);
            FrameCapture.SavePng(camera, CaptureWidth, CaptureTall, Path.Combine(folder, "range-ring.png"));
            Object.Destroy(host);
        }

        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The range ring sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes");
            }

            yield return null;
        }
    }
}

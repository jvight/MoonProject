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
    /// few seconds, drawn on the ground, and working it out does not hitch the frame. With relay.1 lit and 07 full
    /// beside it (R10), the ring runs into the rille notch's mouth but ends at ground 07 cannot climb, short of the
    /// Mare. Top-down captures are written to Logs/gameplay-captures/range-ring.png and range-ring-r10-rille.png.
    /// </summary>
    [Category("RangeRing")]
    public sealed class RangeRingSessions
    {
        private const float WakeTimeout = 20f;
        private const float WorkedOutWithinMilliseconds = 250f;

        /// <summary>Timed workings-out whose fastest is held to the budget, after one untimed warm-up.</summary>
        private const int TimedRuns = 5;
        private const float CaptureHeight = 1000f;
        private const int CaptureWidth = 1280;
        private const int CaptureTall = 720;

        // World's rille notch anchor (WorldAnchors.GateRille): the mouth of the pass on the basin side.
        private const string RilleGate = "gate.rille";

        // The R10 frame looks down from this high over the middle of relay.1 and the notch.
        private const float RilleCaptureHeight = 420f;
        private const float RevealWaitSeconds = 3f;

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
            // not timed. The budget is for the steady-state work: the fastest of several further workings-out, since
            // machine load only adds time and a real regression slows every run.
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
            long fastest = timings[0];
            Assert.Less(fastest, WorkedOutWithinMilliseconds,
                $"working the ring out took {fastest} ms (fastest of {TimedRuns}): no hitch");
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
            Capture(rover.Position, CaptureHeight, "range-ring.png");
            yield return RealSeconds(ring.Mode == RangeRingMode.Faint ? 3.5f : 1f);
            Assert.IsFalse(ring.IsShown, "and gone after its few seconds");
            Assert.IsFalse(_gameplay.RangeRingLine.IsDrawn);
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator AtRelayOneLit_TheRingRunsIntoTheRilleNotch_AndEndsAtGround07CannotClimb()
        {
            yield return Boot();
            var anchors = _context.Get<IWorldAnchors>();
            Assert.IsTrue(anchors.TryGet(WorldAnchorIds.RelayPrefix + 1, out WorldAnchor relay), "relay.1 stands");
            Assert.IsTrue(anchors.TryGet(RilleGate, out WorldAnchor gate), "the rille notch has its gate anchor");
            _gameplay.Relay.Restore(new RelaySaveData { fitted = true });
            _context.Get<IRoverPlacement>().PlaceAt(relay.Position, Quaternion.LookRotation(gate.Position -
                                                                                             relay.Position));
            BatteryModel battery = _gameplay.Battery.Model;
            battery.Restore(new BatterySaveData { cells = battery.Cells, charge = battery.Cells });
            yield return null;
            var rover = _context.Get<IRoverState>();
            var ring = _context.Get<IRangeRing>();
            _gameplay.RangeRing.Show(RangeRingMode.Full);
            Assert.IsTrue(ring.IsShown, "relay.1 lit: the full ring shows");

            Vector3 toGate = gate.Position - rover.Position;
            toGate.y = 0f;
            float best = -1f;
            float reach = 0f;
            for (int i = 0; i < ring.Count; i++)
            {
                Vector3 offset = ring.Point(i) - rover.Position;
                offset.y = 0f;
                float along = Vector3.Dot(offset.normalized, toGate.normalized);
                if (along > best)
                {
                    best = along;
                    reach = offset.magnitude;
                }
            }

            float cap = _gameplay.Battery.Tuning.RangeRing.MaxRangeMetres;
            Debug.Log($"[range-ring] R10: toward the rille the ring reaches {reach:F0} m from relay.1; the notch's " +
                      $"mouth is {toGate.magnitude:F0} m off; a full battery's open-ground reach is {cap:F0} m");
            Assert.Greater(reach, toGate.magnitude, "the ring runs into the notch's mouth: the pass is there to try");
            Assert.Less(reach, cap - _gameplay.Battery.Tuning.RangeRing.StepMetres,
                "and ends at ground 07 cannot climb, short of the Mare, with charge to spare");

            yield return RealSeconds(RevealWaitSeconds);
            Capture(Vector3.Lerp(rover.Position, gate.Position, 0.5f), RilleCaptureHeight,
                "range-ring-r10-rille.png");
        }

        private static IEnumerator RealSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private void Capture(Vector3 centre, float height, string file)
        {
            var host = new GameObject("RangeRingLens");
            Camera camera = host.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 50f;
            camera.farClipPlane = 2000f;
            host.transform.position = centre + Vector3.up * height;
            host.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures"));
            Directory.CreateDirectory(folder);
            FrameCapture.SavePng(camera, CaptureWidth, CaptureTall, Path.Combine(folder, file));
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The crew's lift in the real Main scene (docs/features/M3-16 step 8), seeded with the tower at its third level
    /// and a little metal and wiring: 07 rests on the feed pad and the lift is restored (platform level, cable taut,
    /// lamp lit, saved as restored); 07 drives onto the platform and is carried up to the deck, where the lookout
    /// opens; a drive key at the deck brings it back down. Frames from the game camera at each stage go to
    /// Logs/gameplay-captures/lift-*.png, with lift.md recording 07's height and the lift's phase. Any error in the
    /// log fails it. Slow and needs a GPU: run on demand with --category LiftSession.
    /// </summary>
    [Explicit("Slow session in the real Main scene; run on demand with --category LiftSession.")]
    [Category("LiftSession")]
    public sealed class LiftSession : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 3f;
        private const float PhaseTimeout = 30f;
        private const float BoardRadius = 0.5f;
        private const float DriveTimeout = 40f;
        private const float StopSpeed = 0.2f;
        private const float StopTimeout = 8f;
        private const float LookoutSettleSeconds = 4f;

        /// <summary>07 is carried: its height at the top within this of the deck's (m).</summary>
        private const float CarriedTolerance = 0.6f;

        // The tower at its third level (the lift stage) and enough metal and wiring for the winch, in Core's envelope
        // and Gameplay's upgrades (version 1) and materials (version 2) section formats.
        private const string LiftPoweredSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-10T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":3}]}\"}," +
            "{\"key\":\"gameplay.materials\",\"version\":2,\"json\":\"{\\\"metal\\\":12,\\\"wiring\\\":12," +
            "\\\"optics\\\":4}\"}]}";

        private readonly List<string> _problems = new List<string>();
        private readonly StringBuilder _report = new StringBuilder();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private Autopilot _pilot;
        private IDisposable _awoke;
        private Keyboard _keyboard;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Application.logMessageReceived += OnLog;
        }

        public override void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _rover?.SetDriveSource(null);
            _awoke?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheLift_IsRestored_RiddenUpToTheLookout_AndDown()
        {
            yield return Boot();
            LiftStation lift = _gameplay.Lift;
            _report.AppendLine("# The crew's lift (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Phase | 07's height (m) |");
            _report.AppendLine("|---|---|---:|");
            var beats = new int[3];
            using IDisposable sounds = _context.Events.Subscribe<LiftCued>(cued => beats[(int)cued.Cue]++);
            yield return WaitFor(lift, LiftPhase.Jammed, "The tower's third level powers the jammed lift.");

            _context.Get<IRoverPlacement>().PlaceAt(lift.FeedPadCentre, Quaternion.identity);
            yield return null;
            Capture("lift-0-jammed", lift);
            yield return WaitFor(lift, LiftPhase.Ready, "Resting on the feed pad restores the lift.");
            Assert.IsTrue(lift.Capture().restored, "Restored is saved.");
            yield return new WaitForSeconds(SettleSeconds);
            Capture("lift-1-restored", lift);

            bool atLookout = false;
            using (_context.Events.Subscribe<LiftRideChanged>(ride => atLookout = ride.AtLookout))
            {
                yield return DriveTo(lift.RideSpot, BoardRadius, "the platform");
                float bottom = _rover.Position.y;
                yield return WaitFor(lift, LiftPhase.Top, "Resting on the platform carries 07 up to the deck.");
                Assert.IsTrue(atLookout, "The lookout opens at the deck.");
                yield return new WaitForSeconds(LookoutSettleSeconds);
                Capture("lift-2-lookout", lift);
                float rise = _rover.Position.y - bottom;
                Assert.Greater(rise, LiftRise(lift) - CarriedTolerance, $"07 rode up with the platform ({rise:F2} m).");

                Press(_keyboard.wKey);
                yield return WaitFor(lift, LiftPhase.Lowering, "A drive key at the deck brings the lift down.");
                Release(_keyboard.wKey);
                Assert.IsFalse(atLookout, "The lookout closes.");
                yield return WaitFor(lift, LiftPhase.Ready, "The platform settles at the bottom stop.");
                Capture("lift-3-down", lift);
            }

            Assert.AreEqual(1, beats[(int)LiftCue.WinchCreaked], "The jammed winch creaked once as 07 came near.");
            Assert.AreEqual(2, beats[(int)LiftCue.RideStarted], "The platform started moving up, then down.");
            Assert.AreEqual(2, beats[(int)LiftCue.RideStopped], "The platform reached the top stop, then the bottom.");

            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(Path.Combine(GameplayFixture.CaptureFolder, "lift.md"), _report.ToString());
            Debug.Log("[lift] " + _report);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private static float LiftRise(LiftStation lift)
        {
            return lift.TopSpot.y - lift.RideSpot.y;
        }

        private static IEnumerator WaitFor(LiftStation lift, LiftPhase phase, string what)
        {
            float deadline = Time.time + PhaseTimeout;
            while (lift.Phase != phase)
            {
                Assert.Less(Time.time, deadline, $"{what} (still {lift.Phase})");
                yield return null;
            }
        }

        private IEnumerator Boot()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory,
                PlaythroughScene.SaveSlot + SaveService.Extension), LiftPoweredSave);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The lift session loads the scene through the editor.");
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
            _rover = (RoverController)_context.Get<IRoverRig>();
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            float started = Time.time;
            while (!_awake)
            {
                Assert.Less(Time.time - started, WakeTimeout, "07 wakes up");
                yield return null;
            }

            yield return new WaitForSeconds(SettleSeconds);
            _pilot = new Autopilot(_rover);
            _rover.SetDriveSource(_pilot);
        }

        private IEnumerator DriveTo(Vector3 target, float radius, string what)
        {
            _pilot.GoTo(target, radius, 1f);
            float deadline = Time.time + DriveTimeout;
            while (!_pilot.Arrived && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_pilot.Arrived, $"07 reaches {what} within {DriveTimeout:F0} s");
            _pilot.Target = null;
            deadline = Time.time + StopTimeout;
            while (_rover.Speed > StopSpeed && Time.time < deadline)
            {
                yield return null;
            }
        }

        private void Capture(string name, LiftStation lift)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, CaptureWidth, CaptureHeight,
                Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
            _report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2:F2} |", name, lift.Phase,
                _rover.Position.y));
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _problems.Add(message);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
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
    /// The crew's belly lift and lab in the real Main scene (docs/features/M3-19), seeded with home powered (tower L1)
    /// and the Cargo Cradle's blueprint tube riding on 07: waking on the dock with a find for the lab, the jammed lift
    /// asks for the stitch; 07 drives onto the platform and stitches it level (saved as restored); a breath later the
    /// platform carries 07 up through the hatch into the lab, where the turntable turns to the reader, whose beam feed
    /// and decode publish the delivery, the decode and the hand-off. Then the stick turns the table round all four
    /// stations, looks at the dark chart and comes away, and brings the platform down, turned so 07 faces out toward the
    /// dock. A second session greets the ladder as progression does (07 wakes beside it) and catches 07's look up it,
    /// then down at the lift (Logs/gameplay-captures/ladder-*.png). Frames from the game camera at each stage (the ride
    /// from outside and through the hatch, each station from the ring) go to Logs/gameplay-captures/lift-*.png and
    /// lab-*.png, with lift.md recording 07's height and the lift's phase. Any error in the log fails it. Slow and needs
    /// a GPU: run on demand with --category LiftSession.
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
        private const float TurnSeconds = 3f;
        private const float StationFrameSeconds = 1.5f;
        private const float KeySeconds = 0.25f;

        /// <summary>How far (m) out from the platform, toward the dock, 07 lines up before it drives on.</summary>
        private const float LineUp = 5f;

        /// <summary>07 faces out toward the dock after the ride down: its heading within this of the way out.</summary>
        private const float FacesOut = 0.9f;

        /// <summary>Seconds into each look the frame is taken (the moment has eased most of the way in).</summary>
        private const float LookFrameSeconds = 1.8f;

        /// <summary>The ladder's look is up it: its subject this far (m) above 07 at least.</summary>
        private const float LadderAbove = 1.5f;

        /// <summary>07 is carried up: it rose at least this much short of the platform's rise (m).</summary>
        private const float CarriedTolerance = 0.6f;

        private const string CargoCradle = "rover.cargo_cradle";

        // Home powered (tower L1) and the Cargo Cradle's tube found and riding on 07, in Core's envelope and Gameplay's
        // upgrades and blueprints (version 1) section formats.
        private const string LabSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-10T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":1}]}\"}," +
            "{\"key\":\"gameplay.blueprints\",\"version\":1,\"json\":\"{\\\"carried\\\":[" +
            "\\\"rover.cargo_cradle\\\"]}\"}]}";

        // The tower at its first level: home awake (no wake opening), the lift still dormant.
        private const string HomeAwakeSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-10T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":1}]}\"}]}";

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
        public IEnumerator TheBellyLift_IsStitched_RiddenIntoTheLab_TheReaderDecodes_AndItComesDownFacingOut()
        {
            yield return Boot(LabSave);
            LiftStation lift = _gameplay.Lift;
            _report.AppendLine("# The crew's belly lift and lab (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Phase | 07's height (m) |");
            _report.AppendLine("|---|---|---:|");
            var beats = new int[5];
            var faced = new List<LabStation>();
            var decoded = new List<string>();
            var handOffs = new List<DecodeHandOff>();
            bool inside = false;
            using IDisposable sounds = _context.Events.Subscribe<LiftCued>(cued => beats[(int)cued.Cue]++);
            using IDisposable turns = _context.Events.Subscribe<LabStationFaced>(stop => faced.Add(stop.Station));
            using IDisposable finds = _context.Events.Subscribe<FindDecoded>(find => decoded.Add(find.KitId));
            using IDisposable pulses = _context.Events.Subscribe<DecodeHandOff>(handOffs.Add);
            using IDisposable rides = _context.Events.Subscribe<LiftRideChanged>(ride => inside = ride.Inside);
            yield return WaitFor(lift, LiftPhase.Called,
                "Home powered and a tube for the lab riding on 07: the jammed lift asks for the stitch.");
            Capture("lift-0-called", lift);

            yield return DriveTo(lift.RideSpot - lift.RideHeading * LineUp, BoardRadius, "the apron before the lift");
            yield return DriveTo(lift.StitchSpot, BoardRadius, "the spot by the jammed platform");
            yield return WaitFor(lift, LiftPhase.Stitching, "Resting by the lit platform, 07 stitches the cable.");
            yield return WaitFor(lift, LiftPhase.Ready, "The stitch levels the platform.");
            Assert.IsTrue(lift.Capture().restored, "Restored is saved.");
            Capture("lift-1-restored", lift);

            yield return DriveTo(lift.RideSpot, BoardRadius, "up its ramp onto the platform");
            float bottom = _rover.Position.y;
            yield return WaitFor(lift, LiftPhase.Rising, "Resting on the platform, it rises by itself.");
            yield return new WaitForSeconds(lift.Tuning.RideSeconds * 0.35f);
            Capture("lift-2-ride-outside", lift);
            yield return new WaitForSeconds(lift.Tuning.RideSeconds * 0.35f);
            Capture("lift-3-ride-hatch", lift);
            yield return WaitFor(lift, LiftPhase.Inside, "The platform settles in the lab.");
            Assert.IsTrue(inside, "The ride says it is inside.");
            Assert.IsTrue(lift.Lab.IsInside);
            float rise = _rover.Position.y - bottom;
            Assert.Greater(rise, LiftRise(lift) - CarriedTolerance, $"07 rode up with the platform ({rise:F2} m).");

            yield return WaitUntil(() => faced.Contains(LabStation.Reader), TurnSeconds,
                "The turntable turns to the reader that wants the tube");
            yield return WaitUntil(() => decoded.Count > 0, PhaseTimeout, "The reader feeds the tube in and decodes it");
            Capture("lab-reader", lift);
            Assert.AreEqual(CargoCradle, decoded[0]);
            Assert.AreEqual(1, handOffs.Count, "The hand-off pulse starts down to the bay.");
            Assert.IsTrue(_gameplay.Blueprints.IsPinned(CargoCradle), "Decoded: the bay can build the Cargo Cradle.");
            Assert.IsFalse(_gameplay.Blueprints.IsDrawn(CargoCradle), "The bay sleeps: the sheet waits for L2.");

            LabStation[] round = { LabStation.Board, LabStation.Keepsakes, LabStation.Chart, LabStation.Reader };
            foreach (LabStation next in round)
            {
                int stops = faced.Count;
                yield return Tap(_keyboard.dKey);
                yield return WaitUntil(() => faced.Count > stops && !lift.Turning, TurnSeconds,
                    $"Right turns the table to {next}");
                Assert.AreEqual(next, faced[faced.Count - 1]);
                yield return new WaitForSeconds(StationFrameSeconds);
                Capture("lab-" + next.ToString().ToLowerInvariant(), lift);
            }

            yield return Tap(_keyboard.aKey);
            yield return WaitUntil(() => lift.Lab.Facing == LabStation.Chart && !lift.Turning, TurnSeconds,
                "Left turns it back to the chart");
            yield return Tap(_keyboard.wKey);
            yield return WaitUntil(() => lift.Lab.IsLooking, PhaseTimeout, "Forward looks at the chart");
            yield return new WaitForSeconds(StationFrameSeconds);
            Capture("lab-chart-look", lift);
            yield return Tap(_keyboard.sKey);
            yield return WaitUntil(() => !lift.Lab.IsLooking, PhaseTimeout, "Back comes away from the look");
            Assert.AreEqual(LiftPhase.Inside, lift.Phase, "Coming away from a look keeps 07 in the lab.");

            yield return Tap(_keyboard.sKey);
            yield return WaitFor(lift, LiftPhase.Lowering, "Back brings the platform down.");
            Assert.IsFalse(lift.Lab.IsInside, "07 has left the lab.");
            yield return WaitFor(lift, LiftPhase.Ready, "The platform settles at the bottom stop.");
            Vector3 outward = -lift.RideHeading;
            Vector3 heading = _rover.Rotation * Vector3.forward;
            heading.y = 0f;
            Assert.Greater(Vector3.Dot(heading.normalized, outward.normalized), FacesOut,
                "The platform turned while lowering: 07 faces out toward the dock.");
            yield return new WaitForSeconds(SettleSeconds);
            Capture("lift-4-down", lift);

            Assert.AreEqual(1, beats[(int)LiftCue.WinchCreaked], "The jammed platform creaked once as it asked.");
            Assert.AreEqual(2, beats[(int)LiftCue.RideStarted], "The platform started moving up, then down.");
            Assert.AreEqual(2, beats[(int)LiftCue.RideStopped], "The platform reached the cabin, then the bottom.");
            Assert.AreEqual(1, beats[(int)LiftCue.HatchOpened], "The hatch opened for the ride up.");
            Assert.AreEqual(1, beats[(int)LiftCue.HatchClosed], "The hatch closed after the ride down.");

            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(Path.Combine(GameplayFixture.CaptureFolder, "lift.md"), _report.ToString());
            Debug.Log("[lift] " + _report);
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheLadderLook_LooksUpTheLadder_ThenAtTheLift()
        {
            yield return Boot(HomeAwakeSave);
            LiftStation lift = _gameplay.Lift;
            _report.AppendLine("# The ladder look (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Phase | 07's height (m) |");
            _report.AppendLine("|---|---|---:|");
            var looks = new List<Vector3>();
            bool ended = false;
            using (_context.Events.Subscribe<LadderLookChanged>(look =>
            {
                if (look.Looking)
                {
                    looks.Add(look.Subject);
                }
                else
                {
                    ended = true;
                }
            }))
            {
                _context.Events.Publish(new PlaceFirstApproached(ProgressionKeys.PlaceLadder));
                yield return WaitUntil(() => looks.Count >= 1, PhaseTimeout, "07 looks up the crew's ladder");
                yield return new WaitForSeconds(LookFrameSeconds);
                Capture("ladder-1-up", lift);
                Assert.Greater(looks[0].y, _rover.Position.y + LadderAbove, "The first look is up the ladder.");
                yield return WaitUntil(() => looks.Count >= 2, PhaseTimeout, "then at the lift");
                yield return new WaitForSeconds(LookFrameSeconds);
                Capture("ladder-2-lift", lift);
                yield return WaitUntil(() => ended, PhaseTimeout, "The look ends");
            }

            Assert.AreEqual(2, looks.Count, "Two looks, once.");
            Directory.CreateDirectory(GameplayFixture.CaptureFolder);
            File.WriteAllText(Path.Combine(GameplayFixture.CaptureFolder, "ladder.md"), _report.ToString());
            Assert.IsEmpty(_problems, "errors in the log:\n" + string.Join("\n", _problems));
        }

        private static IEnumerator WaitUntil(Func<bool> done, float timeout, string what)
        {
            float deadline = Time.time + timeout;
            while (!done())
            {
                Assert.Less(Time.time, deadline, what);
                yield return null;
            }
        }

        private static float LiftRise(LiftStation lift)
        {
            return lift.InsideSpot.y - lift.RideSpot.y;
        }

        /// <summary>A short press of <paramref name="key"/>, as a player taps the stick's direction.</summary>
        private IEnumerator Tap(KeyControl key)
        {
            Press(key);
            yield return new WaitForSeconds(KeySeconds);
            Release(key);
            yield return new WaitForSeconds(KeySeconds);
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

        private IEnumerator Boot(string save)
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory,
                PlaythroughScene.SaveSlot + SaveService.Extension), save);
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

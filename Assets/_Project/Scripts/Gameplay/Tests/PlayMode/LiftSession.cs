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
using UnityEngine.UIElements;
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
    /// The crew's belly lift and lab in the real Main scene (docs/features/M3-19), testing both:
    /// <list type="bullet">
    /// <item>A 0.5.5 save stuck in <see cref="LiftPhase.Jammed"/> (<c>restored = false</c>, tower L1) with 07
    /// empty-handed: the jammed lift plays its physical jam cue (platform shudder, winch creak, spark burst at the
    /// stitch point) before asking for <c>next.lift.stitch</c>, then asks for <c>next.lift.stitch</c>, stitches level,
    /// asks for <c>next.lift.ride</c>, and rides 07 up into the lab facing the survey chart.</item>
    /// <item>Carrying a keepsake (the rubber duck in the Cargo Cradle): rides up with 07 and turns the turntable to the
    /// keepsake shelf, turns round every station, looks at the dark chart and comes away, and brings the platform down
    /// facing out toward the dock.</item>
    /// <item>The ladder look: 07's look up the ladder, then down at the lift.</item>
    /// </list>
    /// Slow and needs a GPU: run on demand with --category LiftSession.
    /// </summary>
    [Explicit("Slow session in the real Main scene; run on demand with --category LiftSession.")]
    [Category("LiftSession")]
    public sealed class LiftSession : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const int ReviewWidth = 1920;
        private const int ReviewHeight = 1080;
        private const string ReviewCaptureFolder = "Logs/captures";
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 3f;
        private const float PhaseTimeout = 30f;
        private const float GuidanceTimeout = 30f;
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

        // Home powered (tower L1), the Cargo Cradle fitted on 07 (RoverKitPiece.CargoRack = 3), and the rubber duck
        // riding in it (RelicState.Cradled = 6), in Core's envelope and Gameplay's upgrades, equipment and relics
        // (version 1) section formats.
        private const string LabSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-10T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":1}," +
            "{\\\"id\\\":\\\"rover.cargo_cradle\\\",\\\"level\\\":1}]}\"}," +
            "{\"key\":\"gameplay.equipment\",\"version\":1,\"json\":\"{\\\"fitted\\\":[3]}\"}," +
            "{\"key\":\"gameplay.relics\",\"version\":1,\"json\":\"{\\\"relics\\\":[" +
            "{\\\"id\\\":\\\"rubber_duck\\\",\\\"state\\\":6}]}\"}]}";

        // The tower at its first level: home awake (no wake opening), the lift still dormant.
        private const string HomeAwakeSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-10T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":1}]}\"}]}";

        // A 0.5.5 save with tower L1 and the lift stuck in Jammed (restored = false), with 07 empty-handed.
        private const string JammedSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.5\",\"savedAtUtc\":\"2026-10-14T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":1}]}\"}," +
            "{\"key\":\"gameplay.lift\",\"version\":1,\"json\":\"{\\\"restored\\\":false}\"}," +
            "{\"key\":\"ui.prompts\",\"version\":1,\"json\":\"{\\\"lookUpHinted\\\":true,\\\"lookTaught\\\":true," +
            "\\\"driveTaught\\\":true}\"}]}";

        private readonly List<string> _problems = new List<string>();
        private readonly StringBuilder _report = new StringBuilder();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private Autopilot _pilot;
        private UIDocument _ui;
        private RenderTexture _target;
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
            if (_ui != null && _ui.panelSettings != null)
            {
                _ui.panelSettings.targetTexture = null;
                _ui.panelSettings.clearColor = false;
            }

            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
            }

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
        public IEnumerator TheBellyLift_IsStitched_RiddenIntoTheLab_TurnsToTheShelf_AndItComesDownFacingOut()
        {
            yield return Boot(LabSave);
            LiftStation lift = _gameplay.Lift;
            _report.AppendLine("# The crew's belly lift and lab (real Main scene)");
            _report.AppendLine();
            _report.AppendLine("| Capture | Phase | 07's height (m) |");
            _report.AppendLine("|---|---|---:|");
            var beats = new int[5];
            var faced = new List<LabStation>();
            bool inside = false;
            using IDisposable sounds = _context.Events.Subscribe<LiftCued>(cued => beats[(int)cued.Cue]++);
            using IDisposable turns = _context.Events.Subscribe<LabStationFaced>(stop => faced.Add(stop.Station));
            using IDisposable rides = _context.Events.Subscribe<LiftRideChanged>(ride => inside = ride.Inside);
            yield return WaitFor(lift, LiftPhase.Called,
                "Home powered and a relic for the lab riding on 07: the jammed lift asks for the stitch.");
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

            yield return WaitUntil(() => faced.Contains(LabStation.Keepsakes) && !lift.Turning, TurnSeconds,
                "The turntable turns to the keepsake shelf that wants the duck");
            Capture("lab-shelf-arrival", lift);

            LabStation[] round = { LabStation.Chart, LabStation.Board, LabStation.Keepsakes };
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

            yield return Tap(_keyboard.dKey);
            yield return WaitUntil(() => lift.Lab.Facing == LabStation.Chart && !lift.Turning, TurnSeconds,
                "Right turns it on round to the chart");
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

            Assert.LessOrEqual(beats[(int)LiftCue.WinchCreaked], 1,
                "The jammed platform creaks at most once as it asks (07 may wake carrying the duck, before this listens).");
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
        [Timeout(600000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator EmptyHanded07_FromAJammedSave_SeesTheJamCue_Stitches_AndRidesIntoTheLab()
        {
            yield return Boot(JammedSave);
            _context.Get<ILocalization>().SetLanguage("vi");
            yield return new WaitForSeconds(SettleSeconds);
            LiftStation lift = _gameplay.Lift;
            _report.AppendLine("# Jammed save (0.5.5) empty-handed lift repair and ride");
            _report.AppendLine();
            _report.AppendLine("| Capture | Phase | Shudder | Sparks | Line |");
            _report.AppendLine("|---|---|---:|---:|---|");

            var beats = new int[5];
            int feelBeats = 0;
            var faced = new List<LabStation>();
            using IDisposable sounds = _context.Events.Subscribe<LiftCued>(cued => beats[(int)cued.Cue]++);
            using IDisposable feels = _context.Events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Lift)
                {
                    feelBeats++;
                }
            });
            using IDisposable turns = _context.Events.Subscribe<LabStationFaced>(stop => faced.Add(stop.Station));

            Vector3 apron = lift.RideSpot - lift.RideHeading * LineUp;
            _pilot.GoTo(apron, BoardRadius, 1f);
            yield return WaitUntil(
                () => beats[(int)LiftCue.WinchCreaked] >= 1 && lift.CurrentShudder > 0.05f && lift.JamSparkCount > 0,
                DriveTimeout, "The jammed lift shudders, creaks, and sparks as 07 approaches");
            _pilot.Target = null;
            yield return new WaitForSeconds(0.18f);
            Assert.Greater(lift.CurrentShudder, 0.05f, "The platform is visibly shuddering during the jam cue.");
            Assert.Greater(lift.JamSparkCount, 0, "The stitch point is visibly sparking during the jam cue.");
            Assert.Greater(feelBeats, 0, "The jam cue emits a FeelBeat.");
            Assert.Less(NextLineOpacity(), 0.05f, "Guidance never asks for the repair before the jam cue is visible.");
            yield return CaptureReview("lift-1-jam-cue", lift);

            yield return DriveTo(apron, BoardRadius, "the apron before the lift");
            yield return WaitUntil(() => lift.Phase == LiftPhase.Called && lift.CurrentShudder <= 0.001f, PhaseTimeout,
                "The jam cue settles and the lift is called without carrying anything");
            yield return WaitUntil(() => NextLineOpacity() > 0.95f, GuidanceTimeout,
                "The stitch guidance line fades in after the jam cue");
            Assert.AreEqual("Hàn lại cáp thang nâng.", NextLineText(),
                "One clear Vietnamese line asks to weld the lift cable.");
            yield return CaptureReview("lift-2-stitch-line", lift);

            yield return DriveTo(lift.StitchSpot, BoardRadius, "the spot by the jammed platform");
            yield return WaitFor(lift, LiftPhase.Stitching, "Resting by the lit platform, empty-handed 07 stitches.");
            yield return WaitFor(lift, LiftPhase.Ready, "The stitch levels the platform.");
            Assert.IsTrue(lift.Capture().restored, "The repaired lift state is saved.");

            yield return WaitUntil(
                () => NextLineOpacity() > 0.95f
                    && NextLineText() == "Đỗ lên bệ thang nâng để lên phòng thí nghiệm.",
                GuidanceTimeout, "The ride guidance line fades in once the lift is repaired");
            Assert.AreEqual("Đỗ lên bệ thang nâng để lên phòng thí nghiệm.", NextLineText(),
                "One clear Vietnamese line invites 07 onto the lift platform.");
            yield return CaptureReview("lift-3-ride-line", lift);

            yield return DriveTo(lift.RideSpot, BoardRadius, "up its ramp onto the platform");
            float bottom = _rover.Position.y;
            yield return WaitFor(lift, LiftPhase.Rising, "Resting on the platform empty-handed, it rises by itself.");
            yield return WaitFor(lift, LiftPhase.Inside, "The platform settles in the lab.");
            Assert.IsTrue(lift.Lab.IsInside, "Empty-handed 07 is inside the lab.");
            float rise = _rover.Position.y - bottom;
            Assert.Greater(rise, LiftRise(lift) - CarriedTolerance, $"07 rode up with the platform ({rise:F2} m).");
            yield return WaitUntil(() => faced.Contains(LabStation.Chart) && !lift.Turning, TurnSeconds,
                "An empty-handed ride arrives facing the survey chart");
            Assert.AreEqual(LabStation.Chart, lift.Lab.Facing, "07 faces the survey chart.");

            Directory.CreateDirectory(ReviewCaptureFolder);
            File.WriteAllText(Path.Combine(ReviewCaptureFolder, "lift-review.md"), _report.ToString());
            Debug.Log("[lift-review] " + _report);
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
                _ui = _ui != null ? _ui : root.GetComponentInChildren<UIDocument>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            Assert.IsNotNull(_ui, "the scene has its UI document");
            _context = bootstrap.Context;
            _target = new RenderTexture(ReviewWidth, ReviewHeight, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);
            _target.Create();
            _ui.panelSettings.targetTexture = _target;
            _ui.panelSettings.clearColor = true;
            _ui.panelSettings.colorClearValue = Color.clear;
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

        /// <summary>The game camera's 1920x1080 frame with the UI (premultiplied) laid over it.</summary>
        private IEnumerator CaptureReview(string name, LiftStation lift)
        {
            yield return null;
            Directory.CreateDirectory(ReviewCaptureFolder);
            Texture2D world = FrameCapture.Render(_context.Get<IViewCamera>().Camera, ReviewWidth, ReviewHeight);
            var ui = new Texture2D(ReviewWidth, ReviewHeight, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            ui.ReadPixels(new Rect(0, 0, ReviewWidth, ReviewHeight), 0, 0, false);
            ui.Apply(false);
            RenderTexture.active = previous;
            try
            {
                Color32[] background = world.GetPixels32();
                Color32[] overlay = ui.GetPixels32();
                for (int i = 0; i < background.Length; i++)
                {
                    Color32 over = overlay[i];
                    Color32 under = background[i];
                    int keep = 255 - over.a;
                    background[i] = new Color32((byte)Mathf.Min(255, over.r + under.r * keep / 255),
                        (byte)Mathf.Min(255, over.g + under.g * keep / 255),
                        (byte)Mathf.Min(255, over.b + under.b * keep / 255), 255);
                }

                world.SetPixels32(background);
                world.Apply(false);
                FrameCapture.WritePng(world, Path.Combine(ReviewCaptureFolder, name + ".png"));
                _report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1} | {2:F2} | {3} | {4} |",
                    name, lift.Phase, lift.CurrentShudder, lift.JamSparkCount, NextLineText()));
            }
            finally
            {
                Object.Destroy(world);
                Object.Destroy(ui);
            }
        }

        private float NextLineOpacity()
        {
            VisualElement line = _ui?.rootVisualElement?.Q<VisualElement>("next-line");
            if (line == null || line.style.display == DisplayStyle.None)
            {
                return 0f;
            }

            return line.style.opacity.keyword == StyleKeyword.Null
                ? line.style.opacity.value
                : line.resolvedStyle.opacity;
        }

        private string NextLineText()
        {
            Label label = _ui?.rootVisualElement?.Q<Label>("next-line-text");
            return label?.text ?? string.Empty;
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

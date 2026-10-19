using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07's wake in the real Main scene, with the title on (docs/design/opening-v2.md, docs/features/M3-09 Part A):
    /// nothing wakes on the title; New Game fades the title to black and plays the full opening once, untouched within
    /// 45 s, as one still shot of 07's face (no cut, orbit or body turn) with the self-test printing on the awake face;
    /// Continue over a real save plays the short wake; the opening's frames allocate nothing. Frames of both title
    /// shots, the fade, each stage and each check go to Logs/captures/opening/*.png with opening.md.
    /// Slow and needs a GPU: run on demand with --category OpeningSession.
    /// </summary>
    [Category("OpeningSession")]
    public sealed class OpeningSessionTests : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const int KeyFrameWidth = 1920;
        private const int KeyFrameHeight = 1080;
        private const float LanguageSettleSeconds = 1.5f;

        /// <summary>The title CRT's switch-off (about 0.6 s) plus the fade to black leave it gone within this (s).</summary>
        private const float TitleOffTimeout = 1.4f;

        /// <summary>The longest frame (ms) the visible part of a transition may have; the reload's black may exceed it.</summary>
        private const float MaxVisibleFrameMs = 50f;

        /// <summary>Frames after the reloaded scene is rebound that are still black (its first frames' start-up cost).</summary>
        private const int BlackFramesAfterReload = 4;
        private const int TransitionWidth = 640;
        private const int TransitionHeight = 360;
        private const float BootTimeout = 30f;
        private const int BootCheckStepCount = (int)BootCheckStep.Begin + 1;
        private const float TextCheckEvery = 0.5f;
        private const float TitleQuietSeconds = 4f;
        private const float MaxUntouched = 45f;
        private const float MinSelfTest = 13f;
        private const float MaxSelfTest = 16.5f;

        /// <summary>The opening's lines, as the shipped tuning prints them (the other checks are left out for pace).</summary>
        private static readonly BootCheckStep[] OpeningLines =
        {
            BootCheckStep.Carrier, BootCheckStep.Resume, BootCheckStep.Agenda, BootCheckStep.Lamps,
            BootCheckStep.Cutter, BootCheckStep.Battery, BootCheckStep.Begin,
        };

        /// <summary>The shot may turn this little (deg) and stray this little (m) off its push-in line.</summary>
        private const float ShotAngleSlack = 0.05f;
        private const float ShotLineSlack = 0.01f;

        /// <summary>The inventory's close-up: 07's face screen fills about this share of the frame's height.</summary>
        private const float MinFaceFill = 0.5f;
        private const float MaxFaceFill = 0.85f;

        /// <summary>07's screen may face this far (deg) off the camera during a check line, its head check included.</summary>
        private const float MaxFacingDegrees = 25f;

        /// <summary>The camera may stray this little (m) while it holds, and a move back up or miss its end this much.</summary>
        private const float HoldSlack = 0.02f;
        private const float MoveSlack = 0.01f;
        private const float EndSlack = 0.08f;
        private const float EndTurnSlack = 2f;

        /// <summary>Turn speed is measured over windows this long (s): per-frame angles are float noise at batch rates.</summary>
        private const float TurnWindow = 0.05f;

        /// <summary>No camera move turns faster than this (deg/s): calm, never a whip.</summary>
        private const float MaxTurnSpeed = 150f;

        /// <summary>07's body may yaw this little (deg) through the whole wake: it never turns.</summary>
        private const float BodyYawSlack = 0.5f;
        private const float MaxHeld = 1.5f;
        private const float MaxShortWake = 3f;
        private const float RunTimeout = 70f;
        private const float DriveStick = 0.8f;
        private const float AfterChaseSeconds = 3f;
        private const float TitleSettleSeconds = 4f;
        private const float StripInterval = 2f;

        // Zero GC: extra director updates per measured frame, frames measured and the counter's slack.
        private const int MeasuredCalls = 40;
        private const int MeasuredFrames = 3;
        private const int BaselineFrames = 5;
        private const long Tolerance = 2048L;
        private const string AllocatedInFrame = "GC Allocated In Frame";

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "opening"));

        private readonly List<WakeBeat> _beats = new List<WakeBeat>(32);
        private readonly List<float> _beatTimes = new List<float>(32);
        private readonly StringBuilder _report = new StringBuilder();
        private GameContext _context;
        private OpeningDirector _director;
        private RoverCameraRig _rig;
        private RoverController _rover;
        private RoverBodyLanguage _body;
        private Camera _view;
        private Gamepad _gamepad;
        private IDisposable[] _subscriptions;
        private int _awoke;
        private readonly List<BootCheckStep> _checksStartedList = new List<BootCheckStep>();
        private readonly Dictionary<BootCheckStep, BootCheckResult> _checksCompleted = new Dictionary<BootCheckStep, BootCheckResult>();
        private int _checksFinished;
#if UNITY_EDITOR
        private bool _skippedTitle;
#endif

        [OneTimeSetUp]
        public void StartReport()
        {
            Directory.CreateDirectory(CaptureFolder);
            File.Delete(Path.Combine(CaptureFolder, "opening.md"));
        }

        [TearDown]
        public void CleanUp()
        {
            Time.captureDeltaTime = 0f;
            // The booted Main stays loaded after the test; a leftover world doubles the next fixture's props.
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            Dispose();
            _gamepad = null;
#if UNITY_EDITOR
            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, _skippedTitle);
#endif
            BootstrapHarness.DeleteSaveFiles(RoverSessionScene.SaveSlot);
            if (_report.Length > 0)
            {
                Directory.CreateDirectory(CaptureFolder);
                File.AppendAllText(Path.Combine(CaptureFolder, "opening.md"), _report.ToString());
                _report.Clear();
            }
        }

        /// <summary>
        /// The boot text lives on 07's face (and the HUD), never in the world: a world-space text or canvas is drawn
        /// huge from any free or overhead camera.
        /// </summary>
        private static void AssertNoWorldSpaceText()
        {
            foreach (TextMesh text in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                Assert.Fail($"World-space TextMesh '{text.name}' ('{text.text}') is in the scene.");
            }

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include,
                FindObjectsSortMode.None))
            {
                Assert.AreNotEqual(RenderMode.WorldSpace, canvas.renderMode,
                    $"World-space canvas '{canvas.name}' is in the scene.");
            }
        }

        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator FreshTitle_HoldsTheWake_ThenNewGamePlaysTheFullOpeningOnce()
        {
            yield return Boot(true);
            _report.AppendLine("# The opening (real Main scene, title on, fresh game)").AppendLine();
            Assert.AreEqual(GameFlowState.Title, _context.Get<IGameFlow>().State, "a fresh install boots into the title");
            yield return Wait(TitleQuietSeconds);
            Assert.AreEqual(0, _awoke, "nothing wakes while the title is up");
            Assert.AreEqual(0, _beats.Count, "no wake beat on the title");
            Assert.AreEqual(WakePhase.Asleep, _body.Mood.Wake.Phase, "07 sleeps under the title");
            Assert.IsTrue(_rig.Opening.IsActive, "the fresh title holds the wake shot");
            Assert.IsTrue(_body.FaceDirector.Director.IsHoldingDarkForOpening, "the CRT face is held dark under the title");
            Capture("title_fresh");

            _context.Events.Publish(new NewGameRequested());
            var shots = new (OpeningPhase Phase, float Share, string Name)[]
            {
                (OpeningPhase.FadeOut, 0.6f, "fade_out"),
                (OpeningPhase.Dark, 0.5f, "stage_1_dark"),
                (OpeningPhase.Post, 0.03f, "stage_2_picture_opens_on_the_face"),
                (OpeningPhase.Post, 0.12f, "stage_3_lines_on_the_face"),
                (OpeningPhase.Ready, 0.3f, "end_1_ready_readout"),
                (OpeningPhase.Ready, 0.95f, "end_2_eyes_back"),
                (OpeningPhase.PullBack, 0.5f, "end_3_pull_back"),
                (OpeningPhase.Perform, 0.05f, "end_4_double_blink"),
                (OpeningPhase.Perform, 0.32f, "end_5_lamps_off"),
                (OpeningPhase.Perform, 0.58f, "end_6_dust_settles"),
                (OpeningPhase.Perform, 0.95f, "end_7_glance"),
                (OpeningPhase.MoveBehind, 0.5f, "end_8_move_behind"),
                (OpeningPhase.MoveBehind, 0.98f, "end_9_chase_frame"),
            };
            var keyShots = new (OpeningPhase Phase, float Share, string Name)[]
            {
                (OpeningPhase.Post, 0.12f, "key_01_first_lines_on_face"),
                (OpeningPhase.Post, 0.5f, "key_02_lines_on_face"),
                (OpeningPhase.Perform, 0.58f, "key_03_ready_performance"),
                (OpeningPhase.MoveBehind, 0.98f, "key_04_chase_frame"),
            };
            var taken = new bool[shots.Length];
            var keyTaken = new bool[keyShots.Length];
            int nextStripIndex = 0;
            var postTaken = new bool[BootCheckStepCount];
            float crackleStarted = -1f;
            bool firstLineSeen = false;
            float nextTextCheck = 0f;
            float started = Time.time;
            while (!_beats.Contains(WakeBeat.ChaseFrame))
            {
                Assert.Less(Time.time - started, RunTimeout, "the opening reaches the chase frame");
                OpeningScript script = _director.Script;
                if (crackleStarted < 0f && _beats.Contains(WakeBeat.Crackle))
                {
                    crackleStarted = Time.time;
                }

                if (Time.time >= nextTextCheck)
                {
                    AssertNoWorldSpaceText();
                    nextTextCheck = Time.time + TextCheckEvery;
                }

                FaceDirector face = _body.FaceDirector.Director;
                if (!_beats.Contains(WakeBeat.EyesOpen))
                {
                    Assert.IsTrue(face.IsHoldingDarkForOpening, "no eyes on the face before the checks are done");
                    Assert.AreEqual(0, face.BootCount, "the face does not boot before its eyes come back");
                }

                if (_checksStartedList.Count > 0 && !firstLineSeen)
                {
                    firstLineSeen = true;
                    Assert.IsTrue(_beats.Contains(WakeBeat.ChecksBegin), "the first line comes as the picture opens");
                    Assert.AreEqual(0, face.BootCount, "straight into the lines: no eyes-open stage before them");
                }

                if (script.Phase == OpeningPhase.Post && script.PostStep >= BootCheckStep.Lens)
                {
                    Assert.IsTrue(face.State.IsReadout, $"the lines print on the face ({script.PostStep})");
                }

                if (script.Phase == OpeningPhase.Post && script.PostProgress01 >= 0.6f
                    && !postTaken[(int)script.PostStep])
                {
                    postTaken[(int)script.PostStep] = true;
                    Capture("check_" + script.PostStep.ToString().ToLowerInvariant());
                }

                for (int i = 0; i < shots.Length; i++)
                {
                    if (!taken[i] && PassedShot(script, shots[i].Phase, shots[i].Share))
                    {
                        taken[i] = true;
                        Capture(shots[i].Name);
                    }
                }

                for (int i = 0; i < keyShots.Length; i++)
                {
                    if (!keyTaken[i] && PassedShot(script, keyShots[i].Phase, keyShots[i].Share))
                    {
                        keyTaken[i] = true;
                        CaptureKeyFrame(keyShots[i].Name);
                    }
                }

                if (crackleStarted >= 0f && Time.time - crackleStarted >= nextStripIndex * StripInterval)
                {
                    Capture(string.Format(CultureInfo.InvariantCulture, "strip_{0:00}s", nextStripIndex * 2));
                    nextStripIndex++;
                }

                yield return null;
            }

            for (int i = 0; i < shots.Length; i++)
            {
                if (!taken[i])
                {
                    taken[i] = true;
                    Capture(shots[i].Name);
                }
            }

            for (int i = 0; i < keyShots.Length; i++)
            {
                if (!keyTaken[i])
                {
                    keyTaken[i] = true;
                    CaptureKeyFrame(keyShots[i].Name);
                }
            }

            if (crackleStarted >= 0f && nextStripIndex <= 10)
            {
                Capture(string.Format(CultureInfo.InvariantCulture, "strip_{0:00}s", nextStripIndex * 2));
            }

            yield return Wait(AfterChaseSeconds);
            Capture("o14_first_prompt_frame");

            float crackle = TimeOf(WakeBeat.Crackle);
            float chase = TimeOf(WakeBeat.ChaseFrame);
            WriteBeats();
            _report.AppendLine().AppendLine(Line("untouched opening, O1 to the chase frame (s)", chase - crackle));
            Assert.AreEqual(1, Count(WakeBeat.Crackle), "the full opening plays once");
            Assert.AreEqual(1, Count(WakeBeat.FadedOut), "New Game faded the picture to black");
            Assert.AreEqual(1, _awoke, "RoverAwoke goes with the crackle, once");
            Assert.AreEqual(1, _body.FaceDirector.Director.BootCount, "the CRT face boots once, as the eyes come back");
            Assert.That(chase - crackle, Is.InRange(MinSelfTest, MaxSelfTest), "the wake lasts about 15 s: its two moves are slow and gradual");
            CollectionAssert.AreEqual(OpeningLines, _checksStartedList, "the lines print in the story's order");
            Assert.AreEqual(OpeningLines.Length, _checksCompleted.Count, "every line is completed");
            Assert.AreEqual(1, _checksFinished, "and the test finishes once");
            Assert.AreEqual(BootCheckResult.Fault, _checksCompleted[BootCheckStep.Cutter], "the missing igniter");
            for (int i = 0; i < shots.Length; i++)
            {
                Assert.IsTrue(taken[i], $"{shots[i].Name} was captured");
            }

            for (int i = 0; i < keyShots.Length; i++)
            {
                Assert.IsTrue(keyTaken[i], $"{keyShots[i].Name} was captured");
            }

            Assert.IsFalse(_rover.IsHeldStill, "control is the player's after the opening");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ContinueOverASave_PlaysTheShortWake()
        {
            yield return Boot(false);
            Assert.IsFalse(_context.Get<IGameFlow>().ShowsTitle, "the first boot has no title");
            Assert.AreEqual(0, _beats.Count, "without a title 07 wakes on its own: no opening, no short wake");
            Assert.IsTrue(_context.Get<ISaveService>().SaveNow(), "a save is on disk");
            Dispose();

            yield return Boot(true);
            _report.AppendLine("# Continue (real Main scene, title over a save)").AppendLine();
            IGameFlow flow = _context.Get<IGameFlow>();
            Assert.AreEqual(GameFlowState.Title, flow.State);
            Assert.IsTrue(flow.HasSave, "the title offers Continue");
            yield return Wait(TitleSettleSeconds);
            Assert.AreEqual(0, _awoke, "nothing wakes while the title is up");
            Capture("title_save");

            _context.Events.Publish(new ContinueRequested());
            float started = Time.time;
            float held = 0f;
            bool midCaptured = false;
            while (!_beats.Contains(WakeBeat.ChaseFrame))
            {
                Assert.Less(Time.time - started, MaxShortWake + 1f, "the short wake reaches the chase frame");
                if (_rover.IsHeldStill)
                {
                    held = Time.time - started;
                }

                if (!midCaptured && Time.time - started >= 0.8f)
                {
                    midCaptured = true;
                    Capture("short_wake_lid");
                }

                yield return null;
            }

            float chase = Time.time - started;
            yield return Wait(1f);
            Capture("short_wake_glance");
            WriteBeats();
            _report.AppendLine().AppendLine(Line("Continue to the chase frame (s)", chase));
            Assert.AreEqual(WakeBeat.Stirred, _beats[0], "Continue stirs 07");
            Assert.AreEqual(0, Count(WakeBeat.Crackle), "Continue never plays the opening");
            Assert.LessOrEqual(chase, MaxShortWake, "control returns within 3 s");
            Assert.LessOrEqual(held, MaxHeld + 0.1f, "control is never held more than ~1.5 s");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator OpeningFrames_AllocateNothing()
        {
            yield return Boot(true);
            _context.Events.Publish(new NewGameRequested());
            while (!_beats.Contains(WakeBeat.Crackle))
            {
                yield return null;
            }

            yield return Wait(1f);
            MethodInfo update = typeof(OpeningDirector).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(update, "OpeningDirector.Update");
            var frame = (Action)Delegate.CreateDelegate(typeof(Action), _director, update);
            frame();
            using (ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, AllocatedInFrame))
            {
                yield return null;
                long noise = 0L;
                for (int i = 0; i < BaselineFrames; i++)
                {
                    yield return null;
                    noise = Math.Max(noise, recorder.LastValue);
                }

                long quietest = long.MaxValue;
                for (int i = 0; i < MeasuredFrames; i++)
                {
                    for (int call = 0; call < MeasuredCalls; call++)
                    {
                        frame();
                    }

                    yield return null;
                    quietest = Math.Min(quietest, recorder.LastValue);
                }

                _report.AppendLine("# Zero GC").AppendLine()
                    .AppendLine($"plain frames <= {noise} B; quietest frame with {MeasuredCalls} extra opening updates: "
                        + $"{quietest} B").AppendLine();
                Assert.LessOrEqual(quietest, noise + Tolerance,
                    $"{MeasuredCalls} extra opening updates allocated {quietest} bytes (plain frames: {noise}).");
            }
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheShotPlan_TheFaceCloseUpHoldsAndFacesThe07Screen_ThenTwoSmoothMoves_AndSteeringNeverTurns07()
        {
            yield return Boot(true);
            _context.Events.Publish(new NewGameRequested());
            float started = Time.time;
            while (_director.Script.Phase != OpeningPhase.Post)
            {
                Assert.Less(Time.time - started, RunTimeout, "the picture opens on the face");
                yield return null;
            }

            // A player steering hard while the lines print (the owner's player build: 07 turned in place under the shot).
            Set(_gamepad.leftStick, new Vector2(1f, 0f));

            // The camera cut to the close-up in the black; from the picture opening on it holds still.
            yield return null;
            Vector3 startPos = _view.transform.position;
            Quaternion startRot = _view.transform.rotation;
            float startFov = _view.fieldOfView;
            float bodyYaw = _rover.transform.eulerAngles.y;
            Vector3 viewLine = startRot * Vector3.forward;
            float maxAngle = 0f;
            float maxOffLine = 0f;
            float maxAlong = 0f;
            float maxBodyYaw = 0f;
            float maxFovChange = 0f;
            float faceFill = 0f;
            float maxFacing = 0f;
            bool headHidden = false;
            Renderer screen = _body.FaceDirector.Display.ScreenRenderer;
            // The rendered view trails the script by a frame, so each pose was reached over the previous frame's
            // time: its Seconds is that frame's delta, or a long frame would inflate the turn speed measured over a
            // window.
            float renderedOver = Time.deltaTime;
            var poses = new List<(OpeningPhase Phase, Vector3 Position, Quaternion Rotation, float Seconds)>(4096);
            while (!_beats.Contains(WakeBeat.ChaseFrame))
            {
                Assert.Less(Time.time - started, RunTimeout, "the opening reaches the chase frame");
                OpeningScript script = _director.Script;
                maxBodyYaw = Mathf.Max(maxBodyYaw, Mathf.Abs(Mathf.DeltaAngle(bodyYaw, _rover.transform.eulerAngles.y)));
                poses.Add((script.Phase, _view.transform.position, _view.transform.rotation, renderedOver));
                renderedOver = Time.deltaTime;
                headHidden |= script.ShotLive && screen.shadowCastingMode == UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                bool still = script.Phase == OpeningPhase.Post;
                if (script.Phase == OpeningPhase.Ready)
                {
                    Set(_gamepad.leftStick, Vector2.zero);
                }
                if (still)
                {
                    Vector3 offset = _view.transform.position - startPos;
                    float along = Vector3.Dot(offset, viewLine);
                    maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(startRot, _view.transform.rotation));
                    maxOffLine = Mathf.Max(maxOffLine, (offset - along * viewLine).magnitude);
                    maxAlong = Mathf.Max(maxAlong, along);
                    maxFovChange = Mathf.Max(maxFovChange, Mathf.Abs(_view.fieldOfView - startFov));
                    if (script.Phase == OpeningPhase.Post)
                    {
                        Transform face = _body.ScreenTransform;
                        float facing = Vector3.Angle(face.forward, _view.transform.position - face.position);
                        maxFacing = Mathf.Max(maxFacing, facing);
                        Bounds bounds = screen.bounds;
                        Vector3 top = _view.WorldToViewportPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
                        Vector3 bottom = _view.WorldToViewportPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                        faceFill = top.y - bottom.y;
                    }
                }

                yield return null;
            }

            // The rendered view trails the script by a frame: let the chase frame settle, then read it.
            yield return null;
            yield return null;
            Vector3 chasePos = _view.transform.position;
            Quaternion chaseRot = _view.transform.rotation;

            (Vector3 Position, Quaternion Rotation) wide = Last(poses, OpeningPhase.Perform);
            (Vector3 Position, Quaternion Rotation) close = Last(poses, OpeningPhase.Ready);
            float readyDrift = MaxDrift(poses, OpeningPhase.Ready, close);
            float performDrift = MaxDrift(poses, OpeningPhase.Perform, wide);
            MoveStats pull = Move(poses, OpeningPhase.PullBack, wide.Position);
            MoveStats behind = Move(poses, OpeningPhase.MoveBehind, chasePos);
            float pullEnd = Vector3.Distance(pull.LastPosition, wide.Position);
            float behindEnd = Vector3.Distance(behind.LastPosition, chasePos);
            float behindEndTurn = Quaternion.Angle(behind.LastRotation, chaseRot);

            _report.AppendLine("# The shot plan").AppendLine()
                .AppendLine(Line("still shot turn, max (deg)", maxAngle))
                .AppendLine(Line("still shot off its push-in line, max (m)", maxOffLine))
                .AppendLine(Line("close-up travel during the checks (m)", maxAlong))
                .AppendLine(Line("face screen off the camera during the checks, max (deg)", maxFacing))
                .AppendLine(Line("face screen's share of the frame height at the end of the checks", faceFill))
                .AppendLine(Line("lens change during the checks, max (deg)", maxFovChange))
                .AppendLine(Line("ready hold drift, max (m)", readyDrift))
                .AppendLine(Line("pull-back: frames", pull.Frames))
                .AppendLine(Line("pull-back: seconds", pull.Seconds))
                .AppendLine(Line("pull-back: backing up, max (m)", pull.MaxBackUp))
                .AppendLine(Line("pull-back: turn speed, max (deg/s)", pull.MaxTurnSpeed))
                .AppendLine(Line("pull-back: end off the wide shot (m)", pullEnd))
                .AppendLine(Line("performance drift, max (m)", performDrift))
                .AppendLine(Line("move behind: frames", behind.Frames))
                .AppendLine(Line("move behind: seconds", behind.Seconds))
                .AppendLine(Line("move behind: backing up, max (m)", behind.MaxBackUp))
                .AppendLine(Line("move behind: turn speed, max (deg/s)", behind.MaxTurnSpeed))
                .AppendLine(Line("move behind: end off the chase frame (m)", behindEnd))
                .AppendLine(Line("move behind: end turn off the chase frame (deg)", behindEndTurn))
                .AppendLine(Line("body yaw change, max (deg)", maxBodyYaw)).AppendLine();

            Assert.LessOrEqual(maxAngle, ShotAngleSlack, "the close-up never pans, tilts or orbits");
            Assert.LessOrEqual(maxOffLine, ShotLineSlack, "the close-up never slides");
            Assert.LessOrEqual(maxAlong, ShotLineSlack, "the close-up never pushes in or out");
            Assert.LessOrEqual(maxFacing, MaxFacingDegrees, "07's screen faces the camera through every check line");
            Assert.That(faceFill, Is.InRange(MinFaceFill, MaxFaceFill), "the face fills enough of the frame to read");
            Assert.LessOrEqual(maxFovChange, 1e-3f, "no zoom");
            Assert.LessOrEqual(readyDrift, HoldSlack, "the camera holds while the readout gives way to the eyes");
            Assert.LessOrEqual(performDrift, HoldSlack, "the camera holds still for the ready performance");
            Assert.LessOrEqual(pull.MaxBackUp, MoveSlack, "the pull-back never backs up");
            Assert.LessOrEqual(behind.MaxBackUp, MoveSlack, "the move behind never backs up or swings past");
            Assert.LessOrEqual(pull.MaxTurnSpeed, MaxTurnSpeed, "the pull-back turns gently");
            Assert.LessOrEqual(behind.MaxTurnSpeed, MaxTurnSpeed, "the move behind turns gently");
            Assert.LessOrEqual(pullEnd, EndSlack, "the pull-back ends on the wide shot");
            Assert.LessOrEqual(behindEnd, EndSlack, "the move behind ends on the chase frame, no pop");
            Assert.LessOrEqual(behindEndTurn, EndTurnSlack, "and facing as the chase frame does");
            Assert.LessOrEqual(maxBodyYaw, BodyYawSlack, "07's body never turns during the wake, even steered");
            Assert.IsFalse(headHidden, "07's head is never hidden from the wake's camera (the chase rig's near fade)");
            Vector3 chaseOffset = chasePos - _rover.transform.position;
            Assert.Less(Vector3.Dot(chaseOffset, _rover.transform.forward), 0f, "control starts on the chase frame behind 07");
        }

        /// <summary>One eased camera move, measured frame by frame toward where it must end.</summary>
        private readonly struct MoveStats
        {
            public MoveStats(float maxBackUp, float maxTurnSpeed, Vector3 lastPosition, Quaternion lastRotation,
                int frames, float seconds)
            {
                Frames = frames;
                Seconds = seconds;
                MaxBackUp = maxBackUp;
                MaxTurnSpeed = maxTurnSpeed;
                LastPosition = lastPosition;
                LastRotation = lastRotation;
            }

            public int Frames { get; }

            public float Seconds { get; }

            public float MaxBackUp { get; }

            public float MaxTurnSpeed { get; }

            public Vector3 LastPosition { get; }

            public Quaternion LastRotation { get; }
        }

        private static MoveStats Move(List<(OpeningPhase Phase, Vector3 Position, Quaternion Rotation, float Seconds)> poses,
            OpeningPhase phase, Vector3 end)
        {
            float backUp = 0f;
            float turn = 0f;
            float lastLeft = float.MaxValue;
            Vector3 lastPosition = Vector3.zero;
            Quaternion lastRotation = Quaternion.identity;
            bool any = false;
            int frames = 0;
            float seconds = 0f;
            float window = 0f;
            Quaternion windowStart = Quaternion.identity;
            for (int i = 1; i < poses.Count; i++)
            {
                if (poses[i].Phase != phase)
                {
                    continue;
                }

                float left = Vector3.Distance(poses[i].Position, end);
                if (any)
                {
                    backUp = Mathf.Max(backUp, left - lastLeft);
                    window += poses[i].Seconds;
                    if (window >= TurnWindow)
                    {
                        turn = Mathf.Max(turn, Quaternion.Angle(windowStart, poses[i].Rotation) / window);
                        window = 0f;
                        windowStart = poses[i].Rotation;
                    }
                }
                else
                {
                    windowStart = poses[i].Rotation;
                }

                any = true;
                frames++;
                seconds += poses[i].Seconds;
                lastLeft = left;
                lastPosition = poses[i].Position;
                lastRotation = poses[i].Rotation;
            }

            Assert.IsTrue(any, $"the {phase} move was seen");
            return new MoveStats(backUp, turn, lastPosition, lastRotation, frames, seconds);
        }

        private static (Vector3 Position, Quaternion Rotation) Last(
            List<(OpeningPhase Phase, Vector3 Position, Quaternion Rotation, float Seconds)> poses, OpeningPhase phase)
        {
            for (int i = poses.Count - 1; i >= 0; i--)
            {
                if (poses[i].Phase == phase)
                {
                    return (poses[i].Position, poses[i].Rotation);
                }
            }

            Assert.Fail($"{phase} was never seen");
            return default;
        }

        /// <summary>
        /// How far the camera strays from <paramref name="held"/> through <paramref name="phase"/>, skipping its first
        /// frame (the rendered view trails the script by one).
        /// </summary>
        private static float MaxDrift(List<(OpeningPhase Phase, Vector3 Position, Quaternion Rotation, float Seconds)> poses,
            OpeningPhase phase, (Vector3 Position, Quaternion Rotation) held)
        {
            float drift = 0f;
            bool first = true;
            for (int i = 0; i < poses.Count; i++)
            {
                if (poses[i].Phase != phase)
                {
                    first = true;
                    continue;
                }

                if (!first)
                {
                    drift = Mathf.Max(drift, Vector3.Distance(poses[i].Position, held.Position));
                }

                first = false;
            }

            return drift;
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator NewGameOverASave_FadesTitle_AndReloadsInsideTheBlack()
        {
            yield return Boot(false);
            Assert.IsTrue(_context.Get<ISaveService>().SaveNow(), "a save is on disk");
            Dispose();

            yield return Boot(true);
            IGameFlow flow = _context.Get<IGameFlow>();
            Assert.AreEqual(GameFlowState.Title, flow.State);
            Assert.IsTrue(flow.HasSave, "the title offers Continue over a save");
            yield return Wait(1f);

            GameBootstrap bootstrap = null;
            UIDocument document = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(RoverSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                document = document != null ? document : root.GetComponentInChildren<UIDocument>(true);
            }

            Assert.IsNotNull(bootstrap, "the scene has its GameBootstrap");
            Assert.IsNotNull(document, "the scene has its UIDocument");
            Button newGameButton = document.rootVisualElement.Q<Button>("button-new-game");
            Assert.IsNotNull(newGameButton);
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = newGameButton;
                newGameButton.SendEvent(submit);
            }

            yield return Wait(0.4f);
            Button confirmButton = document.rootVisualElement.Q<Button>("button-new-game-confirm");
            Assert.IsNotNull(confirmButton);
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = confirmButton;
                confirmButton.SendEvent(submit);
            }

            yield return null;
            Assert.AreEqual(OpeningPhase.FadeOut, _director.Script.Phase,
                "confirming New Game over a save immediately begins the fade to black");
            Assert.IsTrue(bootstrap.IsReloadPrewarmed,
                "GameBootstrap pre-warms SceneManager/EditorSceneManager async reload during the fade");

            float maxFadeFrameMs = 0f;
            float elapsed = 0f;
            VisualElement titleCrt = document.rootVisualElement.Q<VisualElement>("title-crt");
            Assert.IsNotNull(titleCrt);
            while (IsShown(titleCrt) && elapsed < TitleOffTimeout)
            {
                yield return null;
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                maxFadeFrameMs = Mathf.Max(maxFadeFrameMs, dt * 1000f);
            }

            Assert.IsFalse(IsShown(titleCrt), "the title CRT has switched off by the time O1's black begins");

            float reloadDeadline = Time.realtimeSinceStartup + BootTimeout;
            while (_director != null && Time.realtimeSinceStartup < reloadDeadline)
            {
                yield return null;
                if (_director != null && _director.Script.Phase == OpeningPhase.FadeOut)
                {
                    maxFadeFrameMs = Mathf.Max(maxFadeFrameMs, Time.unscaledDeltaTime * 1000f);
                }
            }

            float activationStart = Time.realtimeSinceStartup;
            Assert.IsTrue(_director == null, "the scene reloads at WakeBeat.FadedOut inside O1's black");
            Dispose();

            while (Time.realtimeSinceStartup < reloadDeadline)
            {
                Scene reloaded = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
                if (reloaded.IsValid() && reloaded.isLoaded)
                {
                    foreach (GameObject root in reloaded.GetRootGameObjects())
                    {
                        _director = _director != null ? _director : root.GetComponentInChildren<OpeningDirector>(true);
                    }

                    if (_director != null && _director.Script != null && _director.Script.IsRunning)
                    {
                        break;
                    }
                }

                yield return null;
            }

            float activationMs = (Time.realtimeSinceStartup - activationStart) * 1000f;
            _report.AppendLine("# New Game over a save (pre-warmed async reload)").AppendLine()
                .AppendLine(Line("max frame time during the fade (ms)", maxFadeFrameMs))
                .AppendLine(Line("pre-warmed reload activation inside O1 black (ms)", activationMs)).AppendLine();
            File.WriteAllText(Path.Combine(CaptureFolder, "opening.md"), _report.ToString());

            Assert.IsNotNull(_director, "reloaded scene has OpeningDirector");
            Assert.IsTrue(_director.Script.IsRunning,
                "reloaded scene starts the opening inside O1 without showing the title again");
            Assert.AreNotEqual(OpeningPhase.FadeOut, _director.Script.Phase,
                "the fade already happened before the reload");
        }

        /// <summary>
        /// The script is at or past <paramref name="share"/> of <paramref name="phase"/>: in it, or already in a later
        /// phase (a long editor frame can step over a whole phase's window, which used to miss the shot).
        /// </summary>
        private static bool PassedShot(OpeningScript script, OpeningPhase phase, float share)
        {
            if (script.Phase == phase)
            {
                return script.PhaseTime >= share * script.PhaseLength;
            }

            return script.Phase != OpeningPhase.Idle && script.Phase > phase;
        }

        /// <summary>A hidden title element leaves layout (display none) with its opacity as it was.</summary>
        private static bool IsShown(VisualElement element)
        {
            return element.resolvedStyle.display != DisplayStyle.None && element.resolvedStyle.opacity > 0.05f;
        }

        [UnityTest]
        [Explicit("Frame-by-frame capture of the title-to-play transition; run with --category OpeningTransition.")]
        [Category("OpeningTransition")]
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TransitionFrames_NewGameFromAFreshTitle()
        {
            yield return Boot(true);
            yield return Wait(TitleQuietSeconds);
            yield return RecordTransition("new_game", "button-new-game");
        }

        [UnityTest]
        [Explicit("Frame-by-frame capture of the title-to-play transition; run with --category OpeningTransition.")]
        [Category("OpeningTransition")]
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TransitionFrames_ContinueOverASave()
        {
            yield return Boot(false);
            Assert.IsTrue(_context.Get<ISaveService>().SaveNow(), "a save is on disk");
            Dispose();
            yield return Boot(true);
            yield return Wait(TitleQuietSeconds);
            yield return RecordTransition("continue", "button-resume");
        }

        [UnityTest]
        [Explicit("Frame-by-frame capture of the title-to-play transition; run with --category OpeningTransition.")]
        [Category("OpeningTransition")]
        [Timeout(900000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TransitionFrames_NewGameOverASave_ThroughTheReload()
        {
            yield return RecordReloadTransition("new_game_over_save", true);
        }

        [UnityTest]
        [Explicit("Real frame time of the title-to-play transition over a save, without capture overhead; run with --category OpeningTransition.")]
        [Category("OpeningTransition")]
        [Timeout(900000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TransitionTiming_NewGameOverASave_ThroughTheReload()
        {
            yield return RecordReloadTransition("timing_over_save", false);
        }

        /// <summary>
        /// New Game over a save, through the scene reload. With <paramref name="render"/> every frame is rendered and
        /// saved (the capture's own cost shows in the frame times); without it only the real frame time, the phase and
        /// the camera are logged.
        /// </summary>
        private IEnumerator RecordReloadTransition(string tag, bool render)
        {
            yield return Boot(false);
            Assert.IsTrue(_context.Get<ISaveService>().SaveNow(), "a save is on disk");
            Dispose();
            yield return Boot(true);
            yield return Wait(TitleQuietSeconds);
            string folder = Path.Combine(CaptureFolder, "transition_" + tag);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
            UIDocument document = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(RoverSessionScene.ScenePath).GetRootGameObjects())
            {
                document = document != null ? document : root.GetComponentInChildren<UIDocument>(true);
            }

            Assert.IsNotNull(document, "the scene has its UIDocument");
            yield return Submit(document.rootVisualElement.Q<Button>("button-new-game"));
            yield return Wait(0.8f);
            yield return Submit(document.rootVisualElement.Q<Button>("button-new-game-confirm"));
            Time.captureDeltaTime = 1f / 60f;
            var csv = new StringBuilder();
            csv.AppendLine("frame,gameTime,realDtMs,phase,luma,camDistance,camTurnDeg,note");
            float started = Time.time;
            float lastReal = Time.realtimeSinceStartup;
            Vector3 previousPosition = Vector3.zero;
            Quaternion previousRotation = Quaternion.identity;
            bool havePrevious = false;
            bool reloaded = false;
            int framesSinceRebind = -1;
            float chaseAt = -1f;
            var visibleHitches = new StringBuilder();
            for (int frame = 0; Time.time - started < RunTimeout && frame < 6000; frame++)
            {
                yield return null;
                float real = Time.realtimeSinceStartup;
                float realMs = (real - lastReal) * 1000f;
                lastReal = real;
                string note = string.Empty;
                if (framesSinceRebind >= 0)
                {
                    framesSinceRebind++;
                }

                bool inTheBlack = _director == null || _view == null || framesSinceRebind >= 0 && framesSinceRebind <= BlackFramesAfterReload;
                if (render == false && realMs > MaxVisibleFrameMs && !inTheBlack)
                {
                    visibleHitches.AppendLine($"frame {frame} t={Time.time - started:0.000}: {realMs:0} ms");
                }

                if (_director == null || _view == null)
                {
                    reloaded = true;
                    note = "scene reloading";
                    Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
                    OpeningDirector fresh = null;
                    if (scene.IsValid() && scene.isLoaded)
                    {
                        foreach (GameObject root in scene.GetRootGameObjects())
                        {
                            fresh = fresh != null ? fresh : root.GetComponentInChildren<OpeningDirector>(true);
                        }
                    }

                    if (fresh != null)
                    {
                        Dispose();
                        Bind();
                        havePrevious = false;
                        framesSinceRebind = 0;
                        note = "scene rebound";
                    }

                    csv.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:0.000},{2:0.0},{3},-1,0,0,{4}",
                        frame, Time.time - started, realMs, OpeningPhase.Idle, note));
                    continue;
                }

                Texture2D shot = render ? FrameCapture.Render(_view, TransitionWidth, TransitionHeight) : null;
                float luma = 0f;
                if (shot != null)
                {
                    Color32[] pixels = shot.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        luma += (0.2126f * pixels[i].r + 0.7152f * pixels[i].g + 0.0722f * pixels[i].b) / 255f;
                    }

                    luma /= pixels.Length;
                }

                Transform camera = _view.transform;
                float distance = havePrevious ? Vector3.Distance(camera.position, previousPosition) : 0f;
                float turn = havePrevious ? Quaternion.Angle(camera.rotation, previousRotation) : 0f;
                previousPosition = camera.position;
                previousRotation = camera.rotation;
                havePrevious = true;
                string beat = string.Empty;
                foreach (WakeBeat reached in _beats)
                {
                    beat = reached.ToString();
                }

                csv.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:0.000},{2:0.0},{3},{4:0.0000},{5:0.0000},{6:0.00},{7}",
                    frame, Time.time - started, realMs, _director.Script.Phase, luma, distance, turn, beat));
                if (shot != null)
                {
                    if (frame % 3 == 0)
                    {
                        File.WriteAllBytes(Path.Combine(folder, string.Format(CultureInfo.InvariantCulture, "f{0:0000}.png", frame)),
                            shot.EncodeToPNG());
                    }

                    Object.DestroyImmediate(shot);
                }

                if (reloaded && chaseAt < 0f && _beats.Contains(WakeBeat.ChaseFrame))
                {
                    chaseAt = Time.time;
                }

                if (chaseAt >= 0f && Time.time - chaseAt > 1f)
                {
                    break;
                }
            }

            File.WriteAllText(Path.Combine(folder, "frames.csv"), csv.ToString());
            Assert.IsTrue(reloaded, "the scene reloaded under the fade");
            if (!render)
            {
                Assert.AreEqual(string.Empty, visibleHitches.ToString(),
                    $"no frame over {MaxVisibleFrameMs} ms outside the reload's black:" + System.Environment.NewLine);
            }

            Assert.GreaterOrEqual(chaseAt, 0f, "the reloaded game reaches the chase frame");
        }

        private static IEnumerator Submit(Button button)
        {
            Assert.IsNotNull(button, "the title has the button");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }

            yield return null;
        }

        [UnityTest]
        [Explicit("Real frame time of the title-to-play transition, without capture overhead; run with --category OpeningTransition.")]
        [Category("OpeningTransition")]
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TransitionTiming_NewGameFromAFreshTitle()
        {
            yield return Boot(true);
            yield return Wait(TitleQuietSeconds);
            yield return RecordTransition("timing_new_game", "button-new-game", false);
        }

        [UnityTest]
        [Explicit("Real frame time of the title-to-play transition, without capture overhead; run with --category OpeningTransition.")]
        [Category("OpeningTransition")]
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TransitionTiming_ContinueOverASave()
        {
            yield return Boot(false);
            Assert.IsTrue(_context.Get<ISaveService>().SaveNow(), "a save is on disk");
            Dispose();
            yield return Boot(true);
            yield return Wait(TitleQuietSeconds);
            yield return RecordTransition("timing_continue", "button-resume", false);
        }

        /// <summary>
        /// Clicks <paramref name="buttonName"/> on the title and records every frame the player would see (the whole
        /// screen, UI included, at a quarter size) until the chase frame plus a second: a PNG every other frame and a
        /// CSV of each frame's brightness, phase and camera jump, at the game's own frame time.
        /// </summary>
        private IEnumerator RecordTransition(string tag, string buttonName, bool render = true)
        {
            string folder = Path.Combine(CaptureFolder, "transition_" + tag);
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }

            Directory.CreateDirectory(folder);
            UIDocument document = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(RoverSessionScene.ScenePath).GetRootGameObjects())
            {
                document = document != null ? document : root.GetComponentInChildren<UIDocument>(true);
            }

            Assert.IsNotNull(document, "the scene has its UIDocument");
            VisualElement languagePage = document.rootVisualElement.Q<VisualElement>("page-language");
            if (languagePage != null && IsShown(languagePage))
            {
                Button language = languagePage.Q<Button>();
                Assert.IsNotNull(language, "the first boot offers its languages");
                using (NavigationSubmitEvent choose = NavigationSubmitEvent.GetPooled())
                {
                    choose.target = language;
                    language.SendEvent(choose);
                }

                yield return Wait(LanguageSettleSeconds);
            }

            Button button = document.rootVisualElement.Q<Button>(buttonName);
            Assert.IsNotNull(button, buttonName);
            var csv = new StringBuilder();
            csv.AppendLine("frame,time,realMs,phase,luma,camDistance,camTurnDeg,titleShown,uiScaleX,uiScaleY,curtain,beat");
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = button;
                button.SendEvent(submit);
            }

            Time.captureDeltaTime = 1f / 60f;
            VisualElement titleCrt = document.rootVisualElement.Q<VisualElement>("title-crt");
            VisualElement pause = document.rootVisualElement.Q<VisualElement>("pause");
            VisualElement curtain = document.rootVisualElement.Q<VisualElement>("curtain");
            Vector3 previousPosition = _view.transform.position;
            Quaternion previousRotation = _view.transform.rotation;
            int beatsSeen = 0;
            float started = Time.time;
            float chaseAt = -1f;
            float lastReal = Time.realtimeSinceStartup;
            int slowFrames = 0;
            string firstSlow = null;
            {
                for (int frame = 0; Time.time - started < RunTimeout; frame++)
                {
                    yield return null;
                    Texture2D shot = render ? FrameCapture.Render(_view, TransitionWidth, TransitionHeight) : null;
                    float luma = 0f;
                    if (shot != null)
                    {
                        Color32[] pixels = shot.GetPixels32();
                        for (int i = 0; i < pixels.Length; i++)
                        {
                            luma += (0.2126f * pixels[i].r + 0.7152f * pixels[i].g + 0.0722f * pixels[i].b) / 255f;
                        }

                        luma /= pixels.Length;
                    }

                    float real = Time.realtimeSinceStartup;
                    float realMs = (real - lastReal) * 1000f;
                    lastReal = real;
                    if (frame > 0 && realMs > MaxVisibleFrameMs)
                    {
                        slowFrames++;
                        firstSlow = firstSlow ?? $"frame {frame} at {Time.time - started:0.000} s, {realMs:0} ms";
                    }
                    Transform camera = _view.transform;
                    float distance = Vector3.Distance(camera.position, previousPosition);
                    float turn = Quaternion.Angle(camera.rotation, previousRotation);
                    previousPosition = camera.position;
                    previousRotation = camera.rotation;
                    string beat = string.Empty;
                    while (beatsSeen < _beats.Count)
                    {
                        beat += _beats[beatsSeen++] + " ";
                    }

                    OpeningPhase phase = _director != null ? _director.Script.Phase : OpeningPhase.Idle;
                    csv.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1:0.000},{2:0.0},{3},{4:0.0000},{5:0.0000},{6:0.00},{7},{8:0.000},{9:0.000},{10:0.000},{11}",
                        frame, Time.time - started, realMs, phase, luma, distance, turn, titleCrt != null && IsShown(titleCrt) ? 1 : 0,
                        pause.resolvedStyle.scale.value.x, pause.resolvedStyle.scale.value.y,
                        curtain.resolvedStyle.display == DisplayStyle.None ? 0f : curtain.resolvedStyle.opacity, beat.Trim()));
                    if (shot != null)
                    {
                        if (frame % 2 == 0)
                        {
                            File.WriteAllBytes(Path.Combine(folder, string.Format(CultureInfo.InvariantCulture, "f{0:0000}.png", frame)),
                                shot.EncodeToPNG());
                        }

                        Object.DestroyImmediate(shot);
                    }

                    if (frame == 30)
                    {
                        Assert.AreNotEqual(OpeningPhase.Idle, phase, buttonName + " began the wake");
                    }

                    if (chaseAt < 0f && _beats.Contains(WakeBeat.ChaseFrame))
                    {
                        chaseAt = Time.time;
                    }

                    if (chaseAt >= 0f && Time.time - chaseAt > 1f)
                    {
                        break;
                    }
                }
            }

            File.WriteAllText(Path.Combine(folder, "frames.csv"), csv.ToString());
            Assert.GreaterOrEqual(chaseAt, 0f, "the transition reaches the chase frame");
            if (!render)
            {
                Assert.AreEqual(0, slowFrames, $"no frame over {MaxVisibleFrameMs} ms in the transition (first slow: {firstSlow})");
            }
        }

        private IEnumerator Boot(bool title)
        {
#if UNITY_EDITOR
            if (_subscriptions == null && _view == null)
            {
                _skippedTitle = SessionState.GetBool(GameBootstrap.TestRunSkipsTitleKey, false);
            }

            SessionState.SetBool(GameBootstrap.TestRunSkipsTitleKey, !title);
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            if (_gamepad == null)
            {
                _gamepad = InputSystem.AddDevice<Gamepad>();
            }

            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(RoverSessionScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            float started = Time.realtimeSinceStartup;
            while (!loading.isDone)
            {
                Assert.Less(Time.realtimeSinceStartup - started, BootTimeout, "the scene loads");
                yield return null;
            }
#else
            throw new NotSupportedException("The opening session loads the scene through the editor.");
#endif
            Bind();
            yield return null;
        }

        /// <summary>Finds the loaded session's systems and listens for its wake beats (again after a reload).</summary>
        private void Bind()
        {
            GameBootstrap bootstrap = null;
            _director = null;
            _rig = null;
            _rover = null;
            _body = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(RoverSessionScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _director = _director != null ? _director : root.GetComponentInChildren<OpeningDirector>(true);
                _rig = _rig != null ? _rig : root.GetComponentInChildren<RoverCameraRig>(true);
                _rover = _rover != null ? _rover : root.GetComponentInChildren<RoverController>(true);
                _body = _body != null ? _body : root.GetComponentInChildren<RoverBodyLanguage>(true);
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_director, "the scene has the opening");
            Assert.IsNotNull(_rig, "the scene has the camera rig");
            _context = bootstrap.Context;
            _view = _context.Get<IViewCamera>().Camera;
            _beats.Clear();
            _beatTimes.Clear();
            _checksStartedList.Clear();
            _checksCompleted.Clear();
            _checksFinished = 0;
            _awoke = 0;
            _subscriptions = new[]
            {
                _context.Events.Subscribe<RoverAwoke>(_ => _awoke++),
                _context.Events.Subscribe<RoverWakeBeatReached>(OnBeat),
                _context.Events.Subscribe<BootCheckStarted>(started => _checksStartedList.Add(started.Step)),
                _context.Events.Subscribe<BootCheckCompleted>(done => _checksCompleted[done.Step] = done.Result),
                _context.Events.Subscribe<BootCheckFinished>(_ => _checksFinished++),
            };
        }

        private void OnBeat(RoverWakeBeatReached reached)
        {
            _beats.Add(reached.Beat);
            _beatTimes.Add(Time.time);
        }

        private void Dispose()
        {
            if (_subscriptions == null)
            {
                return;
            }

            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions = null;
        }

        private int Count(WakeBeat beat)
        {
            int count = 0;
            foreach (WakeBeat reached in _beats)
            {
                count += reached == beat ? 1 : 0;
            }

            return count;
        }

        private float TimeOf(WakeBeat beat)
        {
            int index = _beats.IndexOf(beat);
            Assert.GreaterOrEqual(index, 0, $"{beat} was reached");
            return _beatTimes[index];
        }

        private void WriteBeats()
        {
            _report.AppendLine("| Beat | At (s) |").AppendLine("|---|---:|");
            float first = _beatTimes.Count > 0 ? _beatTimes[0] : 0f;
            for (int i = 0; i < _beats.Count; i++)
            {
                _report.AppendLine(string.Format(CultureInfo.InvariantCulture, "| {0} | {1:0.00} |", _beats[i],
                    _beatTimes[i] - first));
            }
        }

        private static string Line(string label, float value)
        {
            return string.Format(CultureInfo.InvariantCulture, "- {0}: {1:0.00}", label, value);
        }

        private void Capture(string name)
        {
            Directory.CreateDirectory(CaptureFolder);
            FrameCapture.SavePng(_view, CaptureWidth, CaptureHeight, Path.Combine(CaptureFolder, name + ".png"));
        }

        private void CaptureKeyFrame(string name)
        {
            Directory.CreateDirectory(CaptureFolder);
            FrameCapture.SavePng(_view, KeyFrameWidth, KeyFrameHeight, Path.Combine(CaptureFolder, name + ".png"));
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }
    }
}

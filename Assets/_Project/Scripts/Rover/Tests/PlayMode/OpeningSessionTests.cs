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
    /// 07's wake through its own eye in the real Main scene, with the title on (docs/design/cutscenes.md §1,
    /// docs/features/M3-09 Part A): nothing wakes on the title; New Game pushes into the closed lens and plays the
    /// full opening once, untouched within 45 s; drive input reaches the chase frame within ~4 s and never holds control
    /// over ~1.5 s; Continue over a real save plays the short wake; the opening's frames allocate nothing. Frames of
    /// both title shots, the push and every beat O1-O14 go to Logs/captures/opening/*.png with opening.md.
    /// Slow and needs a GPU: run on demand with --category OpeningSession.
    /// </summary>
    [Category("OpeningSession")]
    public sealed class OpeningSessionTests : InputTestFixture
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float BootTimeout = 30f;
        private const float TitleQuietSeconds = 4f;
        private const float MaxUntouched = 45f;
        private const float MaxToChase = 4f;
        private const float MaxHeld = 1.5f;
        private const float MaxShortWake = 3f;
        private const float RunTimeout = 70f;
        private const float HurryAt = 7f;
        private const float DriveStick = 0.8f;
        private const float AfterChaseSeconds = 3f;
        private const float TitleSettleSeconds = 4f;

        // Zero GC: extra director updates per measured frame, frames measured and the counter's slack.
        private const int MeasuredCalls = 40;
        private const int MeasuredFrames = 3;
        private const int BaselineFrames = 5;
        private const long Tolerance = 2048L;
        private const string AllocatedInFrame = "GC Allocated In Frame";

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "opening"));

        private readonly List<WakeBeat> _beats = new List<WakeBeat>();
        private readonly List<float> _beatTimes = new List<float>();
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
            Assert.IsTrue(_rig.Opening.IsHeld, "the fresh title holds the lens shot on the closed lens");
            Capture("title_fresh");

            _context.Events.Publish(new NewGameRequested());
            var shots = new (OpeningPhase Phase, float Share, string Name)[]
            {
                (OpeningPhase.PushIn, 0.6f, "push_in"),
                (OpeningPhase.Crackle, 0.5f, "o01_darkness"),
                (OpeningPhase.LidCrack, 0.7f, "o02_lid_slit"),
                (OpeningPhase.LidHalf, 0.8f, "o03_lid_half"),
                (OpeningPhase.Hunt, 0.35f, "o04_hunt_near_speck"),
                (OpeningPhase.Flicker, 0.3f, "o05_flicker"),
                (OpeningPhase.GazeHome, 0.7f, "o06_home"),
                (OpeningPhase.GazePilot, 0.8f, "o07_pilot"),
                (OpeningPhase.GazePeak, 0.8f, "o08_peak"),
                (OpeningPhase.GazeEarth, 0.85f, "o09_earth"),
                (OpeningPhase.PullOut, 0.55f, "o10_pull_out"),
                (OpeningPhase.BlinkShot, 0.2f, "o11_blink"),
                (OpeningPhase.OverShoulder, 0.9f, "o12_over_shoulder"),
                (OpeningPhase.ChaseEase, 0.95f, "o13_chase"),
            };
            var taken = new bool[shots.Length];
            float started = Time.time;
            while (!_beats.Contains(WakeBeat.ChaseFrame))
            {
                Assert.Less(Time.time - started, RunTimeout, "the opening reaches the chase frame");
                OpeningScript script = _director.Script;
                for (int i = 0; i < shots.Length; i++)
                {
                    if (!taken[i] && script.Phase == shots[i].Phase && script.PhaseTime >= shots[i].Share * script.PhaseLength)
                    {
                        taken[i] = true;
                        Capture(shots[i].Name);
                    }
                }

                yield return null;
            }

            yield return Wait(AfterChaseSeconds);
            Capture("o14_first_prompt_frame");

            float crackle = TimeOf(WakeBeat.Crackle);
            float chase = TimeOf(WakeBeat.ChaseFrame);
            WriteBeats();
            _report.AppendLine().AppendLine(Line("untouched opening, O1 to the chase frame (s)", chase - crackle));
            Assert.AreEqual(1, Count(WakeBeat.Crackle), "the full opening plays once");
            Assert.AreEqual(1, Count(WakeBeat.PushedIn), "New Game pushed into the closed lens");
            Assert.AreEqual(1, _awoke, "RoverAwoke goes with the crackle, once");
            Assert.LessOrEqual(chase - crackle, MaxUntouched, "the untouched opening lasts at most 45 s");
            for (int i = 0; i < shots.Length; i++)
            {
                Assert.IsTrue(taken[i], $"{shots[i].Name} was captured");
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
        public IEnumerator DriveInput_ReachesTheChaseFrameSoon_AndNeverHoldsControlLong()
        {
            yield return Boot(true);
            _context.Events.Publish(new NewGameRequested());
            while (!_beats.Contains(WakeBeat.Crackle))
            {
                yield return null;
            }

            yield return Wait(HurryAt);
            Set(_gamepad.leftStick, new Vector2(0f, DriveStick));
            float pressed = Time.time;
            float held = 0f;
            bool midCaptured = false;
            while (!_beats.Contains(WakeBeat.ChaseFrame))
            {
                Assert.Less(Time.time - pressed, MaxToChase + 1f, "the hurried opening reaches the chase frame");
                if (_rover.IsHeldStill)
                {
                    held = Time.time - pressed;
                }

                if (!midCaptured && _director.Script.Phase == OpeningPhase.PullOut)
                {
                    midCaptured = true;
                    Capture("hurry_pull_out");
                }

                yield return null;
            }

            float chase = Time.time - pressed;
            Set(_gamepad.leftStick, Vector2.zero);
            _report.AppendLine("# Hurry (drive input at O3/O4)").AppendLine();
            WriteBeats();
            _report.AppendLine().AppendLine(Line("drive input to the chase frame (s)", chase))
                .AppendLine(Line("control held after drive input (s)", held));
            Assert.LessOrEqual(chase, MaxToChase + 0.1f, "drive input reaches the chase frame within ~4 s");
            Assert.LessOrEqual(held, MaxHeld + 0.1f, "control is never held more than ~1.5 s");
            Assert.Contains(WakeBeat.Blinked, _beats, "the blink is never skipped");
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
            _awoke = 0;
            _subscriptions = new[]
            {
                _context.Events.Subscribe<RoverAwoke>(_ => _awoke++),
                _context.Events.Subscribe<RoverWakeBeatReached>(OnBeat),
            };
            yield return null;
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

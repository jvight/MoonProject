using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// 07's wake as its power-on self-test seen on its face: the push-in from a fresh title (or the cover and the
    /// settle around a reload), the tube waking, the maker's mark and the load, every line announced in order (a key
    /// after the first moments fast-forwards the rest, never at once), the eyes back only after the checks, the ready
    /// hold, the two camera moves around the ready performance, and the short wake that Continue plays instead.
    /// </summary>
    public sealed class OpeningScriptTests
    {
        private const float Frame = 1f / 60f;
        private const float MaxHeld = 1.5f;

        private RoverCameraTuning _tuning;
        private OpeningScript _script;
        private readonly List<WakeBeat> _beats = new List<WakeBeat>();
        private readonly List<PostEvent> _lines = new List<PostEvent>();

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _script = new OpeningScript(_tuning);
            _beats.Clear();
            _lines.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void StepOnce()
        {
            _script.Step(Frame);
            TakeAll();
        }

        private void TakeAll()
        {
            while (_script.TryTakeBeat(out WakeBeat beat))
            {
                _beats.Add(beat);
            }

            while (_script.TryTakePostEvent(out PostEvent line))
            {
                _lines.Add(line);
            }
        }

        /// <summary>Steps until the chase frame (or the cap); returns the seconds it took.</summary>
        private float RunToChase(float cap)
        {
            float t = 0f;
            while (!_beats.Contains(WakeBeat.ChaseFrame) && t < cap)
            {
                StepOnce();
                t += Frame;
            }

            return t;
        }

        [Test]
        public void TheChecksPulses_ReachTheBus_WithTheirIndexAndProgress()
        {
            _script.BeginFull(OpeningStart.PushIn);
            RunToChase(120f);
            bool sawIndexed = false;
            bool sawProgress = false;
            foreach (PostEvent line in _lines)
            {
                if (line.Kind != PostEventKind.Pulse)
                {
                    continue;
                }

                sawIndexed |= line.Step == BootCheckStep.Cutter && line.Index == 5;
                sawProgress |= line.Step == BootCheckStep.Battery && line.Progress01 > 0.4f;
            }

            Assert.IsTrue(sawIndexed, "the third spark's beacon blink keeps its index");
            Assert.IsTrue(sawProgress, "the battery fill keeps its progress");
        }

        [Test]
        public void NewGameOnAFreshTitle_PushesInWithNoBlack_ThenTheTubeWakesWithinASecond()
        {
            _script.BeginFull(OpeningStart.PushIn);
            TakeAll();
            Assert.AreEqual(OpeningPhase.PushIn, _script.Phase);
            Assert.IsTrue(_script.ShotLive, "the shot camera takes over where the title's camera is, at once");
            Assert.AreEqual(WakeBeat.Crackle, _beats[0], "07's systems thump on with the click");
            float elapsed = 0f;
            while (_script.Phase == OpeningPhase.PushIn)
            {
                StepOnce();
                elapsed += Frame;
            }

            Assert.AreEqual(OpeningPhase.PowerOn, _script.Phase, "the push-in ends on the face as the tube wakes");
            Assert.LessOrEqual(elapsed, 1f + Frame, "within a second of the click");
            CollectionAssert.DoesNotContain(_beats, WakeBeat.FadedOut, "no cover: nothing for App to reload under");
        }

        [Test]
        public void NewGameOnAFreshTitle_HandsControlOverWithinSixteenSeconds()
        {
            _script.BeginFull(OpeningStart.PushIn);
            TakeAll();
            float seconds = RunToChase(60f);
            Assert.LessOrEqual(seconds, 16f + Frame, "New Game click to control, untouched");
            Assert.Greater(seconds, 12f, "not rushed");
        }

        [Test]
        public void ASlowFrameRate_DoesNotStretchTheWake()
        {
            const float SlowFrame = 0.1f;
            PostSettings post = _tuning.Opening.Post;
            float tuned = post.PushInSeconds + post.PowerOnSeconds + post.LogoSeconds + post.LoadSeconds +
                          post.ReadySeconds + post.PullBackSeconds + post.PerformSeconds + post.MoveBehindSeconds;
            for (int i = 0; i < post.LineCount; i++)
            {
                tuned += post.LineSeconds(i);
            }

            _script.BeginFull(OpeningStart.PushIn);
            TakeAll();
            float seconds = 0f;
            while (!_beats.Contains(WakeBeat.ChaseFrame) && seconds < 60f)
            {
                _script.Step(SlowFrame);
                TakeAll();
                seconds += SlowFrame;
            }

            Assert.AreEqual(tuned, seconds, SlowFrame + 1e-3f, "each phase carries the frame's leftover into the next");
        }

        [Test]
        public void TheWake_RunsItsBeats_InOrder()
        {
            _script.BeginFull(OpeningStart.Settle);
            TakeAll();
            RunToChase(60f);
            CollectionAssert.AreEqual(new[]
            {
                WakeBeat.Crackle, WakeBeat.ChecksBegin, WakeBeat.EyesOpen, WakeBeat.ChaseFrame,
            }, _beats);
            Assert.IsFalse(_script.IsRunning);
        }

        [Test]
        public void TheFaceBoots_PowerOnThenLogoThenLoad_ThenTheFirstLine_AndTheEyesComeBackOnlyAfterTheChecks()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(OpeningStart.PushIn);
            TakeAll();
            var phases = new List<OpeningPhase> { _script.Phase };
            while (_script.Phase != OpeningPhase.Ready)
            {
                StepOnce();
                if (phases[phases.Count - 1] != _script.Phase)
                {
                    phases.Add(_script.Phase);
                }
            }

            CollectionAssert.AreEqual(new[]
            {
                OpeningPhase.PushIn, OpeningPhase.PowerOn, OpeningPhase.Logo, OpeningPhase.Load, OpeningPhase.Post,
                OpeningPhase.Ready,
            }, phases, "power-on, logo, load, lines, ready");
            var parts = new List<PostEvent>();
            int firstLine = -1;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].Kind == PostEventKind.FaceBegan)
                {
                    parts.Add(_lines[i]);
                }
                else if (_lines[i].Kind == PostEventKind.Started && firstLine < 0)
                {
                    firstLine = i;
                    Assert.AreEqual(3, parts.Count, "the face has booted before the first line");
                    Assert.AreEqual(BootCheckStep.Carrier, _lines[i].Step, "the first line is why 07 woke");
                }
            }

            Assert.AreEqual(3, parts.Count, "each part of the face's boot is announced once");
            Assert.AreEqual((int)BootFacePart.PowerOn, parts[0].Index);
            Assert.AreEqual(post.PowerOnSeconds, parts[0].Seconds, 1e-5f);
            Assert.AreEqual((int)BootFacePart.Logo, parts[1].Index);
            Assert.AreEqual(post.LogoSeconds, parts[1].Seconds, 1e-5f);
            Assert.AreEqual((int)BootFacePart.Load, parts[2].Index);
            Assert.AreEqual(post.LoadSeconds, parts[2].Seconds, 1e-5f);
            CollectionAssert.AreEqual(new[] { WakeBeat.Crackle, WakeBeat.ChecksBegin }, _beats,
                "the self-test begins as the tube wakes; no eyes while the face boots and the lines print");

            while (_script.Phase == OpeningPhase.Ready)
            {
                StepOnce();
            }

            Assert.AreEqual(WakeBeat.EyesOpen, _beats[_beats.Count - 1], "the eyes come back in the ready hold");
        }

        [Test]
        public void EveryLine_IsAnnouncedStartedThenCompleted_InTheStoryOrder_AndTheTestFinishesLast()
        {
            _script.BeginFull(OpeningStart.Settle);
            TakeAll();
            RunToChase(60f);

            PostSettings post = _tuning.Opening.Post;
            var expected = new BootCheckStep[post.LineCount];
            for (int i = 0; i < expected.Length; i++)
            {
                expected[i] = post.LineStep(i);
            }

            var started = new List<BootCheckStep>();
            var completed = new List<BootCheckStep>();
            foreach (PostEvent line in _lines)
            {
                if (line.Kind == PostEventKind.Started)
                {
                    started.Add(line.Step);
                }
                else if (line.Kind == PostEventKind.Completed)
                {
                    completed.Add(line.Step);
                }
            }

            CollectionAssert.AreEqual(expected, started);
            CollectionAssert.AreEqual(expected, completed);
            PostEvent lastLine = _lines.FindLast(line => line.Kind != PostEventKind.Pulse && line.Kind != PostEventKind.Blink);
            Assert.AreEqual(PostEventKind.Finished, lastLine.Kind, "the test finishes after its last line");
        }

        [Test]
        public void ControlIsHeld_UntilTheChaseFrame_EvenWhenTheLinesAreFastForwarded()
        {
            foreach (bool fastForward in new[] { false, true })
            {
                _beats.Clear();
                _script.BeginFull(OpeningStart.Settle);
                TakeAll();
                while (!_beats.Contains(WakeBeat.ChaseFrame) && _script.IsRunning)
                {
                    Assert.IsTrue(_script.ControlHeld, $"held in {_script.Phase}");
                    if (fastForward)
                    {
                        _script.FastForward();
                    }

                    StepOnce();
                }

                Assert.IsFalse(_script.ControlHeld);
            }
        }

        [Test]
        public void AKey_BeforeTheTubeWakes_OrOnItsFirstFrame_OrTooEarly_DoesNotFastForward()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(OpeningStart.PushIn);
            TakeAll();
            while (_script.Phase != OpeningPhase.PowerOn)
            {
                Assert.IsFalse(_script.FastForward(), $"nothing to fast-forward in {_script.Phase}");
                StepOnce();
            }

            Assert.AreEqual(0f, _script.ScreenOnSeconds, 1e-6f, "the tube's first frame");
            Assert.IsFalse(_script.FastForward(), "never on the first frame: the press may be the one that started it");
            while (_script.ScreenOnSeconds + Frame < post.FastForwardAfterSeconds)
            {
                StepOnce();
                Assert.IsFalse(_script.FastForward(), $"too early at {_script.ScreenOnSeconds:0.00} s");
            }

            Assert.AreEqual(1f, _script.PostTimeScale);
        }

        [Test]
        public void AKey_DuringTheLogo_RunsTheRestFaster_EveryPartAndLineStillInOrder()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(OpeningStart.PushIn);
            TakeAll();
            while (_script.Phase != OpeningPhase.Logo || _script.ScreenOnSeconds < post.FastForwardAfterSeconds)
            {
                StepOnce();
            }

            float at = _script.ScreenOnSeconds;
            Assert.IsTrue(_script.FastForward(), "the maker's mark can be skipped");
            Assert.IsFalse(_script.FastForward(), "once is enough");
            Assert.AreEqual(post.FastForwardScale, _script.PostTimeScale);
            float seconds = 0f;
            while (_script.Phase != OpeningPhase.Ready)
            {
                StepOnce();
                seconds += Frame;
            }

            float total = post.PowerOnSeconds + post.LogoSeconds + post.LoadSeconds;
            for (int i = 0; i < post.LineCount; i++)
            {
                total += post.LineSeconds(i);
            }

            Assert.AreEqual((total - at) / post.FastForwardScale, seconds, 3f * Frame, "the rest runs that much faster");
            Assert.AreEqual(1f, _script.PostTimeScale, "the ready hold and the moves keep their pace");
            int loads = 0;
            foreach (PostEvent line in _lines)
            {
                loads += line.Kind == PostEventKind.FaceBegan && line.Index == (int)BootFacePart.Load ? 1 : 0;
            }

            Assert.AreEqual(1, loads, "the load still runs, faster");
            var started = new List<BootCheckStep>();
            var completed = new List<BootCheckStep>();
            foreach (PostEvent line in _lines)
            {
                if (line.Kind == PostEventKind.Started)
                {
                    started.Add(line.Step);
                }
                else if (line.Kind == PostEventKind.Completed)
                {
                    completed.Add(line.Step);
                }
            }

            Assert.AreEqual(post.LineCount, started.Count, "every line still prints");
            Assert.AreEqual(post.LineCount, completed.Count);
            for (int i = 0; i < post.LineCount; i++)
            {
                Assert.AreEqual(post.LineStep(i), started[i]);
                Assert.AreEqual(post.LineStep(i), completed[i]);
            }
        }

        [Test]
        public void TheShot_IsLiveFromThePushInToTheChaseFrame_AndAfterACoverOnlyFromTheDarkFace()
        {
            foreach (OpeningStart start in new[] { OpeningStart.PushIn, OpeningStart.Cover })
            {
                _beats.Clear();
                _script.BeginFull(start);
                TakeAll();
                Assert.AreEqual(start == OpeningStart.PushIn, _script.ShotLive,
                    $"{start}: the push-in is the shot's own move; the cover runs on the title's camera");
                while (_script.IsRunning)
                {
                    StepOnce();
                    if (_script.IsRunning && _script.Phase >= OpeningPhase.PushIn)
                    {
                        Assert.IsTrue(_script.ShotLive, $"the opening's camera has the view in {_script.Phase}");
                    }
                }

                Assert.IsFalse(_script.ShotLive);
            }
        }

        [Test]
        public void AfterTheChecks_TheReadyHold_BothMoves_AndThePerformance_TakeTheirTunedSeconds_InOrder()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(OpeningStart.Settle);
            TakeAll();
            while (_script.Phase != OpeningPhase.Ready)
            {
                StepOnce();
            }

            var expected = new[]
            {
                (OpeningPhase.Ready, post.ReadySeconds), (OpeningPhase.PullBack, post.PullBackSeconds),
                (OpeningPhase.Perform, post.PerformSeconds), (OpeningPhase.MoveBehind, post.MoveBehindSeconds),
            };
            foreach ((OpeningPhase phase, float seconds) in expected)
            {
                Assert.AreEqual(phase, _script.Phase);
                float t = 0f;
                while (_script.Phase == phase)
                {
                    Assert.IsTrue(_script.ControlHeld, $"held through {phase}");
                    StepOnce();
                    t += Frame;
                }

                Assert.AreEqual(seconds, t, 2f * Frame, $"{phase} takes its tuned seconds");
            }

            Assert.AreEqual(OpeningPhase.Idle, _script.Phase);
            Assert.IsFalse(_script.ControlHeld, "control comes back at the chase frame");
        }

        [Test]
        public void TheEyesComeBack_ThenThePerformanceBlinksTwice_ClicksTheLampsAndSoundsInOrder()
        {
            _script.BeginFull(OpeningStart.Settle);
            TakeAll();
            while (_script.Phase != OpeningPhase.Ready)
            {
                StepOnce();
            }

            _beats.Clear();
            _lines.Clear();
            while (_script.Phase == OpeningPhase.Ready)
            {
                StepOnce();
            }

            CollectionAssert.AreEqual(new[] { WakeBeat.EyesOpen }, _beats, "the eyes come back to the face");
            while (_script.IsRunning)
            {
                StepOnce();
            }

            var blinks = new List<int>();
            var sounds = new List<int>();
            foreach (PostEvent line in _lines)
            {
                if (line.Kind == PostEventKind.Blink)
                {
                    blinks.Add(line.Index);
                }
                else if (line.Kind == PostEventKind.Pulse)
                {
                    Assert.AreEqual(BootCheckStep.Begin, line.Step, "the performance sounds as the start check's");
                    sounds.Add(line.Index);
                }
            }

            CollectionAssert.AreEqual(new[] { 0, 1 }, blinks, "a double blink");
            CollectionAssert.AreEqual(new[] { 3, 2, 0 }, sounds, "lamps off, lamps on, glance");
        }

        [Test]
        public void ThePerformanceProgress_RunsOnlyInItsPhase_AndHoldsAtItsEnd()
        {
            _script.BeginFull(OpeningStart.Settle);
            TakeAll();
            float last = 0f;
            while (_script.IsRunning)
            {
                StepOnce();
                if (!_script.IsRunning)
                {
                    break;
                }

                if (_script.Phase < OpeningPhase.Perform)
                {
                    Assert.AreEqual(0f, _script.Perform01);
                }
                else if (_script.Phase == OpeningPhase.MoveBehind)
                {
                    Assert.AreEqual(1f, _script.Perform01, "the glance holds while the camera moves behind");
                }

                Assert.GreaterOrEqual(_script.Perform01, last - 1e-6f);
                last = _script.Perform01;
            }
        }

        [Test]
        public void OverASave_TheCoverComesFirst_ThenFadedOutLetsAppReload_ThenTheDarkFaceSettles()
        {
            OpeningSettings settings = _tuning.Opening;
            _script.BeginFull(OpeningStart.Cover);
            TakeAll();
            Assert.AreEqual(OpeningPhase.FadeOut, _script.Phase);
            float covered = 0f;
            while (!_beats.Contains(WakeBeat.Crackle))
            {
                Assert.AreEqual(1f, _script.Sleep, 1e-5f, "asleep under the cover");
                StepOnce();
                covered += Frame;
            }

            Assert.AreEqual(WakeBeat.FadedOut, _beats[0], "App reloads once the cover is closed");
            Assert.AreEqual(settings.FadeOutSeconds, covered, 2f * Frame);
            Assert.LessOrEqual(covered, 0.6f + Frame, "the cover takes at most 0.6 s");
            Assert.AreEqual(OpeningPhase.Dark, _script.Phase);
        }

        [Test]
        public void ThePushIn_Lifts07sHead_SoTheTubeWakesOnAnAwakeHalfLiddedFace()
        {
            OpeningSettings settings = _tuning.Opening;
            foreach (OpeningStart start in new[] { OpeningStart.PushIn, OpeningStart.Settle })
            {
                _script.BeginFull(start);
                Assert.AreEqual(1f, _script.Sleep, 1e-5f, $"{start}: asleep at first");
                float last = 1f;
                while (_script.Phase != OpeningPhase.PowerOn)
                {
                    StepOnce();
                    Assert.LessOrEqual(_script.Sleep, last + 1e-5f, "the head only lifts");
                    last = _script.Sleep;
                }

                Assert.AreEqual(0f, _script.Sleep, 1e-3f, "awake as the tube wakes");
                Assert.AreEqual(settings.LidRest, _script.BodyLid, 1e-3f, "and half-lidded for the checks");
            }
        }

        [Test]
        public void ShortWake_StirsAndBlinks_ReachesTheChaseWithinThreeSeconds_AndGlancesAtEarth()
        {
            _script.BeginShort();
            TakeAll();
            float held = 0f;
            float t = 0f;
            while (!_beats.Contains(WakeBeat.ChaseFrame) && t < 10f)
            {
                StepOnce();
                t += Frame;
                if (_script.ControlHeld)
                {
                    held = t;
                }
            }

            Assert.AreEqual(WakeBeat.Stirred, _beats[0]);
            Assert.LessOrEqual(t, 3f, "control returns within 3 s (M3-09)");
            Assert.LessOrEqual(held, MaxHeld + Frame);
            Assert.IsTrue(_script.GlancingEarth, "the glance at Earth runs on after control returns");
            Assert.IsFalse(_script.ShotLive, "Continue never takes the face shot");
            CollectionAssert.DoesNotContain(_beats, WakeBeat.Crackle, "Continue never plays the self-test");
            Assert.AreEqual(0, _lines.Count, "and prints none of its lines");
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// 07's wake as its power-on self-test seen on its face: the fade to black, the black, the picture opening straight
    /// on the face with the first line, every line announced in order (a key after the first moments fast-forwards
    /// them, never on their first frame), the eyes back only after the checks, the ready hold, the two camera moves
    /// around the ready performance, and the short wake that Continue plays instead.
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
            _script.BeginFull(true);
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
        public void NewGameFromTheTitle_HoldsPureBlackNoLongerThan1AndAHalf_BeforeThePictureOpens()
        {
            const float MaxToFirstImage = 3f;
            const float MaxPureBlack = 1.5f;
            _script.BeginFull(true);
            float elapsed = 0f;
            float blackStart = -1f;
            while (_script.Phase != OpeningPhase.Post && elapsed < 2f * MaxToFirstImage)
            {
                StepOnce();
                elapsed += Frame;
                if (_script.Phase == OpeningPhase.Dark && blackStart < 0f)
                {
                    blackStart = elapsed;
                }
            }

            Assert.AreEqual(OpeningPhase.Post, _script.Phase, "The picture opens on the face.");
            Assert.LessOrEqual(elapsed, MaxToFirstImage, "The first image comes within 3 s of the click.");
            Assert.Greater(blackStart, 0f, "The black plays.");
            Assert.LessOrEqual(elapsed - blackStart, MaxPureBlack, "The pure black is at most 1.5 s.");
        }

        [Test]
        public void TheWake_RunsBlackFaceChecksReadyAndBothMoves_InOrder_InAboutTwelveAndAHalfSeconds()
        {
            _script.BeginFull(false);
            TakeAll();
            float seconds = RunToChase(60f);

            Assert.That(seconds, Is.InRange(11f, 14f), "the wake lasts about 12.5 s");
            CollectionAssert.AreEqual(new[]
            {
                WakeBeat.Crackle, WakeBeat.ChecksBegin, WakeBeat.EyesOpen, WakeBeat.ChaseFrame,
            }, _beats);
            Assert.IsFalse(_script.IsRunning);
        }

        [Test]
        public void ThePictureOpensOnTheFace_WithTheFirstLine_AndTheEyesComeBackOnlyAfterTheChecks()
        {
            _script.BeginFull(false);
            TakeAll();
            while (_lines.Count == 0 && _script.IsRunning)
            {
                StepOnce();
            }

            Assert.AreEqual(OpeningPhase.Post, _script.Phase, "the first line prints as the picture opens");
            Assert.Less(_script.PhaseTime, 2f * Frame, "at once, with no stage before it");
            Assert.AreEqual(PostEventKind.Started, _lines[0].Kind);
            Assert.AreEqual(BootCheckStep.Carrier, _lines[0].Step, "the first line is why 07 woke");
            CollectionAssert.AreEqual(new[] { WakeBeat.Crackle, WakeBeat.ChecksBegin }, _beats);

            while (_script.Phase == OpeningPhase.Post)
            {
                StepOnce();
                CollectionAssert.DoesNotContain(_beats, WakeBeat.EyesOpen, "no eyes while the lines print");
            }

            while (_script.Phase == OpeningPhase.Ready)
            {
                StepOnce();
            }

            Assert.AreEqual(WakeBeat.EyesOpen, _beats[_beats.Count - 1], "the eyes come back in the ready hold");
        }

        [Test]
        public void EveryLine_IsAnnouncedStartedThenCompleted_InTheStoryOrder_AndTheTestFinishesLast()
        {
            _script.BeginFull(false);
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
                _script.BeginFull(false);
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
        public void AKey_BeforeTheLines_OrOnTheirFirstFrame_OrTooEarly_DoesNotFastForward()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(true);
            TakeAll();
            while (_script.Phase != OpeningPhase.Post)
            {
                Assert.IsFalse(_script.FastForward(), $"nothing to fast-forward in {_script.Phase}");
                StepOnce();
            }

            Assert.AreEqual(0f, _script.PhaseTime, 1e-6f, "the lines' first frame");
            Assert.IsFalse(_script.FastForward(), "never on the first frame: the press may be the one that started it");
            while (_script.PhaseTime + Frame < post.FastForwardAfterSeconds)
            {
                StepOnce();
                Assert.IsFalse(_script.FastForward(), $"too early at {_script.PhaseTime:0.00} s");
            }

            Assert.AreEqual(1f, _script.PostTimeScale);
        }

        [Test]
        public void AKey_AfterTheFirstMoments_RunsTheRemainingLinesFaster_EveryLineStillInOrder()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(false);
            TakeAll();
            while (_script.Phase != OpeningPhase.Post || _script.PhaseTime < post.FastForwardAfterSeconds)
            {
                StepOnce();
            }

            float at = _script.PhaseTime;
            Assert.IsTrue(_script.FastForward());
            Assert.IsFalse(_script.FastForward(), "once is enough");
            Assert.AreEqual(post.FastForwardScale, _script.PostTimeScale);
            float seconds = 0f;
            while (_script.Phase == OpeningPhase.Post)
            {
                StepOnce();
                seconds += Frame;
            }

            float total = 0f;
            for (int i = 0; i < post.LineCount; i++)
            {
                total += post.LineSeconds(i);
            }

            Assert.AreEqual((total - at) / post.FastForwardScale, seconds, 2f * Frame, "the rest runs that much faster");
            Assert.AreEqual(1f, _script.PostTimeScale, "the ready hold and the moves keep their pace");
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
        public void TheShot_IsLiveFromTheBlackToTheChaseFrame_AndThePictureOpensOnlyOnce()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(true);
            TakeAll();
            Assert.IsFalse(_script.ShotLive, "the fade runs on the title's own camera");
            float picture = 0f;
            while (_script.IsRunning)
            {
                StepOnce();
                if (!_script.IsRunning)
                {
                    break;
                }

                if (_script.Phase >= OpeningPhase.Dark)
                {
                    Assert.IsTrue(_script.ShotLive, $"the opening's camera has the view in {_script.Phase}");
                    Assert.GreaterOrEqual(_script.Picture01, picture - 1e-6f, "the picture only ever opens");
                    picture = _script.Picture01;
                }

                if (_script.Phase > OpeningPhase.Post
                    || (_script.Phase == OpeningPhase.Post && _script.PhaseTime >= post.FadeInSeconds))
                {
                    Assert.AreEqual(1f, _script.Picture01, 1e-4f, $"no dip to black in {_script.Phase}");
                }
            }

            Assert.IsFalse(_script.ShotLive);
        }

        [Test]
        public void AfterTheChecks_TheReadyHold_BothMoves_AndThePerformance_TakeTheirTunedSeconds_InOrder()
        {
            PostSettings post = _tuning.Opening.Post;
            _script.BeginFull(false);
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
            _script.BeginFull(false);
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
            _script.BeginFull(false);
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
        public void FromTheTitle_FadesToBlackFirst_AndThatBlackLeadsIntoTheWake()
        {
            _script.BeginFull(true);
            TakeAll();
            Assert.AreEqual(OpeningPhase.FadeOut, _script.Phase);
            Assert.AreEqual(1f, _script.Picture01, 1e-4f, "the fade starts on the open picture");

            while (!_beats.Contains(WakeBeat.Crackle))
            {
                StepOnce();
            }

            Assert.AreEqual(WakeBeat.FadedOut, _beats[0]);
            Assert.AreEqual(0f, _script.Picture01, 1e-3f, "the fade ends in black, then the wake begins");
        }

        [Test]
        public void TheLidStaysShutInTheBlack_OpensWithThePicture_AndRestsHalfLiddedForTheChecks()
        {
            OpeningSettings settings = _tuning.Opening;
            _script.BeginFull(false);
            while (_script.IsRunning && _script.Phase == OpeningPhase.Dark)
            {
                Assert.AreEqual(0f, _script.BodyLid, 1e-5f, "asleep in the black, the lid is shut");
                Assert.AreEqual(1f, _script.Sleep, 1e-5f);
                StepOnce();
            }

            while (_script.Phase == OpeningPhase.Post && _script.PhaseTime < settings.Post.FadeInSeconds + 0.1f)
            {
                StepOnce();
            }

            Assert.AreEqual(settings.LidRest, _script.BodyLid, 1e-3f, "the checks run half-lidded");
            Assert.AreEqual(0f, _script.Sleep, 1e-3f, "and awake");
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

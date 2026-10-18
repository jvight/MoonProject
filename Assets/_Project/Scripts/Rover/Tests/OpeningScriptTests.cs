using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// 07's wake as its power-on self-test seen on its face: the fade to black, the black, the picture opening, the eyes
    /// waking before any line prints, every line announced in order and never skipped, the one shot held through the
    /// checks, the ready hold, the two camera moves around the ready performance, and the short wake that Continue
    /// plays instead.
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
            _script.SetFaceTarget(new Vector2(35f, -3f));
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

                sawIndexed |= line.Step == BootCheckStep.Wheels && line.Index == 3;
                sawProgress |= line.Step == BootCheckStep.Battery && line.Progress01 > 0.4f;
            }

            Assert.IsTrue(sawIndexed, "the fourth wheel's twitch keeps its index");
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
            while (_script.Phase != OpeningPhase.FadeIn && elapsed < 2f * MaxToFirstImage)
            {
                StepOnce();
                elapsed += Frame;
                if (_script.Phase == OpeningPhase.Dark && blackStart < 0f)
                {
                    blackStart = elapsed;
                }
            }

            Assert.AreEqual(OpeningPhase.FadeIn, _script.Phase, "The picture opens.");
            Assert.LessOrEqual(elapsed, MaxToFirstImage, "The first image comes within 3 s of the click.");
            Assert.Greater(blackStart, 0f, "The black plays.");
            Assert.LessOrEqual(elapsed - blackStart, MaxPureBlack, "The pure black is at most 1.5 s.");
        }

        [Test]
        public void TheWake_RunsBlackPictureEyesChecksReadyAndBothMoves_InOrder_InAboutThirtySixSeconds()
        {
            _script.BeginFull(false);
            TakeAll();
            float seconds = RunToChase(60f);

            Assert.That(seconds, Is.InRange(32f, 40f), "the wake lasts about 36 s");
            CollectionAssert.AreEqual(new[]
            {
                WakeBeat.Crackle, WakeBeat.PictureOpens, WakeBeat.EyesOpen, WakeBeat.Blinked, WakeBeat.ChecksBegin,
                WakeBeat.Blinked, WakeBeat.ChaseFrame,
            }, _beats);
            Assert.IsFalse(_script.IsRunning);
        }

        [Test]
        public void TheEyesOpenAndBlink_BeforeTheFirstLinePrintsOnTheFace()
        {
            _script.BeginFull(false);
            TakeAll();
            while (_lines.Count == 0 && _script.IsRunning)
            {
                StepOnce();
            }

            int faceOn = _beats.IndexOf(WakeBeat.EyesOpen);
            int blink = _beats.IndexOf(WakeBeat.Blinked);
            Assert.GreaterOrEqual(faceOn, 0, "the face screen flickers on");
            Assert.Greater(blink, faceOn, "the eyes blink awake after it");
            Assert.AreEqual(OpeningPhase.Post, _script.Phase, "only then do the lines start");
            Assert.AreEqual(PostEventKind.Started, _lines[0].Kind);
            Assert.AreEqual(BootCheckStep.Carrier, _lines[0].Step, "the first line is why 07 woke");
            Assert.AreEqual(0f, _script.Sleep, 1e-3f, "07 is awake by the first line");
        }

        [Test]
        public void EveryLine_IsAnnouncedStartedThenCompleted_InTheStoryOrder_AndTheTestFinishesLast()
        {
            _script.BeginFull(false);
            TakeAll();
            RunToChase(60f);

            var expected = new[]
            {
                BootCheckStep.Carrier, BootCheckStep.Resume, BootCheckStep.Agenda, BootCheckStep.Lens,
                BootCheckStep.Lamps, BootCheckStep.Head, BootCheckStep.Wheels, BootCheckStep.Motor,
                BootCheckStep.Cutter, BootCheckStep.SolarWing, BootCheckStep.Antenna, BootCheckStep.Battery,
                BootCheckStep.Assembly, BootCheckStep.Begin,
            };
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
        public void ControlIsHeld_UntilTheChaseFrame_TheWakeCannotBeSkipped()
        {
            _script.BeginFull(false);
            TakeAll();
            while (!_beats.Contains(WakeBeat.ChaseFrame) && _script.IsRunning)
            {
                Assert.IsTrue(_script.ControlHeld, $"held in {_script.Phase}");
                StepOnce();
            }

            Assert.IsFalse(_script.ControlHeld);
        }

        [Test]
        public void TheShot_IsLiveFromTheBlackToTheChaseFrame_AndThePushInOnlyGrows()
        {
            _script.BeginFull(true);
            TakeAll();
            Assert.IsFalse(_script.ShotLive, "the fade runs on the title's own camera");
            float push = 0f;
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
                    if (_script.Phase > OpeningPhase.FadeIn)
                    {
                        Assert.AreEqual(1f, _script.Picture01, 1e-4f, $"no dip to black in {_script.Phase}");
                    }
                }

                Assert.GreaterOrEqual(_script.Push01, push - 1e-6f, "the push-in never backs off");
                if (_script.Phase < OpeningPhase.Post)
                {
                    Assert.AreEqual(0f, _script.Push01, "the shot holds wide while 07 wakes");
                }

                push = _script.Push01;
            }

            Assert.AreEqual(1f, push, 1e-3f, "the push-in completes by the end of the checks");
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
        public void TheEyesComeBackWithABlink_ThenThePerformanceBlinksTwice_ClicksTheLampsAndSoundsInOrder()
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

            CollectionAssert.AreEqual(new[] { WakeBeat.Blinked }, _beats, "the eyes come back to the face with a blink");
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
            CollectionAssert.AreEqual(new[] { 3, 2, 1, 0 }, sounds, "lamps off, lamps on, shiver, glance");
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
        public void TheLidStaysShutAsleep_OpensWithTheEyes_AndRestsHalfLiddedForTheChecks()
        {
            OpeningSettings settings = _tuning.Opening;
            _script.BeginFull(false);
            while (_script.IsRunning && _script.Phase != OpeningPhase.Post)
            {
                StepOnce();
                if (_script.Phase == OpeningPhase.Dark || _script.Phase == OpeningPhase.FadeIn)
                {
                    Assert.AreEqual(0f, _script.BodyLid, 1e-5f, "asleep, the lid is shut");
                    Assert.AreEqual(1f, _script.Sleep, 1e-5f);
                }
            }

            for (int i = 0; i < 20; i++)
            {
                StepOnce();
            }

            Assert.AreEqual(settings.LidRest, _script.BodyLid, 1e-3f, "the checks run half-lidded");
        }

        [Test]
        public void TheHead_StaysBowedAsleep_ThenLiftsIntoTheShot()
        {
            OpeningSettings settings = _tuning.Opening;
            _script.BeginFull(false);
            while (_script.Phase != OpeningPhase.EyesOpen)
            {
                StepOnce();
                Assert.AreEqual(settings.BowedPitch, _script.Gaze.AimPitch, 1e-4f, "bowed while asleep");
            }

            while (_script.Phase != OpeningPhase.Ready)
            {
                StepOnce();
            }

            Assert.AreEqual(35f, _script.Gaze.Yaw, 1f, "the head has turned into the shot");
            Assert.AreEqual(-3f, _script.Gaze.Pitch, 1f);
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

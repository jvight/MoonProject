using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// 07's wake as its power-on self-test: the fade to black, the lens stages, the pull-out, every check announced in
    /// order and never skipped, control held until the end, and the short wake that Continue plays instead.
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
            _script.SetEarthTarget(new Vector2(4f, 32f));
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
        public void NewGameFromTheTitle_HoldsPureBlackNoLongerThan1AndAHalf_BeforeTheLidOpens()
        {
            const float MaxToFirstLensImage = 3f;
            const float MaxPureBlack = 1.5f;
            _script.BeginFull(true);
            float elapsed = 0f;
            float blackStart = -1f;
            while (_script.Phase != OpeningPhase.LidCrack && elapsed < 2f * MaxToFirstLensImage)
            {
                StepOnce();
                elapsed += Frame;
                if (_script.Phase == OpeningPhase.Crackle && blackStart < 0f)
                {
                    blackStart = elapsed;
                }
            }

            Assert.AreEqual(OpeningPhase.LidCrack, _script.Phase, "The lens opens.");
            Assert.LessOrEqual(elapsed, MaxToFirstLensImage, "The first lens image comes within 3 s of the click.");
            Assert.Greater(blackStart, 0f, "O1 plays.");
            Assert.LessOrEqual(elapsed - blackStart, MaxPureBlack, "O1's pure black is at most 1.5 s.");
        }

        [Test]
        public void TheSelfTest_RunsTheLensStagesThePullOutTheChecksAndTheRelease_InOrder_InAboutThirtySeconds()
        {
            _script.BeginFull(false);
            TakeAll();
            float seconds = RunToChase(60f);

            Assert.That(seconds, Is.InRange(25f, 31f), "the self-test lasts 25-30 s");
            CollectionAssert.AreEqual(new[]
            {
                WakeBeat.Crackle, WakeBeat.LidCracked, WakeBeat.LidHalf, WakeBeat.FocusHunting, WakeBeat.FocusSettled,
                WakeBeat.PullingOut, WakeBeat.PulledOut, WakeBeat.OverShoulder, WakeBeat.ChaseFrame,
            }, _beats);
            Assert.IsFalse(_script.IsRunning);
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
            Assert.AreEqual(PostEventKind.Finished, _lines[_lines.Count - 1].Kind);
        }

        [Test]
        public void TheChecks_TakeNothingFromTheirTunedTime_TheStartCannotBeSkipped()
        {
            _script.BeginFull(false);
            TakeAll();
            float controlHeldUntil = 0f;
            float t = 0f;
            while (!_beats.Contains(WakeBeat.OverShoulder) && t < 60f)
            {
                StepOnce();
                t += Frame;
                if (_script.ControlHeld)
                {
                    controlHeldUntil = t;
                }
            }

            Assert.AreEqual(t, controlHeldUntil, 2f * Frame, "control is held right up to the end of the checks");
            Assert.IsTrue(_script.ControlHeld);
        }

        [Test]
        public void ControlComesBack_OnlyWhenTheCameraBeginsToSettleIntoTheChaseFrame()
        {
            _script.BeginFull(false);
            TakeAll();
            while (_script.Phase != OpeningPhase.ChaseEase && _script.IsRunning)
            {
                StepOnce();
                Assert.IsTrue(_script.ControlHeld, "held through the lens stages, the checks and the release");
            }

            StepOnce();
            Assert.AreEqual(OpeningPhase.ChaseEase, _script.Phase);
            Assert.IsFalse(_script.ControlHeld);
        }

        [Test]
        public void FromTheTitle_FadesToBlackFirst_AndThatBlackLeadsIntoO1()
        {
            _script.BeginFull(true);
            TakeAll();
            Assert.AreEqual(OpeningPhase.FadeOut, _script.Phase);
            Assert.AreEqual(1f, _script.Lid01, 1e-4f, "the fade starts on the open picture");

            while (!_beats.Contains(WakeBeat.Crackle))
            {
                StepOnce();
            }

            Assert.AreEqual(WakeBeat.FadedOut, _beats[0]);
            Assert.AreEqual(0f, _script.Lid01, 1e-3f, "the fade ends in black, then O1 begins");
        }

        [Test]
        public void TheLidOpensInStages_AndRestsHalfLiddedForTheChecks()
        {
            OpeningSettings settings = _tuning.Opening;
            _script.BeginFull(false);
            while (_script.IsRunning && _script.Phase != OpeningPhase.Post)
            {
                StepOnce();
                if (_script.Phase == OpeningPhase.Crackle)
                {
                    Assert.AreEqual(0f, _script.Lid01, 1e-5f, "O1 is black");
                }

                if (_script.Phase == OpeningPhase.LidCrack)
                {
                    Assert.LessOrEqual(_script.Lid01, settings.LidCrack + 1e-3f, "O2 is only a slit");
                }
            }

            for (int i = 0; i < 20; i++)
            {
                StepOnce();
            }

            Assert.AreEqual(settings.LidRest, _script.Lid01, 1e-3f, "the checks run half-lidded");
        }

        [Test]
        public void TheFocusHunts_ThenSettlesAtInfinity()
        {
            OpeningSettings settings = _tuning.Opening;
            _script.BeginFull(false);
            float nearest = float.MaxValue;
            float farthest = 0f;
            while (_script.IsRunning && _script.Phase != OpeningPhase.PullOut)
            {
                StepOnce();
                if (_script.Phase == OpeningPhase.Hunt)
                {
                    nearest = Mathf.Min(nearest, _script.FocusDistance);
                    farthest = Mathf.Max(farthest, _script.FocusDistance);
                }
            }

            Assert.Less(nearest, settings.FocusBlur, "the hunt goes too near");
            Assert.Greater(farthest, settings.FocusSettle, "then overshoots far");
            StepOnce();
            Assert.AreEqual(settings.FocusSettle, _script.FocusDistance, settings.FocusSettle * 0.25f);
        }

        [Test]
        public void TheLensCameraLetsGo_DuringThePullOut_AndTheCheckClockOnlyRunsInThePostPhase()
        {
            _script.BeginFull(false);
            bool sawLensGone = false;
            while (_script.IsRunning && _script.Phase != OpeningPhase.Post)
            {
                StepOnce();
                if (_script.Phase == OpeningPhase.PullOut && !_script.LensActive)
                {
                    sawLensGone = true;
                }
            }

            Assert.IsTrue(sawLensGone, "the lens view releases partway through the pull-out");
            Assert.AreEqual(OpeningPhase.Post, _script.Phase);
            Assert.AreEqual(BootCheckStep.Lens, _script.PostStep, "the checks begin with the lens");
        }

        [Test]
        public void BootReadout_ShowsFromTheHuntForAboutTwoSeconds_InsideTheLens()
        {
            _script.BeginFull(false);
            float first = -1f;
            float last = -1f;
            float t = 0f;
            while (_script.IsRunning)
            {
                StepOnce();
                t += Frame;
                if (_script.Readout01 > 0f)
                {
                    Assert.IsTrue(_script.InLens, "the readout belongs to the lens stages");
                    first = first < 0f ? t : first;
                    last = t;
                }
            }

            Assert.Greater(first, 0f, "the readout shows");
            Assert.LessOrEqual(last - first, _tuning.Opening.ReadoutSeconds + 0.1f);
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
            CollectionAssert.DoesNotContain(_beats, WakeBeat.Crackle, "Continue never plays the self-test");
            Assert.AreEqual(0, _lines.Count, "and prints none of its lines");
        }
    }
}

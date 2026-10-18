using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// 07's wake through its own eye (docs/design/cutscenes.md §1.2, §1.3, §1.6): the storyboard in order, the length
    /// guard, the hurry rules (chase frame within ~4 s, control never held over ~1.5 s, the blink never skipped), the
    /// photosensitivity rules for the flicker, the player finding Earth on their own, and the short wake.
    /// </summary>
    public sealed class OpeningScriptTests
    {
        private const float Frame = 1f / 60f;
        private const float MaxUntouched = 45f;
        private const float MaxToChase = 4f;
        private const float MaxHeld = 1.5f;

        private static readonly Vector2 HomeTarget = new Vector2(-50f, 2f);
        private static readonly Vector2 PilotTarget = new Vector2(40f, 8f);
        private static readonly Vector2 PeakTarget = new Vector2(10f, -10f);
        private static readonly Vector2 EarthTarget = new Vector2(4f, 32f);

        private RoverCameraTuning _tuning;
        private OpeningScript _script;
        private readonly List<WakeBeat> _beats = new List<WakeBeat>();

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _script = new OpeningScript(_tuning);
            _script.SetTargets(HomeTarget, PilotTarget, PeakTarget, EarthTarget);
            _beats.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void StepOnce(bool drive = false, Vector2 look = default)
        {
            _script.Step(Frame, look, drive);
            while (_script.TryTakeBeat(out WakeBeat beat))
            {
                _beats.Add(beat);
            }
        }

        /// <summary>Steps until the chase frame (or the cap); returns the seconds it took.</summary>
        private float RunToChase(float cap, bool drive = false)
        {
            float t = 0f;
            while (!_beats.Contains(WakeBeat.ChaseFrame) && t < cap)
            {
                StepOnce(drive);
                t += Frame;
            }

            return t;
        }

        [Test]
        public void NewGameFromTheTitle_ShowsThe_LensWithin3Seconds_AndHoldsPureBlackNoLongerThan1AndAHalf()
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
        public void Untouched_PlaysEveryBeatInOrder_WithinFortyFiveSeconds()
        {
            _script.BeginFull(false);
            TakeBeats();
            float seconds = RunToChase(60f);

            Assert.LessOrEqual(seconds, MaxUntouched, "the untouched opening stays at or under 45 s");
            Assert.AreEqual(19.2f, seconds, 0.35f, "the untouched opening is ~19 s after the tighter first stages");
            CollectionAssert.AreEqual(new[]
            {
                WakeBeat.Crackle, WakeBeat.LidCracked, WakeBeat.LidHalf, WakeBeat.FocusHunting, WakeBeat.FocusSettled,
                WakeBeat.Flicker, WakeBeat.GazeHome, WakeBeat.GazePilot, WakeBeat.GazePeak, WakeBeat.FarFocusSettled,
                WakeBeat.GazeEarth, WakeBeat.PullingOut, WakeBeat.PulledOut, WakeBeat.Blinked, WakeBeat.OverShoulder,
                WakeBeat.ChaseFrame,
            }, _beats);
            Assert.IsFalse(_script.IsRunning);
        }

        [Test]
        public void FromTheTitle_PushesIntoTheLensFirst_AndItsBlackIsO1()
        {
            _script.BeginFull(true);
            TakeBeats();
            Assert.AreEqual(OpeningPhase.PushIn, _script.Phase);
            Assert.AreEqual(1f, _script.Lid01, 1e-4f, "the push starts on the open picture");

            while (!_beats.Contains(WakeBeat.Crackle))
            {
                StepOnce();
            }

            Assert.AreEqual(WakeBeat.PushedIn, _beats[0]);
            Assert.AreEqual(0f, _script.Lid01, 1e-3f, "the push ends in black, which is O1");
        }

        [Test]
        public void TheLidOpensInStages_RestsHalfLidded_AndOpensWiderForEarth()
        {
            OpeningSettings settings = _tuning.Opening;
            _script.BeginFull(false);
            float peakBeforeEarth = 0f;
            float atEarth = 0f;
            while (_script.IsRunning && _script.Phase != OpeningPhase.PullOut)
            {
                StepOnce();
                if (_script.Phase == OpeningPhase.Crackle)
                {
                    Assert.AreEqual(0f, _script.Lid01, 1e-5f, "O1 is black");
                }

                if (_script.Phase < OpeningPhase.GazeEarth)
                {
                    peakBeforeEarth = Mathf.Max(peakBeforeEarth, _script.Lid01);
                }

                atEarth = _script.Lid01;
            }

            Assert.AreEqual(settings.LidRest, peakBeforeEarth, 0.03f, "the lid rests half-lidded before Earth");
            Assert.AreEqual(settings.LidWonder, atEarth, 0.01f, "wonder at Earth opens it wider");
        }

        [TestCase(0.5f)]
        [TestCase(4f)]
        [TestCase(7f)]
        [TestCase(12f)]
        [TestCase(18f)]
        [TestCase(27f)]
        [TestCase(30f)]
        [TestCase(34f)]
        [TestCase(37f)]
        [TestCase(41f)]
        public void DriveInput_ReachesTheChaseFrameSoon_HoldsControlBriefly_AndNeverSkipsTheBlink(float at)
        {
            _script.BeginFull(false);
            for (float t = 0f; t < at; t += Frame)
            {
                StepOnce();
            }

            float held = 0f;
            float waited = 0f;
            while (!_beats.Contains(WakeBeat.ChaseFrame) && waited < 10f)
            {
                StepOnce(true);
                waited += Frame;
                if (_script.ControlHeld)
                {
                    held = waited;
                }
            }

            Assert.LessOrEqual(waited, MaxToChase + 2f * Frame, "drive input reaches the chase frame within ~4 s");
            Assert.LessOrEqual(held, MaxHeld + 2f * Frame, "control is never held more than ~1.5 s after drive input");
            Assert.Contains(WakeBeat.Blinked, _beats, "the blink is never skipped");
            Assert.Less(_beats.IndexOf(WakeBeat.Blinked), _beats.IndexOf(WakeBeat.ChaseFrame));
        }

        [Test]
        public void Flicker_StaysUnderThirtyPercent_AndNothingStrobesAtThreeHertzOrMore()
        {
            _script.BeginFull(false);
            var samples = new List<float>();
            while (_script.IsRunning)
            {
                StepOnce();
                samples.Add(_script.Flicker01);
            }

            float lastPeak = float.NegativeInfinity;
            float lastTrough = 0f;
            for (int i = 1; i < samples.Count - 1; i++)
            {
                Assert.LessOrEqual(samples[i], 1f, "Flicker01 never asks for more than the lens view's 30% dip");
                bool peak = samples[i] > samples[i - 1] && samples[i] >= samples[i + 1] && samples[i] > 0.05f;
                bool trough = samples[i] < samples[i - 1] && samples[i] <= samples[i + 1];
                float time = i * Frame;
                if (trough)
                {
                    lastTrough = time;
                }

                if (!peak)
                {
                    continue;
                }

                Assert.GreaterOrEqual(time - lastPeak, 1f / 3f, "dips repeat slower than 3 Hz");
                Assert.GreaterOrEqual(time - lastTrough, 1f / 6f - Frame,
                    "each dip takes at least half a 3 Hz period to come in");
                lastPeak = time;
            }
        }

        [Test]
        public void LookingAtEarthOnYourOwn_SkipsAheadToO9()
        {
            _script.BeginFull(false);
            while (_script.Phase != OpeningPhase.Hunt)
            {
                StepOnce();
            }

            float waited = 0f;
            while (_script.Phase != OpeningPhase.GazeEarth && waited < 6f)
            {
                LensGaze gaze = _script.Gaze;
                Vector2 toEarth = new Vector2(EarthTarget.x - gaze.AimYaw, EarthTarget.y - gaze.AimPitch);
                StepOnce(false, Vector2.ClampMagnitude(toEarth, 1.5f));
                waited += Frame;
            }

            Assert.AreEqual(OpeningPhase.GazeEarth, _script.Phase, "holding Earth in view skips to O9");
            CollectionAssert.DoesNotContain(_beats, WakeBeat.GazeHome, "the attention list was skipped");
        }

        [Test]
        public void LookInput_PausesTheAttention_ThenResumesWithTheNextTargetNotYetSeen()
        {
            _script.BeginFull(false);
            while (_script.Phase != OpeningPhase.GazeHome)
            {
                StepOnce();
            }

            while (_script.Gaze.AngleTo(HomeTarget.x, HomeTarget.y) > _tuning.Opening.SeenAngle)
            {
                StepOnce();
            }

            float waited = 0f;
            while (waited < 3f)
            {
                LensGaze gaze = _script.Gaze;
                StepOnce(false, Vector2.ClampMagnitude(new Vector2(PilotTarget.x - gaze.AimYaw,
                    PilotTarget.y - gaze.AimPitch), 2f));
                waited += Frame;
            }

            for (float t = 0f; t < _tuning.Opening.AttentionResume + 6f && _script.Phase == OpeningPhase.GazeHome;
                t += Frame)
            {
                StepOnce();
            }

            Assert.AreEqual(OpeningPhase.GazePeak, _script.Phase,
                "home and the pilot were seen, so the attention resumes at The Peak");
        }

        [Test]
        public void ShortWake_StirsAndBlinks_ReachesTheChaseWithinThreeSeconds_AndGlancesAtEarth()
        {
            _script.BeginShort();
            TakeBeats();
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
            CollectionAssert.DoesNotContain(_beats, WakeBeat.Crackle, "Continue never plays the opening");
        }

        [Test]
        public void BootReadout_ShowsForAboutTwoSecondsFromTheFlicker_AndNeverWhenHurried()
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
                    Assert.IsTrue(_script.Phase == OpeningPhase.Flicker || _script.Phase == OpeningPhase.GazeHome,
                        "the readout belongs to the flicker and the first look");
                    first = first < 0f ? t : first;
                    last = t;
                }
            }

            Assert.Greater(first, 0f, "the readout shows");
            Assert.AreEqual(_tuning.Opening.ReadoutSeconds, last - first, 0.1f);

            _script.BeginFull(false);
            while (_script.Phase != OpeningPhase.Flicker)
            {
                StepOnce();
            }

            StepOnce(true);
            for (float s = 0f; s < 3f; s += Frame)
            {
                StepOnce();
                Assert.AreEqual(0f, _script.Readout01, "a hurried wake skips the readout");
            }
        }

        [Test]
        public void TheBlinkTiltsTheHead_AndTheTiltEasesBackOut()
        {
            _script.BeginFull(false);
            float most = 0f;
            while (_script.IsRunning)
            {
                StepOnce();
                most = Mathf.Max(most, _script.HeadTilt);
            }

            Assert.AreEqual(_tuning.Opening.BlinkTilt, most, 0.05f);
            for (float t = 0f; t < 5f; t += Frame)
            {
                StepOnce();
            }

            Assert.AreEqual(0f, _script.HeadTilt, 1e-4f);
            Assert.IsFalse(_script.TiltActive);
        }

        private void TakeBeats()
        {
            while (_script.TryTakeBeat(out WakeBeat beat))
            {
                _beats.Add(beat);
            }
        }
    }
}

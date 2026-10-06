using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class WakeUpSequenceTests
    {
        private const float Frame = 1f / 60f;
        private RoverCharacterTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private static MoodTransition Run(WakeUpSequence wake, float seconds, bool playerActive)
        {
            MoodTransition last = MoodTransition.None;
            for (float t = 0f; t < seconds; t += Frame)
            {
                MoodTransition step = wake.Step(playerActive, Frame);
                last = step != MoodTransition.None ? step : last;
            }

            return last;
        }

        [Test]
        public void StartingAwake_StaysAwake()
        {
            var wake = new WakeUpSequence(_tuning, false);
            Assert.AreEqual(WakePhase.Awake, wake.Phase);
            Assert.AreEqual(MoodTransition.None, Run(wake, 10f, false));
            Assert.AreEqual(0f, wake.Sleep);
        }

        [Test]
        public void Asleep_WaitsForTheSceneToSettle_ThenBeginsWaking()
        {
            var wake = new WakeUpSequence(_tuning, true);
            Assert.AreEqual(1f, wake.Sleep);
            Run(wake, _tuning.WakeDelay - 0.1f, false);
            Assert.AreEqual(WakePhase.Asleep, wake.Phase);

            MoodTransition began = Run(wake, 0.2f, false);
            Assert.AreEqual(MoodTransition.BeganWaking, began);
            Assert.AreEqual(WakePhase.Waking, wake.Phase);
            Assert.IsFalse(wake.WokenByPlayer);
        }

        [Test]
        public void LoadingHitch_DoesNotSkipTheQuietMoment()
        {
            var wake = new WakeUpSequence(_tuning, true);
            Assert.AreEqual(MoodTransition.None, wake.Step(false, 5f));
            Assert.AreEqual(WakePhase.Asleep, wake.Phase);
        }

        [Test]
        public void SlowWake_OpensTheEye_GlancesAtEarth_ThenSettles()
        {
            var wake = new WakeUpSequence(_tuning, true);
            Run(wake, _tuning.WakeDelay + Frame, false);
            float previousSleep = wake.Sleep;
            float peakGlance = 0f;
            float sleepWhenGlancePeaked = 1f;
            MoodTransition finished = MoodTransition.None;
            for (float t = 0f; t < _tuning.WakeDuration + 0.5f; t += Frame)
            {
                MoodTransition step = wake.Step(false, Frame);
                finished = step == MoodTransition.FinishedWaking ? step : finished;
                Assert.LessOrEqual(wake.Sleep, previousSleep + 1e-5f, "The eye only ever opens while waking.");
                Assert.Less(previousSleep - wake.Sleep, 0.05f, "The eye never pops open.");
                previousSleep = wake.Sleep;
                if (wake.EarthLook > peakGlance)
                {
                    peakGlance = wake.EarthLook;
                    sleepWhenGlancePeaked = wake.Sleep;
                }
            }

            Assert.AreEqual(MoodTransition.FinishedWaking, finished);
            Assert.AreEqual(WakePhase.Awake, wake.Phase);
            Assert.Greater(peakGlance, 0.95f, "07 looks up at Earth while waking.");
            Assert.Less(sleepWhenGlancePeaked, 0.1f, "It looks at Earth with its eye open.");
            Assert.AreEqual(0f, wake.EarthLook);
            Assert.AreEqual(0f, wake.Sleep);
        }

        [Test]
        public void DrivingWhileAsleep_WakesQuicklyButEased()
        {
            var wake = new WakeUpSequence(_tuning, true);
            Run(wake, 0.3f, false);
            Assert.AreEqual(MoodTransition.BeganWaking, wake.Step(true, Frame));
            Assert.IsTrue(wake.WokenByPlayer);
            Assert.Greater(wake.Sleep, 0.5f, "No snap on the first frame.");

            MoodTransition finished = Run(wake, 1.5f, true);
            Assert.AreEqual(MoodTransition.FinishedWaking, finished);
            Assert.AreEqual(WakePhase.Awake, wake.Phase);
        }

        [Test]
        public void DrivingDuringASlowWake_HurriesTheRest()
        {
            var wake = new WakeUpSequence(_tuning, true);
            Run(wake, _tuning.WakeDelay + _tuning.WakeDuration * 0.4f, false);
            Assert.AreEqual(WakePhase.Waking, wake.Phase);
            float before = wake.Sleep;
            wake.Step(true, Frame);
            Assert.Less(Mathf.Abs(before - wake.Sleep), 0.2f, "Hurrying continues from where it was.");
            Run(wake, 1.5f, true);
            Assert.AreEqual(WakePhase.Awake, wake.Phase);
            Assert.IsFalse(wake.WokenByPlayer, "07 had already started waking on its own.");
        }

        [Test]
        public void Mood_AsleepHasAShutDarkEyeAndBowedHead_AndDoesNotDaydream()
        {
            var mood = new RoverMood(_tuning, 7u, true);
            Assert.AreEqual(1f, mood.LidClosure, 1e-4f);
            Assert.AreEqual(0f, mood.EyeGlow, 1e-4f);
            Assert.AreEqual(-_tuning.SleepHeadBow, mood.HeadPitchOffset, 1e-3f);

            MoodTransition began = MoodTransition.None;
            for (float t = 0f; t < _tuning.WakeDelay + 0.1f; t += Frame)
            {
                MoodTransition step = mood.Step(0f, 0f, Frame);
                began = step != MoodTransition.None ? step : began;
            }

            Assert.AreEqual(MoodTransition.BeganWaking, began);
            Assert.AreEqual(0f, mood.Idle, 1e-4f, "Sleeping is not daydreaming.");
        }

        [Test]
        public void Mood_AfterWaking_SettlesToTheRestingFace_ThenDaydreamsLater()
        {
            var mood = new RoverMood(_tuning, 7u, true);
            bool finished = false;
            for (float t = 0f; t < _tuning.WakeDelay + _tuning.WakeDuration + 0.2f; t += Frame)
            {
                finished |= mood.Step(0f, 0f, Frame) == MoodTransition.FinishedWaking;
            }

            Assert.IsTrue(finished);
            Assert.Greater(mood.Blink, 0f, "Waking ends with a slow, sleepy blink.");
            for (float t = 0f; t < _tuning.BlinkDuration + 0.1f; t += Frame)
            {
                mood.Step(0f, 0f, Frame);
            }

            Assert.IsFalse(mood.IsDaydreaming, "The daydream clock starts once 07 is awake.");
            Assert.Greater(mood.EyeGlow, 0.8f);
            Assert.AreEqual(_tuning.ActiveLid, mood.LidClosure, 0.02f, "Resting half-lid.");
            for (float t = 0f; t < _tuning.IdleDelay; t += Frame)
            {
                mood.Step(0f, 0f, Frame);
            }

            Assert.IsTrue(mood.IsDaydreaming);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    /// <summary>Stillness, the servos and the cooling ticks behind the soundscape of solitude.</summary>
    public sealed class SolitudePartsTests
    {
        private const float Frame = 1f / 60f;

        private SoundscapeTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<SoundscapeTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        /// <summary>Feeds the tracker as the Rover would: seconds of rest counting up, or 0 while 07 moves.</summary>
        private static void Run(StillnessTracker tracker, float seconds, bool resting)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                tracker.Step(resting ? tracker.StillSeconds + Frame : 0f, Frame);
            }
        }

        [Test]
        public void Stillness_WaitsOutAShortStop_ThenSettlesOverAboutSixSeconds()
        {
            var still = new StillnessTracker(_tuning);
            Run(still, _tuning.StillDelay - 0.1f, true);
            Assert.AreEqual(0f, still.Amount, "a short stop is not stillness");

            Run(still, 6f - _tuning.StillDelay + 0.1f, true);
            Assert.Greater(still.Amount, 0.9f, "settled about six seconds after stopping");
            Assert.AreEqual(6f, still.StillSeconds, 0.05f);
        }

        [Test]
        public void Stillness_FallsBackWithinASecond_WhenTheRoverSaysItMoved()
        {
            var still = new StillnessTracker(_tuning);
            Run(still, 10f, true);
            Run(still, 1f, false);
            Assert.Less(still.Amount, 0.05f, "moving restores the world within a second");
            Assert.AreEqual(0f, still.StillSeconds);
        }

        [Test]
        public void Stillness_AlreadyLong_SettlesSmoothly_NeverJumps()
        {
            var still = new StillnessTracker(_tuning);
            still.Step(30f, Frame);
            Assert.Less(still.Amount, 0.05f, "waking after a long rest eases in");
            Run(still, 6f, true);
            Assert.Greater(still.Amount, 0.9f);
        }

        [Test]
        public void Servo_IsSilentBelowItsDeadRate_FullAtItsFullRate_AndSpinsDownAfter()
        {
            var servo = new ServoWhir();
            for (int i = 0; i < 60; i++)
            {
                servo.Step(10f, Frame, 25f, 160f, 0.05f, 0.15f);
            }

            Assert.AreEqual(0f, servo.Amount, "idle sway stays quiet");
            for (int i = 0; i < 60; i++)
            {
                servo.Step(200f, Frame, 25f, 160f, 0.05f, 0.15f);
            }

            Assert.AreEqual(1f, servo.Amount, 1e-3f);
            for (int i = 0; i < 60; i++)
            {
                servo.Step(0f, Frame, 25f, 160f, 0.05f, 0.15f);
            }

            Assert.AreEqual(0f, servo.Amount, 1e-3f);
        }

        [Test]
        public void Servo_AngularRate_IsDegreesPerSecond()
        {
            Vector3 a = Vector3.forward;
            Vector3 b = Quaternion.Euler(0f, 30f, 0f) * Vector3.forward;
            Assert.AreEqual(60f, ServoWhir.AngularRate(a, b, 0.5f), 1e-3f);
            Assert.AreEqual(0f, ServoWhir.AngularRate(a, b, 0f), "a paused frame turns nothing");

            Vector3 tiny = Quaternion.Euler(0.05f, 0f, 0f) * Vector3.forward;
            Assert.AreEqual(50f, ServoWhir.AngularRate(a, tiny, 0.001f), 0.5f,
                "a thousand frames a second still hear a fast look");
        }

        private static int Cool(MotorCooling cooling, float seconds, out float firstTick, out float lastVolume,
            out float firstVolume)
        {
            int ticks = 0;
            firstTick = -1f;
            firstVolume = 0f;
            lastVolume = 0f;
            for (float t = 0f; t < seconds; t += Frame)
            {
                if (cooling.Step(Frame, 0f, false, out float volume))
                {
                    if (ticks == 0)
                    {
                        firstTick = t;
                        firstVolume = volume;
                    }

                    ticks++;
                    lastVolume = volume;
                }
            }

            return ticks;
        }

        private static void Drive(MotorCooling cooling, float seconds, float effort)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                Assert.IsFalse(cooling.Step(Frame, effort, true, out _), "no ticks while driving");
            }
        }

        [Test]
        public void ACold07_NeverTicks()
        {
            var cooling = new MotorCooling(_tuning, new AudioRandom(3u));
            Assert.AreEqual(0, Cool(cooling, 60f, out _, out _, out _));
        }

        [Test]
        public void AfterALongDrive_TheMetalTicksAFewSecondsLater_SparserAndSofterAsItCools_ThenSettles()
        {
            var cooling = new MotorCooling(_tuning, new AudioRandom(3u));
            Drive(cooling, 60f, 1f);
            Assert.Greater(cooling.Heat, 0.9f);

            int early = Cool(cooling, 20f, out float firstTick, out float lateVolume, out float firstVolume);
            Assert.AreEqual(_tuning.TickDelay, firstTick, 2f * Frame, "a moment after the motor stops");
            Assert.Greater(early, 5, "hot metal ticks often");
            Assert.Less(lateVolume, firstVolume, "cooler ticks are softer");

            Cool(cooling, 40f, out _, out _, out _);
            int later = Cool(cooling, 20f, out _, out _, out _);
            Assert.Less(later, early / 2, "sparser as it cools");

            Cool(cooling, 120f, out _, out _, out _);
            Assert.Less(cooling.Heat, _tuning.TickMinHeat);
            Assert.AreEqual(0, Cool(cooling, 30f, out _, out _, out _), "settled: silence");
        }

        [Test]
        public void DrivingOff_StopsTheTicking_AndTheNextStopWaitsAgain()
        {
            var cooling = new MotorCooling(_tuning, new AudioRandom(5u));
            Drive(cooling, 60f, 1f);
            Assert.Greater(Cool(cooling, 10f, out _, out _, out _), 0);

            Drive(cooling, 1f, 0.5f);
            Cool(cooling, _tuning.TickDelay + 0.5f, out float firstTick, out _, out _);
            Assert.AreEqual(_tuning.TickDelay, firstTick, 2f * Frame);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class WakeAndRecoveryTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void RadioWakeUp_IsSilentAsleep_CracklesOn_ThenResolvesIntoMusic()
        {
            var wake = new RadioWakeUp();
            Assert.IsFalse(wake.IsAwake);
            Assert.AreEqual(0f, wake.Power);
            Assert.AreEqual(0f, wake.MusicGain);
            Assert.AreEqual(0f, wake.CrackleBoost);
            Assert.IsFalse(wake.Step(10f), "nothing starts while asleep");

            wake.Begin(1f, 2f, 0.3f);
            int starts = 0;
            float t = 0f;
            float musicStartTime = -1f;
            while (t < 4f)
            {
                if (wake.Step(Frame))
                {
                    starts++;
                    musicStartTime = t;
                }

                t += Frame;
                if (t > 0.35f && t < 0.95f)
                {
                    Assert.AreEqual(1f, wake.Power, 1e-5f, "powered on");
                    Assert.AreEqual(0f, wake.MusicGain, "static only before the music");
                    Assert.AreEqual(1f, wake.CrackleBoost, 1e-5f);
                }
            }

            Assert.AreEqual(1, starts);
            Assert.AreEqual(1f, musicStartTime, 2f * Frame);
            Assert.AreEqual(1f, wake.MusicGain, 1e-5f);
            Assert.AreEqual(0f, wake.CrackleBoost, 1e-5f);
        }

        [Test]
        public void RadioWakeUp_OnlyWakesOnce()
        {
            var wake = new RadioWakeUp();
            wake.Begin(0.5f, 1f, 0.1f);
            for (int i = 0; i < 200; i++)
            {
                wake.Step(Frame);
            }

            wake.Begin(5f, 5f, 5f);
            Assert.AreEqual(1f, wake.MusicGain, 1e-5f, "a second wake does not restart the radio");
            Assert.AreEqual(1f, wake.Power, 1e-5f);
        }

        [Test]
        public void RecoveryLift_SwellsRisesAndSettlesExactlyAtTheEnd()
        {
            var lift = new RecoveryLift();
            Assert.IsFalse(lift.Active);
            Assert.AreEqual(0f, lift.Gain);

            lift.Begin(2f, 0.4f, 0.35f);
            Assert.AreEqual(0f, lift.Gain, "starts from silence");
            Assert.AreEqual(1f, lift.PitchFactor(2f), 1e-6f);

            float t = 0f;
            int settles = 0;
            float peakGain = 0f;
            while (t < 2.5f)
            {
                settles += lift.Step(Frame) ? 1 : 0;
                t += Frame;
                peakGain = Mathf.Max(peakGain, lift.Gain);
            }

            Assert.AreEqual(1, settles);
            Assert.IsFalse(lift.Active);
            Assert.AreEqual(1f, peakGain, 1e-5f);
            Assert.AreEqual(Mathf.Pow(2f, 2f / 12f), lift.PitchFactor(2f), 1e-5f, "lands a whole tone up (D to E)");
        }

        [Test]
        public void RecoveryLift_ShortLiftsStillFadeBothWays()
        {
            var lift = new RecoveryLift();
            lift.Begin(0.2f, 0.4f, 0.35f);
            lift.Step(0.1f);
            Assert.AreEqual(1f, lift.Gain, 1e-4f, "fades are capped at half of a short lift");
            Assert.IsTrue(lift.Step(0.1f));
        }

        [Test]
        public void RoverHum_IsSilentAsleep_AndPowersUpFromTheWakePitch()
        {
            var tuning = ScriptableObject.CreateInstance<RoverAudioTuning>();
            try
            {
                var model = new RoverAudioModel(tuning);
                var idle = new RoverAudioInput(0f, 0f, true, Vector3.up);
                for (int i = 0; i < 300; i++)
                {
                    model.Step(Frame, idle);
                }

                Assert.AreEqual(0f, model.HumVolume, 1e-6f, "asleep: no motor");

                model.NotifyAwoke();
                Assert.AreEqual(tuning.WakeStartPitch, model.HumPitch, 1e-6f);
                model.Step(Frame, idle);
                Assert.Greater(model.HumVolume, 0f);
                for (int i = 0; i < 600; i++)
                {
                    model.Step(Frame, idle);
                }

                Assert.AreEqual(tuning.IdlePitch, model.HumPitch, 1e-3f);
                Assert.AreEqual(tuning.IdleVolume, model.HumVolume, 1e-3f);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}

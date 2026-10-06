using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class RoverAudioModelTests
    {
        private const float Frame = 1f / 60f;

        private RoverAudioTuning _tuning;
        private AudioMixTuning _mix;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverAudioTuning>();
            _mix = ScriptableObject.CreateInstance<AudioMixTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_mix);
        }

        private static float Run(RoverAudioModel model, RoverAudioInput input, int frames)
        {
            float creaks = 0f;
            for (int i = 0; i < frames; i++)
            {
                creaks += model.Step(Frame, input);
            }

            return creaks;
        }

        [Test]
        public void Hum_StartsSilent_SettlesAtIdle_AndRisesWithSpeed()
        {
            var model = new RoverAudioModel(_tuning);
            Assert.AreEqual(0f, model.HumVolume);
            model.NotifyAwoke();

            Run(model, new RoverAudioInput(0f, 0f, true, Vector3.up), 300);
            Assert.AreEqual(_tuning.IdlePitch, model.HumPitch, 1e-3f);
            Assert.AreEqual(_tuning.IdleVolume, model.HumVolume, 1e-3f);

            Run(model, new RoverAudioInput(1f, 0f, true, Vector3.up), 300);
            Assert.AreEqual(_tuning.TopSpeedPitch, model.HumPitch, 1e-3f);
            Assert.AreEqual(_tuning.TopSpeedVolume, model.HumVolume, 1e-3f);
        }

        [Test]
        public void Hum_EasesInsteadOfSnapping()
        {
            var model = new RoverAudioModel(_tuning);
            model.NotifyAwoke();
            Run(model, new RoverAudioInput(0f, 0f, true, Vector3.up), 300);

            model.Step(Frame, new RoverAudioInput(1f, 1f, true, Vector3.up));

            Assert.Less(model.HumPitch - _tuning.IdlePitch, 0.1f * (_tuning.TopSpeedPitch - _tuning.IdlePitch));
        }

        [Test]
        public void Crunch_FollowsSpeedOnlyWhileGrounded()
        {
            var model = new RoverAudioModel(_tuning);
            Run(model, new RoverAudioInput(1f, 1f, true, Vector3.up), 120);
            Assert.AreEqual(_tuning.CrunchMaxVolume, model.CrunchVolume, 1e-3f);

            Run(model, new RoverAudioInput(1f, 1f, false, Vector3.up), 60);
            Assert.Less(model.CrunchVolume, 1e-3f);

            Run(model, new RoverAudioInput(0f, 0f, true, Vector3.up), 120);
            Assert.Less(model.CrunchVolume, 1e-3f);
        }

        [Test]
        public void Landing_CreaksOnceAfterTheDelay_SoftLandingsStayQuiet()
        {
            var model = new RoverAudioModel(_tuning);
            var resting = new RoverAudioInput(0f, 0f, true, Vector3.up);

            model.NotifyLanding(_tuning.LandingCreakMinImpact * 0.5f);
            Assert.AreEqual(0f, Run(model, resting, 60));

            model.NotifyLanding(_tuning.LandingCreakFullImpact);
            Assert.AreEqual(0f, model.Step(_tuning.LandingCreakDelay * 0.5f, resting));
            float creak = Run(model, resting, 60);
            Assert.AreEqual(1f, creak, 1e-5f);
        }

        [Test]
        public void FastGroundNormalChanges_Creak_WithCooldown()
        {
            var model = new RoverAudioModel(_tuning);
            Run(model, new RoverAudioInput(0.5f, 0.5f, true, Vector3.up), 10);

            Vector3 tilted = Quaternion.Euler(10f, 0f, 0f) * Vector3.up;
            float first = model.Step(Frame, new RoverAudioInput(0.5f, 0.5f, true, tilted));
            float second = model.Step(Frame, new RoverAudioInput(0.5f, 0.5f, true, Vector3.up));

            Assert.Greater(first, 0f);
            Assert.AreEqual(0f, second);
        }

        [Test]
        public void SlowSlopeChanges_DoNotCreak()
        {
            var model = new RoverAudioModel(_tuning);
            float creaks = 0f;
            for (int i = 0; i < 120; i++)
            {
                Vector3 normal = Quaternion.Euler(i * 0.1f, 0f, 0f) * Vector3.up;
                creaks += model.Step(Frame, new RoverAudioInput(0.5f, 0.5f, true, normal));
            }

            Assert.AreEqual(0f, creaks);
        }

        [Test]
        public void Thump_IsSilentBelowThreshold_LouderAndLowerForHarderImpacts()
        {
            Assert.IsFalse(ImpactSound.ForLanding(_mix.ThumpMinImpact * 0.5f, _mix).Audible);

            ImpactSound soft = ImpactSound.ForLanding(_mix.ThumpMinImpact, _mix);
            ImpactSound hard = ImpactSound.ForLanding(_mix.ThumpFullImpact * 2f, _mix);

            Assert.IsTrue(soft.Audible);
            Assert.AreEqual(_mix.ThumpSoftVolume, soft.Volume, 1e-5f);
            Assert.AreEqual(_mix.ThumpHardVolume, hard.Volume, 1e-5f);
            Assert.Less(hard.Pitch, soft.Pitch);
        }
    }
}

using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    public sealed class FriendVoiceModelTests
    {
        private const float Frame = 1f / 60f;

        private FriendAudioTuning _tuning;
        private FriendVoiceModel _model;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<FriendAudioTuning>();
            _model = new FriendVoiceModel(_tuning, new AudioRandom(9u));
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private int Run(float seconds, FriendActivity activity, float rotor, out FriendMood lastMood)
        {
            int chirps = 0;
            lastMood = FriendMood.Curious;
            for (float t = 0f; t < seconds; t += Frame)
            {
                if (_model.Step(Frame, activity, rotor, out FriendMood mood))
                {
                    chirps++;
                    lastMood = mood;
                }
            }

            return chirps;
        }

        [Test]
        public void Dormant_IsSilent_NoRotor_NoStitch_NoChirps()
        {
            Assert.AreEqual(0, Run(120f, FriendActivity.Dormant, 0f, out _));
            Assert.AreEqual(0f, _model.RotorVolume);
            Assert.IsFalse(_model.StitchAudible);
            Assert.IsFalse(_model.TakeBoot());
        }

        [Test]
        public void Rotor_SpinsUpWithEffort_PitchRises_AndWindsDownToSilence()
        {
            Run(0.05f, FriendActivity.Following, 0.4f, out _);
            float early = _model.RotorVolume;
            Run(5f, FriendActivity.Following, 0.4f, out _);
            float hover = _model.RotorPitch;
            Assert.Greater(_model.RotorVolume, early, "spin-up eases in");
            Run(5f, FriendActivity.Following, 1f, out _);
            Assert.Greater(_model.RotorPitch, hover, "dashing is higher");
            Assert.AreEqual(_tuning.RotorDashPitch, _model.RotorPitch, 1e-3f);

            Run(10f, FriendActivity.Napping, 0f, out _);
            Assert.AreEqual(0f, _model.RotorVolume, "rotors stopped on the perch");
        }

        [Test]
        public void Stitching_IsHeardWhileTheBeamStitches_AndRisesOverTheStitchTime()
        {
            Run(0.6f, FriendActivity.Repairing, 0f, out _);
            Assert.IsTrue(_model.StitchAudible);
            float earlyGain = _model.StitchGain;
            float earlyPitch = _model.StitchPitch;

            Run(_tuning.StitchRiseTime, FriendActivity.Repairing, 0f, out _);
            Assert.Greater(_model.StitchGain, earlyGain);
            Assert.Greater(_model.StitchPitch, earlyPitch);
            Assert.AreEqual(Mathf.Pow(2f, _tuning.StitchRiseSemitones / 12f), _model.StitchPitch, 1e-4f,
                "lands exactly on the in-key interval and holds");
            Assert.IsFalse(_model.TakeBoot(), "no boot while still stitching");
        }

        [Test]
        public void Boot_IsDueOnceWhenTheRotorsStartDuringTheRepair_AndTheStitchingFades()
        {
            Run(2f, FriendActivity.Repairing, 0f, out _);
            _model.Step(Frame, FriendActivity.Repairing, 0.05f, out _);
            Assert.IsTrue(_model.TakeBoot());
            Assert.IsFalse(_model.TakeBoot(), "taken once");

            Run(_tuning.StitchFadeOut + 0.1f, FriendActivity.Repairing, 0.3f, out _);
            Assert.IsFalse(_model.StitchAudible);
            Assert.IsFalse(_model.TakeBoot(), "one boot per repair");
            Assert.AreEqual(0, Run(2f, FriendActivity.Repairing, 0.4f, out _), "no ambient chirps while repairing");
        }

        [Test]
        public void FinishStitching_EndsTheStitchingWithoutABoot_UntilTheRepairEnds()
        {
            Run(2f, FriendActivity.Repairing, 0f, out _);
            _model.FinishStitching();
            Run(_tuning.StitchFadeOut + 0.1f, FriendActivity.Repairing, 0f, out _);
            Assert.IsFalse(_model.StitchAudible, "the tape went in: the beam's stitching fades");
            Assert.IsFalse(_model.TakeBoot(), "her own cue stands in for the shared boot");

            Run(1f, FriendActivity.Repairing, 0.3f, out _);
            Assert.IsFalse(_model.TakeBoot(), "not even when her legs start moving");

            Run(1f, FriendActivity.Home, 0f, out _);
            Run(0.6f, FriendActivity.Repairing, 0f, out _);
            Assert.IsTrue(_model.StitchAudible, "a later repair stitches again");
        }

        [Test]
        public void ASecondRepair_StitchesFromTheStartAgain()
        {
            Run(4f, FriendActivity.Repairing, 0f, out _);
            Run(1f, FriendActivity.Repairing, 0.4f, out _);
            _model.TakeBoot();
            Run(3f, FriendActivity.Home, 0.4f, out _);

            Run(0.3f, FriendActivity.Repairing, 0f, out _);
            Assert.Less(_model.StitchPitch, 1.1f, "restarts low");
            Run(1f, FriendActivity.Repairing, 0.4f, out _);
            Assert.IsTrue(_model.TakeBoot());
        }

        [TestCase(FriendActivity.Following, FriendMood.Curious)]
        [TestCase(FriendActivity.Spotting, FriendMood.Curious)]
        [TestCase(FriendActivity.Home, FriendMood.Happy)]
        [TestCase(FriendActivity.Napping, FriendMood.Sleepy)]
        public void AmbientChirps_FitTheActivity_AndStayOccasional(FriendActivity activity, FriendMood expected)
        {
            int chirps = Run(120f, activity, activity == FriendActivity.Napping ? 0f : 0.4f, out FriendMood mood);
            Assert.AreEqual(expected, mood);
            Assert.That(chirps, Is.InRange(3, 25), "now and then, never chatter");
        }
    }
}

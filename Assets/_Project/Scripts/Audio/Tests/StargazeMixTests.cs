using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class StargazeMixTests
    {
        private const float Frame = 1f / 60f;

        private SoundscapeTuning _tuning;
        private StargazeMix _mix;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<SoundscapeTuning>();
            _mix = new StargazeMix(_tuning);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _mix.Step(Frame);
            }
        }

        [Test]
        public void TheMusic_IsUntouched_UntilStargazing()
        {
            Run(1f);
            Assert.AreEqual(0f, _mix.Amount);
            Assert.AreEqual(1f, _mix.MusicGain);
            Assert.AreEqual(5000f, _mix.MusicCutoff(5000f));
        }

        [Test]
        public void Stargazing_ThinsTheMusicOverItsTime_ThenItComesBackFaster()
        {
            Assert.IsTrue(_mix.SetGazing(true), "the beat begins with a swell");
            Run(_tuning.StargazeThinTime * 0.5f);
            Assert.Greater(_mix.Amount, 0.2f, "easing out");
            Assert.Less(_mix.Amount, 0.8f, "not a snap");
            Run(_tuning.StargazeThinTime * 0.5f + 2f * Frame);
            Assert.AreEqual(1f, _mix.Amount, 1e-4f);
            Assert.AreEqual(SoundscapeModel.FromDb(_tuning.StargazeMusicDb), _mix.MusicGain, 1e-4f,
                "a few notes over the wind");
            Assert.AreEqual(_tuning.StargazeCutoff, _mix.MusicCutoff(8000f), 1f, "only the soft low notes");
            Assert.AreEqual(500f, _mix.MusicCutoff(500f), 1e-3f, "an already thinner radio stays as it is");

            Assert.IsFalse(_mix.SetGazing(false), "no swell on the way out");
            Assert.Less(_tuning.StargazeReturnTime, _tuning.StargazeThinTime, "the music returns sooner");
            Run(_tuning.StargazeReturnTime + 2f * Frame);
            Assert.AreEqual(0f, _mix.Amount);
            Assert.AreEqual(1f, _mix.MusicGain);
        }

        [Test]
        public void TheSwell_PlaysOncePerBeat_AndNotAgainWithinItsRest()
        {
            Assert.IsTrue(_mix.SetGazing(true));
            Assert.IsFalse(_mix.SetGazing(true), "the same state again is not a new beat");
            Run(3f);
            _mix.SetGazing(false);
            Run(1f);
            Assert.IsFalse(_mix.SetGazing(true), "a beat restarting soon after stays quiet");
            Assert.IsTrue(_mix.Gazing, "but the music still thins");
            _mix.SetGazing(false);
            Run(_tuning.StargazeSwellRest);
            Assert.IsTrue(_mix.SetGazing(true), "a later beat swells again");
        }
    }
}

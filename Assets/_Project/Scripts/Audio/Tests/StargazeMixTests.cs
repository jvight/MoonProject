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
        public void Stargazing_StartsWithASwell_AndEndsWithout()
        {
            Assert.IsFalse(_mix.Gazing);
            Assert.IsTrue(_mix.SetGazing(true), "the beat begins with a swell");
            Assert.IsTrue(_mix.Gazing);
            Run(1f);
            Assert.IsFalse(_mix.SetGazing(false), "no swell on the way out");
            Assert.IsFalse(_mix.Gazing);
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
            Assert.IsTrue(_mix.Gazing, "but the beat itself is on");
            _mix.SetGazing(false);
            Run(_tuning.StargazeSwellRest);
            Assert.IsTrue(_mix.SetGazing(true), "a later beat swells again");
        }
    }
}

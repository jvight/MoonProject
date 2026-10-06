using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class RoverMoodReactionTests
    {
        private const float Frame = 1f / 60f;
        private RoverCharacterTuning _tuning;
        private RoverMood _mood;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCharacterTuning>();
            _mood = new RoverMood(_tuning, 7u);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        private void Run(float seconds, float speed = 5f, float input = 1f)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _mood.Step(speed, input, Frame);
            }
        }

        [Test]
        public void ContentedNod_DipsTheHeadWithASoftBlinkAndComesBack()
        {
            Run(1f);
            _mood.NodContentedly(1f);
            float lowest = 0f;
            bool blinked = false;
            for (float t = 0f; t < 1.5f; t += Frame)
            {
                _mood.Step(5f, 1f, Frame);
                lowest = Mathf.Min(lowest, _mood.HeadPitchOffset);
                blinked |= _mood.Blink > 0.5f;
            }

            Assert.AreEqual(-_tuning.NodDepth, lowest, 0.6f);
            Assert.IsTrue(blinked, "A content nod comes with a slow blink.");
            Run(4f);
            Assert.AreEqual(0f, _mood.HeadPitchOffset, 0.1f);
        }

        [Test]
        public void Sigh_DroopsHeadAndLidAndOpensTheWingALittle()
        {
            Run(1f);
            float calmLid = _mood.LidClosure;
            _mood.Sigh(1f);
            Run(1f / (2f * Mathf.PI * _tuning.SighFrequency));
            Assert.Less(_mood.HeadPitchOffset, -0.8f * _tuning.SighHeadDrop);
            Assert.Greater(_mood.LidClosure, calmLid + 0.5f * _tuning.SighLidDroop);
            Assert.Greater(_mood.WingOpen, 0.02f, "Even while driving, a sigh lifts the tired wing a touch.");
            Run(12f);
            Assert.Less(_mood.Sighing, 0.02f);
            Assert.Less(_mood.WingOpen, 0.01f);
        }

        [Test]
        public void BlinkNow_StartsABlinkOnlyWhenNoneIsRunning()
        {
            Run(0.5f);
            _mood.BlinkNow();
            Run(_tuning.BlinkDuration * 0.5f);
            float mid = _mood.Blink;
            _mood.BlinkNow();
            _mood.Step(5f, 1f, Frame);
            Assert.Greater(mid, 0.9f);
            Assert.Greater(_mood.Blink, 0.8f, "A second request does not restart a running blink.");
        }

        [Test]
        public void SwellsNeverSnap()
        {
            _mood.PerkUp(1f);
            _mood.FeelImpact(1f);
            _mood.NodContentedly(1f);
            _mood.Sigh(1f);
            _mood.Step(5f, 1f, Frame);
            Assert.Less(_mood.Perk, 0.25f);
            Assert.Less(_mood.Oof, 0.35f);
            Assert.Less(_mood.Nodding, 0.3f);
            Assert.Less(_mood.Sighing, 0.1f);
        }
    }
}

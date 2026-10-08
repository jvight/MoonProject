using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>IRoverStillness: when 07 counts as resting, and what resets the count.</summary>
    public sealed class RoverStillnessTests
    {
        private const float Frame = 1f / 60f;

        private StillnessSettings _settings;
        private RoverStillness _stillness;

        [SetUp]
        public void SetUp()
        {
            _settings = new StillnessSettings();
            _stillness = new RoverStillness(_settings);
        }

        private static StillnessSample Resting()
        {
            return new StillnessSample(true, 0f, Vector2.zero, Vector2.zero, Vector2.zero, false);
        }

        private void Run(StillnessSample sample, float seconds)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Frame)
            {
                _stillness.Step(sample, Frame);
            }
        }

        [Test]
        public void Resting_CountsUpInGameSeconds()
        {
            Run(Resting(), 3f);
            Assert.AreEqual(3f, _stillness.StillSeconds, 0.02f);
        }

        [Test]
        public void SpeedThreshold_SeparatesCreepingFromMoving()
        {
            float limit = _settings.MaxSpeed;
            var creeping = new StillnessSample(true, 0.9f * limit, Vector2.zero, Vector2.zero, Vector2.zero, false);
            var moving = new StillnessSample(true, 1.1f * limit, Vector2.zero, Vector2.zero, Vector2.zero, false);
            Assert.IsTrue(RoverStillness.IsStill(_settings, creeping));
            Assert.IsFalse(RoverStillness.IsStill(_settings, moving));
            Run(Resting(), 2f);
            _stillness.Step(moving, Frame);
            Assert.AreEqual(0f, _stillness.StillSeconds);
        }

        [Test]
        public void DriveInput_InsideTheDeadZoneIsStickDrift_BeyondItResets()
        {
            float zone = _settings.DriveDeadZone;
            var drift = new StillnessSample(true, 0f, new Vector2(0f, 0.9f * zone), Vector2.zero, Vector2.zero, false);
            var pushed = new StillnessSample(true, 0f, new Vector2(1.2f * zone, 0f), Vector2.zero, Vector2.zero, false);
            Run(drift, 2f);
            Assert.Greater(_stillness.StillSeconds, 1.9f, "Stick drift does not count as driving.");
            _stillness.Step(pushed, Frame);
            Assert.AreEqual(0f, _stillness.StillSeconds, "Any real push on the stick resets it.");
        }

        [Test]
        public void LookInput_FromMouseOrStick_ResetsIt()
        {
            float mouse = _settings.LookMouseDeadZone;
            float stick = _settings.LookStickDeadZone;
            Run(Resting(), 2f);
            _stillness.Step(new StillnessSample(true, 0f, Vector2.zero, new Vector2(0.5f * mouse, 0f), Vector2.zero,
                false), Frame);
            Assert.Greater(_stillness.StillSeconds, 1.9f, "A resting hand's jitter is not looking around.");

            _stillness.Step(new StillnessSample(true, 0f, Vector2.zero, new Vector2(0f, 2f * mouse + 1f),
                Vector2.zero, false), Frame);
            Assert.AreEqual(0f, _stillness.StillSeconds, "Moving the mouse resets it.");

            Run(Resting(), 2f);
            _stillness.Step(new StillnessSample(true, 0f, Vector2.zero, Vector2.zero, new Vector2(1.5f * stick, 0f),
                false), Frame);
            Assert.AreEqual(0f, _stillness.StillSeconds, "Pushing the look stick resets it.");
        }

        [Test]
        public void Airborne_IsNeverStill()
        {
            var floating = new StillnessSample(false, 0f, Vector2.zero, Vector2.zero, Vector2.zero, false);
            Run(floating, 2f);
            Assert.AreEqual(0f, _stillness.StillSeconds);
        }

        [Test]
        public void Engaged_ARecoveryLiftJumpChargeOrHold_IsNeverStill()
        {
            var engaged = new StillnessSample(true, 0f, Vector2.zero, Vector2.zero, Vector2.zero, true);
            Run(Resting(), 2f);
            Run(engaged, 1f);
            Assert.AreEqual(0f, _stillness.StillSeconds);
        }

        [Test]
        public void Reset_StartsTheCountOver()
        {
            Run(Resting(), 3f);
            _stillness.Reset();
            Assert.AreEqual(0f, _stillness.StillSeconds);
            Run(Resting(), 1f);
            Assert.AreEqual(1f, _stillness.StillSeconds, 0.02f);
        }

        [Test]
        public void Paused_NoGameTime_HoldsTheCount()
        {
            Run(Resting(), 2f);
            float before = _stillness.StillSeconds;
            for (int i = 0; i < 30; i++)
            {
                _stillness.Step(Resting(), 0f);
            }

            Assert.AreEqual(before, _stillness.StillSeconds);
        }
    }
}

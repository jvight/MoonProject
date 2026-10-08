using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The Boost Coils: when they engage, how they ease, and the gentle extra cruise they give.</summary>
    public sealed class BoostDriveTests
    {
        private const float Step = 0.02f;
        private const float Top = 8f;

        private BoostSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new BoostSettings();
        }

        private static BoostSample Cruising(bool owned = true, float throttle = 1f, float steer = 0f, float speed = Top,
            float slope = 2f, bool free = true)
        {
            return new BoostSample(owned, throttle, steer, speed, Top, slope, free);
        }

        [Test]
        public void Wants_CruiseOnOpenFlatGround_WithDriveHeld()
        {
            Assert.IsTrue(BoostDrive.Wants(_settings, Cruising()));
            Assert.IsFalse(BoostDrive.Wants(_settings, Cruising(owned: false)), "Not without the coils.");
            Assert.IsFalse(BoostDrive.Wants(_settings, Cruising(throttle: 0.6f)), "Not without Drive held.");
            Assert.IsFalse(BoostDrive.Wants(_settings, Cruising(steer: 0.6f)), "Not in a turn.");
            Assert.IsFalse(BoostDrive.Wants(_settings, Cruising(speed: 0.5f * Top)), "Not from a crawl.");
            Assert.IsFalse(BoostDrive.Wants(_settings, Cruising(slope: 15f)), "Not on a slope.");
            Assert.IsFalse(BoostDrive.Wants(_settings, Cruising(free: false)), "Not while held, leaping or lifted.");
        }

        [Test]
        public void Level_EasesIn_NeverJumps_AndSettlesOnTheExtraSpeed()
        {
            var boost = new BoostDrive(_settings);
            Assert.IsTrue(boost.Step(Cruising(), Step), "Engaging is announced.");
            float previous = boost.Level;
            float largest = previous;
            for (float t = 0f; t < 6f; t += Step)
            {
                Assert.IsFalse(boost.Step(Cruising(), Step));
                largest = Mathf.Max(largest, boost.Level - previous);
                previous = boost.Level;
            }

            Assert.Less(largest, 0.03f, "Eased in, a little per step.");
            Assert.AreEqual(_settings.ExtraSpeed, boost.ExtraSpeed, 0.02f);
            Assert.Less(_settings.ExtraSpeed, 0.3f * Top, "Never racy: a gentle extra cruise.");
        }

        [Test]
        public void SmallBumps_DoNotLetGo_ButASlopeOrATurnDoes()
        {
            var boost = new BoostDrive(_settings);
            for (float t = 0f; t < 2f; t += Step)
            {
                boost.Step(Cruising(), Step);
            }

            for (float t = 0f; t < 0.5f * _settings.ReleaseDelay; t += Step)
            {
                Assert.IsFalse(boost.Step(Cruising(slope: 12f), Step), "A bump: still boosting.");
            }

            boost.Step(Cruising(), Step);
            Assert.IsTrue(boost.Engaged);
            bool letGo = false;
            for (float t = 0f; t < 2f * _settings.ReleaseDelay; t += Step)
            {
                letGo |= boost.Step(Cruising(steer: 0.8f), Step);
            }

            Assert.IsTrue(letGo, "A real turn lets it go (announced).");
            Assert.IsFalse(boost.Engaged);
            float level = boost.Level;
            boost.Step(Cruising(steer: 0.8f), Step);
            Assert.Less(boost.Level, level, "Easing out.");
            Assert.Greater(boost.Level, 0.8f * level, "Gently.");
        }

        [Test]
        public void HeldOrLifted_LetsGoAtOnce()
        {
            var boost = new BoostDrive(_settings);
            boost.Step(Cruising(), Step);
            Assert.IsTrue(boost.Step(Cruising(free: false), Step));
            Assert.IsFalse(boost.Engaged);
        }
    }
}

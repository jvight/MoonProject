using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The reach ring (polish wave 1) rolls out once from its centre to its reach, eased out, fading in and away, and
    /// a new roll-out replaces the one in flight.
    /// </summary>
    public sealed class ReachRingTests
    {
        private const float Start = 5f;
        private const float Reach = 120f;

        private ReachRingSettings _settings;
        private ReachRing _ring;

        [SetUp]
        public void SetUp()
        {
            _settings = new ReachRingSettings();
            _ring = new ReachRing(_settings);
        }

        [Test]
        public void Ring_IsQuietUntilStarted_AndGoneAfterItsDuration()
        {
            Assert.IsFalse(_ring.Sample(Start, out _, out _), "no ring before a reach grows");
            _ring.Start(Vector3.zero, Reach, Start);
            Assert.IsTrue(_ring.Sample(Start + _settings.Duration * 0.5f, out _, out _));
            Assert.IsFalse(_ring.Sample(Start + _settings.Duration, out _, out _), "the ring is seen once");
        }

        [Test]
        public void Ring_RollsOutEasedOut_FadingInAndAway()
        {
            _ring.Start(Vector3.zero, Reach, Start);
            _ring.Sample(Start, out float firstRadius, out float firstStrength);
            Assert.AreEqual(0f, firstRadius, 1e-4f, "from the source");
            Assert.AreEqual(0f, firstStrength, 1e-4f, "fading in, never popping");
            _ring.Sample(Start + _settings.Duration * 0.5f, out float halfRadius, out float halfStrength);
            Assert.Greater(halfRadius, Reach * 0.5f, "quick at first, settling (eased out)");
            Assert.Greater(halfStrength, 0f);
            _ring.Sample(Start + _settings.Duration * 0.999f, out float lastRadius, out float lastStrength);
            Assert.AreEqual(Reach, lastRadius, Reach * 0.01f, "it reaches the new reach");
            Assert.Less(lastStrength, 0.05f, "and fades away there");
        }
    }
}

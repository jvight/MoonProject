using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// How the socket rack looks (<see cref="RackLook"/>): revealed live it rises, eased, then lights its bays left to
    /// right; a seen part's outline glows dim and breathes, a fitted part's brightens while its fill and lamp warm and
    /// its jaws close; a loaded rack stands lit at once.
    /// </summary>
    public sealed class RackLookTests
    {
        private const float Frame = 1f / 60f;

        private RackViewSettings _settings;
        private FakeRack _rack;
        private RackLook _look;

        [SetUp]
        public void SetUp()
        {
            _settings = new RackViewSettings();
            _rack = new FakeRack(RackBayState.Outline, RackBayState.Outline, RackBayState.Hidden);
            _look = new RackLook(_rack.Count, _settings);
        }

        [Test]
        public void BeforeTheReveal_NothingRisesOrGlows()
        {
            Run(2f);
            Assert.AreEqual(0f, _look.Rise01);
            Assert.AreEqual(0f, _look.Outline(0));
        }

        [Test]
        public void RevealedLive_ItRisesEased_ThenLightsItsBaysLeftToRight()
        {
            _look.Reveal(true);
            Run(_settings.RiseSeconds * 0.5f);
            Assert.That(_look.Rise01, Is.InRange(0.3f, 0.7f), "halfway up at half the time (eased)");
            Assert.AreEqual(0f, _look.Outline(0), "no glow while it rises");

            Run(_settings.RiseSeconds * 0.5f + Frame);
            Assert.IsTrue(_look.Standing);
            Run(_settings.BayStaggerSeconds * 0.5f);
            Assert.Greater(_look.Outline(0), 0f, "the left bay lights first");
            Assert.AreEqual(0f, _look.Outline(1), "the next a beat after");

            Run(_settings.BayStaggerSeconds + _settings.GlowSeconds);
            Assert.Greater(_look.Outline(1), 0f);
            Assert.AreEqual(0f, _look.Outline(2), "a part not yet seen has no outline");
        }

        [Test]
        public void ASeenOutline_IsDimAndBreathes_AFittedOne_IsBrightWithItsFillLampAndJawsClosed()
        {
            _look.Reveal(false);
            Step(true);
            float max = 0f;
            float min = float.MaxValue;
            for (float t = 0f; t < _settings.BreathSeconds; t += Frame)
            {
                Step(false);
                max = Mathf.Max(max, _look.Outline(0));
                min = Mathf.Min(min, _look.Outline(0));
            }

            Assert.LessOrEqual(max, _settings.OutlineDim + 1e-4f, "dim while only seen");
            Assert.Less(min, max - _settings.OutlineDim * _settings.BreathDepth * 0.5f, "and it breathes");
            Assert.AreEqual(0f, _look.Fill(0));
            Assert.AreEqual(0f, _look.Jaw(0), "the jaws stand open");

            _rack.States[0] = RackBayState.Filled;
            Run(_settings.JawSeconds * 0.5f);
            Assert.That(_look.Jaw(0), Is.InRange(0.1f, 0.9f), "the jaws ease closed");
            Run(Mathf.Max(_settings.JawSeconds, _settings.GlowSeconds));
            Assert.AreEqual(1f, _look.Jaw(0), 1e-4f, "closed");
            Assert.AreEqual(_settings.OutlineBright, _look.Outline(0), 1e-4f, "bright");
            Assert.AreEqual(1f, _look.Fill(0), 1e-4f);
            Assert.AreEqual(1f, _look.Lamp(0), 1e-4f);
            Assert.AreEqual(0f, _look.Fill(1), "only its own bay");
        }

        [Test]
        public void ALoadedRack_StandsLitAtOnce()
        {
            _rack.States[1] = RackBayState.Filled;
            _look.Reveal(false);
            Step(true);
            Assert.AreEqual(1f, _look.Rise01);
            Assert.Greater(_look.Outline(0), 0f);
            Assert.AreEqual(1f, _look.Jaw(1), 1e-4f, "a fitted part's jaws stand closed");
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                Step(false);
            }
        }

        private void Step(bool snap)
        {
            _look.Step(Frame, _rack, snap);
        }

        private sealed class FakeRack : IRackStatus
        {
            public FakeRack(params RackBayState[] states)
            {
                States = states;
            }

            public RackBayState[] States { get; }

            public bool Shown => true;

            public Vector3 Position => Vector3.zero;

            public Quaternion Rotation => Quaternion.identity;

            public int Count => States.Length;

            public string IdOf(int bay)
            {
                return "rack." + bay;
            }

            public RackBayState StateOf(int bay)
            {
                return States[bay];
            }
        }
    }
}

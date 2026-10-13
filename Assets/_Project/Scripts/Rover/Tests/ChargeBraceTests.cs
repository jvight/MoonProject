using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>07's brace through the charge-up (0.5.5 §5.3): squat, a settle per stage, dead still at the full lock,
    /// and one soft overshoot rising back.</summary>
    public sealed class ChargeBraceTests
    {
        private const float Frame = 1f / 120f;
        private const float BraceSeconds = 0.5f;

        private readonly ChargeBraceSettings _settings = new ChargeBraceSettings();

        private static void Run(ChargeBrace brace, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                brace.Step(Frame);
            }
        }

        [Test]
        public void AFreshCharge_PlantsTheBodyWithinTheBrace_AndStiffensTheRockers()
        {
            var brace = new ChargeBrace(_settings);
            brace.Begin(false);
            brace.Step(Frame);
            Assert.Greater(brace.Heave, -0.5f * _settings.Squat, "Eased down, not snapped.");
            Run(brace, BraceSeconds);
            Assert.AreEqual(-_settings.Squat, brace.Heave, 0.15f * _settings.Squat, "Down 3 cm by the brace's end.");
            Assert.Greater(brace.Stiffness, 0.95f, "The rockers stop following the ground.");
            Assert.IsTrue(brace.IsBraced);
        }

        [Test]
        public void AWarmCharge_DropsFasterThanAFreshBrace()
        {
            var fresh = new ChargeBrace(_settings);
            var warm = new ChargeBrace(_settings);
            fresh.Begin(false);
            warm.Begin(true);
            Run(fresh, 0.1f);
            Run(warm, 0.1f);
            Assert.Less(warm.Heave, fresh.Heave, "No brace: it is already low.");
        }

        [Test]
        public void EachStage_SettlesHalfACentimetre_WithATremorThatFades()
        {
            var brace = new ChargeBrace(_settings);
            var still = new ChargeBrace(_settings);
            brace.Begin(false);
            still.Begin(false);
            Run(brace, 1f);
            Run(still, 1f);
            brace.Stage(1, 3);
            still.Stage(1, 1);
            float swing = 0f;
            for (float t = 0f; t < _settings.TremorSeconds; t += Frame)
            {
                brace.Step(Frame);
                still.Step(Frame);
                swing = Mathf.Max(swing, Mathf.Abs(brace.Heave - still.Heave));
            }

            Assert.Greater(swing, 0.2f * _settings.Tremor, "A tremor runs through the body; the full lock settles the same way without one.");
            Run(brace, 0.5f);
            Assert.AreEqual(-_settings.Squat - _settings.SettlePerStage, brace.Heave, 1e-4f, "Settled one step.");
            Assert.IsFalse(brace.IsLocked);
        }

        [Test]
        public void TheFullLock_SettlesOnceMore_ThenHoldsDeadStill()
        {
            var brace = new ChargeBrace(_settings);
            brace.Begin(false);
            Run(brace, 1f);
            brace.Stage(1, 2);
            Run(brace, 0.5f);
            brace.Stage(2, 2);
            Assert.IsTrue(brace.IsLocked);
            Run(brace, 0.4f);
            float held = brace.Heave;
            Assert.AreEqual(-_settings.Squat - 2f * _settings.SettlePerStage, held, 2e-4f);
            Run(brace, 0.12f);
            Assert.AreEqual(held, brace.Heave, 1e-5f, "Nothing moves through the held breath.");
            brace.Stage(3, 2);
            Assert.AreEqual(held, brace.Heave, 1e-5f, "Locked: a stray stage changes nothing.");
        }

        [Test]
        public void AReleaseRisesBack_WithOneSoftOvershoot_AndTheRockersFollowAgain()
        {
            var brace = new ChargeBrace(_settings);
            brace.Begin(false);
            Run(brace, 1f);
            brace.Release();
            float peak = float.NegativeInfinity;
            for (float t = 0f; t < 2f; t += Frame)
            {
                brace.Step(Frame);
                peak = Mathf.Max(peak, brace.Heave);
            }

            Assert.Greater(peak, 0.05f * _settings.Squat, "One soft overshoot above rest.");
            Assert.Less(peak, 0.5f * _settings.Squat, "Soft, not a bounce.");
            Assert.IsTrue(brace.IsAtRest(1e-3f));
            Assert.AreEqual(0f, brace.Stiffness, "Free again, so the next brace holds the ground's new pitch.");
        }

        [Test]
        public void AStageWithoutABrace_DoesNothing()
        {
            var brace = new ChargeBrace(_settings);
            brace.Stage(1, 1);
            Run(brace, 0.5f);
            Assert.AreEqual(0f, brace.Heave);
            Assert.IsFalse(brace.IsLocked);
        }
    }
}

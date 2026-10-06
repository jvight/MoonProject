using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class HoverJumpTests
    {
        private const float Step = 0.02f;
        private const float RiseGravity = 2.6f;
        private HoverJumpSettings _settings;
        private HoverJump _jump;

        [SetUp]
        public void SetUp()
        {
            _settings = new HoverJumpSettings();
            _jump = new HoverJump(_settings);
        }

        /// <summary>Holds Jump for at least one step (a tap) or <paramref name="seconds"/>, then releases.</summary>
        /// <returns>The leap strength, or -1 when there was no leap.</returns>
        private float Hold(float seconds, bool enabled = true, bool grounded = true)
        {
            _jump.Step(enabled, false, grounded, Step);
            float held = 0f;
            do
            {
                _jump.Step(enabled, true, grounded, Step);
                held += Step;
            }
            while (held < seconds);

            return _jump.Step(enabled, false, grounded, Step) == HoverJumpEvent.Leap ? _jump.LastStrength : -1f;
        }

        [Test]
        public void WithoutTheAbility_NothingHappens()
        {
            Assert.AreEqual(HoverJumpEvent.None, _jump.Step(false, true, true, Step));
            Assert.AreEqual(-1f, Hold(2f, false));
            Assert.IsFalse(_jump.IsCharging);
        }

        [Test]
        public void InTheAir_NothingHappens()
        {
            Assert.AreEqual(-1f, Hold(1f, true, false));
        }

        [Test]
        public void Tap_IsASmallHop()
        {
            float strength = Hold(0f);
            Assert.That(strength, Is.InRange(0f, 0.01f));
            Assert.AreEqual(_settings.TapHeight, _jump.HeightFor(strength), 0.05f);
        }

        [Test]
        public void FullCharge_TakesChargeTime_AndLeapsToFullHeight()
        {
            float half = Hold(_settings.ChargeTime * 0.5f);
            Assert.That(half, Is.InRange(0.4f, 0.6f));
            _jump.NotifyLanded();
            Hold(_settings.Cooldown + Step);
            float full = Hold(_settings.ChargeTime + 0.1f);
            Assert.AreEqual(1f, full, 1e-4f);
            Assert.AreEqual(_settings.FullHeight, _jump.HeightFor(full), 1e-4f);
        }

        [Test]
        public void TakeOffSpeed_ReachesTheHeightUnderRisingGravity()
        {
            float speed = _jump.TakeOffSpeed(1f, RiseGravity);
            Assert.AreEqual(_settings.FullHeight, speed * speed / (2f * RiseGravity), 1e-3f);
        }

        [Test]
        public void Charge_ReportsItsStartAndEachStepOnce()
        {
            _jump.Step(true, false, true, Step);
            Assert.AreEqual(HoverJumpEvent.ChargeProgress, _jump.Step(true, true, true, Step));
            Assert.AreEqual(0f, _jump.LastStrength);
            int reports = 1;
            float last = 0f;
            for (float t = 0f; t < _settings.ChargeTime * 2f; t += Step)
            {
                if (_jump.Step(true, true, true, Step) == HoverJumpEvent.ChargeProgress)
                {
                    reports++;
                    Assert.Greater(_jump.LastStrength, last);
                    last = _jump.LastStrength;
                }
            }

            Assert.AreEqual(_settings.ChargeSteps + 1, reports);
            Assert.AreEqual(1f, last, 1e-4f);
        }

        [Test]
        public void LeavingTheGroundWhileCharging_CancelsQuietly()
        {
            _jump.Step(true, false, true, Step);
            _jump.Step(true, true, true, Step);
            Assert.IsTrue(_jump.IsCharging);
            Assert.AreEqual(HoverJumpEvent.None, _jump.Step(true, true, false, Step));
            Assert.IsFalse(_jump.IsCharging);
            Assert.AreEqual(HoverJumpEvent.None, _jump.Step(true, false, true, Step), "No leap after the cancel.");
        }

        [Test]
        public void AfterLanding_ACooldownThenHopsChain()
        {
            _jump.NotifyLanded();
            Assert.AreEqual(-1f, Hold(0f), "Too soon after landing.");
            for (float t = 0f; t < _settings.Cooldown; t += Step)
            {
                _jump.Step(true, false, true, Step);
            }

            Assert.GreaterOrEqual(Hold(0f), 0f, "Hops chain once the short cooldown is over.");
        }

        [Test]
        public void Cushion_LetsGentleDescentsBe_AndSoftensFastOnesNearTheGround()
        {
            Assert.AreEqual(0f, _jump.CushionVelocityChange(2f, 1f), "Never pushes while rising.");
            Assert.AreEqual(0f, _jump.CushionVelocityChange(-1f, 1f), "Slow enough already.");
            Assert.AreEqual(0f, _jump.CushionVelocityChange(-7f, _settings.CushionProbe + 1f), "Too high to act.");

            float atGround = _jump.CushionVelocityChange(-7f, 0f);
            Assert.AreEqual(7f - _settings.CushionLandingSpeed, atGround, 1e-4f);
            Assert.Less(_jump.CushionVelocityChange(-7f, 2f), atGround, "Higher up it allows a faster fall.");
        }

        [Test]
        public void SimulatedFullLeap_LandsSoftly()
        {
            float speed = _jump.TakeOffSpeed(1f, RiseGravity);
            float height = 0f;
            float vy = speed;
            float apex = 0f;
            const float FallGravity = 3.6f;
            while (height > 0f || vy > 0f)
            {
                vy -= (vy > 0f ? RiseGravity : FallGravity) * Step;
                vy += _jump.CushionVelocityChange(vy, height);
                height += vy * Step;
                apex = Mathf.Max(apex, height);
            }

            Assert.AreEqual(_settings.FullHeight, apex, 0.1f);
            Assert.LessOrEqual(-vy, _settings.CushionLandingSpeed + 2f * FallGravity * Step, "A soft touchdown.");
        }
    }
}

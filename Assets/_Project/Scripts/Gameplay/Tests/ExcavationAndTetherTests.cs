using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    public sealed class ExcavationAndTetherTests
    {
        private TetherTuning _tether;

        [SetUp]
        public void SetUp()
        {
            _tether = ScriptableObject.CreateInstance<TetherTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tether);
        }

        [Test]
        public void Rise_ComesUpOutOfTheGroundBeforeDriftingAcross()
        {
            var buried = new Vector3(0f, -1f, 0f);
            var present = new Vector3(4f, 1.3f, 0f);
            Vector3 early = ExcavationRise.Position(buried, present, 0.25f);
            float up = (early.y - buried.y) / (present.y - buried.y);
            float across = early.x / present.x;
            Assert.Greater(up, across + 0.3f, "height leads, so it never slides through the soil");
            Assert.AreEqual(present, ExcavationRise.Position(buried, present, 1f));
            Assert.AreEqual(buried, ExcavationRise.Position(buried, present, 0f));
        }

        [Test]
        public void Rise_ProgressIsKeptAcrossHolds()
        {
            float progress = ExcavationRise.Advance(0f, 2f, 5f);
            Assert.AreEqual(0.4f, progress, 1e-5f);
            Assert.AreEqual(0f, ExcavationRise.Segment(progress, progress), "a new hold starts its own segment");
            Assert.AreEqual(0.5f, ExcavationRise.Segment(0.7f, 0.4f), 1e-5f);
            Assert.AreEqual(1f, ExcavationRise.Advance(0.9f, 5f, 5f), "never past fully up");
        }

        [Test]
        public void Rise_PresentPointIsAheadOfTheEye_Horizontally()
        {
            Vector3 point = ExcavationRise.Ahead(new Vector3(1f, 2f, 3f), new Vector3(0f, -0.5f, 2f), 3f);
            Assert.AreEqual(new Vector3(1f, 2f, 6f), point);
        }

        [Test]
        public void Excavation_HeavierRelicsTakeLonger()
        {
            var tuning = ScriptableObject.CreateInstance<ExcavationTuning>();
            Assert.Greater(tuning.DurationFor(14f), tuning.DurationFor(3f));
            Assert.AreEqual(tuning.BaseDuration, tuning.DurationFor(0f), 1e-5f);
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void Winch_ANotchEasesInByAStep_AtTheReelSpeed()
        {
            var winch = new WinchControl(2.5f, 12f, 0.9f, 4f, 1.5f);
            winch.Reset(8f);
            winch.Step(1f, 0.016f);
            Assert.AreEqual(7.1f, winch.Requested, 1e-4f, "reel in by one step");
            Assert.AreEqual(8f - 4f * 0.016f, winch.Length, 1e-4f, "the length eases at the reel speed");
            for (int i = 0; i < 60; i++)
            {
                winch.Step(0f, 0.016f);
            }

            Assert.AreEqual(7.1f, winch.Length, 1e-4f);
        }

        [Test]
        public void Winch_AHeldButtonNeverRunsFarAhead_AndStaysInRange()
        {
            var winch = new WinchControl(2.5f, 12f, 0.9f, 4f, 1.5f);
            winch.Reset(6f);
            for (int i = 0; i < 10; i++)
            {
                winch.Step(-1f, 0.016f);
            }

            Assert.LessOrEqual(winch.Requested - winch.Length, 1.5f + 1e-4f, "a release never overshoots far");
            for (int i = 0; i < 1000; i++)
            {
                winch.Step(-1f, 0.016f);
            }

            Assert.AreEqual(12f, winch.Length, 1e-4f);
            winch.Reset(30f);
            Assert.AreEqual(30f, winch.Length, "latching far away starts at the real distance...");
            Assert.AreEqual(12f, winch.Requested, "...and reels in smoothly");
        }

        [Test]
        public void TetherTarget_FloatsAboveTheGround_AndNeverAboveTheLiftLimit()
        {
            var anchor = new Vector3(0f, 1.3f, 0f);
            Vector3 low = TetherPhysics.Target(anchor, new Vector3(0f, 0f, -10f), 6f, 0f, 0.8f, 5f);
            Assert.AreEqual(0.8f, low.y, 1e-4f);
            Vector3 high = TetherPhysics.Target(anchor, new Vector3(0f, 20f, -1f), 10f, 0f, 0.8f, 4f);
            Assert.AreEqual(4f, high.y, 1e-4f);
            Vector3 trailing = TetherPhysics.Target(anchor, new Vector3(0f, 1.3f, -9f), 6f, 0f, 0.8f, 5f);
            Assert.AreEqual(-6f, trailing.z, 1e-3f, "at the winch length, in the direction it already is");
        }

        [Test]
        public void TetherForce_HoldsARelicAtItsTarget_AgainstGravity()
        {
            Vector3 force = TetherPhysics.Force(Vector3.one, Vector3.zero, Vector3.one, Vector3.zero, 6f, 1.62f,
                _tether);
            Assert.AreEqual(0f, force.x, 1e-4f);
            Assert.AreEqual(6f * 1.62f, force.y, 1e-3f);
        }

        [Test]
        public void TetherForce_HeavierRelicsAreSlower_AndEverythingIsCapped()
        {
            Assert.Greater(TetherPhysics.Frequency(3f, _tether), TetherPhysics.Frequency(14f, _tether));
            Vector3 far = TetherPhysics.Force(Vector3.zero, Vector3.zero, new Vector3(100f, 100f, 0f), Vector3.zero,
                40f, 1.62f, _tether);
            Assert.LessOrEqual(far.magnitude, _tether.MaxForce + 1e-3f);
            Assert.LessOrEqual(far.y, 40f * 1.62f * _tether.LiftMultiplier + 1e-3f, "lifting is gentle");
            Vector3 light = TetherPhysics.Force(Vector3.zero, Vector3.zero, new Vector3(100f, 0f, 0f), Vector3.zero,
                1f, 1.62f, _tether);
            Assert.LessOrEqual(light.magnitude / 1f, _tether.MaxAcceleration + 1e-3f);
        }

        [Test]
        public void TetherForce_DampsTowardTheRoverVelocity_NotTowardRest()
        {
            var roverVelocity = new Vector3(0f, 0f, 5f);
            Vector3 force = TetherPhysics.Force(Vector3.zero, roverVelocity, Vector3.zero, roverVelocity, 6f, 0f,
                _tether);
            Assert.Less(force.magnitude, 1e-4f, "a relic keeping pace at its target is left alone");
        }

        [Test]
        public void SnapRule_LetsGoWhenCaughtForAMoment_OrWhenFarTooFar()
        {
            var rule = new TetherSnapRule(5f, 0.8f, 24f);
            Assert.IsFalse(rule.Step(10f, 6f, 0.5f), "a little stretch is fine");
            Assert.IsFalse(rule.Step(12f, 6f, 0.5f), "caught, but give it a moment");
            Assert.AreEqual(1f, rule.Strain);
            Assert.IsTrue(rule.Step(12f, 6f, 0.5f), "caught for longer than the grace: let go softly");
            rule.Reset();
            Assert.IsFalse(rule.Step(12f, 6f, 0.5f));
            Assert.IsFalse(rule.Step(7f, 6f, 0.5f), "freed: the clock restarts");
            Assert.IsFalse(rule.Step(12f, 6f, 0.5f));
            Assert.IsTrue(rule.Step(25f, 24f, 0.01f), "far too far lets go at once");
        }

        [Test]
        public void Aim_PicksInsideTheCone_PrefersCentralThenNear()
        {
            Vector3 eye = Vector3.zero;
            Vector3 forward = Vector3.forward;
            Assert.IsTrue(TetherAim.TryScore(eye, forward, new Vector3(0.5f, 0f, 10f), 8f, 30f, 0.08f, out float a));
            Assert.IsFalse(TetherAim.TryScore(eye, forward, new Vector3(3f, 0f, 10f), 8f, 30f, 0.08f, out _),
                "17 degrees off is outside the cone");
            Assert.IsTrue(TetherAim.TryScore(eye, forward, new Vector3(3f, 0f, 10f), 20f, 30f, 0.08f, out _));
            Assert.IsFalse(TetherAim.TryScore(eye, forward, new Vector3(0f, 0f, -5f), 8f, 30f, 0.08f, out _));
            Assert.IsFalse(TetherAim.TryScore(eye, forward, new Vector3(0f, 0f, 40f), 8f, 30f, 0.08f, out _));
            Assert.IsTrue(TetherAim.TryScore(eye, forward, new Vector3(0f, 0f, 20f), 8f, 30f, 0.08f, out float b));
            Assert.IsTrue(TetherAim.TryScore(eye, forward, new Vector3(0f, 0f, 8f), 8f, 30f, 0.08f, out float c));
            Assert.Less(c, b, "dead centre: the nearer one wins");
            Assert.Less(c, a);
        }

        [Test]
        public void Beam_RunsFromEyeToRelic_BowingUp()
        {
            var start = new Vector3(0f, 1f, 0f);
            var end = new Vector3(0f, 1f, -10f);
            Vector3 control = TetherBeamShape.Control(start, end, 0.12f, 0f, 1f, 0f);
            Assert.AreEqual(start, TetherBeamShape.Point(start, control, end, 0f));
            Assert.AreEqual(end, TetherBeamShape.Point(start, control, end, 1f));
            Assert.Greater(TetherBeamShape.Point(start, control, end, 0.5f).y, 1.5f);
        }
    }
}

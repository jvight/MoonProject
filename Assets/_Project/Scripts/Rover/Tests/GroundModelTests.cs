using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    public sealed class GroundModelTests
    {
        private static readonly Vector3 LunarGravity = new Vector3(0f, -1.62f, 0f);
        private GroundSettings _ground;

        [SetUp]
        public void SetUp()
        {
            _ground = new GroundSettings();
        }

        private static Vector3 SlopeNormal(float degrees)
        {
            return Quaternion.Euler(-degrees, 0f, 0f) * Vector3.up;
        }

        [Test]
        public void SlopeAssist_IsFullOnGentleSlopes_AndGoneOnWalls()
        {
            Assert.AreEqual(1f, GroundModel.SlopeAssist(_ground, 0f));
            Assert.AreEqual(1f, GroundModel.SlopeAssist(_ground, _ground.SlopeAssistFullAngle));
            Assert.AreEqual(0f, GroundModel.SlopeAssist(_ground, _ground.SlopeAssistZeroAngle + 1f));
        }

        [Test]
        public void SlopeAcceleration_WhenParked_CancelsTheSlidingPull()
        {
            Vector3 normal = SlopeNormal(30f);
            Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized;
            Vector3 total = LunarGravity + GroundModel.SlopeAcceleration(_ground, LunarGravity, normal, forward, 0f);
            Vector3 alongSurface = Vector3.ProjectOnPlane(total, normal);
            Assert.Less(alongSurface.magnitude, 1e-4f, "A parked rover must not creep down a 30 degree slope.");
        }

        [Test]
        public void SlopeAcceleration_WhenRolling_KeepsALittleHillCharacter()
        {
            Vector3 normal = SlopeNormal(20f);
            Vector3 forward = Vector3.ProjectOnPlane(Vector3.forward, normal).normalized;
            Vector3 total = LunarGravity + GroundModel.SlopeAcceleration(_ground, LunarGravity, normal, forward, 5f);
            float along = Vector3.Dot(total, forward);
            float raw = Vector3.Dot(LunarGravity, forward);
            Assert.Less(along, 0f, "Uphill still pulls back a little.");
            Assert.AreEqual(raw * _ground.SlopeInfluence, along, 1e-4f);
        }

        [Test]
        public void Grip_PreservesSpeed_AndTurnsVelocityTowardHeading()
        {
            Vector3 velocity = Quaternion.Euler(0f, 30f, 0f) * Vector3.forward * 6f;
            Vector3 change =
                GroundModel.GripVelocityChange(velocity, Vector3.up, Vector3.forward, _ground.GripRate, 0.02f);
            Vector3 after = velocity + change;
            Assert.AreEqual(velocity.magnitude, after.magnitude, 1e-3f);
            Assert.Less(Vector3.Angle(after, Vector3.forward), 30f);
        }

        [Test]
        public void Grip_WhenReversing_AlignsBackwards()
        {
            Vector3 velocity = Quaternion.Euler(0f, 170f, 0f) * Vector3.forward * 2f;
            Vector3 after = velocity + GroundModel.GripVelocityChange(velocity, Vector3.up, Vector3.forward, 20f, 0.1f);
            Assert.Less(Vector3.Angle(after, Vector3.back), 10f);
        }

        [Test]
        public void Grip_IgnoresVelocityAlongTheNormal()
        {
            Vector3 change = GroundModel.GripVelocityChange(Vector3.up * 3f, Vector3.up, Vector3.forward, 10f, 0.02f);
            Assert.AreEqual(Vector3.zero, change);
        }

        [Test]
        public void ExtraAirGravity_TopsWorldGravityUpToTheTunedTotals()
        {
            Assert.AreEqual(_ground.AirRiseGravity - 1.62f, GroundModel.ExtraAirGravity(_ground, 1f, -1.62f), 1e-5f);
            Assert.AreEqual(_ground.AirFallGravity - 1.62f, GroundModel.ExtraAirGravity(_ground, -1f, -1.62f), 1e-5f);
        }

        [Test]
        public void SlopeAcceleration_OnCrossSlopeWhileRolling_PullsGentlyDownhill()
        {
            Vector3 normal = SlopeNormal(30f);
            Vector3 crossForward = Vector3.right;
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.back, normal).normalized;
            Vector3 total = LunarGravity + GroundModel.SlopeAcceleration(_ground, LunarGravity, normal, crossForward, 5f);
            float downhillPull = Vector3.Dot(total, downhill);
            Assert.Greater(downhillPull, 0.35f,
                "Traversing across a 30 deg slope at speed must feel a gentle downhill pull.");
        }

        [Test]
        public void ObstacleCollision_HeadOnReboundsAndClampsClimb_GlancingSlidesAndDeflects()
        {
            Vector3 wallNormal = Vector3.back;
            Vector3 headOnVel = new Vector3(0f, 1.5f, 6f);
            Vector3 bounced = GroundModel.ResolveObstacleVelocity(_ground, headOnVel, wallNormal, true,
                out float headOnImpact, out bool bouncedHeadOn);
            Assert.IsTrue(bouncedHeadOn, "Head-on impact must rebound.");
            Assert.AreEqual(6f, headOnImpact, 1e-4f);
            Assert.AreEqual(0f, bounced.y, 1e-4f, "Grounded collision must clamp upward wedging velocity.");
            Assert.Less(bounced.z, -1f, "Head-on impact must rebound away from the obstacle.");

            Vector3 glancingVel = Quaternion.Euler(0f, -65f, 0f) * Vector3.forward * 7f;
            Vector3 slid = GroundModel.ResolveObstacleVelocity(_ground, glancingVel, wallNormal, true,
                out _, out bool glancingBounced);
            Assert.IsFalse(glancingBounced, "Glancing wall contact must slide rather than rebound.");
            Assert.AreEqual(0f, slid.z, 1e-4f, "Inward velocity component is removed.");
            Assert.Less(slid.x, -4.5f, "Tangential speed is preserved along the wall.");

            float deflected = GroundModel.DeflectHeadingOnWall(_ground, 315f, wallNormal, 1f, 0.1f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(deflected, 270f)), Mathf.Abs(Mathf.DeltaAngle(315f, 270f)),
                "Glancing wall contact gently deflects heading toward the wall tangent.");
        }
    }
}

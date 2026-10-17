using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    public sealed class ObstacleContactTests
    {
        private GroundSettings _ground;
        private DriveSettings _drive;
        private ContactResponse _contacts;

        [SetUp]
        public void SetUp()
        {
            _ground = new GroundSettings();
            _drive = new DriveSettings();
            _contacts = new ContactResponse(_ground, _drive);
        }

        [Test]
        public void HeadOnCollision_Rebounds_HoldsThrottle_AndPushesOutWithinSkin()
        {
            Vector3 wallNormal = Vector3.back;
            Vector3 approachVelocity = new Vector3(0f, 1.2f, 6f);
            Vector3 resolved = _contacts.StepContact(approachVelocity, wallNormal, 0.005f, true, 0.02f,
                out Vector3 pushOut, out float impactSpeed, out bool publishCollision);

            Assert.IsTrue(publishCollision, "Head-on impact above threshold must publish RoverCollided.");
            Assert.AreEqual(6f, impactSpeed, 1e-4f);
            Assert.AreEqual(0f, resolved.y, 1e-4f, "Grounded collision clamps upward wedging velocity.");
            Assert.Less(resolved.z, -1f, "Head-on impact rebounds away from the obstacle.");
            Assert.Less(pushOut.z, -0.01f, "Grounded contact inside obstacle skin pushes out along the wall normal.");
            Assert.AreEqual(_ground.CollisionBounceHoldTime, _contacts.BounceHoldRemaining, 1e-4f);
            Assert.IsTrue(_contacts.IsRebounding(resolved), "Rebounding velocity counts as moving during bounce hold.");

            Assert.AreEqual(0f, _contacts.FilterThrottle(1f, Vector3.forward), 1e-4f,
                "Forward throttle into the obstacle is held off during the bounce window.");
            Assert.AreEqual(-1f, _contacts.FilterThrottle(-1f, Vector3.forward), 1e-4f,
                "Reversing away from the obstacle cancels the bounce hold early.");
            Assert.AreEqual(0f, _contacts.BounceHoldRemaining, 1e-4f);
        }

        [Test]
        public void GlancingWallContact_GlidesAlongTangent_DeflectsHeading_AndClampsInwardAcceleration()
        {
            Vector3 wallNormal = Vector3.back;
            Vector3 glancingVelocity = Quaternion.Euler(0f, -65f, 0f) * Vector3.forward * 7f;
            Vector3 resolved = _contacts.StepContact(glancingVelocity, wallNormal, 0.01f, true, 0.02f,
                out _, out _, out _);

            Assert.AreEqual(0f, _contacts.BounceHoldRemaining, 1e-4f,
                "Glancing wall contact must glide without triggering a head-on bounce hold.");
            Assert.AreEqual(0f, resolved.z, 1e-4f, "Inward velocity component is removed.");
            Assert.Less(resolved.x, -4.5f, "Tangential speed is preserved along the wall.");

            float deflected = _contacts.AdjustHeading(315f, 1f, 0f, true, 0.1f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(deflected, 270f)), Mathf.Abs(Mathf.DeltaAngle(315f, 270f)),
                "Glancing contact deflects heading toward the wall tangent when steer input is small.");

            float playerSteered = _contacts.AdjustHeading(315f, 1f, _ground.WallDeflectMaxSteer + 0.1f, true, 0.1f);
            Assert.AreEqual(315f, playerSteered, 1e-4f,
                "Active steering past WallDeflectMaxSteer overrides automatic wall deflection.");

            float drive = 5f;
            Vector3 glancingForward = Quaternion.Euler(0f, 315f, 0f) * Vector3.forward;
            Vector3 redirected = _contacts.RedirectDrive(glancingForward, Vector3.up, 1f, ref drive);
            Assert.AreEqual(5f, drive, 1e-4f);
            Assert.AreEqual(0f, Vector3.Dot(redirected, wallNormal), 1e-4f,
                "Glancing drive direction is projected onto the wall tangent.");

            Vector3 clampedAccel = _contacts.ClampAcceleration(new Vector3(-3f, 0f, 2f));
            Assert.AreEqual(-3f, clampedAccel.x, 1e-4f);
            Assert.AreEqual(0f, clampedAccel.z, 1e-4f, "Inward acceleration into the wall is stripped.");
        }

        [Test]
        public void ObstacleProbe_IgnoresDownwardFacingSlabUnderside_AndDetectsSteepBoulders()
        {
            Vector3 slabUndersideNormal = new Vector3(0f, -0.85f, -0.52f).normalized;
            var slabHit = new ObstacleRayHit(new Vector3(0f, 0.5f, 0.6f), slabUndersideNormal, 0.6f, Layers.Ground);
            Assert.IsFalse(ObstacleProbe.IsObstacleHit(_ground, in slabHit, false, out _),
                "Downward-facing slab undersides must be ignored by the obstacle probe.");

            var fakeQuery = new FakeObstacleQuery { RayHit = slabHit, HasRayHit = true };
            var probe = new ObstacleProbe(_ground, fakeQuery);
            probe.ProbeObstacle(Vector3.zero, Vector3.forward * 4f, 0f, 1f, 0.02f, true, false,
                out Vector3 slabNormal, out _, out float slabGap);
            Assert.AreEqual(Vector3.zero, slabNormal);
            Assert.AreEqual(float.MaxValue, slabGap);

            Vector3 boulderNormal = Quaternion.Euler(-35f, 0f, 0f) * Vector3.up;
            var boulderHit = new ObstacleRayHit(new Vector3(0f, 0.5f, 0.7f), boulderNormal, 0.7f, Layers.Prop);
            fakeQuery.RayHit = boulderHit;
            probe.ProbeObstacle(Vector3.zero, Vector3.forward * 4f, 0f, 1f, 0.02f, true, false,
                out Vector3 detectedNormal, out Vector3 detectedPoint, out float detectedGap);

            Assert.AreEqual(Vector3.back, detectedNormal);
            Assert.AreEqual(boulderHit.Point, detectedPoint);
            Assert.Less(detectedGap, 0f);
        }

        [Test]
        public void CollisionCooldown_SuppressesRepeatedCollisionEvents_UntilExpired()
        {
            Vector3 wallNormal = Vector3.back;
            Vector3 impactVelocity = new Vector3(0f, 0f, 5f);

            _contacts.StepContact(impactVelocity, wallNormal, 0.01f, true, 0.02f,
                out _, out _, out bool firstPublished);
            Assert.IsTrue(firstPublished, "First impact publishes RoverCollided.");
            Assert.AreEqual(_ground.CollisionCooldown, _contacts.CooldownRemaining, 1e-4f);

            _contacts.StepContact(impactVelocity, wallNormal, 0.01f, true, 0.10f,
                out _, out _, out bool duringCooldownPublished);
            Assert.IsFalse(duringCooldownPublished, "Impact while cooldown is active must not re-publish.");
            Assert.Greater(_contacts.CooldownRemaining, 0f);

            _contacts.StepContact(impactVelocity, wallNormal, 0.01f, true, _ground.CollisionCooldown,
                out _, out _, out bool afterCooldownPublished);
            Assert.IsTrue(afterCooldownPublished, "Impact after cooldown expires publishes again.");
        }

        private sealed class FakeObstacleQuery : IObstacleQuery
        {
            public bool HasRayHit;
            public ObstacleRayHit RayHit;
            public Vector3[] OverlapPoints = new Vector3[0];

            public int OverlapClosestPoints(Vector3 origin, float radius, Vector3[] closestPoints)
            {
                int count = Mathf.Min(OverlapPoints.Length, closestPoints.Length);
                for (int i = 0; i < count; i++)
                {
                    closestPoints[i] = OverlapPoints[i];
                }

                return count;
            }

            public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out ObstacleRayHit hit)
            {
                if (HasRayHit && RayHit.Distance <= maxDistance)
                {
                    hit = RayHit;
                    return true;
                }

                hit = default;
                return false;
            }
        }
    }
}

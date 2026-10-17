using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Ground-contact forces for the physics sphere, expressed as accelerations so they are independent of mass.
    /// </summary>
    public static class GroundModel
    {
        private const float HasNormalSqrMagnitude = 0.5f;
        private const float PlanarEpsilonSqr = 1e-6f;
        private const float GripSpeedEpsilon = 1e-4f;
        private const float MinSpeedEpsilon = 1e-3f;
        private const float FullCircleDegrees = 360f;

        /// <summary>1 on gentle slopes, easing to 0 between the full and zero assist angles.</summary>
        public static float SlopeAssist(GroundSettings settings, float slopeAngle)
        {
            return 1f - Smoothing.SmoothStep(settings.SlopeAssistFullAngle, settings.SlopeAssistZeroAngle, slopeAngle);
        }

        /// <summary>
        /// Acceleration that cancels the along-slope pull of gravity (scaled by slope assist) and then gives back a
        /// fraction of it along the heading while rolling, so hills keep some character but a parked rover stays put.
        /// </summary>
        public static Vector3 SlopeAcceleration(GroundSettings settings, Vector3 gravity, Vector3 normal,
            Vector3 forward, float forwardSpeed)
        {
            Vector3 tangential = gravity - normal * Vector3.Dot(gravity, normal);
            float assist = SlopeAssist(settings, Vector3.Angle(normal, Vector3.up));
            float rolling = Smoothing.SmoothStep(0f, settings.SlopeRollSpeed, Mathf.Abs(forwardSpeed));
            Vector3 felt = tangential * (settings.SlopeInfluence * rolling);
            return felt - tangential * assist;
        }

        /// <summary>
        /// Gentle yaw rate (deg/s) pulling 07's heading downhill when traversing across a side-slope at speed,
        /// ramping to 0 at rest so a parked rover never turns on a slope.
        /// </summary>
        public static float CrossSlopeYawRate(GroundSettings settings, Vector3 gravity, Vector3 normal,
            Vector3 forward, float forwardSpeed)
        {
            Vector3 tangential = gravity - normal * Vector3.Dot(gravity, normal);
            float assist = SlopeAssist(settings, Vector3.Angle(normal, Vector3.up));
            float rolling = Smoothing.SmoothStep(0f, settings.SlopeRollSpeed, Mathf.Abs(forwardSpeed));
            Vector3 felt = tangential * (settings.SlopeInfluence * rolling * assist);
            Vector3 right = Vector3.Cross(normal, forward);
            if (right.sqrMagnitude <= PlanarEpsilonSqr)
            {
                return 0f;
            }

            right.Normalize();
            float sign = forwardSpeed >= 0f ? 1f : -1f;
            return Vector3.Dot(felt, right) * sign * settings.CrossSlopeYawPull;
        }

        /// <summary>
        /// Velocity change that swings the in-plane velocity toward the heading (forward or backward, whichever is
        /// closer) without changing its magnitude: turns keep their speed, and a sideways landing straightens out.
        /// </summary>
        public static Vector3 GripVelocityChange(Vector3 velocity, Vector3 normal, Vector3 forward, float gripRate,
            float deltaTime)
        {
            Vector3 planar = velocity - normal * Vector3.Dot(velocity, normal);
            float speed = planar.magnitude;
            if (speed < GripSpeedEpsilon)
            {
                return Vector3.zero;
            }

            Vector3 heading = Vector3.Dot(planar, forward) >= 0f ? forward : -forward;
            float angle = Vector3.Angle(planar, heading) * Mathf.Deg2Rad;
            float turn = angle * (1f - Mathf.Exp(-gripRate * deltaTime));
            Vector3 aligned = Vector3.RotateTowards(planar, heading * speed, turn, 0f);
            return aligned - planar;
        }

        /// <summary>Extra downward acceleration (m/s^2) on top of world gravity while airborne.</summary>
        public static float ExtraAirGravity(GroundSettings settings, float verticalVelocity, float worldGravity)
        {
            float total = verticalVelocity > 0f ? settings.AirRiseGravity : settings.AirFallGravity;
            return total - Mathf.Abs(worldGravity);
        }

        /// <summary>
        /// Outward horizontal normal (pointing away from the obstacle) when <paramref name="contactNormal"/> is steep
        /// enough to be an obstacle wall/crate rather than driveable ground; <see cref="Vector3.zero"/> when gentle.
        /// </summary>
        public static Vector3 ObstaclePlanarNormal(GroundSettings settings, Vector3 contactNormal)
        {
            if (Vector3.Angle(contactNormal, Vector3.up) <= settings.MaxGroundAngle)
            {
                return Vector3.zero;
            }

            Vector3 planar = new Vector3(contactNormal.x, 0f, contactNormal.z);
            return planar.sqrMagnitude > PlanarEpsilonSqr ? planar.normalized : Vector3.zero;
        }

        /// <summary>
        /// Resolves <paramref name="velocity"/> against an obstacle with horizontal outward normal
        /// <paramref name="wallNormal"/>:
        /// <list type="bullet">
        /// <item>Clamps any upward wedging velocity when grounded so the sphere never climbs crates or walls.</item>
        /// <item>When moving into the obstacle faster than <see cref="GroundSettings.CollisionMinImpactSpeed"/>:
        /// head-on impacts (<c>cos &gt;= CollisionHeadOnCosine</c>) rebound with <see cref="GroundSettings.CollisionRestitution"/>;
        /// glancing impacts slide along the wall tangent with <see cref="GroundSettings.WallScrapeFriction"/>.</item>
        /// <item>When pressing into the obstacle at low speed, strips the inward velocity component so 07 cannot wedge.</item>
        /// </list>
        /// </summary>
        public static Vector3 ResolveObstacleVelocity(GroundSettings settings, Vector3 velocity, Vector3 wallNormal,
            bool grounded, out float impactSpeed, out bool bouncedHeadOn)
        {
            impactSpeed = 0f;
            bouncedHeadOn = false;
            if (wallNormal.sqrMagnitude < HasNormalSqrMagnitude)
            {
                return velocity;
            }

            Vector3 result = velocity;
            if (grounded && result.y > 0f)
            {
                result.y = 0f;
            }

            Vector3 planar = new Vector3(result.x, 0f, result.z);
            float planarSpeed = planar.magnitude;
            float into = -Vector3.Dot(planar, wallNormal);
            if (into <= 0f)
            {
                return result;
            }

            Vector3 tangent = planar + wallNormal * into;
            if (planarSpeed >= settings.CollisionMinImpactSpeed && into >= settings.CollisionMinImpactSpeed)
            {
                impactSpeed = into;
                float headOnCosine = into / planarSpeed;
                if (headOnCosine >= settings.CollisionHeadOnCosine)
                {
                    bouncedHeadOn = true;
                    Vector3 rebound = wallNormal * (into * settings.CollisionRestitution)
                        + tangent * (1f - settings.WallScrapeFriction);
                    return new Vector3(rebound.x, result.y, rebound.z);
                }
            }

            float scrapeScale = Mathf.Clamp01(
                1f - settings.WallScrapeFriction * (into / Mathf.Max(planarSpeed, MinSpeedEpsilon)));
            Vector3 slid = tangent * scrapeScale;
            return new Vector3(slid.x, result.y, slid.z);
        }

        /// <summary>
        /// Gently steers <paramref name="heading"/> (deg) toward the wall's slide tangent when 07 is pressing into a
        /// glancing wall (neither head-on nor already parallel), so held throttle scrapes smoothly past the obstacle
        /// instead of pinning against it.
        /// </summary>
        public static float DeflectHeadingOnWall(GroundSettings settings, float heading, Vector3 wallNormal,
            float throttle, float deltaTime)
        {
            if (wallNormal.sqrMagnitude < HasNormalSqrMagnitude || Mathf.Abs(throttle) <= MinSpeedEpsilon)
            {
                return heading;
            }

            float sign = throttle >= 0f ? 1f : -1f;
            Vector3 moveDir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward * sign;
            float into = -Vector3.Dot(moveDir, wallNormal);
            if (into <= settings.ObstacleSkin || into >= settings.WallDeadOnCosine)
            {
                return heading;
            }

            Vector3 tangent = Vector3.ProjectOnPlane(moveDir, wallNormal);
            if (tangent.sqrMagnitude < PlanarEpsilonSqr)
            {
                return heading;
            }

            tangent.Normalize();
            Vector3 desiredForward = tangent * sign;
            float targetHeading = Mathf.Repeat(Mathf.Atan2(desiredForward.x, desiredForward.z) * Mathf.Rad2Deg,
                FullCircleDegrees);
            float maxStep = settings.WallDeflectRate * into * deltaTime;
            return Mathf.Repeat(Mathf.MoveTowardsAngle(heading, targetHeading, maxStep), FullCircleDegrees);
        }
    }
}

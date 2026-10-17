using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Pure obstacle contact decisions for 07's physics sphere: head-on rebound vs. glancing wall glide, collision
    /// event cooldown, post-bounce throttle hold, depenetration skin push-out, and wall-tangent drive projection.
    /// </summary>
    public sealed class ContactResponse
    {
        private const float HasNormalSqrMagnitude = 0.5f;
        private const float ReverseThrottleEpsilon = -1e-3f;
        private const float MinSpeedEpsilon = 1e-3f;
        private const float MinTangentSqrMagnitude = 1e-4f;
        private const float FullCircleDegrees = 360f;

        private readonly GroundSettings _ground;
        private readonly DriveSettings _drive;
        private float _bounceHoldTimer;
        private float _collisionCooldownTimer;
        private float _cornerTimer;
        private Vector3 _obstacleNormal;
        private Vector3 _cornerNormal;

        public ContactResponse(GroundSettings ground, DriveSettings drive)
        {
            _ground = ground ?? throw new ArgumentNullException(nameof(ground));
            _drive = drive ?? throw new ArgumentNullException(nameof(drive));
        }

        public ContactResponse(GroundSettings ground)
            : this(ground, new DriveSettings())
        {
        }

        /// <summary>Outward horizontal normal of the current step's obstacle contact, or zero when clear.</summary>
        public Vector3 ObstacleNormal => _obstacleNormal;

        /// <summary>Outward horizontal normal of a secondary obstacle forming a corner with <see cref="ObstacleNormal"/>.</summary>
        public Vector3 CornerNormal => _cornerNormal;

        /// <summary>True when an obstacle normal was detected on the current physics step.</summary>
        public bool HasObstacle => _obstacleNormal.sqrMagnitude > HasNormalSqrMagnitude;

        /// <summary>True when a secondary obstacle normal is active alongside <see cref="ObstacleNormal"/>.</summary>
        public bool HasCorner => _cornerNormal.sqrMagnitude > HasNormalSqrMagnitude;

        /// <summary>Seconds remaining on the post-bounce forward throttle hold.</summary>
        public float BounceHoldRemaining => _bounceHoldTimer;

        /// <summary>Seconds remaining on the collision event and bounce-hold cooldown.</summary>
        public float CooldownRemaining => _collisionCooldownTimer;

        /// <summary>Clears any active obstacle normal, corner memory, bounce hold and collision cooldown.</summary>
        public void Reset()
        {
            _bounceHoldTimer = 0f;
            _collisionCooldownTimer = 0f;
            _cornerTimer = 0f;
            _obstacleNormal = Vector3.zero;
            _cornerNormal = Vector3.zero;
        }

        /// <summary>
        /// Advances cooldown/hold timers by <paramref name="dt"/>, resolves <paramref name="velocity"/> against
        /// <paramref name="obstacleNormal"/>, computes skin depenetration <paramref name="pushOut"/> when grounded,
        /// and reports whether a collision event should be published this step.
        /// </summary>
        public Vector3 StepContact(Vector3 velocity, Vector3 obstacleNormal, float surfaceGap, bool grounded, float dt,
            out Vector3 pushOut, out float impactSpeed, out bool publishCollision)
        {
            return StepContact(velocity, obstacleNormal, Vector3.zero, surfaceGap, grounded, dt, out pushOut,
                out impactSpeed, out publishCollision);
        }

        /// <summary>
        /// Advances cooldown/hold timers by <paramref name="dt"/>, resolves <paramref name="velocity"/> against
        /// <paramref name="obstacleNormal"/> and optional corner wall <paramref name="secondaryNormal"/>, computes
        /// skin depenetration <paramref name="pushOut"/> when grounded, and reports whether a collision event should
        /// be published this step.
        /// </summary>
        public Vector3 StepContact(Vector3 velocity, Vector3 obstacleNormal, Vector3 secondaryNormal, float surfaceGap,
            bool grounded, float dt, out Vector3 pushOut, out float impactSpeed, out bool publishCollision)
        {
            _bounceHoldTimer = Mathf.Max(0f, _bounceHoldTimer - dt);
            _collisionCooldownTimer = Mathf.Max(0f, _collisionCooldownTimer - dt);
            _cornerTimer = Mathf.Max(0f, _cornerTimer - dt);
            UpdateCorner(obstacleNormal, secondaryNormal);
            _obstacleNormal = obstacleNormal;
            pushOut = Vector3.zero;
            impactSpeed = 0f;
            publishCollision = false;

            if (!HasObstacle)
            {
                return velocity;
            }

            Vector3 resolved = GroundModel.ResolveObstacleVelocity(_ground, velocity, _obstacleNormal, grounded,
                out impactSpeed, out bool bouncedHeadOn);
            if (HasCorner && !bouncedHeadOn)
            {
                float intoCorner = Vector3.Dot(resolved, _cornerNormal);
                if (intoCorner < 0f)
                {
                    resolved -= _cornerNormal * intoCorner;
                }
            }

            if (impactSpeed >= _ground.CollisionMinImpactSpeed && _collisionCooldownTimer <= 0f)
            {
                _collisionCooldownTimer = _ground.CollisionCooldown;
                publishCollision = true;
                if (bouncedHeadOn)
                {
                    _bounceHoldTimer = _ground.CollisionBounceHoldTime;
                }
            }

            if (grounded && surfaceGap < _ground.ObstacleSkin)
            {
                pushOut = _obstacleNormal * (_ground.ObstacleSkin - surfaceGap);
            }

            return resolved;
        }

        /// <summary>
        /// Deflects <paramref name="heading"/> along a glancing obstacle wall (or out of a two-wall concave corner)
        /// when grounded, not in post-bounce hold, and the player is not actively steering past
        /// <see cref="GroundSettings.WallDeflectMaxSteer"/>.
        /// </summary>
        public float AdjustHeading(float heading, float throttle, float steer, bool hasGroundContact, float dt)
        {
            if (!hasGroundContact || !HasObstacle || _bounceHoldTimer > 0f
                || Mathf.Abs(steer) >= _ground.WallDeflectMaxSteer)
            {
                return heading;
            }

            if (Mathf.Abs(throttle) > MinSpeedEpsilon)
            {
                float sign = throttle >= 0f ? 1f : -1f;
                Vector3 moveDir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward * sign;
                if (TryGetCornerEscapeTangent(moveDir, out Vector3 escapeTangent))
                {
                    Vector3 desiredForward = escapeTangent * sign;
                    float targetHeading = Mathf.Repeat(Mathf.Atan2(desiredForward.x, desiredForward.z) * Mathf.Rad2Deg,
                        FullCircleDegrees);
                    float maxStep = _ground.WallDeflectRate * dt;
                    return Mathf.Repeat(Mathf.MoveTowardsAngle(heading, targetHeading, maxStep), FullCircleDegrees);
                }
            }

            return GroundModel.DeflectHeadingOnWall(_ground, heading, _obstacleNormal, throttle, dt);
        }

        /// <summary>
        /// Suppresses forward throttle during the post-bounce hold window so a head-on rebound can roll back cleanly,
        /// unless the player pulls away from the obstacle past <see cref="DriveSettings.BounceCancelAwayDot"/>.
        /// </summary>
        public float FilterThrottle(float throttle, Vector3 forward)
        {
            if (_bounceHoldTimer <= 0f)
            {
                return throttle;
            }

            if (HasObstacle && throttle * Vector3.Dot(forward, _obstacleNormal) > _drive.BounceCancelAwayDot)
            {
                _bounceHoldTimer = 0f;
                return throttle;
            }

            return 0f;
        }

        /// <summary>
        /// Redirects grounded drive direction along the wall tangent on glancing obstacle contact (or along the open
        /// escape tangent in a concave corner), or zeroes inward drive when pressing dead-on into an isolated wall
        /// (<see cref="GroundSettings.WallDeadOnCosine"/>).
        /// </summary>
        public Vector3 RedirectDrive(Vector3 forward, Vector3 groundNormal, float effectiveThrottle, ref float drive)
        {
            if (!HasObstacle || _bounceHoldTimer > 0f)
            {
                return forward;
            }

            float moveSign = effectiveThrottle < ReverseThrottleEpsilon ? -1f : 1f;
            Vector3 moveDir = forward * moveSign;
            if (TryGetCornerEscapeTangent(moveDir, out Vector3 escapeTangent))
            {
                Vector3 onSlope = Vector3.ProjectOnPlane(escapeTangent * moveSign, groundNormal);
                if (onSlope.sqrMagnitude > MinTangentSqrMagnitude)
                {
                    return onSlope.normalized;
                }
            }

            float intoWall = -Vector3.Dot(moveDir, _obstacleNormal);
            if (intoWall >= _ground.WallDeadOnCosine)
            {
                if (drive * Vector3.Dot(forward, _obstacleNormal) < 0f)
                {
                    drive = 0f;
                }

                return forward;
            }

            if (intoWall > 0f)
            {
                Vector3 wallTangent = Vector3.ProjectOnPlane(forward, _obstacleNormal);
                Vector3 onSlope = Vector3.ProjectOnPlane(wallTangent, groundNormal);
                if (onSlope.sqrMagnitude > MinTangentSqrMagnitude)
                {
                    return onSlope.normalized;
                }
            }

            return forward;
        }

        /// <summary>
        /// Strips any inward acceleration component pressing into the active obstacle (and any secondary corner wall)
        /// while gliding along it.
        /// </summary>
        public Vector3 ClampAcceleration(Vector3 acceleration)
        {
            if (!HasObstacle || _bounceHoldTimer > 0f)
            {
                return acceleration;
            }

            float intoAccel = Vector3.Dot(acceleration, _obstacleNormal);
            if (intoAccel < 0f)
            {
                acceleration -= _obstacleNormal * intoAccel;
            }

            if (HasCorner)
            {
                float intoCorner = Vector3.Dot(acceleration, _cornerNormal);
                if (intoCorner < 0f)
                {
                    acceleration -= _cornerNormal * intoCorner;
                }
            }

            return acceleration;
        }

        /// <summary>
        /// True while a head-on bounce hold is active and 07 is still moving faster than
        /// <see cref="DriveSettings.BounceMovingSpeedSqr"/>, so the stuck detector does not count a rebound as stuck.
        /// </summary>
        public bool IsRebounding(Vector3 velocity)
        {
            return _bounceHoldTimer > 0f && velocity.sqrMagnitude >= _drive.BounceMovingSpeedSqr;
        }

        private void UpdateCorner(Vector3 obstacleNormal, Vector3 secondaryNormal)
        {
            if (obstacleNormal.sqrMagnitude <= HasNormalSqrMagnitude)
            {
                if (_cornerTimer <= 0f)
                {
                    _cornerNormal = Vector3.zero;
                }

                return;
            }

            float maxCornerCosine = _ground.CornerMaxNormalCosine;
            if (secondaryNormal.sqrMagnitude > HasNormalSqrMagnitude
                && Vector3.Dot(obstacleNormal, secondaryNormal) < maxCornerCosine)
            {
                _cornerNormal = secondaryNormal;
                _cornerTimer = _ground.CollisionCooldown;
            }
            else if (_obstacleNormal.sqrMagnitude > HasNormalSqrMagnitude
                     && Vector3.Dot(obstacleNormal, _obstacleNormal) < maxCornerCosine)
            {
                _cornerNormal = _obstacleNormal;
                _cornerTimer = _ground.CollisionCooldown;
            }
            else if (HasCorner && Vector3.Dot(obstacleNormal, _cornerNormal) < maxCornerCosine)
            {
                _cornerTimer = _ground.CollisionCooldown;
            }
            else if (_cornerTimer <= 0f || Vector3.Dot(obstacleNormal, _cornerNormal) >= maxCornerCosine)
            {
                _cornerNormal = Vector3.zero;
            }
        }

        private bool TryGetCornerEscapeTangent(Vector3 moveDir, out Vector3 escapeTangent)
        {
            escapeTangent = Vector3.zero;
            if (!HasObstacle || !HasCorner)
            {
                return false;
            }

            float intoPrimary = -Vector3.Dot(moveDir, _obstacleNormal);
            if (intoPrimary <= 0f)
            {
                return false;
            }

            Vector3 naiveTangent = Vector3.ProjectOnPlane(moveDir, _obstacleNormal);
            if (intoPrimary < _ground.WallDeadOnCosine
                && naiveTangent.sqrMagnitude > MinTangentSqrMagnitude
                && Vector3.Dot(naiveTangent, _cornerNormal) >= 0f)
            {
                return false;
            }

            Vector3 tangentAlongPrimary = Vector3.ProjectOnPlane(_cornerNormal, _obstacleNormal);
            Vector3 tangentAlongCorner = Vector3.ProjectOnPlane(_obstacleNormal, _cornerNormal);
            if (tangentAlongPrimary.sqrMagnitude <= MinTangentSqrMagnitude
                || tangentAlongCorner.sqrMagnitude <= MinTangentSqrMagnitude)
            {
                return false;
            }

            tangentAlongPrimary.Normalize();
            tangentAlongCorner.Normalize();
            escapeTangent = Vector3.Dot(moveDir, tangentAlongPrimary) >= Vector3.Dot(moveDir, tangentAlongCorner)
                ? tangentAlongPrimary
                : tangentAlongCorner;
            return true;
        }
    }
}

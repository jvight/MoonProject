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
        private const float MinTangentSqrMagnitude = 1e-4f;

        private readonly GroundSettings _ground;
        private readonly DriveSettings _drive;
        private float _bounceHoldTimer;
        private float _collisionCooldownTimer;
        private Vector3 _obstacleNormal;

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

        /// <summary>True when an obstacle normal was detected on the current physics step.</summary>
        public bool HasObstacle => _obstacleNormal.sqrMagnitude > HasNormalSqrMagnitude;

        /// <summary>Seconds remaining on the post-bounce forward throttle hold.</summary>
        public float BounceHoldRemaining => _bounceHoldTimer;

        /// <summary>Seconds remaining on the collision event and bounce-hold cooldown.</summary>
        public float CooldownRemaining => _collisionCooldownTimer;

        /// <summary>Clears any active obstacle normal, bounce hold and collision cooldown.</summary>
        public void Reset()
        {
            _bounceHoldTimer = 0f;
            _collisionCooldownTimer = 0f;
            _obstacleNormal = Vector3.zero;
        }

        /// <summary>
        /// Advances cooldown/hold timers by <paramref name="dt"/>, resolves <paramref name="velocity"/> against
        /// <paramref name="obstacleNormal"/>, computes skin depenetration <paramref name="pushOut"/> when grounded,
        /// and reports whether a collision event should be published this step.
        /// </summary>
        public Vector3 StepContact(Vector3 velocity, Vector3 obstacleNormal, float surfaceGap, bool grounded, float dt,
            out Vector3 pushOut, out float impactSpeed, out bool publishCollision)
        {
            _bounceHoldTimer = Mathf.Max(0f, _bounceHoldTimer - dt);
            _collisionCooldownTimer = Mathf.Max(0f, _collisionCooldownTimer - dt);
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
        /// Deflects <paramref name="heading"/> along a glancing obstacle wall when grounded, not in post-bounce hold,
        /// and the player is not actively steering past <see cref="GroundSettings.WallDeflectMaxSteer"/>.
        /// </summary>
        public float AdjustHeading(float heading, float throttle, float steer, bool hasGroundContact, float dt)
        {
            if (!hasGroundContact || !HasObstacle || _bounceHoldTimer > 0f
                || Mathf.Abs(steer) >= _ground.WallDeflectMaxSteer)
            {
                return heading;
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
        /// Redirects grounded drive direction along the wall tangent on glancing obstacle contact, or zeroes inward
        /// drive when pressing dead-on into a wall (<see cref="GroundSettings.WallDeadOnCosine"/>).
        /// </summary>
        public Vector3 RedirectDrive(Vector3 forward, Vector3 groundNormal, float effectiveThrottle, ref float drive)
        {
            if (!HasObstacle || _bounceHoldTimer > 0f)
            {
                return forward;
            }

            float moveSign = effectiveThrottle < ReverseThrottleEpsilon ? -1f : 1f;
            float intoWall = -Vector3.Dot(forward * moveSign, _obstacleNormal);
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
        /// Strips any inward acceleration component pressing into the active obstacle while gliding along it.
        /// </summary>
        public Vector3 ClampAcceleration(Vector3 acceleration)
        {
            if (!HasObstacle || _bounceHoldTimer > 0f)
            {
                return acceleration;
            }

            float intoAccel = Vector3.Dot(acceleration, _obstacleNormal);
            return intoAccel < 0f ? acceleration - _obstacleNormal * intoAccel : acceleration;
        }

        /// <summary>
        /// True while a head-on bounce hold is active and 07 is still moving faster than
        /// <see cref="DriveSettings.BounceMovingSpeedSqr"/>, so the stuck detector does not count a rebound as stuck.
        /// </summary>
        public bool IsRebounding(Vector3 velocity)
        {
            return _bounceHoldTimer > 0f && velocity.sqrMagnitude >= _drive.BounceMovingSpeedSqr;
        }
    }
}

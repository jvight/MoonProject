using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The hidden physics sphere and how it meets the ground: contact probe, downforce, grip, slope help and the
    /// extra gravity that shapes low-gravity hops. World gravity is lunar (ProjectSettings); everything here is added
    /// on top explicitly.
    /// </summary>
    [Serializable]
    public sealed class GroundSettings
    {
        [Tooltip("Mass of the physics sphere (kg). Heavy, so tethered relics never yank the rover around.")]
        [Min(1f)]
        [SerializeField] private float _mass = 250f;

        [Tooltip("Radius (m) of the hidden physics sphere. Smaller squeezes between rocks; larger rolls over bumps.")]
        [Range(0.2f, 1.2f)]
        [SerializeField] private float _sphereRadius = 0.5f;

        [Tooltip("Radius of the ground probe as a fraction of the sphere radius.")]
        [Range(0.5f, 0.99f)]
        [SerializeField] private float _probeRadiusFactor = 0.9f;

        [Tooltip("Gap (m) under the sphere that still counts as touching the ground. Larger hugs crests longer.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _groundSnapDistance = 0.12f;

        [Tooltip("Surfaces steeper than this (deg) are walls, not ground.")]
        [Range(20f, 89f)]
        [SerializeField] private float _maxGroundAngle = 48f;

        [Tooltip("Extra acceleration (m/s^2) pressing the rover into the ground along its normal while grounded. "
            + "Keeps it hugging gentle dunes; sharp crests at speed still launch joyful hops.")]
        [Range(0f, 20f)]
        [SerializeField] private float _downforce = 4f;

        [Tooltip("How quickly (1/s) the velocity swings back in line with the heading while grounded. Lower = more "
            + "lunar drift in turns; speed is preserved, never scrubbed.")]
        [Range(0.5f, 40f)]
        [SerializeField] private float _gripRate = 5.5f;

        [Tooltip("Slopes up to this angle (deg) are fully assisted: gravity neither slows you uphill nor drags "
            + "you down.")]
        [Range(0f, 60f)]
        [SerializeField] private float _slopeAssistFullAngle = 32f;

        [Tooltip("Slope angle (deg) where assistance has faded out completely and the rover gently slides back.")]
        [Range(0f, 80f)]
        [SerializeField] private float _slopeAssistZeroAngle = 45f;

        [Tooltip("Fraction of tangential gravity still felt while rolling, so hills and side-slopes have character.")]
        [Range(0f, 4f)]
        [SerializeField] private float _slopeInfluence = 2.6f;

        [Tooltip("Speed (m/s) above which Slope Influence fully applies; below it the rover parks firmly on slopes.")]
        [Range(0.05f, 4f)]
        [SerializeField] private float _slopeRollSpeed = 0.8f;

        [Tooltip("Degrees per second of downhill heading pull per m/s^2 of cross-slope gravity while rolling.")]
        [Range(0f, 20f)]
        [SerializeField] private float _crossSlopeYawPull = 3.5f;

        [Tooltip("Total downward acceleration (m/s^2) while airborne and rising. Lunar is 1.62; a little more keeps "
            + "hops floaty but short enough to stay in control.")]
        [Range(0.5f, 9.81f)]
        [SerializeField] private float _airRiseGravity = 2.2f;

        [Tooltip("Total downward acceleration (m/s^2) while airborne and falling.")]
        [Range(0.5f, 9.81f)]
        [SerializeField] private float _airFallGravity = 3.0f;

        [Tooltip("Fraction of drive acceleration available in the air (minimal air control).")]
        [Range(0f, 1f)]
        [SerializeField] private float _airThrottleFactor = 0.1f;

        [Tooltip("Half-life (s) smoothing the published ground normal across low-poly facet edges.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _groundNormalHalfLife = 0.06f;

        [Header("Collisions")]
        [Tooltip("Fraction of into-obstacle speed reflected as a soft bounce-back on head-on impacts.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _collisionRestitution = 0.25f;

        [Tooltip("Incidence cosine above which an obstacle contact bounces back rather than scraping along the face.")]
        [Range(0.5f, 0.98f)]
        [SerializeField] private float _collisionHeadOnCosine = 0.85f;

        [Tooltip("Seconds after a head-on bounce during which forward throttle into the obstacle is held off.")]
        [Range(0f, 1f)]
        [SerializeField] private float _collisionBounceHoldTime = 0.45f;

        [Tooltip("Seconds after publishing RoverCollided before another collision event or bounce hold can fire.")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _collisionCooldown = 0.35f;

        [Tooltip("Fraction of speed bled off by friction per unit of grazing incidence when scraping a wall.")]
        [Range(0f, 1f)]
        [SerializeField] private float _wallScrapeFriction = 0.2f;

        [Tooltip("Degrees per second at which a glancing wall contact steers 07's heading toward the wall tangent.")]
        [Range(0f, 360f)]
        [SerializeField] private float _wallDeflectRate = 120f;

        [Tooltip("Steer input magnitude above which automatic wall heading deflection yields to the player.")]
        [Range(0f, 1f)]
        [SerializeField] private float _wallDeflectMaxSteer = 0.25f;

        [Tooltip("Into-wall cosine above which drive is treated as dead-on into the wall rather than gliding.")]
        [Range(0.9f, 0.999f)]
        [SerializeField] private float _wallDeadOnCosine = 0.995f;

        [Tooltip("Minimum normal impact speed (m/s) that publishes RoverCollided for a contact thud and body jolt.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _collisionMinImpactSpeed = 1.8f;

        [Tooltip("Horizontal obstacle bumper radius as a multiple of SphereRadius (matches 07's 4-wheel footprint).")]
        [Range(1f, 2.5f)]
        [SerializeField] private float _obstacleBumperRadiusFactor = 1.65f;

        [Tooltip("Maximum vertical drop below the sphere centre (as a fraction of SphereRadius) for overlap "
            + "closest-point obstacle contacts, ignoring flat ground underfoot.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _obstacleMinHeightDropFactor = 0.75f;

        [Tooltip("Vertical drop below the sphere centre (as a fraction of SphereRadius) for the low obstacle raycast "
            + "fan that catches low boulders and crates.")]
        [Range(0.1f, 0.95f)]
        [SerializeField] private float _obstacleLowRayDropFactor = 0.15f;

        [Tooltip("Vertical lift above the sphere centre (as a fraction of SphereRadius) for the upper obstacle raycast "
            + "fan that catches workbench tops and overhangs within the sphere's upper hemisphere.")]
        [Range(0.1f, 0.95f)]
        [SerializeField] private float _obstacleHighRayLiftFactor = 0.6f;

        [Tooltip("Angular step (deg) between rays in the horizontal obstacle probe fan.")]
        [Range(5f, 45f)]
        [SerializeField] private float _obstacleFanStepDegrees = 22.5f;

        [Tooltip("Minimum angle (deg) between velocity and heading before casting a second obstacle fan along heading.")]
        [Range(1f, 45f)]
        [SerializeField] private float _obstacleHeadingFanAngle = 5f;

        [Tooltip("Minimum lookahead distance (m) beyond the bumper radius when probing for obstacles.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _obstacleMinLookahead = 0.08f;

        [Tooltip("Multiplier on one physics step's planar travel (speed * dt) for dynamic obstacle lookahead.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _obstacleLookaheadStepScale = 1.25f;

        [Tooltip("Skin clearance (m) kept between the bumper radius and a grounded obstacle so the sphere cannot wedge.")]
        [Range(0.005f, 0.1f)]
        [SerializeField] private float _obstacleSkin = 0.02f;

        [Tooltip("Minimum surface normal Y component for an obstacle raycast hit; rejects downward-facing slab undersides.")]
        [Range(-0.8f, 0.2f)]
        [SerializeField] private float _obstacleMinNormalY = -0.15f;

        [Tooltip("Minimum surface slope (deg) on Prop or Relic colliders to count as a bumper obstacle.")]
        [Range(5f, 60f)]
        [SerializeField] private float _propObstacleMinSlope = 55f;

        [Tooltip("Maximum dot product between two obstacle normals for them to register as a concave two-wall corner.")]
        [Range(0f, 0.9f)]
        [SerializeField] private float _cornerMaxNormalCosine = 0.5f;

        public float Mass => _mass;

        public float SphereRadius => _sphereRadius;

        public float ProbeRadiusFactor => _probeRadiusFactor;

        public float GroundSnapDistance => _groundSnapDistance;

        public float MaxGroundAngle => _maxGroundAngle;

        public float Downforce => _downforce;

        public float GripRate => _gripRate;

        public float SlopeAssistFullAngle => _slopeAssistFullAngle;

        public float SlopeAssistZeroAngle => Mathf.Max(_slopeAssistFullAngle, _slopeAssistZeroAngle);

        public float SlopeInfluence => _slopeInfluence;

        public float SlopeRollSpeed => _slopeRollSpeed;

        public float CrossSlopeYawPull => _crossSlopeYawPull;

        public float AirRiseGravity => _airRiseGravity;

        public float AirFallGravity => _airFallGravity;

        public float AirThrottleFactor => _airThrottleFactor;

        public float GroundNormalHalfLife => _groundNormalHalfLife;

        public float CollisionRestitution => _collisionRestitution;

        public float CollisionHeadOnCosine => _collisionHeadOnCosine;

        public float CollisionBounceHoldTime => _collisionBounceHoldTime;

        public float CollisionCooldown => _collisionCooldown;

        public float WallScrapeFriction => _wallScrapeFriction;

        public float WallDeflectRate => _wallDeflectRate;

        public float WallDeflectMaxSteer => _wallDeflectMaxSteer;

        public float WallDeadOnCosine => _wallDeadOnCosine;

        public float CollisionMinImpactSpeed => _collisionMinImpactSpeed;

        public float ObstacleBumperRadiusFactor => _obstacleBumperRadiusFactor;

        public float ObstacleMinHeightDropFactor => _obstacleMinHeightDropFactor;

        public float ObstacleLowRayDropFactor => _obstacleLowRayDropFactor;

        public float ObstacleHighRayLiftFactor => _obstacleHighRayLiftFactor;

        public float ObstacleFanStepDegrees => _obstacleFanStepDegrees;

        public float ObstacleHeadingFanAngle => _obstacleHeadingFanAngle;

        public float ObstacleMinLookahead => _obstacleMinLookahead;

        public float ObstacleLookaheadStepScale => _obstacleLookaheadStepScale;

        public float ObstacleSkin => _obstacleSkin;

        public float ObstacleMinNormalY => _obstacleMinNormalY;

        public float PropObstacleMinSlope => _propObstacleMinSlope;

        public float CornerMaxNormalCosine => _cornerMaxNormalCosine;
    }
}

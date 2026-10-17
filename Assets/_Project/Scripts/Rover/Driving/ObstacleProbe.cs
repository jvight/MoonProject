using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Probes a horizontal bumper ring around 07's physics sphere using closest-point collider overlaps and a
    /// horizontal raycast fan, rejecting flat ground and downward-facing slab undersides.
    /// </summary>
    public sealed class ObstacleProbe
    {
        private const int MaxClosestPoints = 16;
        private const int FanHalfSteps = 4;
        private const float ThrottleEpsilon = 1e-3f;
        private const float MinHorizontalDistance = 1e-4f;
        private const float PlanarNormalEpsilonSqr = 1e-6f;

        private readonly GroundSettings _ground;
        private readonly IObstacleQuery _query;
        private readonly Vector3[] _closestPoints = new Vector3[MaxClosestPoints];

        public ObstacleProbe(GroundSettings ground, IObstacleQuery query)
        {
            _ground = ground ?? throw new ArgumentNullException(nameof(ground));
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        /// <summary>
        /// Finds the nearest obstacle wall normal, contact point and horizontal surface gap around
        /// <paramref name="origin"/> for the current velocity, heading and throttle.
        /// </summary>
        public void ProbeObstacle(Vector3 origin, Vector3 velocity, float heading, float throttle, float dt,
            bool grounded, bool leaping, out Vector3 wallNormal, out Vector3 contactPoint, out float surfaceGap)
        {
            wallNormal = Vector3.zero;
            contactPoint = origin;
            surfaceGap = float.MaxValue;

            float radius = _ground.SphereRadius * _ground.ObstacleBumperRadiusFactor;
            Vector3 planarVel = new Vector3(velocity.x, 0f, velocity.z);
            float planarSpeed = planarVel.magnitude;
            float sign = throttle < -ThrottleEpsilon ? -1f : 1f;
            Vector3 headingDir = Quaternion.Euler(0f, heading, 0f) * Vector3.forward * sign;
            Vector3 primaryDir = planarSpeed > _ground.ObstacleMinLookahead ? planarVel / planarSpeed : headingDir;
            float lookahead = Mathf.Max(_ground.ObstacleMinLookahead,
                planarSpeed * dt * _ground.ObstacleLookaheadStepScale);

            if (!leaping)
            {
                int count = _query.OverlapClosestPoints(origin, radius + lookahead, _closestPoints);
                float minHeight = origin.y - _ground.SphereRadius * _ground.ObstacleMinHeightDropFactor;
                for (int i = 0; i < count; i++)
                {
                    Vector3 closest = _closestPoints[i];
                    if (closest.y < minHeight)
                    {
                        continue;
                    }

                    Vector3 planar = new Vector3(origin.x - closest.x, 0f, origin.z - closest.z);
                    float horizDist = planar.magnitude;
                    if (horizDist <= MinHorizontalDistance)
                    {
                        continue;
                    }

                    float gap = horizDist - radius;
                    if (gap <= lookahead && gap < surfaceGap)
                    {
                        surfaceGap = gap;
                        wallNormal = planar / horizDist;
                        contactPoint = closest;
                    }
                }
            }

            ProbeObstacleFan(origin, primaryDir, radius, lookahead, grounded, leaping, ref wallNormal,
                ref contactPoint, ref surfaceGap);
            if (Mathf.Abs(throttle) > ThrottleEpsilon
                && Vector3.Angle(primaryDir, headingDir) > _ground.ObstacleHeadingFanAngle)
            {
                ProbeObstacleFan(origin, headingDir, radius, lookahead, grounded, leaping, ref wallNormal,
                    ref contactPoint, ref surfaceGap);
            }
        }

        /// <summary>
        /// Casts a horizontal ray fan centered on <paramref name="centerDir"/> at sphere-centre height and (when
        /// grounded) lower-bumper height, updating the nearest obstacle hit.
        /// </summary>
        public void ProbeObstacleFan(Vector3 origin, Vector3 centerDir, float radius, float lookahead,
            bool grounded, bool leaping, ref Vector3 bestNormal, ref Vector3 bestPoint, ref float bestGap)
        {
            float maxDist = radius + lookahead;
            Vector3 originHigh = origin;
            Vector3 originLow = origin - Vector3.up * (_ground.SphereRadius * _ground.ObstacleLowRayDropFactor);

            for (int i = -FanHalfSteps; i <= FanHalfSteps; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * _ground.ObstacleFanStepDegrees, 0f) * centerDir;
                if (_query.Raycast(originHigh, dir, maxDist, out ObstacleRayHit hitHigh)
                    && IsObstacleHit(_ground, in hitHigh, leaping, out Vector3 normalHigh))
                {
                    float gap = hitHigh.Distance - radius;
                    if (gap < bestGap)
                    {
                        bestGap = gap;
                        bestNormal = normalHigh;
                        bestPoint = hitHigh.Point;
                    }
                }

                if (grounded
                    && _query.Raycast(originLow, dir, maxDist, out ObstacleRayHit hitLow)
                    && IsObstacleHit(_ground, in hitLow, leaping, out Vector3 normalLow))
                {
                    float gap = hitLow.Distance - radius;
                    if (gap < bestGap)
                    {
                        bestGap = gap;
                        bestNormal = normalLow;
                        bestPoint = hitLow.Point;
                    }
                }
            }
        }

        /// <summary>
        /// True when <paramref name="hit"/> is a bumper obstacle (a steep wall or a sloped Prop/Relic while not
        /// leaping) rather than driveable ground or a downward-facing slab underside.
        /// </summary>
        public static bool IsObstacleHit(GroundSettings ground, in ObstacleRayHit hit, bool leaping,
            out Vector3 planarNormal)
        {
            planarNormal = Vector3.zero;
            if (hit.Normal.y < ground.ObstacleMinNormalY)
            {
                return false;
            }

            float slope = Vector3.Angle(hit.Normal, Vector3.up);
            bool propObstacle = (hit.Layer == Layers.Prop || hit.Layer == Layers.Relic)
                && !leaping && slope > ground.PropObstacleMinSlope;
            bool steepObstacle = slope > ground.MaxGroundAngle;
            if (!propObstacle && !steepObstacle)
            {
                return false;
            }

            Vector3 planar = new Vector3(hit.Normal.x, 0f, hit.Normal.z);
            if (planar.sqrMagnitude <= PlanarNormalEpsilonSqr)
            {
                return false;
            }

            planarNormal = planar.normalized;
            return true;
        }
    }
}

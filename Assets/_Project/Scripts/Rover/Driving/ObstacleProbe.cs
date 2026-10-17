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
        private const float HasNormalSqrMagnitude = 0.5f;

        private readonly GroundSettings _ground;
        private readonly IObstacleQuery _query;
        private readonly Vector3[] _closestPoints = new Vector3[MaxClosestPoints];

        public ObstacleProbe(GroundSettings ground, IObstacleQuery query)
        {
            _ground = ground ?? throw new ArgumentNullException(nameof(ground));
            _query = query ?? throw new ArgumentNullException(nameof(query));
        }

        /// <summary>
        /// Outward horizontal normal of a secondary obstacle wall forming a concave corner with the primary wall on
        /// the most recent <see cref="ProbeObstacle"/> call, or <see cref="Vector3.zero"/> when none.
        /// </summary>
        public Vector3 SecondaryNormal { get; private set; }

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
            Vector3 secondaryNormal = Vector3.zero;
            float secondaryGap = float.MaxValue;

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
                    if (gap <= lookahead)
                    {
                        RecordCandidate(planar / horizDist, closest, gap, ref wallNormal, ref contactPoint,
                            ref surfaceGap, ref secondaryNormal, ref secondaryGap);
                    }
                }
            }

            ProbeObstacleFanInternal(origin, primaryDir, radius, lookahead, grounded, leaping, ref wallNormal,
                ref contactPoint, ref surfaceGap, ref secondaryNormal, ref secondaryGap);
            if (Mathf.Abs(throttle) > ThrottleEpsilon
                && Vector3.Angle(primaryDir, headingDir) > _ground.ObstacleHeadingFanAngle)
            {
                ProbeObstacleFanInternal(origin, headingDir, radius, lookahead, grounded, leaping, ref wallNormal,
                    ref contactPoint, ref surfaceGap, ref secondaryNormal, ref secondaryGap);
            }

            SecondaryNormal = wallNormal.sqrMagnitude > HasNormalSqrMagnitude
                && secondaryNormal.sqrMagnitude > HasNormalSqrMagnitude
                && Vector3.Dot(wallNormal, secondaryNormal) < _ground.CornerMaxNormalCosine
                ? secondaryNormal
                : Vector3.zero;
        }

        /// <summary>
        /// Casts a horizontal ray fan centered on <paramref name="centerDir"/> at sphere-centre height and (when
        /// grounded) lower and upper bumper heights, updating the nearest obstacle hit.
        /// </summary>
        public void ProbeObstacleFan(Vector3 origin, Vector3 centerDir, float radius, float lookahead,
            bool grounded, bool leaping, ref Vector3 bestNormal, ref Vector3 bestPoint, ref float bestGap)
        {
            Vector3 secondaryNormal = Vector3.zero;
            float secondaryGap = float.MaxValue;
            ProbeObstacleFanInternal(origin, centerDir, radius, lookahead, grounded, leaping, ref bestNormal,
                ref bestPoint, ref bestGap, ref secondaryNormal, ref secondaryGap);
        }

        private void ProbeObstacleFanInternal(Vector3 origin, Vector3 centerDir, float radius, float lookahead,
            bool grounded, bool leaping, ref Vector3 bestNormal, ref Vector3 bestPoint, ref float bestGap,
            ref Vector3 secondaryNormal, ref float secondaryGap)
        {
            float maxDist = radius + lookahead;
            Vector3 originMid = origin;
            Vector3 originLow = origin - Vector3.up * (_ground.SphereRadius * _ground.ObstacleLowRayDropFactor);
            Vector3 originHigh = origin + Vector3.up * (_ground.SphereRadius * _ground.ObstacleHighRayLiftFactor);

            for (int i = -FanHalfSteps; i <= FanHalfSteps; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * _ground.ObstacleFanStepDegrees, 0f) * centerDir;
                if (_query.Raycast(originMid, dir, maxDist, out ObstacleRayHit hitMid)
                    && IsObstacleHit(_ground, in hitMid, leaping, out Vector3 normalMid))
                {
                    RecordCandidate(normalMid, hitMid.Point, hitMid.Distance - radius, ref bestNormal, ref bestPoint,
                        ref bestGap, ref secondaryNormal, ref secondaryGap);
                }

                if (grounded
                    && _query.Raycast(originLow, dir, maxDist, out ObstacleRayHit hitLow)
                    && IsObstacleHit(_ground, in hitLow, leaping, out Vector3 normalLow))
                {
                    RecordCandidate(normalLow, hitLow.Point, hitLow.Distance - radius, ref bestNormal, ref bestPoint,
                        ref bestGap, ref secondaryNormal, ref secondaryGap);
                }

                if (grounded
                    && _query.Raycast(originHigh, dir, maxDist, out ObstacleRayHit hitHigh)
                    && IsObstacleHit(_ground, in hitHigh, leaping, out Vector3 normalHigh))
                {
                    RecordCandidate(normalHigh, hitHigh.Point, hitHigh.Distance - radius, ref bestNormal, ref bestPoint,
                        ref bestGap, ref secondaryNormal, ref secondaryGap);
                }
            }
        }

        private void RecordCandidate(Vector3 normal, Vector3 point, float gap, ref Vector3 bestNormal,
            ref Vector3 bestPoint, ref float bestGap, ref Vector3 secondaryNormal, ref float secondaryGap)
        {
            float maxCornerCosine = _ground.CornerMaxNormalCosine;
            if (gap < bestGap)
            {
                if (bestNormal.sqrMagnitude > HasNormalSqrMagnitude
                    && Vector3.Dot(bestNormal, normal) < maxCornerCosine
                    && bestGap < secondaryGap)
                {
                    secondaryNormal = bestNormal;
                    secondaryGap = bestGap;
                }

                bestGap = gap;
                bestNormal = normal;
                bestPoint = point;
            }
            else if (bestNormal.sqrMagnitude > HasNormalSqrMagnitude
                     && Vector3.Dot(bestNormal, normal) < maxCornerCosine
                     && gap < secondaryGap)
            {
                secondaryNormal = normal;
                secondaryGap = gap;
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
            bool isPropOrRelic = hit.Layer == Layers.Prop || hit.Layer == Layers.Relic;
            bool isObstacle = isPropOrRelic
                ? !leaping && slope > ground.PropObstacleMinSlope
                : slope > ground.MaxGroundAngle;
            if (!isObstacle)
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

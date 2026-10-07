using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A repaired friend that makes its own way home (Bell, docs/features/M3-05 "Home without escort"): it walks its
    /// route on the surface (the canyon's way out, then across the basin; the last waypoint is its home socket),
    /// hopping gently down any step too steep to walk (the exit's one-way step), and the moment nobody could see it
    /// go it is set down at home: it is off screen or behind the terrain (a hill, the canyon walls), so is its home,
    /// and it is far enough from 07. The check samples the terrain, so it runs a few times a second, not every frame.
    /// Watched all the way, it simply walks all the way. Never a visible teleport, never an escort. Pure and
    /// allocation-free.
    /// </summary>
    public sealed class WalkHome
    {
        private readonly Vector3[] _route;
        private int _next;
        private bool _hopping;
        private float _sinceCheck = float.PositiveInfinity;
        private float _hopTime;
        private Vector3 _hopFrom;
        private Vector3 _hopTo;

        /// <param name="start">Where it stands up after its repair.</param>
        /// <param name="route">Waypoints on the way home; the last one is home.</param>
        public WalkHome(Vector3 start, IReadOnlyList<Vector3> route)
        {
            if (route == null)
            {
                throw new ArgumentNullException(nameof(route));
            }

            if (route.Count < 1)
            {
                throw new ArgumentException("The way home needs at least its home.", nameof(route));
            }

            _route = new Vector3[route.Count];
            for (int i = 0; i < _route.Length; i++)
            {
                _route[i] = route[i];
            }

            Position = start;
            Vector3 toFirst = _route[0] - start;
            Heading = SurfaceRules.Bearing(toFirst);
        }

        /// <summary>On the surface.</summary>
        public Vector3 Position { get; private set; }

        /// <summary>Bearing (degrees from +Z toward +X) it walks along.</summary>
        public float Heading { get; private set; }

        /// <summary>It is home (walked in, or set down there unseen).</summary>
        public bool IsHome { get; private set; }

        /// <summary>It was set down at home while nobody was looking (rather than walking in).</summary>
        public bool PlacedUnseen { get; private set; }

        /// <summary>0..1 through a hop down a step (0 while walking).</summary>
        public float Hop { get; private set; }

        /// <summary>Waypoint it heads for now.</summary>
        public int NextWaypoint => _next;

        /// <summary>The last waypoint: home.</summary>
        public Vector3 Home => _route[_route.Length - 1];

        /// <summary>One frame: set down at home if unseen, otherwise a step along the route.</summary>
        /// <param name="rover">07's position.</param>
        public void Step(float deltaTime, ITerrainQuery terrain, ViewFrustum view, Vector3 rover, FriendTuning tuning)
        {
            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (IsHome)
            {
                return;
            }

            _sinceCheck += Mathf.Max(0f, deltaTime);
            if (_sinceCheck >= tuning.UnseenCheckInterval && Unseen(terrain, view, rover, tuning))
            {
                Position = Home;
                IsHome = true;
                PlacedUnseen = true;
                return;
            }

            if (_hopping)
            {
                StepHop(deltaTime, tuning);
                return;
            }

            float step = tuning.WalkSpeed * Mathf.Max(0f, deltaTime);
            Vector3 position = Position;
            while (step > 0f && _next < _route.Length)
            {
                Vector3 target = _route[_next];
                Vector3 toTarget = target - position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;
                bool home = _next == _route.Length - 1;
                if (!home && distance <= tuning.WalkWaypointReach)
                {
                    // Close enough to a waypoint on the way: round the corner toward the next one.
                    _next++;
                    continue;
                }

                if (distance <= step)
                {
                    position = new Vector3(target.x, position.y, target.z);
                    step -= distance;
                    _next++;
                    continue;
                }

                Vector3 direction = toTarget / distance;
                if (StepDownAhead(terrain, position, direction, tuning))
                {
                    BeginHop(terrain, position, direction, tuning);
                    return;
                }

                position += direction * step;
                Heading = SurfaceRules.Bearing(toTarget);
                break;
            }

            Position = SurfaceRules.OnSurface(terrain, position.x, position.z);
            if (_next >= _route.Length)
            {
                Position = Home;
                IsHome = true;
            }
        }

        private static bool StepDownAhead(ITerrainQuery terrain, Vector3 position, Vector3 direction,
            FriendTuning tuning)
        {
            Vector3 ahead = position + direction * tuning.HopProbe;
            return terrain.SampleHeight(position.x, position.z) - terrain.SampleHeight(ahead.x, ahead.z) >
                   tuning.HopDrop;
        }

        /// <summary>A step too steep to walk: a little arc over the edge down to the ground beyond.</summary>
        private void BeginHop(ITerrainQuery terrain, Vector3 position, Vector3 direction, FriendTuning tuning)
        {
            _hopping = true;
            _hopTime = 0f;
            _hopFrom = SurfaceRules.OnSurface(terrain, position.x, position.z);
            Vector3 landing = position + direction * tuning.HopLength;
            _hopTo = SurfaceRules.OnSurface(terrain, landing.x, landing.z);
            Position = _hopFrom;
        }

        private void StepHop(float deltaTime, FriendTuning tuning)
        {
            _hopTime += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(_hopTime / tuning.HopDuration);
            Vector3 across = Vector3.Lerp(_hopFrom, _hopTo, Ease.InOutSine(t));
            float height = Mathf.Lerp(_hopFrom.y, _hopTo.y, Ease.InOutCubic(t)) + tuning.HopArc * Ease.Hump(t);
            Position = new Vector3(across.x, height, across.z);
            Hop = t;
            if (t < 1f)
            {
                return;
            }

            _hopping = false;
            Hop = 0f;
            Position = _hopTo;
        }

        private bool Unseen(ITerrainQuery terrain, ViewFrustum view, Vector3 rover, FriendTuning tuning)
        {
            _sinceCheck = 0f;
            return SurfaceRules.HorizontalDistance(Position, rover) >= tuning.UnseenDistance &&
                   Hidden(terrain, view, Position, tuning) && Hidden(terrain, view, Home, tuning);
        }

        /// <summary>A body standing at <paramref name="point"/> is off screen, or the terrain hides it.</summary>
        private static bool Hidden(ITerrainQuery terrain, ViewFrustum view, Vector3 point, FriendTuning tuning)
        {
            float margin = tuning.UnseenMargin;
            Vector3 centre = point + Vector3.up * margin;
            if (!view.Sees(centre, margin))
            {
                return true;
            }

            // Behind a hill or a canyon wall: the ground rises above the line from the eye to the top of the body.
            Vector3 eye = view.Position;
            Vector3 top = point + Vector3.up * (margin * 2f);
            int samples = tuning.UnseenSightSamples;
            for (int i = 1; i < samples; i++)
            {
                Vector3 sample = Vector3.Lerp(eye, top, (float)i / samples);
                if (terrain.SampleHeight(sample.x, sample.z) > sample.y)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

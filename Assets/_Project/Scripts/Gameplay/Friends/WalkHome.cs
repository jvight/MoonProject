using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A repaired friend that makes its own way home (Bell, docs/features/M3-05 "Home without escort"): it walks its
    /// route on the surface (the canyon's way out, then across the basin; the last waypoint is its home socket), and
    /// the moment nobody could see it go it is set down at home: it is out of the camera's view, so is its home, and it
    /// is far enough from 07. Watched all the way, it simply walks all the way. Never a visible teleport, never an
    /// escort. Pure and allocation-free.
    /// </summary>
    public sealed class WalkHome
    {
        private readonly Vector3[] _route;
        private int _next;

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

            if (Unseen(view, rover, tuning))
            {
                Position = Home;
                IsHome = true;
                PlacedUnseen = true;
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

                position += toTarget / distance * step;
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

        private bool Unseen(ViewFrustum view, Vector3 rover, FriendTuning tuning)
        {
            float margin = tuning.UnseenMargin;
            Vector3 lift = Vector3.up * margin;
            return SurfaceRules.HorizontalDistance(Position, rover) >= tuning.UnseenDistance &&
                   !view.Sees(Position + lift, margin) && !view.Sees(Home + lift, margin);
        }
    }
}

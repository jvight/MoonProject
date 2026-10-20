using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// When the first dusk begins (docs/design/opening-v3-purpose.md R5–R6): after power-on the sky holds in the
    /// afternoon while 07 is free to look round the two near sites (the 1971 seismic station, the habitat ruin), and
    /// dusk follows progress, never a clock. It begins once, at the first of: 07 docks on a pad after visiting either
    /// site since power-on, or 07 has learned the battery bank from the habitat's power module. Publishes
    /// <see cref="OpeningDuskBegan"/> then, and again with Restored at each session start of a game where it has
    /// begun. Plain logic; allocation-free per step.
    /// </summary>
    public sealed class DuskCue : IDisposable
    {
        private readonly EventBus _events;
        private readonly Vector3[] _sites;
        private readonly float _reach;
        private readonly IDisposable _docks;

        // 07 docked since the last step: only a dock after the visit counts.
        private bool _docked;

        /// <param name="sites">Where the R5 sites stand.</param>
        /// <param name="reach">Metres (flat) from a site within which 07 has visited it.</param>
        public DuskCue(EventBus events, IReadOnlyList<Vector3> sites, float reach)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            if (sites == null || sites.Count == 0)
            {
                throw new ArgumentException("The first dusk needs at least one site to visit.", nameof(sites));
            }

            _sites = new Vector3[sites.Count];
            for (int i = 0; i < sites.Count; i++)
            {
                _sites[i] = sites[i];
            }

            _reach = reach;
            _docks = events.Subscribe<RoverDockChanged>(OnDockChanged);
        }

        /// <summary>07 has visited an R5 site since power-on.</summary>
        public bool Visited { get; private set; }

        /// <summary>The first dusk has begun.</summary>
        public bool Begun { get; private set; }

        /// <summary>
        /// One frame of the opening: <paramref name="powerOn"/> (a built array powers the pad), where 07 stands and
        /// whether it knows the battery bank (the habitat's power module scanned).
        /// </summary>
        public void Step(bool powerOn, Vector3 rover, bool moduleScanned)
        {
            bool docked = _docked;
            _docked = false;
            if (Begun || !powerOn)
            {
                return;
            }

            if (moduleScanned || (Visited && docked))
            {
                Begun = true;
                _events.Publish(new OpeningDuskBegan(false));
                return;
            }

            Visited |= NearASite(rover);
        }

        /// <summary>The session starts (after the save is applied): a dusk already begun is stated, Restored.</summary>
        public void Announce()
        {
            if (Begun)
            {
                _events.Publish(new OpeningDuskBegan(true));
            }
        }

        public void Capture(OpeningSaveData data)
        {
            data.duskSiteVisited = Visited;
            data.duskBegun = Begun;
        }

        public void Restore(OpeningSaveData data)
        {
            Visited = data.duskSiteVisited;
            Begun = data.duskBegun;
            _docked = false;
        }

        public void Dispose()
        {
            _docks.Dispose();
        }

        private void OnDockChanged(RoverDockChanged changed)
        {
            _docked |= changed.Docked;
        }

        private bool NearASite(Vector3 rover)
        {
            for (int i = 0; i < _sites.Length; i++)
            {
                if (SurfaceRules.HorizontalDistanceSquared(rover, _sites[i]) <= _reach * _reach)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

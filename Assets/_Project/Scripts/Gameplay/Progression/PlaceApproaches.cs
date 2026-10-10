using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// "Every place has one job, said in one line" (docs/features/M3-16 rule 3): the first time 07 comes within a
    /// place's radius, <see cref="PlaceFirstApproached"/> is published, once per save. Places reached together
    /// (07 wakes beside its dock, the ladder and the lift) are told one at a time, a gap apart, in the order they
    /// were added. Nothing allocates per frame.
    /// </summary>
    public sealed class PlaceApproaches
    {
        private readonly EventBus _events;
        private readonly float _gap;
        private readonly List<string> _ids = new List<string>();
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<float> _radii = new List<float>();
        private readonly List<Func<bool>> _present = new List<Func<bool>>();
        private readonly HashSet<string> _approached = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _told = new HashSet<string>(StringComparer.Ordinal);
        private float _lastTold = float.NegativeInfinity;

        /// <param name="gap">Seconds between two places' lines.</param>
        public PlaceApproaches(EventBus events, float gap)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _gap = gap;
        }

        /// <param name="present">
        /// Whether the place is there to be seen yet (Bell's tape rack only once Bell is home); null: always.
        /// </param>
        public void Add(string placeId, Vector3 position, float radius, Func<bool> present = null)
        {
            if (string.IsNullOrEmpty(placeId) || _ids.Contains(placeId))
            {
                throw new ArgumentException($"Place '{placeId}' is empty or added twice.", nameof(placeId));
            }

            _ids.Add(placeId);
            _positions.Add(position);
            _radii.Add(radius);
            _present.Add(present);
        }

        public bool HasApproached(string placeId)
        {
            return _told.Contains(placeId);
        }

        /// <summary>Notes the places 07 is within and tells the next one whose turn has come.</summary>
        public void Tick(Vector3 rover, float now)
        {
            for (int i = 0; i < _ids.Count; i++)
            {
                if (SurfaceRules.HorizontalDistance(rover, _positions[i]) <= _radii[i] &&
                    (_present[i] == null || _present[i]()))
                {
                    _approached.Add(_ids[i]);
                }
            }

            if (now - _lastTold < _gap)
            {
                return;
            }

            for (int i = 0; i < _ids.Count; i++)
            {
                if (_approached.Contains(_ids[i]) && _told.Add(_ids[i]))
                {
                    _lastTold = now;
                    _events.Publish(new PlaceFirstApproached(_ids[i]));
                    return;
                }
            }
        }

        /// <summary>The places already told, in the order they were added (saved).</summary>
        public string[] Capture()
        {
            var told = new List<string>();
            foreach (string id in _ids)
            {
                if (_told.Contains(id))
                {
                    told.Add(id);
                }
            }

            return told.ToArray();
        }

        /// <summary>Marks <paramref name="told"/> as greeted; unknown ids (a removed place) are ignored.</summary>
        public void Restore(string[] told)
        {
            _told.Clear();
            _approached.Clear();
            if (told == null)
            {
                return;
            }

            foreach (string id in told)
            {
                if (_ids.Contains(id))
                {
                    _told.Add(id);
                    _approached.Add(id);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Map;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// What the station remembers for the crew's map (docs/features/M3-07), registered as <see cref="IMapKnowledge"/>.
    /// One record per place, written by every source alike (the rover's visits, the sweep, the sonar, the evening
    /// pillar), each state only moving forward and published once per save (<see cref="MapPlaceSeen"/>,
    /// <see cref="MapPlaceVisited"/>). "The map never lies": a place is inked only while visited and in the station's
    /// reach, and the chart pen draws each inked place once, in place order, at the next look. Nothing allocates after
    /// construction except <see cref="Capture"/>.
    /// </summary>
    public sealed class MapKnowledge : IMapKnowledge
    {
        private readonly EventBus _events;
        private readonly IStationReach _reach;
        private readonly MapPlace[] _places;
        private readonly MapPlaceProbe[] _probes;
        private readonly MapPlaceState[] _states;
        private readonly bool[] _drawn;
        private readonly Dictionary<string, int> _index = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <param name="places">Home first, then in the order the chart pen inks them.</param>
        /// <param name="probes">One per place, in the same order.</param>
        public MapKnowledge(EventBus events, IStationReach reach, IReadOnlyList<MapPlace> places,
            IReadOnlyList<MapPlaceProbe> probes)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _reach = reach ?? throw new ArgumentNullException(nameof(reach));
            if (places == null || probes == null || places.Count != probes.Count)
            {
                throw new ArgumentException("Every map place needs exactly one probe.", nameof(probes));
            }

            _places = new MapPlace[places.Count];
            _probes = new MapPlaceProbe[places.Count];
            for (int i = 0; i < places.Count; i++)
            {
                MapPlace place = places[i];
                if (string.IsNullOrEmpty(place.Id) || _index.ContainsKey(place.Id))
                {
                    throw new ArgumentException($"Map place '{place.Id}' is empty or listed twice.", nameof(places));
                }

                _places[i] = place;
                _probes[i] = probes[i];
                _index.Add(place.Id, i);
            }

            _states = new MapPlaceState[_places.Length];
            _drawn = new bool[_places.Length];
        }

        public int PlaceCount => _places.Length;

        public int InkedSinceLastLook
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _places.Length; i++)
                {
                    if (!_drawn[i] && IsInked(i))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public MapPlace GetPlace(int index)
        {
            return _places[index];
        }

        /// <summary>The index of place <paramref name="placeId"/>, or -1.</summary>
        public int IndexOf(string placeId)
        {
            return placeId != null && _index.TryGetValue(placeId, out int index) ? index : -1;
        }

        public MapPlaceState GetState(string placeId)
        {
            int index = IndexOf(placeId);
            return index >= 0 ? _states[index] : MapPlaceState.Sketched;
        }

        public MapPlaceState GetState(int index)
        {
            return _states[index];
        }

        public bool IsInked(string placeId)
        {
            int index = IndexOf(placeId);
            return index >= 0 && IsInked(index);
        }

        public bool IsInked(int index)
        {
            return _states[index] == MapPlaceState.Visited && _reach.IsInReach(_places[index].Position);
        }

        public bool IsInkDrawn(string placeId)
        {
            int index = IndexOf(placeId);
            return index >= 0 && _drawn[index];
        }

        public MapPlaceDeeds GetDeeds(string placeId)
        {
            int index = IndexOf(placeId);
            Func<MapPlaceDeeds> deeds = index >= 0 ? _probes[index].Deeds : null;
            return deeds != null ? deeds() : MapPlaceDeeds.None;
        }

        public string GetStateLineKey(string placeId)
        {
            int index = IndexOf(placeId);
            Func<string> line = index >= 0 ? _probes[index].StateLine : null;
            return line?.Invoke();
        }

        /// <summary>
        /// Sketched becomes seen (a pencil ring); true when it changed. <paramref name="announce"/> publishes
        /// <see cref="MapPlaceSeen"/> (not while catching up with a loaded save).
        /// </summary>
        public bool MarkSeen(int index, bool announce = true)
        {
            if (_states[index] != MapPlaceState.Sketched)
            {
                return false;
            }

            _states[index] = MapPlaceState.Seen;
            if (announce)
            {
                _events.Publish(new MapPlaceSeen(_places[index].Id, _places[index].Position));
            }

            return true;
        }

        /// <summary>
        /// Any state becomes visited; true when it changed. A place beyond the rim is never visited.
        /// <paramref name="announce"/> publishes <see cref="MapPlaceVisited"/>.
        /// </summary>
        public bool MarkVisited(int index, bool announce = true)
        {
            if (_states[index] == MapPlaceState.Visited || !_probes[index].Visitable)
            {
                return false;
            }

            _states[index] = MapPlaceState.Visited;
            if (announce)
            {
                _events.Publish(new MapPlaceVisited(_places[index].Id, _places[index].Position));
            }

            return true;
        }

        /// <summary>
        /// Notes the places 07 is within (<paramref name="margin"/> metres beyond their radius) as visited; with
        /// <paramref name="readState"/>, also reads every probe (saved progress, answers and pillars). The first poll
        /// after a load passes <paramref name="announce"/> false: what the save already shows is not news.
        /// </summary>
        public void Poll(Vector3 rover, float margin, bool readState, bool announce)
        {
            for (int i = 0; i < _places.Length; i++)
            {
                if (_states[i] == MapPlaceState.Visited)
                {
                    continue;
                }

                MapPlace place = _places[i];
                float reach = place.Radius + margin;
                MapPlaceProbe probe = _probes[i];
                if (SurfaceRules.HorizontalDistanceSquared(rover, place.Position) <= reach * reach
                    || (readState && probe.Visited != null && probe.Visited()))
                {
                    MarkVisited(i, announce);
                }

                if (readState && _states[i] == MapPlaceState.Sketched && probe.Seen != null && probe.Seen())
                {
                    MarkSeen(i, announce);
                }
            }
        }

        /// <summary>
        /// The next place the chart pen inks (inked, not drawn yet, in place order), marked drawn; -1 when the pen
        /// has nothing left to draw.
        /// </summary>
        public int TakeNextInk()
        {
            for (int i = 0; i < _places.Length; i++)
            {
                if (!_drawn[i] && IsInked(i))
                {
                    _drawn[i] = true;
                    return i;
                }
            }

            return -1;
        }

        public MapSaveData Capture(bool hatchOpen)
        {
            return new MapSaveData
            {
                seen = Ids(MapPlaceState.Seen, false),
                visited = Ids(MapPlaceState.Visited, false),
                inked = Ids(MapPlaceState.Visited, true),
                hatchOpen = hatchOpen,
            };
        }

        /// <summary>Restores the saved record; unknown ids (a place no longer on the map) are ignored.</summary>
        public void Restore(MapSaveData data)
        {
            Array.Clear(_states, 0, _states.Length);
            Array.Clear(_drawn, 0, _drawn.Length);
            if (data == null)
            {
                return;
            }

            Apply(data.seen, MapPlaceState.Seen);
            Apply(data.visited, MapPlaceState.Visited);
            if (data.inked == null)
            {
                return;
            }

            foreach (string id in data.inked)
            {
                int index = IndexOf(id);
                if (index >= 0 && _states[index] == MapPlaceState.Visited)
                {
                    _drawn[index] = true;
                }
            }
        }

        private void Apply(string[] ids, MapPlaceState state)
        {
            if (ids == null)
            {
                return;
            }

            foreach (string id in ids)
            {
                int index = IndexOf(id);
                if (index >= 0 && (state != MapPlaceState.Visited || _probes[index].Visitable))
                {
                    _states[index] = state;
                }
            }
        }

        private string[] Ids(MapPlaceState state, bool drawnOnly)
        {
            var ids = new List<string>();
            for (int i = 0; i < _places.Length; i++)
            {
                if (_states[i] == state && (!drawnOnly || _drawn[i]))
                {
                    ids.Add(_places[i].Id);
                }
            }

            return ids.ToArray();
        }
    }
}

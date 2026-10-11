using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Map;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// The deck lookout's "seen" sweep (level review L5, cutscenes §6 shot 3): every place in view that 07 has not
    /// visited answers once with a faint blink, in turn round the horizon clockwise from where 07 faces. Places beyond
    /// the rim never take part (they stay pencil notes). The order is picked once per ride; ticking never allocates.
    /// </summary>
    public sealed class LookoutSweep
    {
        private readonly List<int> _order = new List<int>();
        private readonly List<float> _turns = new List<float>();
        private int _next;
        private float _nextAt;

        /// <summary>Places still to blink this ride.</summary>
        public int Remaining => _order.Count - _next;

        public bool Done => _next >= _order.Count;

        /// <summary>Every place has blinked and the last blink has had its moment.</summary>
        public bool IsFinished(float now)
        {
            return Done && now >= _nextAt;
        }

        /// <summary>
        /// Picks the places to blink: in view of <paramref name="eye"/>, not visited and, unless
        /// <paramref name="everyUnvisited"/> (the first ride), not seen before either; the first blinks
        /// <paramref name="delay"/> seconds after <paramref name="now"/>.
        /// </summary>
        public void Begin(MapKnowledge knowledge, ISightline sightline, Vector3 eye, Vector3 facing,
            bool everyUnvisited, MapTuning tuning, float now, float delay)
        {
            _order.Clear();
            _turns.Clear();
            _next = 0;
            _nextAt = now + delay;
            float facingAngle = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
            for (int i = 0; i < knowledge.PlaceCount; i++)
            {
                MapPlace place = knowledge.GetPlace(i);
                MapPlaceState state = knowledge.GetState(i);
                bool wanted = place.Kind != MapPlaceKind.Region && state != MapPlaceState.Visited
                    && (everyUnvisited || state == MapPlaceState.Sketched);
                if (!wanted || !sightline.IsClear(eye, place.Position + Vector3.up * tuning.LightHeight, place.Radius,
                        tuning.SightStep, tuning.SightClearance))
                {
                    continue;
                }

                Vector3 to = place.Position - eye;
                float turn = Mathf.Repeat(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg - facingAngle, 360f);
                int at = 0;
                while (at < _turns.Count && _turns[at] <= turn)
                {
                    at++;
                }

                _turns.Insert(at, turn);
                _order.Insert(at, i);
            }
        }

        /// <summary>The place whose turn to blink has come, or -1.</summary>
        public int Tick(float now, float interval)
        {
            if (Done || now < _nextAt)
            {
                return -1;
            }

            _nextAt = now + interval;
            return _order[_next++];
        }

        /// <summary>Stops the sweep where it is (07 left the deck).</summary>
        public void Stop()
        {
            _next = _order.Count;
            _nextAt = float.NegativeInfinity;
        }
    }
}

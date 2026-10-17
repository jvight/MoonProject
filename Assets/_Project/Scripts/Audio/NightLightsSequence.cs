using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The first nightfall's light sequence (opening v2 B10): the base's built structures light one by one, nearest
    /// to the base first, each a step higher on the pentatonic ladder. Holds the built structures (added and removed
    /// as the base changes) and, once begun, hands out one lamp per interval. Allocation-free while it runs.
    /// </summary>
    public sealed class NightLightsSequence
    {
        private const int InitialCapacity = 32;

        private readonly List<Lamp> _lamps = new List<Lamp>(InitialCapacity);
        private readonly NearestFirst _order = new NearestFirst();
        private int _next = -1;
        private float _wait;

        /// <summary>How many built structures will light.</summary>
        public int Count => _lamps.Count;

        /// <summary>True while lamps are still to light.</summary>
        public bool Running => _next >= 0 && _next < _lamps.Count;

        /// <summary>A structure was built (or restored built): it lights at nightfall.</summary>
        public void Add(int instanceId, Vector3 position)
        {
            for (int i = 0; i < _lamps.Count; i++)
            {
                if (_lamps[i].InstanceId == instanceId)
                {
                    _lamps[i] = new Lamp(instanceId, position);
                    return;
                }
            }

            _lamps.Add(new Lamp(instanceId, position));
        }

        /// <summary>A structure was taken down: it no longer lights.</summary>
        public void Remove(int instanceId)
        {
            for (int i = 0; i < _lamps.Count; i++)
            {
                if (_lamps[i].InstanceId == instanceId)
                {
                    _lamps.RemoveAt(i);
                    if (_next > i)
                    {
                        _next--;
                    }

                    return;
                }
            }
        }

        /// <summary>Night fell: after <paramref name="delay"/> seconds the lamps start lighting, nearest to
        /// <paramref name="origin"/> first.</summary>
        public void Begin(Vector3 origin, float delay)
        {
            _order.Origin = origin;
            _lamps.Sort(_order);
            _next = 0;
            _wait = Mathf.Max(0f, delay);
        }

        /// <summary>Stops the sequence (a sunrise before every lamp lit).</summary>
        public void Stop()
        {
            _next = -1;
        }

        /// <summary>
        /// Advances by <paramref name="deltaTime"/>; true when the next lamp lights now, with its
        /// <paramref name="position"/> and its <paramref name="order"/> (0 = the first).
        /// </summary>
        public bool Step(float deltaTime, float interval, out Vector3 position, out int order)
        {
            position = default;
            order = -1;
            if (!Running)
            {
                return false;
            }

            _wait -= Mathf.Max(0f, deltaTime);
            if (_wait > 0f)
            {
                return false;
            }

            order = _next;
            position = _lamps[_next].Position;
            _next++;
            _wait += Mathf.Max(0f, interval);
            return true;
        }

        private readonly struct Lamp
        {
            public Lamp(int instanceId, Vector3 position)
            {
                InstanceId = instanceId;
                Position = position;
            }

            public int InstanceId { get; }
            public Vector3 Position { get; }
        }

        private sealed class NearestFirst : IComparer<Lamp>
        {
            public Vector3 Origin { get; set; }

            public int Compare(Lamp a, Lamp b)
            {
                int byDistance = (a.Position - Origin).sqrMagnitude.CompareTo((b.Position - Origin).sqrMagnitude);
                return byDistance != 0 ? byDistance : a.InstanceId.CompareTo(b.InstanceId);
            }
        }
    }
}

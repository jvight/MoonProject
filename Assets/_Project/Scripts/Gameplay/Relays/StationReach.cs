using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How far home reaches (docs/features/M3-06): node 0 is home, a circle of the radio tower's clear-signal radius
    /// around the base pad; every other node is a relay mast with a fixed circle. A restored mast is lit only while it
    /// links back to home, meaning its circle overlaps the circle of a node that is itself lit, so the network grows
    /// outward from home and a mast restored beyond the lit frontier waits until a neighbour or a stronger tower
    /// reaches it. Reach is the union of the lit circles. Pure and allocation-free after construction; relinks
    /// whenever the home radius or a mast changes.
    /// </summary>
    public sealed class StationReach : IStationReach
    {
        /// <summary>Home's node id (the radio tower at the base).</summary>
        public const string HomeId = "home";

        public const int Home = 0;

        private readonly string[] _ids;
        private readonly Vector3[] _positions;
        private readonly float[] _radii;
        private readonly bool[] _restored;
        private readonly bool[] _lit;
        private readonly int[] _queue;
        private readonly int[] _depth;

        /// <param name="home">The base pad's centre on the surface (home's node and hop pad).</param>
        /// <param name="homeRadius">The tower's clear-signal radius (m).</param>
        /// <param name="mastIds">Each mast's anchor id ("relay.0", ...).</param>
        /// <param name="mastPositions">Each mast's pad centre on the surface.</param>
        /// <param name="mastRadius">Every mast's reach (m).</param>
        public StationReach(Vector3 home, float homeRadius, IReadOnlyList<string> mastIds,
            IReadOnlyList<Vector3> mastPositions, float mastRadius)
        {
            if (mastIds == null)
            {
                throw new ArgumentNullException(nameof(mastIds));
            }

            if (mastPositions == null || mastPositions.Count != mastIds.Count)
            {
                throw new ArgumentException("Every mast needs one position.", nameof(mastPositions));
            }

            int count = mastIds.Count + 1;
            _ids = new string[count];
            _positions = new Vector3[count];
            _radii = new float[count];
            _restored = new bool[count];
            _lit = new bool[count];
            _queue = new int[count];
            _depth = new int[count];
            _ids[Home] = HomeId;
            _positions[Home] = home;
            _radii[Home] = Mathf.Max(0f, homeRadius);
            _restored[Home] = true;
            for (int i = 1; i < count; i++)
            {
                _ids[i] = mastIds[i - 1];
                _positions[i] = mastPositions[i - 1];
                _radii[i] = Mathf.Max(0f, mastRadius);
            }

            Relink();
        }

        public int NodeCount => _ids.Length;

        public int LitCount { get; private set; }

        /// <summary>Lit masts (home not counted).</summary>
        public int LitMasts => LitCount - 1;

        public float HomeRadius => _radii[Home];

        public RelayNode GetNode(int index)
        {
            return new RelayNode(_ids[index], _positions[index], _lit[index]);
        }

        public string Id(int node)
        {
            return _ids[node];
        }

        public Vector3 Position(int node)
        {
            return _positions[node];
        }

        public float Radius(int node)
        {
            return _radii[node];
        }

        public bool IsLit(int node)
        {
            return _lit[node];
        }

        public bool IsRestored(int node)
        {
            return _restored[node];
        }

        /// <summary>Links between lit <paramref name="node"/> and home (0 for home; meaningless while unlit).</summary>
        public int Depth(int node)
        {
            return _depth[node];
        }

        /// <summary>The node with id <paramref name="id"/>, or -1.</summary>
        public int IndexOf(string id)
        {
            for (int i = 0; i < _ids.Length; i++)
            {
                if (string.Equals(_ids[i], id, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsInReach(Vector3 position)
        {
            for (int i = 0; i < _ids.Length; i++)
            {
                if (_lit[i] && SurfaceRules.HorizontalDistanceSquared(position, _positions[i]) <= _radii[i] * _radii[i])
                {
                    return true;
                }
            }

            return false;
        }

        public float DistanceToNearestNode(Vector3 position)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < _ids.Length; i++)
            {
                if (_lit[i])
                {
                    nearest = Mathf.Min(nearest, SurfaceRules.HorizontalDistanceSquared(position, _positions[i]));
                }
            }

            return Mathf.Sqrt(nearest);
        }

        /// <summary>The lit node nearest to lit <paramref name="node"/> whose circle overlaps its own, or -1.</summary>
        public int NearestLink(int node)
        {
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < _ids.Length; i++)
            {
                if (i == node || !_lit[i] || !Overlap(i, node))
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistanceSquared(_positions[i], _positions[node]);
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>Widens or narrows home's circle (a tower level) and relinks.</summary>
        public void SetHomeRadius(float radius)
        {
            _radii[Home] = Mathf.Max(0f, radius);
            Relink();
        }

        /// <summary>Marks mast node <paramref name="node"/> restored (or not) and relinks.</summary>
        public void SetRestored(int node, bool restored)
        {
            if (node == Home)
            {
                throw new ArgumentException("Home is always restored.", nameof(node));
            }

            _restored[node] = restored;
            Relink();
        }

        /// <summary>Copies every node's lit flag into <paramref name="into"/> (for spotting newly lit masts).</summary>
        public void CopyLit(bool[] into)
        {
            Array.Copy(_lit, into, _lit.Length);
        }

        private bool Overlap(int a, int b)
        {
            float reach = _radii[a] + _radii[b];
            return SurfaceRules.HorizontalDistanceSquared(_positions[a], _positions[b]) <= reach * reach;
        }

        /// <summary>Breadth-first from home over restored masts whose circles overlap a lit one.</summary>
        private void Relink()
        {
            Array.Clear(_lit, 0, _lit.Length);
            _lit[Home] = true;
            _depth[Home] = 0;
            _queue[0] = Home;
            int head = 0;
            int tail = 1;
            while (head < tail)
            {
                int from = _queue[head++];
                for (int i = 0; i < _ids.Length; i++)
                {
                    if (!_lit[i] && _restored[i] && Overlap(from, i))
                    {
                        _lit[i] = true;
                        _depth[i] = _depth[from] + 1;
                        _queue[tail++] = i;
                    }
                }
            }

            LitCount = tail;
        }
    }
}

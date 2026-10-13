using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One handshake across the relay network (north-star §4a beat 7). The lit nodes (home first) are joined into a
    /// tree, each one linked to the nearest node already joined, growing out from home as the masts were linked. From
    /// the newly lit mast a bead runs every link one hop at a time, so the pulse races mast to mast out to the far
    /// masts and back to home, and each lamp flashes as the bead reaches it. Plain logic: arrays grow once to the
    /// network's size, then starting and sampling are allocation-free.
    /// </summary>
    public sealed class NetworkPulse
    {
        private const int Home = 0;

        private readonly NetworkPulseSettings _settings;
        private Vector3[] _lamps = Array.Empty<Vector3>();
        private int[] _parent = Array.Empty<int>();
        private int[] _hops = Array.Empty<int>();
        private int[] _queue = Array.Empty<int>();
        private float[] _nearest = Array.Empty<float>();
        private bool[] _joined = Array.Empty<bool>();
        private int _count;
        private int _origin = -1;
        private int _farthestHop;
        private float _startedAt = float.NegativeInfinity;

        public NetworkPulse(NetworkPulseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Nodes of the network the last handshake was started over (home is node 0).</summary>
        public int Count => _count;

        /// <summary>Hops from the newly lit mast to the farthest node the handshake reaches.</summary>
        public int FarthestHop => _farthestHop;

        /// <summary>
        /// Starts a handshake at <paramref name="time"/> from node <paramref name="origin"/> over the first
        /// <paramref name="count"/> nodes: <paramref name="lamps"/> are their lamps, <paramref name="lit"/> which are
        /// lit. Home (node 0) and the origin always take part.
        /// </summary>
        public void Start(Vector3[] lamps, bool[] lit, int count, int origin, float time)
        {
            if (origin <= Home || origin >= count)
            {
                throw new ArgumentOutOfRangeException(nameof(origin), origin, "The origin must be a mast in the network.");
            }

            Grow(count);
            _count = count;
            _origin = origin;
            _startedAt = time;
            Array.Copy(lamps, _lamps, count);
            JoinTree(lit);
            CountHops();
        }

        /// <summary>
        /// Hops from the newly lit mast to <paramref name="node"/> along the network's links; -1 when the node is
        /// not lit (it takes no part).
        /// </summary>
        public int Hops(int node)
        {
            return node < _count ? _hops[node] : -1;
        }

        /// <summary>The node <paramref name="node"/> links to on its way home; -1 for home and nodes not lit.</summary>
        public int Parent(int node)
        {
            return node < _count ? _parent[node] : -1;
        }

        /// <summary>When the bead reaches <paramref name="node"/>; infinity when the node takes no part.</summary>
        public float ArrivalTime(int node)
        {
            int hops = Hops(node);
            return hops < 0 ? float.PositiveInfinity : _startedAt + hops * _settings.HopSeconds;
        }

        /// <summary>True while any flash or bead of the handshake is still showing at <paramref name="time"/>.</summary>
        public bool IsActive(float time)
        {
            float length = _farthestHop * _settings.HopSeconds + _settings.FlashRise + _settings.FlashFall;
            return _origin > Home && time >= _startedAt && time - _startedAt < length;
        }

        /// <summary>
        /// How bright <paramref name="node"/>'s flash is at <paramref name="time"/>: 0 dark, 1 a lamp's full flash
        /// (the newly lit mast's own flash is boosted).
        /// </summary>
        public float Flash(int node, float time)
        {
            float since = time - ArrivalTime(node);
            if (since <= 0f || since >= _settings.FlashRise + _settings.FlashFall)
            {
                return 0f;
            }

            float rise = Mathf.SmoothStep(0f, 1f, since / _settings.FlashRise);
            float fall = 1f - Mathf.SmoothStep(0f, 1f, (since - _settings.FlashRise) / _settings.FlashFall);
            float boost = node == _origin ? _settings.OriginBoost : 1f;
            return boost * Mathf.Min(rise, fall);
        }

        /// <summary>
        /// The bead on the link from <paramref name="node"/> to its <see cref="Parent"/> at <paramref name="time"/>:
        /// false when it is not on that link. It leaves whichever end the handshake reaches first.
        /// </summary>
        public bool Bead(int node, float time, out Vector3 position, out float level)
        {
            position = Vector3.zero;
            level = 0f;
            int parent = Parent(node);
            if (parent < 0)
            {
                return false;
            }

            int from = _hops[parent] < _hops[node] ? parent : node;
            int to = from == parent ? node : parent;
            float t = (time - ArrivalTime(from)) / _settings.HopSeconds;
            if (t <= 0f || t >= 1f)
            {
                return false;
            }

            float eased = Mathf.SmoothStep(0f, 1f, t);
            Vector3 a = _lamps[from];
            Vector3 b = _lamps[to];
            float span = Horizontal(a, b);
            position = Vector3.Lerp(a, b, eased) + Vector3.up * (4f * eased * (1f - eased) * span
                * _settings.ArcPerMetre);
            float fade = _settings.BeadFade;
            level = fade > 0f
                ? Mathf.Min(Mathf.SmoothStep(0f, 1f, t / fade), Mathf.SmoothStep(0f, 1f, (1f - t) / fade))
                : 1f;
            return true;
        }

        private void Grow(int count)
        {
            if (_lamps.Length >= count)
            {
                return;
            }

            _lamps = new Vector3[count];
            _parent = new int[count];
            _hops = new int[count];
            _queue = new int[count];
            _nearest = new float[count];
            _joined = new bool[count];
        }

        // Prim's tree from home over the nodes taking part: each joins through the nearest node already joined.
        private void JoinTree(bool[] lit)
        {
            for (int i = 0; i < _count; i++)
            {
                _joined[i] = false;
                _parent[i] = -1;
                _nearest[i] = float.PositiveInfinity;
            }

            int joining = Home;
            while (joining >= 0)
            {
                _joined[joining] = true;
                int next = -1;
                for (int i = 0; i < _count; i++)
                {
                    if (_joined[i] || !TakesPart(i, lit))
                    {
                        continue;
                    }

                    float distance = Horizontal(_lamps[joining], _lamps[i]);
                    if (distance < _nearest[i])
                    {
                        _nearest[i] = distance;
                        _parent[i] = joining;
                    }

                    if (next < 0 || _nearest[i] < _nearest[next])
                    {
                        next = i;
                    }
                }

                joining = next;
            }
        }

        private bool TakesPart(int node, bool[] lit)
        {
            return node == Home || node == _origin || lit[node];
        }

        // Breadth first from the origin over the tree's links (a node's parent and its children).
        private void CountHops()
        {
            for (int i = 0; i < _count; i++)
            {
                _hops[i] = -1;
            }

            _hops[_origin] = 0;
            _farthestHop = 0;
            int head = 0;
            int tail = 0;
            _queue[tail++] = _origin;
            while (head < tail)
            {
                int node = _queue[head++];
                for (int i = 0; i < _count; i++)
                {
                    bool linked = _joined[i] && (_parent[node] == i || _parent[i] == node);
                    if (linked && _hops[i] < 0)
                    {
                        _hops[i] = _hops[node] + 1;
                        _farthestHop = Mathf.Max(_farthestHop, _hops[i]);
                        _queue[tail++] = i;
                    }
                }
            }
        }

        private static float Horizontal(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}

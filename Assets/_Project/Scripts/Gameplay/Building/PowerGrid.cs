using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Pure electrical power grid for 07's base (0.5.7). Connects nodes by horizontal reach (sources and pylons with
    /// <c>ConnectionRadius &gt; 0</c> extend the network via union-find, rebuilt only when nodes are added or removed)
    /// and steps generation, battery storage charge/discharge, and consumer supply ratios (0..1) with zero GC per step.
    /// </summary>
    public sealed class PowerGrid
    {
        public const float SecondsPerHour = 3600f;
        private const int InitialCapacity = 16;

        private PowerNode[] _nodes;
        private int[] _parent;
        private float[] _compGenerated;
        private float[] _compConsumed;
        private float[] _compCapacity;
        private float[] _compStored;
        private int _count;
        private bool _dirty;

        public PowerGrid(int initialCapacity = InitialCapacity)
        {
            int cap = Mathf.Max(4, initialCapacity);
            _nodes = new PowerNode[cap];
            _parent = new int[cap];
            _compGenerated = new float[cap];
            _compConsumed = new float[cap];
            _compCapacity = new float[cap];
            _compStored = new float[cap];
        }

        /// <summary>Number of active nodes currently in the grid.</summary>
        public int Count => _count;

        /// <summary>Number of times union-find connectivity has been rebuilt since creation.</summary>
        public int RebuildCount { get; private set; }

        public PowerNode NodeAt(int index)
        {
            if (index < 0 || index >= _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _nodes[index];
        }

        /// <summary>Looks up a node by its <paramref name="id"/>.</summary>
        public bool TryGetNode(int id, out PowerNode node)
        {
            int index = IndexOf(id);
            if (index >= 0)
            {
                node = _nodes[index];
                return true;
            }

            node = default;
            return false;
        }

        /// <summary>Returns the current supply ratio (0..1) of node <paramref name="id"/>, or 0 if not found.</summary>
        public float GetSupplyRatio(int id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _nodes[index].SupplyRatio : 0f;
        }

        /// <summary>Returns the current stored energy (Wh) of node <paramref name="id"/>, or 0 if not found.</summary>
        public float GetStoredWattHours(int id)
        {
            int index = IndexOf(id);
            return index >= 0 ? _nodes[index].StoredWattHours : 0f;
        }

        /// <summary>Sets the stored energy (Wh) of an existing node without rebuilding connectivity.</summary>
        public bool SetStoredWattHours(int id, float storedWattHours)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                return false;
            }

            _nodes[index].StoredWattHours = Mathf.Clamp(storedWattHours, 0f, _nodes[index].StoreWattHours);
            return true;
        }

        /// <summary>Adds a node to the grid and marks connectivity dirty.</summary>
        public void AddNode(PowerNode node)
        {
            if (IndexOf(node.Id) >= 0)
            {
                throw new InvalidOperationException($"PowerGrid already contains a node with id {node.Id}.");
            }

            EnsureCapacity(_count + 1);
            _nodes[_count] = node;
            _count++;
            _dirty = true;
        }

        /// <summary>Removes the node with <paramref name="id"/> from the grid and marks connectivity dirty.</summary>
        public bool RemoveNode(int id)
        {
            int index = IndexOf(id);
            if (index < 0)
            {
                return false;
            }

            int last = _count - 1;
            if (index != last)
            {
                _nodes[index] = _nodes[last];
            }

            _nodes[last] = default;
            _count = last;
            _dirty = true;
            return true;
        }

        /// <summary>Removes all nodes from the grid.</summary>
        public void Clear()
        {
            if (_count == 0)
            {
                return;
            }

            Array.Clear(_nodes, 0, _count);
            _count = 0;
            _dirty = false;
        }

        /// <summary>True when <paramref name="idA"/> and <paramref name="idB"/> belong to the same connected subgrid.</summary>
        /// <summary>
        /// True when a node at <paramref name="a"/> with reach <paramref name="radiusA"/> and one at
        /// <paramref name="b"/> with reach <paramref name="radiusB"/> share a grid: two nodes with reach link within the
        /// longer one, a node without reach links to one with reach within that one's, and two without reach never link.
        /// </summary>
        public static bool CanLink(Vector3 a, float radiusA, Vector3 b, float radiusB)
        {
            float reach = Mathf.Max(radiusA, radiusB);
            return (radiusA > 0f || radiusB > 0f) && SurfaceRules.HorizontalDistanceSquared(a, b) <= reach * reach;
        }

        public bool AreConnected(int idA, int idB)
        {
            RebuildIfNeeded();
            int indexA = IndexOf(idA);
            int indexB = IndexOf(idB);
            return indexA >= 0 && indexB >= 0 && _parent[indexA] == _parent[indexB];
        }

        /// <summary>
        /// Steps power generation, battery storage charging/discharging, and consumer supply ratios over
        /// <paramref name="deltaTime"/> seconds at <paramref name="sunlight01"/> (0 = night/eclipse, 1 = full sun).
        /// Rebuilds connectivity only if nodes changed since the last step. Zero GC allocations.
        /// </summary>
        public void Step(float deltaTime, float sunlight01)
        {
            if (_count == 0)
            {
                return;
            }

            RebuildIfNeeded();

            float sun = Mathf.Clamp01(sunlight01);
            float dt = Mathf.Max(0f, deltaTime);
            float dtHours = dt / SecondsPerHour;

            for (int i = 0; i < _count; i++)
            {
                _compGenerated[i] = 0f;
                _compConsumed[i] = 0f;
                _compCapacity[i] = 0f;
                _compStored[i] = 0f;
            }

            for (int i = 0; i < _count; i++)
            {
                int root = _parent[i];
                PowerNode node = _nodes[i];
                _compGenerated[root] += node.ProduceWatts * sun;
                _compConsumed[root] += node.ConsumeWatts;
                _compCapacity[root] += node.StoreWattHours;
                _compStored[root] += node.StoredWattHours;
            }

            for (int i = 0; i < _count; i++)
            {
                int root = _parent[i];
                PowerNode node = _nodes[i];
                float generatedW = _compGenerated[root];
                float demandedW = _compConsumed[root];
                float capacityWh = _compCapacity[root];
                float storedWh = _compStored[root];

                if (generatedW >= demandedW)
                {
                    node.SupplyRatio = node.ConsumeWatts > 0f ? 1f : 0f;
                    float roomWh = capacityWh - storedWh;
                    if (node.StoreWattHours > 0f && roomWh > 0f && dtHours > 0f)
                    {
                        float surplusWh = (generatedW - demandedW) * dtHours;
                        float totalChargeWh = Mathf.Min(roomWh, surplusWh);
                        float nodeRoomWh = Mathf.Max(0f, node.StoreWattHours - node.StoredWattHours);
                        float share = nodeRoomWh / roomWh;
                        node.StoredWattHours = Mathf.Min(
                            node.StoreWattHours,
                            node.StoredWattHours + totalChargeWh * share);
                    }
                }
                else
                {
                    float deficitW = demandedW - generatedW;
                    float neededWh = deficitW * dtHours;
                    float dischargedWh = Mathf.Min(storedWh, neededWh);
                    float batteryWatts = dtHours > 0f ? dischargedWh / dtHours : (storedWh > 0f ? deficitW : 0f);
                    float suppliedW = generatedW + batteryWatts;
                    node.SupplyRatio = node.ConsumeWatts > 0f ? Mathf.Clamp01(suppliedW / demandedW) : 0f;

                    if (node.StoreWattHours > 0f && storedWh > 0f && dischargedWh > 0f)
                    {
                        float share = node.StoredWattHours / storedWh;
                        node.StoredWattHours = Mathf.Max(0f, node.StoredWattHours - dischargedWh * share);
                    }
                }

                _nodes[i] = node;
            }
        }

        private void RebuildIfNeeded()
        {
            if (!_dirty)
            {
                return;
            }

            _dirty = false;
            RebuildCount++;

            for (int i = 0; i < _count; i++)
            {
                _parent[i] = i;
            }

            for (int i = 0; i < _count; i++)
            {
                float radiusI = _nodes[i].ConnectionRadius;
                if (radiusI <= 0f)
                {
                    continue;
                }

                for (int j = i + 1; j < _count; j++)
                {
                    float radiusJ = _nodes[j].ConnectionRadius;
                    if (radiusJ <= 0f)
                    {
                        continue;
                    }

                    if (CanLink(_nodes[i].Position, radiusI, _nodes[j].Position, radiusJ))
                    {
                        Union(i, j);
                    }
                }
            }

            for (int k = 0; k < _count; k++)
            {
                if (_nodes[k].ConnectionRadius > 0f)
                {
                    continue;
                }

                int bestHub = -1;
                float bestDistSq = float.MaxValue;
                for (int h = 0; h < _count; h++)
                {
                    float hubRadius = _nodes[h].ConnectionRadius;
                    if (hubRadius <= 0f)
                    {
                        continue;
                    }

                    float distSq = SurfaceRules.HorizontalDistanceSquared(_nodes[k].Position, _nodes[h].Position);
                    if (distSq <= hubRadius * hubRadius && distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestHub = h;
                    }
                }

                if (bestHub >= 0)
                {
                    _parent[k] = Find(bestHub);
                }
            }

            for (int i = 0; i < _count; i++)
            {
                _parent[i] = Find(i);
            }
        }

        private int Find(int index)
        {
            int root = index;
            while (_parent[root] != root)
            {
                root = _parent[root];
            }

            int current = index;
            while (current != root)
            {
                int next = _parent[current];
                _parent[current] = root;
                current = next;
            }

            return root;
        }

        private void Union(int a, int b)
        {
            int rootA = Find(a);
            int rootB = Find(b);
            if (rootA != rootB)
            {
                if (rootA < rootB)
                {
                    _parent[rootB] = rootA;
                }
                else
                {
                    _parent[rootA] = rootB;
                }
            }
        }

        private int IndexOf(int id)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_nodes[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _nodes.Length)
            {
                return;
            }

            int newCap = _nodes.Length * 2;
            while (newCap < required)
            {
                newCap *= 2;
            }

            Array.Resize(ref _nodes, newCap);
            Array.Resize(ref _parent, newCap);
            Array.Resize(ref _compGenerated, newCap);
            Array.Resize(ref _compConsumed, newCap);
            Array.Resize(ref _compCapacity, newCap);
            Array.Resize(ref _compStored, newCap);
        }
    }
}

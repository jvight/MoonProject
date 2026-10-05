using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Geometry of one tire track: a ring buffer of ground samples turned into a flat ribbon. Unlike a TrailRenderer
    /// (which orients every point by the emitter's current rotation and therefore twists), each point stores its own
    /// ground normal and a side vector = normal x travel direction, so the ribbon always lies flat on the surface it was
    /// laid on. A reversal of travel direction starts a new strip instead of folding the ribbon into a bow tie.
    /// Fixed capacity, no allocations after construction.
    /// </summary>
    public sealed class TrackRibbon
    {
        private const float MinHeadDistance = 0.01f;

        private readonly Vector3[] _positions;
        private readonly Vector3[] _normals;
        private readonly Vector3[] _sides;
        private readonly float[] _times;
        private readonly float[] _distances;
        private readonly bool[] _starts;
        private int _first;
        private int _count;
        private bool _open;
        private bool _hasDirection;
        private Vector3 _lastDirection;
        private Vector3 _head;
        private Vector3 _headNormal;

        public TrackRibbon(int capacity)
        {
            Capacity = Mathf.Max(2, capacity);
            _positions = new Vector3[Capacity];
            _normals = new Vector3[Capacity];
            _sides = new Vector3[Capacity];
            _times = new float[Capacity];
            _distances = new float[Capacity];
            _starts = new bool[Capacity];
        }

        public int Capacity { get; }

        /// <summary>Stored points (the live head is extra).</summary>
        public int Count => _count;

        /// <summary>True while a strip is being laid (the wheel is on the ground).</summary>
        public bool IsOpen => _open;

        /// <summary>Vertex array length <see cref="Build"/> needs.</summary>
        public int VertexCapacity => (Capacity + 1) * 2;

        /// <summary>Index array length <see cref="Build"/> needs.</summary>
        public int IndexCapacity => Capacity * 6;

        /// <summary>Feeds the wheel's ground contact for this frame.</summary>
        public void Sample(Vector3 position, Vector3 normal, float time, float segmentLength)
        {
            _head = position;
            _headNormal = normal;

            if (!_open)
            {
                Append(position, normal, Vector3.zero, time, 0f, true);
                _open = true;
                _hasDirection = false;
                return;
            }

            int last = Index(_count - 1);
            Vector3 delta = Vector3.ProjectOnPlane(position - _positions[last], normal);
            float length = delta.magnitude;
            if (length < segmentLength)
            {
                return;
            }

            Vector3 direction = delta / length;
            Vector3 side = Vector3.Cross(normal, direction).normalized;
            float distance = _distances[last] + length;

            if (_hasDirection && Vector3.Dot(direction, _lastDirection) < 0f)
            {
                // Reversing: restart from the last point so coverage stays continuous without folding the ribbon.
                Append(_positions[last], _normals[last], Vector3.Cross(_normals[last], direction).normalized,
                    time, _distances[last], true);
            }
            else if (!_hasDirection)
            {
                _sides[last] = Vector3.Cross(_normals[last], direction).normalized;
            }

            Append(position, normal, side, time, distance, false);
            _lastDirection = direction;
            _hasDirection = true;
        }

        /// <summary>Ends the current strip (the wheel left the ground).</summary>
        public void Break()
        {
            _open = false;
            _hasDirection = false;
        }

        /// <summary>Drops points older than <paramref name="lifetime"/> seconds.</summary>
        public void Expire(float time, float lifetime)
        {
            while (_count > 0 && time - _times[_first] > lifetime)
            {
                _first = (_first + 1) % Capacity;
                _count--;
            }

            if (_count == 0)
            {
                Break();
            }
        }

        /// <summary>
        /// Writes the ribbon into the given arrays (local space of <paramref name="worldToLocal"/>), oldest to newest,
        /// plus the live head segment while the strip is open. Alpha fades from <paramref name="opacity"/> to 0 between
        /// <paramref name="fadeStartAge"/> and <paramref name="lifetime"/> seconds of age.
        /// </summary>
        public void Build(Matrix4x4 worldToLocal, float time, float halfWidth, float lift, float opacity,
            float fadeStartAge, float lifetime, Vector3[] vertices, Color32[] colors, Vector2[] uvs, int[] indices,
            out int vertexCount, out int indexCount)
        {
            vertexCount = 0;
            indexCount = 0;

            for (int n = 0; n < _count; n++)
            {
                int i = Index(n);
                bool connects = n > 0 && !_starts[i];
                WritePair(worldToLocal, _positions[i], _normals[i], _sides[i], halfWidth, lift,
                    Alpha(time - _times[i], opacity, fadeStartAge, lifetime), _distances[i],
                    vertices, colors, uvs, indices, connects, ref vertexCount, ref indexCount);
            }

            if (!_open || _count == 0)
            {
                return;
            }

            int lastIndex = Index(_count - 1);
            Vector3 delta = Vector3.ProjectOnPlane(_head - _positions[lastIndex], _headNormal);
            float length = delta.magnitude;
            if (length < MinHeadDistance)
            {
                return;
            }

            Vector3 direction = delta / length;
            if (_hasDirection && Vector3.Dot(direction, _lastDirection) < 0f)
            {
                return;
            }

            Vector3 headSide = Vector3.Cross(_headNormal, direction).normalized;
            if (!_hasDirection)
            {
                // A one-point strip has no side yet: borrow the head's so the first quad is not degenerate.
                RewriteLastPairSide(worldToLocal, lastIndex, headSide, halfWidth, lift, vertices, vertexCount);
            }

            WritePair(worldToLocal, _head, _headNormal, headSide, halfWidth, lift, opacity,
                _distances[lastIndex] + length, vertices, colors, uvs, indices, true, ref vertexCount, ref indexCount);
        }

        private static float Alpha(float age, float opacity, float fadeStartAge, float lifetime)
        {
            return opacity * (1f - Smoothing.SmoothStep(fadeStartAge, lifetime, age));
        }

        private static void WritePair(Matrix4x4 worldToLocal, Vector3 position, Vector3 normal, Vector3 side,
            float halfWidth, float lift, float alpha, float distance, Vector3[] vertices, Color32[] colors,
            Vector2[] uvs, int[] indices, bool connects, ref int vertexCount, ref int indexCount)
        {
            Vector3 centre = position + normal * lift;
            Vector3 offset = side * halfWidth;
            byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f);
            var color = new Color32(255, 255, 255, a);
            int left = vertexCount;
            int right = vertexCount + 1;

            vertices[left] = worldToLocal.MultiplyPoint3x4(centre - offset);
            vertices[right] = worldToLocal.MultiplyPoint3x4(centre + offset);
            colors[left] = color;
            colors[right] = color;
            uvs[left] = new Vector2(0f, distance);
            uvs[right] = new Vector2(1f, distance);

            if (connects)
            {
                int previousLeft = left - 2;
                int previousRight = left - 1;
                indices[indexCount++] = previousLeft;
                indices[indexCount++] = left;
                indices[indexCount++] = right;
                indices[indexCount++] = previousLeft;
                indices[indexCount++] = right;
                indices[indexCount++] = previousRight;
            }

            vertexCount += 2;
        }

        private void RewriteLastPairSide(Matrix4x4 worldToLocal, int pointIndex, Vector3 side, float halfWidth,
            float lift, Vector3[] vertices, int vertexCount)
        {
            Vector3 centre = _positions[pointIndex] + _normals[pointIndex] * lift;
            Vector3 offset = side * halfWidth;
            vertices[vertexCount - 2] = worldToLocal.MultiplyPoint3x4(centre - offset);
            vertices[vertexCount - 1] = worldToLocal.MultiplyPoint3x4(centre + offset);
        }

        private void Append(Vector3 position, Vector3 normal, Vector3 side, float time, float distance, bool start)
        {
            int slot;
            if (_count == Capacity)
            {
                slot = _first;
                _first = (_first + 1) % Capacity;
            }
            else
            {
                slot = Index(_count);
                _count++;
            }

            _positions[slot] = position;
            _normals[slot] = normal;
            _sides[slot] = side;
            _times[slot] = time;
            _distances[slot] = distance;
            _starts[slot] = start;
        }

        private int Index(int n)
        {
            return (_first + n) % Capacity;
        }
    }
}

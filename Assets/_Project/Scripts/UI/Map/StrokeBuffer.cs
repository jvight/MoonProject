using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI.Map
{
    /// <summary>
    /// Pencil and ink strokes for the crew's map, tessellated once into UI Toolkit vertices (docs/features/M3-07: strokes
    /// pre-built at load, revealed by a parameter). A line becomes one quad per span, nudged sideways by a seeded,
    /// slowly drifting hand wobble, its darkness wavering with the pencil's grain; a soft disc becomes a fan that fades
    /// to its rim. Vertices go into chunks small enough for 16-bit indices. <see cref="Write"/> copies the first
    /// quads of the buffer into a mesh without allocating.
    /// </summary>
    internal sealed class StrokeBuffer
    {
        /// <summary>Vertices per chunk: four per quad, under the 16-bit index limit.</summary>
        public const int ChunkVertices = 60000;

        private const float Join = 0.5f;

        private readonly List<Vertex> _vertices = new List<Vertex>();
        private readonly List<ushort> _indices = new List<ushort>();
        private readonly List<Vertex[]> _chunkVertices = new List<Vertex[]>();
        private readonly List<ushort[]> _chunkIndices = new List<ushort[]>();

        /// <summary>Quads (two triangles each) added so far.</summary>
        public int QuadCount { get; private set; }

        public int ChunkCount => _chunkVertices.Count;

        public void Clear()
        {
            _vertices.Clear();
            _indices.Clear();
            _chunkVertices.Clear();
            _chunkIndices.Clear();
            QuadCount = 0;
        }

        /// <summary>
        /// Adds a hand-drawn line through <paramref name="points"/> (paper units): <paramref name="wobble"/> units of
        /// sideways drift that changes over <paramref name="wobbleLength"/> units, and <paramref name="grain"/> of
        /// darkness wavering, both from <paramref name="seed"/>.
        /// </summary>
        public void AddLine(List<Vector2> points, bool closed, float width, Color color, float wobble,
            float wobbleLength, float grain, uint seed)
        {
            int count = points.Count;
            if (count < 2)
            {
                return;
            }

            int spans = closed ? count : count - 1;
            float arc = 0f;
            Vector2 from = Wobbled(points, 0, count, 0f, wobble, wobbleLength, seed);
            for (int i = 0; i < spans; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % count];
                arc += Vector2.Distance(a, b);
                Vector2 to = Wobbled(points, (i + 1) % count, count, arc, wobble, wobbleLength, seed);
                float shade = 1f - grain * Noise01(seed ^ 0x9E3779B9u, arc / wobbleLength);
                Quad(from, to, width * 0.5f, new Color(color.r, color.g, color.b, color.a * shade));
                from = to;
            }
        }

        /// <summary>A soft warm disc: full <paramref name="color"/> out to <paramref name="core"/> of its radius,
        /// fading to nothing at the rim.</summary>
        public void AddDisc(Vector2 centre, float radius, float core, Color color, int segments)
        {
            Color rim = new Color(color.r, color.g, color.b, 0f);
            float inner = radius * Mathf.Clamp01(core);
            for (int s = 0; s < segments; s++)
            {
                float a0 = 2f * Mathf.PI * s / segments;
                float a1 = 2f * Mathf.PI * (s + 1) / segments;
                var d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                var d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                Vector2 i0 = centre + d0 * inner;
                Vector2 i1 = centre + d1 * inner;
                QuadOf(centre, centre, i1, i0, color, color, color, color);
                QuadOf(i0, i1, centre + d1 * radius, centre + d0 * radius, color, color, rim, rim);
            }
        }

        /// <summary>Closes the last chunk; call once after adding, before drawing.</summary>
        public void Finish()
        {
            if (_vertices.Count == 0)
            {
                return;
            }

            _chunkVertices.Add(_vertices.ToArray());
            _chunkIndices.Add(_indices.ToArray());
            _vertices.Clear();
            _indices.Clear();
        }

        /// <summary>Draws every chunk.</summary>
        public void Draw(MeshGenerationContext context)
        {
            for (int c = 0; c < _chunkVertices.Count; c++)
            {
                Vertex[] vertices = _chunkVertices[c];
                ushort[] indices = _chunkIndices[c];
                MeshWriteData mesh = context.Allocate(vertices.Length, indices.Length);
                mesh.SetAllVertices(vertices);
                mesh.SetAllIndices(indices);
            }
        }

        /// <summary>
        /// Draws the first <paramref name="quads"/> quads (a stroke being inked) tinted to <paramref name="alpha"/>.
        /// </summary>
        public void Write(MeshGenerationContext context, int quads, float alpha)
        {
            int remaining = Mathf.Clamp(quads, 0, QuadCount);
            for (int c = 0; c < _chunkVertices.Count && remaining > 0; c++)
            {
                Vertex[] vertices = _chunkVertices[c];
                int take = Mathf.Min(remaining, vertices.Length / 4);
                remaining -= take;
                MeshWriteData mesh = context.Allocate(take * 4, take * 6);
                for (int v = 0; v < take * 4; v++)
                {
                    Vertex vertex = vertices[v];
                    Color32 tint = vertex.tint;
                    tint.a = (byte)(tint.a * Mathf.Clamp01(alpha));
                    vertex.tint = tint;
                    mesh.SetNextVertex(vertex);
                }

                ushort[] indices = _chunkIndices[c];
                for (int i = 0; i < take * 6; i++)
                {
                    mesh.SetNextIndex(indices[i]);
                }
            }
        }

        private void Quad(Vector2 a, Vector2 b, float halfWidth, Color color)
        {
            Vector2 along = b - a;
            float length = along.magnitude;
            if (length <= Mathf.Epsilon)
            {
                return;
            }

            Vector2 direction = along / length;
            Vector2 side = new Vector2(-direction.y, direction.x) * halfWidth;
            Vector2 start = a - direction * (halfWidth * Join);
            Vector2 end = b + direction * (halfWidth * Join);
            QuadOf(start - side, start + side, end + side, end - side, color, color, color, color);
        }

        /// <summary>Adds quad p0 p1 p2 p3 (in order round it), wound clockwise on screen (y down).</summary>
        private void QuadOf(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, Color c0, Color c1, Color c2, Color c3)
        {
            if (_vertices.Count + 4 > ChunkVertices)
            {
                Finish();
            }

            int first = _vertices.Count;
            _vertices.Add(Vert(p0, c0));
            _vertices.Add(Vert(p1, c1));
            _vertices.Add(Vert(p2, c2));
            _vertices.Add(Vert(p3, c3));
            Vector2 e1 = p2 - p0;
            Vector2 e2 = p3 - p0;
            bool clockwise = e1.x * e2.y - e1.y * e2.x >= 0f;
            ushort i0 = (ushort)first;
            ushort i1 = (ushort)(first + (clockwise ? 1 : 3));
            ushort i3 = (ushort)(first + (clockwise ? 3 : 1));
            ushort i2 = (ushort)(first + 2);
            _indices.Add(i0);
            _indices.Add(i1);
            _indices.Add(i2);
            _indices.Add(i0);
            _indices.Add(i2);
            _indices.Add(i3);
            QuadCount++;
        }

        private static Vertex Vert(Vector2 position, Color color)
        {
            return new Vertex { position = new Vector3(position.x, position.y, Vertex.nearZ), tint = color };
        }

        /// <summary>Point <paramref name="index"/> moved sideways by the hand's drift at <paramref name="arc"/>.</summary>
        private static Vector2 Wobbled(List<Vector2> points, int index, int count, float arc, float wobble,
            float wobbleLength, uint seed)
        {
            if (wobble <= 0f)
            {
                return points[index];
            }

            Vector2 before = points[(index - 1 + count) % count];
            Vector2 after = points[(index + 1) % count];
            Vector2 tangent = after - before;
            if (tangent.sqrMagnitude <= Mathf.Epsilon)
            {
                return points[index];
            }

            tangent.Normalize();
            var normal = new Vector2(-tangent.y, tangent.x);
            return points[index] + normal * (wobble * (Noise01(seed, arc / wobbleLength) * 2f - 1f));
        }

        /// <summary>Smooth value noise in [0, 1] along a line, the same for the same seed and position.</summary>
        internal static float Noise01(uint seed, float x)
        {
            float floor = Mathf.Floor(x);
            int cell = (int)floor;
            float t = x - floor;
            t = t * t * (3f - 2f * t);
            return Mathf.Lerp(Hash01(seed, cell), Hash01(seed, cell + 1), t);
        }

        private static float Hash01(uint seed, int cell)
        {
            uint h = seed ^ unchecked((uint)cell * 0x27D4EB2Du);
            h ^= h >> 15;
            h *= 0x2C1B3C6Du;
            h ^= h >> 12;
            h *= 0x297A2D39u;
            h ^= h >> 15;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Collects flat marks pressed into the dust (07's worn tracks, the lander's landing-blast scar, crew bootprints)
    /// into one mesh for the rover's track shader: u runs across each strip or quad (0..1) for its feathered edge, v
    /// along it, and the vertex alpha is the mark's strength.
    /// </summary>
    public sealed class GroundMarkMesh
    {
        private readonly List<Vector3> _positions = new List<Vector3>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();

        public int VertexCount => _positions.Count;

        /// <summary>A strip <paramref name="halfWidth"/> either side of <paramref name="points"/>, flat across.</summary>
        /// <param name="strength">Mark strength (0..1) at each point.</param>
        public void AddStrip(IReadOnlyList<Vector3> points, IReadOnlyList<float> strength, float halfWidth)
        {
            if (points == null || strength == null || points.Count != strength.Count)
            {
                throw new ArgumentException("A strip needs one strength per point.", nameof(strength));
            }

            if (points.Count < 2)
            {
                return;
            }

            int first = _positions.Count;
            float along = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 ahead = points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)];
                ahead.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized) * halfWidth;
                along += i > 0 ? Vector3.Distance(points[i], points[i - 1]) : 0f;
                Color32 color = Strength(strength[i]);
                Add(points[i] - side, new Vector2(0f, along), color);
                Add(points[i] + side, new Vector2(1f, along), color);
                if (i > 0)
                {
                    int a = first + (i - 1) * 2;
                    Quad(a, a + 1, a + 3, a + 2);
                }
            }
        }

        /// <summary>A flat quad centred on <paramref name="centre"/>, its length along <paramref name="forward"/>.</summary>
        public void AddQuad(Vector3 centre, Vector3 forward, float halfLength, float halfWidth, float strength)
        {
            forward.y = 0f;
            Vector3 along = forward.normalized * halfLength;
            Vector3 side = Vector3.Cross(Vector3.up, forward.normalized) * halfWidth;
            Color32 color = Strength(strength);
            int a = _positions.Count;
            Add(centre - along - side, new Vector2(0f, 0f), color);
            Add(centre - along + side, new Vector2(1f, 0f), color);
            Add(centre + along - side, new Vector2(0f, 1f), color);
            Add(centre + along + side, new Vector2(1f, 1f), color);
            Quad(a, a + 1, a + 3, a + 2);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = _positions.Count > ushort.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(_positions);
            mesh.SetColors(_colors);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void Add(Vector3 position, Vector2 uv, Color32 color)
        {
            _positions.Add(position);
            _uvs.Add(uv);
            _colors.Add(color);
        }

        private void Quad(int a, int b, int c, int d)
        {
            _triangles.Add(a);
            _triangles.Add(c);
            _triangles.Add(b);
            _triangles.Add(a);
            _triangles.Add(d);
            _triangles.Add(c);
        }

        private static Color32 Strength(float strength)
        {
            return new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(strength) * 255f));
        }
    }
}

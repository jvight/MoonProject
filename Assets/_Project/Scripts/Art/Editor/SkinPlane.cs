using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The faces of a model that lie in one plane with one paint (see <see cref="WeatherSkins.Planes"/>), with flat
    /// in-plane axes: <see cref="V"/> runs as close to up as the plane allows (to the model's +Z on level faces) and
    /// <see cref="U"/> across it, so a panel's top edge is its largest v.
    /// </summary>
    internal sealed class SkinPlane
    {
        private readonly List<int> _triangles = new List<int>();

        public SkinPlane(Vector3 normal, PaletteSwatch swatch, int key)
        {
            Normal = normal;
            Swatch = swatch;
            Key = key;
            Vector3 reference = Mathf.Abs(normal.y) > 0.9f ? Vector3.forward : Vector3.up;
            V = (reference - normal * Vector3.Dot(reference, normal)).normalized;
            U = Vector3.Cross(V, normal);
            Min = new Vector2(float.MaxValue, float.MaxValue);
            Max = new Vector2(float.MinValue, float.MinValue);
        }

        public Vector3 Normal { get; }

        public PaletteSwatch Swatch { get; }

        /// <summary>A repeatable number naming the plane (seeds its panels' tones).</summary>
        public int Key { get; }

        public Vector3 U { get; }

        public Vector3 V { get; }

        public IReadOnlyList<int> Triangles => _triangles;

        /// <summary>The smallest (u, v) of the plane's corners.</summary>
        public Vector2 Min { get; private set; }

        /// <summary>The largest (u, v) of the plane's corners.</summary>
        public Vector2 Max { get; private set; }

        /// <summary>The plane's total face area (square metres).</summary>
        public float Area { get; private set; }

        /// <summary>A point's in-plane coordinates (u, v).</summary>
        public Vector2 Project(Vector3 point)
        {
            return new Vector2(Vector3.Dot(point, U), Vector3.Dot(point, V));
        }

        /// <summary>Adds triangle <paramref name="triangle"/> with its three corners.</summary>
        public void Add(int triangle, Vector3 a, Vector3 b, Vector3 c)
        {
            _triangles.Add(triangle);
            Area += Vector3.Cross(b - a, c - a).magnitude * 0.5f;
            foreach (Vector3 corner in new[] { a, b, c })
            {
                Vector2 flat = Project(corner);
                Min = Vector2.Min(Min, flat);
                Max = Vector2.Max(Max, flat);
            }
        }
    }
}

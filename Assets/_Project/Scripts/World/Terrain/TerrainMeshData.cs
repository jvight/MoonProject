using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// CPU-side result of meshing one terrain piece, produced off the main thread and uploaded by
    /// <see cref="TerrainBuilder"/>: flat-shaded render vertices (three per triangle, drawn in order) plus an
    /// optional shared-vertex collider mesh describing exactly the same surface.
    /// </summary>
    public sealed class TerrainMeshData
    {
        public TerrainMeshData(string name, Vector3 origin, TerrainVertex[] vertices, Vector3[] colliderVertices,
            int[] colliderIndices, Bounds localBounds)
        {
            Name = name;
            Origin = origin;
            Vertices = vertices;
            ColliderVertices = colliderVertices;
            ColliderIndices = colliderIndices;
            LocalBounds = localBounds;
        }

        public string Name { get; }

        /// <summary>World position of the mesh's local origin; vertices are relative to it.</summary>
        public Vector3 Origin { get; }

        public TerrainVertex[] Vertices { get; }

        /// <summary>Shared collider vertices, or null for collider-free pieces (the backdrop).</summary>
        public Vector3[] ColliderVertices { get; }

        public int[] ColliderIndices { get; }

        public Bounds LocalBounds { get; }

        public bool HasCollider => ColliderVertices != null;

        public int TriangleCount => Vertices.Length / 3;
    }
}

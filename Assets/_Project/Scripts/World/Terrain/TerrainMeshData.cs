using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// CPU-side result of meshing one terrain piece, produced off the main thread and uploaded by
    /// <see cref="TerrainBuilder"/>: indexed smooth-shaded render vertices plus triangle indices and an optional
    /// shared-vertex collider mesh describing the exact same surface.
    /// </summary>
    public sealed class TerrainMeshData
    {
        public TerrainMeshData(string name, Vector3 origin, TerrainVertex[] vertices, Vector3[] colliderVertices,
            int[] colliderIndices, Bounds localBounds, int[] renderIndices = null)
        {
            Name = name;
            Origin = origin;
            Vertices = vertices;
            ColliderVertices = colliderVertices;
            ColliderIndices = colliderIndices;
            Indices = renderIndices ?? colliderIndices;
            LocalBounds = localBounds;
        }

        public string Name { get; }

        /// <summary>World position of the mesh's local origin; vertices are relative to it.</summary>
        public Vector3 Origin { get; }

        public TerrainVertex[] Vertices { get; }

        /// <summary>Triangle index buffer for the render mesh.</summary>
        public int[] Indices { get; }

        /// <summary>Shared collider vertices, or null for collider-free pieces (the backdrop).</summary>
        public Vector3[] ColliderVertices { get; }

        public int[] ColliderIndices { get; }

        public Bounds LocalBounds { get; }

        public bool HasCollider => ColliderVertices != null;

        public int TriangleCount => Indices != null ? Indices.Length / 3 : Vertices.Length / 3;
    }
}

using System.Runtime.InteropServices;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Interleaved vertex of the flat-shaded terrain mesh (position, face normal, palette UV): uploaded in one
    /// SetVertexBufferData call, matching the attribute layout declared by the terrain builder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;

        public TerrainVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            Uv = uv;
        }
    }
}

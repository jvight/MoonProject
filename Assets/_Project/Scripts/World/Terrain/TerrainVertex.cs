using System.Runtime.InteropServices;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Interleaved vertex of the flat-shaded terrain mesh (position, face normal, sRGB colour): uploaded in one
    /// SetVertexBufferData call, matching the attribute layout declared by the terrain builder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Color32 Color;

        public TerrainVertex(Vector3 position, Vector3 normal, Color32 color)
        {
            Position = position;
            Normal = normal;
            Color = color;
        }
    }
}

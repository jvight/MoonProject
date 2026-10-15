using System.Runtime.InteropServices;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Interleaved vertex of a merged scatter mesh (position, normal, tangent, R1 vertex colour, and triplanar UV for
    /// the R1_Basalt material): uploaded in one SetVertexBufferData call, matching the layout declared by the scatter
    /// builder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ScatterVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Tangent;
        public Color32 Color;
        public Vector2 Uv;

        public ScatterVertex(Vector3 position, Vector3 normal, Vector4 tangent, Color32 color, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            Color = color;
            Uv = uv;
        }
    }
}

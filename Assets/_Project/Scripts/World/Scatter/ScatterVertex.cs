using System.Runtime.InteropServices;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Interleaved vertex of a merged scatter mesh (position, normal, palette UV for the art kit's palette
    /// material): uploaded in one SetVertexBufferData call, matching the layout declared by the scatter builder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ScatterVertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;

        public ScatterVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Position = position;
            Normal = normal;
            Uv = uv;
        }
    }
}

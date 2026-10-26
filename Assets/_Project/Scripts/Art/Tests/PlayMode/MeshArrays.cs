using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// A mesh's positions, normals, first UVs and per-submesh triangles, read from the CPU copy when the mesh keeps one
    /// and otherwise back from its GPU vertex and index buffers (imported models drop their CPU copy unless Read/Write
    /// is on, and the export must not change any import setting).
    /// </summary>
    public sealed class MeshArrays
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public Vector2[] Uvs;
        public int[][] Triangles;

        public static MeshArrays Read(Mesh mesh)
        {
            var arrays = new MeshArrays { Triangles = new int[mesh.subMeshCount][] };
            if (mesh.isReadable)
            {
                arrays.Vertices = mesh.vertices;
                arrays.Normals = mesh.normals;
                arrays.Uvs = mesh.uv;
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    arrays.Triangles[sub] = mesh.GetTriangles(sub);
                }

                return arrays;
            }

            int count = mesh.vertexCount;
            arrays.Vertices = new Vector3[count];
            arrays.Normals = mesh.HasVertexAttribute(VertexAttribute.Normal) ? new Vector3[count] : new Vector3[0];
            arrays.Uvs = mesh.HasVertexAttribute(VertexAttribute.TexCoord0) ? new Vector2[count] : new Vector2[0];
            var streams = new byte[mesh.vertexBufferCount][];
            for (int stream = 0; stream < streams.Length; stream++)
            {
                using (GraphicsBuffer buffer = mesh.GetVertexBuffer(stream))
                {
                    streams[stream] = new byte[buffer.count * buffer.stride];
                    buffer.GetData(streams[stream]);
                }
            }

            Decode(mesh, streams, VertexAttribute.Position, count, (i, v) => arrays.Vertices[i] = v);
            if (arrays.Normals.Length > 0)
            {
                Decode(mesh, streams, VertexAttribute.Normal, count, (i, v) => arrays.Normals[i] = v);
            }

            if (arrays.Uvs.Length > 0)
            {
                Decode(mesh, streams, VertexAttribute.TexCoord0, count, (i, v) => arrays.Uvs[i] = v);
            }

            int[] indices;
            using (GraphicsBuffer buffer = mesh.GetIndexBuffer())
            {
                indices = new int[buffer.count];
                if (mesh.indexFormat == IndexFormat.UInt16)
                {
                    var shorts = new ushort[buffer.count];
                    buffer.GetData(shorts);
                    for (int i = 0; i < shorts.Length; i++)
                    {
                        indices[i] = shorts[i];
                    }
                }
                else
                {
                    buffer.GetData(indices);
                }
            }

            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                SubMeshDescriptor descriptor = mesh.GetSubMesh(sub);
                var tris = new int[descriptor.topology == MeshTopology.Triangles ? descriptor.indexCount : 0];
                for (int i = 0; i < tris.Length; i++)
                {
                    tris[i] = indices[descriptor.indexStart + i] + descriptor.baseVertex;
                }

                arrays.Triangles[sub] = tris;
            }

            return arrays;
        }

        private static void Decode(Mesh mesh, byte[][] streams, VertexAttribute attribute, int count,
            Action<int, Vector4> store)
        {
            int stream = mesh.GetVertexAttributeStream(attribute);
            int offset = mesh.GetVertexAttributeOffset(attribute);
            int stride = mesh.GetVertexBufferStride(stream);
            VertexAttributeFormat format = mesh.GetVertexAttributeFormat(attribute);
            int dimension = mesh.GetVertexAttributeDimension(attribute);
            byte[] data = streams[stream];
            for (int i = 0; i < count; i++)
            {
                var value = Vector4.zero;
                for (int d = 0; d < dimension && d < 4; d++)
                {
                    value[d] = Component(data, i * stride + offset, d, format);
                }

                store(i, value);
            }
        }

        private static float Component(byte[] data, int start, int index, VertexAttributeFormat format)
        {
            switch (format)
            {
                case VertexAttributeFormat.Float32:
                    return BitConverter.ToSingle(data, start + index * 4);
                case VertexAttributeFormat.Float16:
                    return Mathf.HalfToFloat(BitConverter.ToUInt16(data, start + index * 2));
                case VertexAttributeFormat.UNorm8:
                    return data[start + index] / 255f;
                case VertexAttributeFormat.SNorm8:
                    return Mathf.Max(-1f, (sbyte)data[start + index] / 127f);
                case VertexAttributeFormat.UNorm16:
                    return BitConverter.ToUInt16(data, start + index * 2) / 65535f;
                case VertexAttributeFormat.SNorm16:
                    return Mathf.Max(-1f, BitConverter.ToInt16(data, start + index * 2) / 32767f);
                default:
                    throw new NotSupportedException($"Vertex format {format} is not read by the look-max export.");
            }
        }
    }
}

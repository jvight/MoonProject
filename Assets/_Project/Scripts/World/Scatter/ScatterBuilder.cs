using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Turns planned <see cref="ScatterInstance"/>s into rocks using the R1 basalt rock meshes
    /// (Art/R1/Models/Rocks): every instance copies one indexed source rock, scaled to its size, into one combined
    /// smooth-shaded mesh per terrain chunk and rock class (built in parallel). Pebble chunks are shadowless and
    /// distance-culled by a LODGroup; boulder chunks cast shadows, and every boulder gets a convex MeshCollider on
    /// <see cref="Layers.Prop"/>.
    /// </summary>
    public sealed class ScatterBuilder
    {
        // Rocks lean with the ground but not fully: a little defiance reads as "placed", not "stamped".
        private const float NormalAlignment = 0.7f;

        // LODGroup sizes refer to a field of view; pebble chunks are culled at their distance for this reference.
        private const float ReferenceFieldOfView = 60f;
        private const float PebbleCullScreenHeight = 0.05f;

        private const MeshUpdateFlags UploadFlags = MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;

        private readonly VertexAttributeDescriptor[] _layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
            new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
        };

        private readonly List<Mesh> _meshes = new List<Mesh>();

        /// <param name="pebbleRocks">Readable source meshes for pebbles (origin at the ground contact point).</param>
        /// <param name="boulderRocks">Readable source meshes for boulders; also used as their convex colliders.</param>
        public ScatterBuildReport Build(Transform parent, IReadOnlyList<ScatterInstance> instances,
            ScatterSettings settings, float chunkSize, IReadOnlyList<Mesh> pebbleRocks,
            IReadOnlyList<Mesh> boulderRocks, Material material, HideFlags hideFlags)
        {
            if (parent == null || instances == null || settings == null || pebbleRocks == null || boulderRocks == null
                || material == null)
            {
                throw new ArgumentNullException(nameof(parent), "Scatter build inputs must all be provided.");
            }

            var watch = Stopwatch.StartNew();
            RockShape[] pebbleShapes = ReadShapes(pebbleRocks);
            RockShape[] boulderShapes = ReadShapes(boulderRocks);

            // Group by (class, chunk) in plan order, so every combined mesh is deterministic.
            var groups = new List<ChunkGroup>();
            var lookup = new Dictionary<Vector3Int, int>();
            int pebbles = 0;
            int boulders = 0;
            for (int i = 0; i < instances.Count; i++)
            {
                bool boulder = instances[i].Kind == ScatterKind.Boulder;
                Vector3 position = instances[i].Position;
                var cell = new Vector2Int(Mathf.FloorToInt(position.x / chunkSize),
                    Mathf.FloorToInt(position.z / chunkSize));
                var key = new Vector3Int(cell.x, cell.y, boulder ? 1 : 0);
                if (!lookup.TryGetValue(key, out int group))
                {
                    group = groups.Count;
                    lookup.Add(key, group);
                    groups.Add(new ChunkGroup(cell, boulder));
                }

                RockShape[] shapes = boulder ? boulderShapes : pebbleShapes;
                RockShape shape = shapes[instances[i].Variant % shapes.Length];
                groups[group].Members.Add(i);
                groups[group].VertexCount += shape.Positions.Length;
                groups[group].IndexCount += shape.Triangles.Length;
                if (boulder)
                {
                    boulders++;
                }
                else
                {
                    pebbles++;
                }
            }

            var vertices = new ScatterVertex[groups.Count][];
            var indices = new int[groups.Count][];
            var bounds = new Bounds[groups.Count];
            Parallel.For(0, groups.Count, g =>
            {
                ChunkGroup group = groups[g];
                vertices[g] = Combine(group, instances, group.Boulders ? boulderShapes : pebbleShapes, chunkSize,
                    out indices[g], out bounds[g]);
            });

            int triangles = 0;
            float lodSize = PebbleCullScreenHeight * 2f * settings.PebbleCullDistance
                * Mathf.Tan(ReferenceFieldOfView * 0.5f * Mathf.Deg2Rad);
            for (int g = 0; g < groups.Count; g++)
            {
                ChunkGroup group = groups[g];
                MeshRenderer chunkRenderer = CreateChunk(parent, group, vertices[g], indices[g], bounds[g], chunkSize,
                    material, hideFlags);
                triangles += indices[g].Length / 3;
                if (!group.Boulders)
                {
                    var lodGroup = chunkRenderer.gameObject.AddComponent<LODGroup>();
                    lodGroup.localReferencePoint = new Vector3(chunkSize * 0.5f, 0f, chunkSize * 0.5f);
                    lodGroup.size = lodSize;
                    lodGroup.SetLODs(new[] { new LOD(PebbleCullScreenHeight, new Renderer[] { chunkRenderer }) });
                }
            }

            AddBoulderColliders(parent, instances, boulderRocks, boulderShapes, hideFlags);
            return new ScatterBuildReport(pebbles, boulders, triangles, groups.Count, watch.Elapsed.TotalMilliseconds);
        }

        public void DestroyMeshes()
        {
            foreach (Mesh mesh in _meshes)
            {
                if (mesh == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            _meshes.Clear();
        }

        /// <summary>Rotation of a rock: its yaw, leaned part of the way onto the ground normal.</summary>
        public static Quaternion Orientation(ScatterInstance instance)
        {
            return Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(Vector3.up, instance.Normal),
                NormalAlignment) * Quaternion.Euler(0f, instance.YawDegrees, 0f);
        }

        private static RockShape[] ReadShapes(IReadOnlyList<Mesh> meshes)
        {
            if (meshes.Count == 0)
            {
                throw new ArgumentException("At least one rock mesh is needed per class.", nameof(meshes));
            }

            var shapes = new RockShape[meshes.Count];
            for (int i = 0; i < meshes.Count; i++)
            {
                Mesh mesh = meshes[i];
                if (mesh == null || !mesh.isReadable)
                {
                    throw new ArgumentException($"Rock mesh {i} is missing or not CPU-readable.", nameof(meshes));
                }

                shapes[i] = new RockShape(mesh);
            }

            return shapes;
        }

        private static ScatterVertex[] Combine(ChunkGroup group, IReadOnlyList<ScatterInstance> instances,
            RockShape[] shapes, float chunkSize, out int[] indices, out Bounds bounds)
        {
            var vertices = new ScatterVertex[group.VertexCount];
            indices = new int[group.IndexCount];
            Vector3 origin = new Vector3(group.Cell.x, 0f, group.Cell.y) * chunkSize;
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            int writtenVertices = 0;
            int writtenIndices = 0;
            foreach (int member in group.Members)
            {
                ScatterInstance instance = instances[member];
                RockShape shape = shapes[instance.Variant % shapes.Length];
                Quaternion rotation = Orientation(instance);
                float baseScale = instance.Size / shape.HorizontalExtent;
                Vector3 scale3D = instance.ScaleAspect * baseScale;
                Vector3 invScale3D = new Vector3(
                    1f / Mathf.Max(1e-4f, scale3D.x),
                    1f / Mathf.Max(1e-4f, scale3D.y),
                    1f / Mathf.Max(1e-4f, scale3D.z));
                Vector3 buriedPosition = instance.Position - instance.Normal * instance.BurialDepth;
                Vector3 offset = buriedPosition - origin;
                int baseVertex = writtenVertices;
                for (int v = 0; v < shape.Positions.Length; v++)
                {
                    Vector3 position = offset + rotation * Vector3.Scale(shape.Positions[v], scale3D);
                    Vector3 normal = (rotation * Vector3.Scale(shape.Normals[v], invScale3D)).normalized;
                    Vector4 srcTangent = shape.Tangents[v];
                    Vector3 rotTangent = (rotation * Vector3.Scale(new Vector3(srcTangent.x, srcTangent.y, srcTangent.z), scale3D)).normalized;
                    var tangent = new Vector4(rotTangent.x, rotTangent.y, rotTangent.z, srcTangent.w);
                    vertices[writtenVertices++] = new ScatterVertex(position, normal, tangent, shape.Colors[v],
                        shape.Uvs[v]);
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);
                }

                for (int t = 0; t < shape.Triangles.Length; t++)
                {
                    indices[writtenIndices++] = baseVertex + shape.Triangles[t];
                }
            }

            bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return vertices;
        }

        private MeshRenderer CreateChunk(Transform parent, ChunkGroup group, ScatterVertex[] vertices, int[] indices,
            Bounds bounds, float chunkSize, Material material, HideFlags hideFlags)
        {
            string name = $"{(group.Boulders ? "Boulders" : "Pebbles")}_{group.Cell.x}_{group.Cell.y}";
            var chunk = new GameObject(name) { hideFlags = hideFlags, layer = Layers.Prop };
            chunk.transform.SetParent(parent, false);
            chunk.transform.localPosition = new Vector3(group.Cell.x, 0f, group.Cell.y) * chunkSize;

            int vertexCount = vertices.Length;
            int indexCount = indices.Length;
            var mesh = new Mesh { name = name, hideFlags = hideFlags };
            mesh.SetVertexBufferParams(vertexCount, _layout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount, 0, UploadFlags);
            if (vertexCount <= ushort.MaxValue + 1)
            {
                var indices16 = new ushort[indexCount];
                for (int i = 0; i < indexCount; i++)
                {
                    indices16[i] = (ushort)indices[i];
                }

                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
                mesh.SetIndexBufferData(indices16, 0, 0, indexCount, UploadFlags);
            }
            else
            {
                mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt32);
                mesh.SetIndexBufferData(indices, 0, 0, indexCount, UploadFlags);
            }

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, indexCount) { bounds = bounds, vertexCount = vertexCount },
                UploadFlags);
            mesh.bounds = bounds;
            mesh.UploadMeshData(true);
            _meshes.Add(mesh);

            chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return meshRenderer;
        }

        private static void AddBoulderColliders(Transform parent, IReadOnlyList<ScatterInstance> instances,
            IReadOnlyList<Mesh> boulderRocks, RockShape[] boulderShapes, HideFlags hideFlags)
        {
            var colliders = new GameObject("BoulderColliders") { hideFlags = hideFlags };
            colliders.transform.SetParent(parent, false);
            foreach (ScatterInstance instance in instances)
            {
                if (instance.Kind != ScatterKind.Boulder)
                {
                    continue;
                }

                int variant = instance.Variant % boulderShapes.Length;
                var collider = new GameObject("Boulder") { hideFlags = hideFlags, layer = Layers.Prop };
                collider.transform.SetParent(colliders.transform, false);
                Vector3 buriedPosition = instance.Position - instance.Normal * instance.BurialDepth;
                collider.transform.SetPositionAndRotation(buriedPosition, Orientation(instance));
                float baseScale = instance.Size / boulderShapes[variant].HorizontalExtent;
                collider.transform.localScale = instance.ScaleAspect * baseScale;
                var meshCollider = collider.AddComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.sharedMesh = boulderRocks[variant];
            }
        }

        /// <summary>An indexed source rock read once on the main thread.</summary>
        private sealed class RockShape
        {
            public RockShape(Mesh mesh)
            {
                Positions = mesh.vertices;
                int vCount = Positions.Length;

                Vector3[] normals = mesh.normals;
                if (normals == null || normals.Length != vCount)
                {
                    normals = new Vector3[vCount];
                    for (int i = 0; i < vCount; i++)
                    {
                        normals[i] = Vector3.up;
                    }
                }
                Normals = normals;

                Vector4[] tangents = mesh.tangents;
                if (tangents == null || tangents.Length != vCount)
                {
                    tangents = new Vector4[vCount];
                    for (int i = 0; i < vCount; i++)
                    {
                        tangents[i] = new Vector4(1f, 0f, 0f, 1f);
                    }
                }
                Tangents = tangents;

                Color32[] colors = mesh.colors32;
                if (colors == null || colors.Length != vCount)
                {
                    colors = new Color32[vCount];
                    var defaultColor = new Color32(255, 0, 0, 255);
                    for (int i = 0; i < vCount; i++)
                    {
                        colors[i] = defaultColor;
                    }
                }
                Colors = colors;

                Vector2[] uvs = mesh.uv;
                if (uvs == null || uvs.Length != vCount)
                {
                    uvs = new Vector2[vCount];
                }
                Uvs = uvs;

                Triangles = mesh.triangles;

                Bounds bounds = mesh.bounds;
                HorizontalExtent = Mathf.Max(bounds.size.x, bounds.size.z);
            }

            public Vector3[] Positions { get; }

            public Vector3[] Normals { get; }

            public Vector4[] Tangents { get; }

            public Color32[] Colors { get; }

            public Vector2[] Uvs { get; }

            public int[] Triangles { get; }

            /// <summary>The larger of the X and Z sizes: the rock's horizontal extent.</summary>
            public float HorizontalExtent { get; }
        }

        private sealed class ChunkGroup
        {
            public ChunkGroup(Vector2Int cell, bool boulders)
            {
                Cell = cell;
                Boulders = boulders;
            }

            public Vector2Int Cell { get; }

            public bool Boulders { get; }

            public List<int> Members { get; } = new List<int>();

            public int VertexCount { get; set; }

            public int IndexCount { get; set; }
        }
    }
}

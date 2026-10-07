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
    /// Turns planned <see cref="ScatterInstance"/>s into rocks using the art box's rock meshes
    /// (Generated/Art/Rocks): every instance copies one source rock, scaled to its size, into one combined
    /// flat-shaded mesh per terrain chunk and rock class (built in parallel). Pebble chunks are shadowless and
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
                groups[group].Members.Add(i);
                groups[group].VertexCount += shapes[instances[i].Variant % shapes.Length].Positions.Length;
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
            var bounds = new Bounds[groups.Count];
            Parallel.For(0, groups.Count, g =>
            {
                ChunkGroup group = groups[g];
                vertices[g] = Combine(group, instances, group.Boulders ? boulderShapes : pebbleShapes, chunkSize,
                    out bounds[g]);
            });

            int triangles = 0;
            float lodSize = PebbleCullScreenHeight * 2f * settings.PebbleCullDistance
                * Mathf.Tan(ReferenceFieldOfView * 0.5f * Mathf.Deg2Rad);
            for (int g = 0; g < groups.Count; g++)
            {
                ChunkGroup group = groups[g];
                MeshRenderer chunkRenderer = CreateChunk(parent, group, vertices[g], bounds[g], chunkSize, material,
                    hideFlags);
                triangles += vertices[g].Length / 3;
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
            RockShape[] shapes, float chunkSize, out Bounds bounds)
        {
            var vertices = new ScatterVertex[group.VertexCount];
            Vector3 origin = new Vector3(group.Cell.x, 0f, group.Cell.y) * chunkSize;
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            int written = 0;
            foreach (int member in group.Members)
            {
                ScatterInstance instance = instances[member];
                RockShape shape = shapes[instance.Variant % shapes.Length];
                Quaternion rotation = Orientation(instance);
                float scale = instance.Size / shape.HorizontalExtent;
                Vector3 offset = instance.Position - origin;
                for (int v = 0; v < shape.Positions.Length; v++)
                {
                    Vector3 position = offset + rotation * (shape.Positions[v] * scale);
                    vertices[written++] = new ScatterVertex(position, rotation * shape.Normals[v], shape.Uvs[v]);
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);
                }
            }

            bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return vertices;
        }

        private MeshRenderer CreateChunk(Transform parent, ChunkGroup group, ScatterVertex[] vertices, Bounds bounds,
            float chunkSize, Material material, HideFlags hideFlags)
        {
            string name = $"{(group.Boulders ? "Boulders" : "Pebbles")}_{group.Cell.x}_{group.Cell.y}";
            var chunk = new GameObject(name) { hideFlags = hideFlags, layer = Layers.Prop };
            chunk.transform.SetParent(parent, false);
            chunk.transform.localPosition = new Vector3(group.Cell.x, 0f, group.Cell.y) * chunkSize;

            int count = vertices.Length;
            var mesh = new Mesh { name = name, hideFlags = hideFlags };
            mesh.SetVertexBufferParams(count, _layout);
            mesh.SetVertexBufferData(vertices, 0, 0, count, 0, UploadFlags);
            if (count <= ushort.MaxValue + 1)
            {
                var indices = new ushort[count];
                for (int i = 0; i < count; i++)
                {
                    indices[i] = (ushort)i;
                }

                mesh.SetIndexBufferParams(count, IndexFormat.UInt16);
                mesh.SetIndexBufferData(indices, 0, 0, count, UploadFlags);
            }
            else
            {
                var indices = new int[count];
                for (int i = 0; i < count; i++)
                {
                    indices[i] = i;
                }

                mesh.SetIndexBufferParams(count, IndexFormat.UInt32);
                mesh.SetIndexBufferData(indices, 0, 0, count, UploadFlags);
            }

            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, count) { bounds = bounds, vertexCount = count }, UploadFlags);
            mesh.bounds = bounds;
            mesh.UploadMeshData(true);
            _meshes.Add(mesh);

            chunk.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = group.Boulders ? ShadowCastingMode.On : ShadowCastingMode.Off;
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
                collider.transform.SetPositionAndRotation(instance.Position, Orientation(instance));
                collider.transform.localScale = Vector3.one * (instance.Size / boulderShapes[variant].HorizontalExtent);
                var meshCollider = collider.AddComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.sharedMesh = boulderRocks[variant];
            }
        }

        /// <summary>A source rock unrolled to one vertex per triangle corner, read once on the main thread.</summary>
        private sealed class RockShape
        {
            public RockShape(Mesh mesh)
            {
                Vector3[] positions = mesh.vertices;
                Vector3[] normals = mesh.normals;
                Vector2[] uvs = mesh.uv;
                int[] triangles = mesh.triangles;
                Positions = new Vector3[triangles.Length];
                Normals = new Vector3[triangles.Length];
                Uvs = new Vector2[triangles.Length];
                for (int i = 0; i < triangles.Length; i++)
                {
                    Positions[i] = positions[triangles[i]];
                    Normals[i] = normals[triangles[i]];
                    Uvs[i] = uvs[triangles[i]];
                }

                Bounds bounds = mesh.bounds;
                HorizontalExtent = Mathf.Max(bounds.size.x, bounds.size.z);
            }

            public Vector3[] Positions { get; }

            public Vector3[] Normals { get; }

            public Vector2[] Uvs { get; }

            /// <summary>The larger of the X and Z sizes: the rock's "size" in RockGenerator terms.</summary>
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
        }
    }
}

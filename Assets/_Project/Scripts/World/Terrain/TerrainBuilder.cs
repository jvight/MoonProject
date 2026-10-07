using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Builds the whole terrain under a parent transform: chunk meshes (in parallel, off the main thread), one
    /// GameObject per chunk on <see cref="Layers.Ground"/> with a MeshCollider whose PhysX data is cooked by
    /// parallel jobs, plus the collider-free backdrop ring. Returns timings for the boot budget.
    /// </summary>
    public sealed class TerrainBuilder
    {
        private const MeshUpdateFlags UploadFlags = MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers;

        private readonly VertexAttributeDescriptor[] _layout =
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),
        };

        private readonly List<Mesh> _meshes = new List<Mesh>();

        /// <summary>Every mesh this builder created; released by <see cref="DestroyMeshes"/>.</summary>
        public IReadOnlyList<Mesh> Meshes => _meshes;

        /// <param name="lightDirection">Direction toward the earthlight: facets are painted by their tilt.</param>
        public TerrainBuildReport Build(Transform parent, MoonSurface surface, TerrainMeshSettings meshSettings,
            TerrainPaintSettings paintSettings, Vector3 lightDirection, Material material, HideFlags hideFlags)
        {
            if (parent == null || surface == null || meshSettings == null || paintSettings == null || material == null)
            {
                throw new ArgumentNullException(parent == null ? nameof(parent)
                    : surface == null ? nameof(surface)
                    : meshSettings == null ? nameof(meshSettings)
                    : paintSettings == null ? nameof(paintSettings) : nameof(material));
            }

            string error = meshSettings.Validate();
            if (error != null)
            {
                throw new ArgumentException("Invalid terrain mesh settings: " + error, nameof(meshSettings));
            }

            var total = Stopwatch.StartNew();
            TerrainChunkPlan[] plans = TerrainChunkPlanner.Plan(meshSettings, surface.Canyon.Touches);
            var painter = new TerrainPainter(paintSettings, surface.Seed, lightDirection);
            var mesher = new TerrainChunkMesher(surface, painter, meshSettings);
            var pieces = new TerrainMeshData[plans.Length + 1];
            Parallel.For(0, pieces.Length, i =>
            {
                pieces[i] = i < plans.Length
                    ? mesher.Build(plans[i])
                    : BackdropMesher.Build(surface, painter, meshSettings);
            });
            double meshingMs = total.Elapsed.TotalMilliseconds;

            int largest = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                largest = Mathf.Max(largest, pieces[i].Vertices.Length);
            }

            ushort[] sequence = Sequence(largest);
            var colliderMeshes = new Mesh[plans.Length];
            var colliderComponents = new MeshCollider[plans.Length];
            int triangles = 0;
            int colliderTriangles = 0;
            int colliders = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                TerrainMeshData piece = pieces[i];
                var gameObject = new GameObject(piece.Name)
                {
                    hideFlags = hideFlags,
                    layer = piece.HasCollider ? Layers.Ground : 0,
                };
                gameObject.transform.SetParent(parent, false);
                gameObject.transform.localPosition = piece.Origin;
                if (piece.HasCollider)
                {
                    // Added before the MeshFilter so Unity doesn't auto-assign the non-readable render mesh to it,
                    // which fails PhysX cooking in players.
                    colliderComponents[colliders] = gameObject.AddComponent<MeshCollider>();
                }

                gameObject.AddComponent<MeshFilter>().sharedMesh = CreateRenderMesh(piece, sequence, hideFlags);
                var meshRenderer = gameObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                triangles += piece.TriangleCount;
                if (piece.HasCollider)
                {
                    colliderMeshes[colliders] = CreateColliderMesh(piece, hideFlags);
                    colliderTriangles += piece.ColliderIndices.Length / 3;
                    colliders++;
                }
            }

            double uploadMs = total.Elapsed.TotalMilliseconds - meshingMs;
            var meshIds = new NativeArray<int>(colliders, Allocator.TempJob);
            for (int i = 0; i < colliders; i++)
            {
                meshIds[i] = colliderMeshes[i].GetInstanceID();
            }

            new ColliderBakeJob { MeshIds = meshIds }.Schedule(colliders, 1).Complete();
            meshIds.Dispose();
            for (int i = 0; i < colliders; i++)
            {
                colliderComponents[i].sharedMesh = colliderMeshes[i];
            }

            double colliderMs = total.Elapsed.TotalMilliseconds - meshingMs - uploadMs;
            return new TerrainBuildReport(plans.Length, triangles, colliderTriangles, meshingMs, uploadMs, colliderMs,
                total.Elapsed.TotalMilliseconds);
        }

        public void DestroyMeshes()
        {
            for (int i = 0; i < _meshes.Count; i++)
            {
                if (_meshes[i] == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(_meshes[i]);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(_meshes[i]);
                }
            }

            _meshes.Clear();
        }

        private Mesh CreateRenderMesh(TerrainMeshData piece, ushort[] sequence, HideFlags hideFlags)
        {
            int count = piece.Vertices.Length;
            var mesh = new Mesh { name = piece.Name, hideFlags = hideFlags };
            mesh.SetVertexBufferParams(count, _layout);
            mesh.SetVertexBufferData(piece.Vertices, 0, 0, count, 0, UploadFlags);
            mesh.SetIndexBufferParams(count, IndexFormat.UInt16);
            mesh.SetIndexBufferData(sequence, 0, 0, count, UploadFlags);
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, count) { bounds = piece.LocalBounds, vertexCount = count },
                UploadFlags);
            mesh.bounds = piece.LocalBounds;
            mesh.UploadMeshData(true);
            _meshes.Add(mesh);
            return mesh;
        }

        private Mesh CreateColliderMesh(TerrainMeshData piece, HideFlags hideFlags)
        {
            var mesh = new Mesh { name = piece.Name + "_Collider", hideFlags = hideFlags };
            mesh.SetVertices(piece.ColliderVertices);
            mesh.SetIndices(piece.ColliderIndices, MeshTopology.Triangles, 0, false);
            mesh.bounds = piece.LocalBounds;
            _meshes.Add(mesh);
            return mesh;
        }

        private static ushort[] Sequence(int count)
        {
            if (count > ushort.MaxValue + 1)
            {
                throw new InvalidOperationException(
                    $"A terrain piece has {count} vertices; 16-bit indices hold 65536.");
            }

            var sequence = new ushort[count];
            for (int i = 0; i < count; i++)
            {
                sequence[i] = (ushort)i;
            }

            return sequence;
        }
    }
}

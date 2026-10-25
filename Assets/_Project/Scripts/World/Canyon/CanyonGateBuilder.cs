using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Builds Whispering Canyon's gate slabs and the exit's gate posts: bevelled rock slabs with smooth bevel normals,
    /// drawn in the terrain's own material and coloured by the terrain's painter where each vertex stands (so they
    /// read as the canyon's rock, never as boxes of another stone), plus a box collider each on
    /// <see cref="Layers.Ground"/>, so the chasm's far face and the exit's step are truly vertical to the rover (design
    /// ruling 10: never climbable) while their tops are flat ground to land and drive on.
    /// </summary>
    public sealed class CanyonGateBuilder
    {
        private const float Chamfer = 0.25f;
        private const float TextureWorldMeters = 2.5f;

        private static readonly Vector3[] FaceNormals =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.left,
            Vector3.up,
            Vector3.down
        };

        private static readonly Vector3[] FaceRights =
        {
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back,
            Vector3.right,
            Vector3.right
        };

        private static readonly Vector3[] FaceUps =
        {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.forward,
            Vector3.back
        };

        private readonly List<Mesh> _meshes = new List<Mesh>();

        public void Build(Transform parent, IReadOnlyList<CanyonSlab> slabs, MoonSurface surface,
            TerrainPainter painter, Material material, HideFlags hideFlags)
        {
            if (parent == null || slabs == null || surface == null || painter == null || material == null)
            {
                throw new ArgumentNullException(nameof(parent), "Gate build inputs must all be provided.");
            }

            var root = new GameObject("CanyonGates") { hideFlags = hideFlags };
            root.transform.SetParent(parent, false);
            for (int i = 0; i < slabs.Count; i++)
            {
                CanyonSlab slab = slabs[i];
                Quaternion turn = Quaternion.Euler(0f, slab.YawDegrees, 0f);
                Mesh mesh = BuildSlabMesh($"CanyonGate_{i}", slab.Size, Chamfer, (position, normal) =>
                {
                    Vector3 world = slab.Centre + turn * position;
                    return painter.PaintVertex(world, turn * normal, surface.Sample(world.x, world.z));
                });
                mesh.hideFlags = hideFlags;
                _meshes.Add(mesh);

                var piece = new GameObject($"CanyonGate_{i}") { hideFlags = hideFlags, layer = Layers.Ground };
                piece.transform.SetParent(root.transform, false);
                piece.transform.SetPositionAndRotation(slab.Centre, turn);
                piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = piece.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                piece.AddComponent<BoxCollider>().size = slab.Size;
            }
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

        // colourAt gives a vertex its colour from its position and normal in the slab's own space.
        private static Mesh BuildSlabMesh(string name, Vector3 size, float chamfer,
            Func<Vector3, Vector3, Color32> colourAt)
        {
            Vector3 half = size * 0.5f;
            float minHalf = Mathf.Min(half.x, Mathf.Min(half.y, half.z));
            float bevel = Mathf.Clamp(chamfer, 0.02f, minHalf * 0.4f);
            Vector3 inner = half - Vector3.one * bevel;

            const int BevelSteps = 3;
            const int FlatSteps = 2;
            const int Grid = BevelSteps * 2 + FlatSteps;
            const int Stride = Grid + 1;

            int vertexCapacity = 6 * Stride * Stride;
            var vertices = new List<Vector3>(vertexCapacity);
            var normals = new List<Vector3>(vertexCapacity);
            var uvs = new List<Vector2>(vertexCapacity);
            var colors = new List<Color32>(vertexCapacity);
            var triangles = new List<int>(6 * Grid * Grid * 6);

            float[] samplesU = new float[Stride];
            float[] samplesV = new float[Stride];

            for (int f = 0; f < 6; f++)
            {
                int baseVertex = vertices.Count;
                Vector3 n = FaceNormals[f];
                Vector3 r = FaceRights[f];
                Vector3 u = FaceUps[f];

                float halfU = Mathf.Abs(Vector3.Dot(r, half));
                float halfV = Mathf.Abs(Vector3.Dot(u, half));
                float halfN = Mathf.Abs(Vector3.Dot(n, half));
                float innerU = Mathf.Max(0.001f, halfU - bevel);
                float innerV = Mathf.Max(0.001f, halfV - bevel);

                FillProfileSamples(samplesU, halfU, innerU, BevelSteps, FlatSteps);
                FillProfileSamples(samplesV, halfV, innerV, BevelSteps, FlatSteps);

                for (int iy = 0; iy <= Grid; iy++)
                {
                    float py = samplesV[iy];
                    for (int ix = 0; ix <= Grid; ix++)
                    {
                        float px = samplesU[ix];
                        Vector3 boxPt = r * px + u * py + n * halfN;
                        Vector3 corePt = new Vector3(
                            Mathf.Clamp(boxPt.x, -inner.x, inner.x),
                            Mathf.Clamp(boxPt.y, -inner.y, inner.y),
                            Mathf.Clamp(boxPt.z, -inner.z, inner.z));
                        Vector3 delta = boxPt - corePt;
                        Vector3 normal = delta.sqrMagnitude > 1e-6f ? delta.normalized : n;
                        Vector3 pos = corePt + normal * bevel;

                        vertices.Add(pos);
                        normals.Add(normal);
                        uvs.Add(new Vector2((px + halfU) / TextureWorldMeters, (py + halfV) / TextureWorldMeters));
                        colors.Add(colourAt(pos, normal));
                    }
                }

                for (int iy = 0; iy < Grid; iy++)
                {
                    for (int ix = 0; ix < Grid; ix++)
                    {
                        int i0 = baseVertex + iy * Stride + ix;
                        int i1 = i0 + 1;
                        int i2 = i0 + Stride;
                        int i3 = i2 + 1;

                        triangles.Add(i0);
                        triangles.Add(i2);
                        triangles.Add(i1);

                        triangles.Add(i1);
                        triangles.Add(i2);
                        triangles.Add(i3);
                    }
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void FillProfileSamples(float[] samples, float half, float inner, int bevelSteps, int flatSteps)
        {
            int idx = 0;
            for (int i = 0; i < bevelSteps; i++)
            {
                samples[idx++] = Mathf.Lerp(-half, -inner, (float)i / bevelSteps);
            }

            for (int i = 0; i <= flatSteps; i++)
            {
                samples[idx++] = Mathf.Lerp(-inner, inner, (float)i / flatSteps);
            }

            for (int i = 1; i <= bevelSteps; i++)
            {
                samples[idx++] = Mathf.Lerp(inner, half, (float)i / bevelSteps);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Auto-smooths vertex normals by dihedral angle (corner-angle weighted) and bakes per-vertex ambient occlusion
    /// into <see cref="LowPolyMeshBuilder"/>'s UV1 occlusion channel for the soft low-poly look variants (V1-V3).
    /// </summary>
    public static class MeshNormalSmoother
    {
        private const float WeldScale = 10000f;

        /// <summary>
        /// Smooths vertex normals across shared positions in <paramref name="range"/> whenever the angle between
        /// incident face normals is at most <paramref name="maxAngleDegrees"/>. Hard edges above that angle stay crisp.
        /// </summary>
        public static void Smooth(LowPolyMeshBuilder builder, MeshRange range, float maxAngleDegrees)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (range.TriangleCount <= 0 || maxAngleDegrees <= 0f)
            {
                return;
            }

            int firstTri = range.FirstTriangle;
            int triCount = range.TriangleCount;
            var faceNormals = new Vector3[triCount];
            var cornerWeights = new float[triCount * 3];
            var buckets = new Dictionary<Vector3Int, List<int>>(triCount * 2);
            IReadOnlyList<Vector3> positions = builder.Positions;

            for (int i = 0; i < triCount; i++)
            {
                int t = firstTri + i;
                faceNormals[i] = builder.FaceNormal(t);
                int baseV = t * 3;
                Vector3 p0 = positions[baseV];
                Vector3 p1 = positions[baseV + 1];
                Vector3 p2 = positions[baseV + 2];
                cornerWeights[i * 3] = CornerAngle(p0, p1, p2);
                cornerWeights[i * 3 + 1] = CornerAngle(p1, p2, p0);
                cornerWeights[i * 3 + 2] = CornerAngle(p2, p0, p1);

                for (int c = 0; c < 3; c++)
                {
                    int localCorner = i * 3 + c;
                    Vector3Int key = WeldKey(positions[baseV + c]);
                    if (!buckets.TryGetValue(key, out List<int> list))
                    {
                        list = new List<int>(6);
                        buckets.Add(key, list);
                    }

                    list.Add(localCorner);
                }
            }

            float minDot = Mathf.Cos(maxAngleDegrees * Mathf.Deg2Rad);
            foreach (KeyValuePair<Vector3Int, List<int>> pair in buckets)
            {
                List<int> corners = pair.Value;
                int count = corners.Count;
                if (count <= 1)
                {
                    continue;
                }

                for (int a = 0; a < count; a++)
                {
                    int cornerA = corners[a];
                    int triA = cornerA / 3;
                    Vector3 normalA = faceNormals[triA];
                    Vector3 sum = Vector3.zero;
                    for (int b = 0; b < count; b++)
                    {
                        int cornerB = corners[b];
                        int triB = cornerB / 3;
                        Vector3 normalB = faceNormals[triB];
                        if (triA == triB || Vector3.Dot(normalA, normalB) >= minDot)
                        {
                            sum += normalB * cornerWeights[cornerB];
                        }
                    }

                    if (sum.sqrMagnitude > 1e-12f)
                    {
                        builder.SetNormal(firstTri * 3 + cornerA, sum.normalized);
                    }
                }
            }
        }

        /// <summary>
        /// Smooths each connected component of <paramref name="builder"/> using <paramref name="baseAngleDegrees"/> for
        /// main hulls and <paramref name="smallPartAngleDegrees"/> for thin cylindrical parts (lugs, cables, struts)
        /// whose smallest bounding-box axis is at most <paramref name="smallExtentThreshold"/>.
        /// </summary>
        public static void SmoothAdaptive(LowPolyMeshBuilder builder, float baseAngleDegrees = 52f,
            float smallPartAngleDegrees = 65f, float smallExtentThreshold = 0.38f)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            int triCount = builder.TriangleCount;
            if (triCount <= 0 || baseAngleDegrees <= 0f)
            {
                return;
            }

            IReadOnlyList<Vector3> positions = builder.Positions;
            var faceNormals = new Vector3[triCount];
            var cornerWeights = new float[triCount * 3];
            var buckets = new Dictionary<Vector3Int, List<int>>(triCount * 2);

            for (int t = 0; t < triCount; t++)
            {
                faceNormals[t] = builder.FaceNormal(t);
                int baseV = t * 3;
                Vector3 p0 = positions[baseV];
                Vector3 p1 = positions[baseV + 1];
                Vector3 p2 = positions[baseV + 2];
                cornerWeights[t * 3] = CornerAngle(p0, p1, p2);
                cornerWeights[t * 3 + 1] = CornerAngle(p1, p2, p0);
                cornerWeights[t * 3 + 2] = CornerAngle(p2, p0, p1);

                for (int c = 0; c < 3; c++)
                {
                    Vector3Int key = WeldKey(positions[baseV + c]);
                    if (!buckets.TryGetValue(key, out List<int> list))
                    {
                        list = new List<int>(6);
                        buckets.Add(key, list);
                    }

                    list.Add(t * 3 + c);
                }
            }

            float[] minDotByTri = ComputeComponentMinDots(positions, triCount, buckets, baseAngleDegrees,
                smallPartAngleDegrees, smallExtentThreshold);

            foreach (KeyValuePair<Vector3Int, List<int>> pair in buckets)
            {
                List<int> corners = pair.Value;
                int count = corners.Count;
                if (count <= 1)
                {
                    continue;
                }

                for (int a = 0; a < count; a++)
                {
                    int cornerA = corners[a];
                    int triA = cornerA / 3;
                    Vector3 normalA = faceNormals[triA];
                    float minDot = minDotByTri[triA];
                    Vector3 sum = Vector3.zero;
                    for (int b = 0; b < count; b++)
                    {
                        int cornerB = corners[b];
                        int triB = cornerB / 3;
                        Vector3 normalB = faceNormals[triB];
                        if (triA == triB || Vector3.Dot(normalA, normalB) >= minDot)
                        {
                            sum += normalB * cornerWeights[cornerB];
                        }
                    }

                    if (sum.sqrMagnitude > 1e-12f)
                    {
                        builder.SetNormal(cornerA, sum.normalized);
                    }
                }
            }
        }

        /// <summary>
        /// Bakes ground-contact and crevice ambient occlusion into a rock mesh's UV1.x channel (V2/V3).
        /// </summary>
        public static void BakeRockOcclusion(LowPolyMeshBuilder builder, float strength = 0.62f)
        {
            if (builder == null || builder.VertexCount == 0 || strength <= 0f)
            {
                return;
            }

            Bounds bounds = builder.Bounds;
            float maxY = Mathf.Max(0.05f, bounds.max.y);
            float footReach = maxY * 0.42f;
            IReadOnlyList<Vector3> positions = builder.Positions;
            IReadOnlyList<Vector3> normals = builder.Normals;
            for (int v = 0; v < positions.Count; v++)
            {
                Vector3 p = positions[v];
                Vector3 n = normals[v];
                float heightFactor = 1f - Mathf.Clamp01(p.y / footReach);
                heightFactor = heightFactor * heightFactor * (3f - 2f * heightFactor);
                float skyOpen = Mathf.Clamp01(n.y);
                float under = Mathf.Clamp01(-n.y);
                float footAo = heightFactor * Mathf.Lerp(1f, 0.25f, skyOpen);
                float underAo = under * 0.45f;
                float ao = Mathf.Clamp01((footAo * 0.85f + underAo * 0.4f) * strength);
                if (ao > 0f)
                {
                    builder.Occlude(v, ao);
                }
            }
        }

        /// <summary>
        /// Bakes self and ground-proximity ambient occlusion into a vehicle or prop mesh's UV1.x channel in root space.
        /// </summary>
        public static void BakeRootOcclusion(LowPolyMeshBuilder builder, Matrix4x4 toRoot, float footHeight = 0.48f,
            float strength = 0.45f)
        {
            if (builder == null || builder.VertexCount == 0 || strength <= 0f)
            {
                return;
            }

            IReadOnlyList<Vector3> positions = builder.Positions;
            IReadOnlyList<Vector3> normals = builder.Normals;
            for (int v = 0; v < positions.Count; v++)
            {
                Vector3 p = toRoot.MultiplyPoint3x4(positions[v]);
                Vector3 n = toRoot.MultiplyVector(normals[v]).normalized;
                float t = Mathf.Clamp01(p.y / Mathf.Max(0.05f, footHeight));
                float ground = 1f - t * t * (3f - 2f * t);
                float skyOpen = Mathf.Clamp01(n.y);
                float under = Mathf.Clamp01(-n.y);
                float chassisUnder = p.y < 0.62f ? under * 0.55f : under * 0.25f;
                float ao = Mathf.Clamp01((ground * Mathf.Lerp(0.85f, 0.2f, skyOpen) + chassisUnder) * strength);
                if (ao > 0f)
                {
                    builder.Occlude(v, ao);
                }
            }
        }

        private static float[] ComputeComponentMinDots(IReadOnlyList<Vector3> positions, int triCount,
            Dictionary<Vector3Int, List<int>> buckets, float baseAngleDegrees, float smallPartAngleDegrees,
            float smallExtentThreshold)
        {
            var minDotByTri = new float[triCount];
            var visited = new bool[triCount];
            var queue = new List<int>(64);
            float baseMinDot = Mathf.Cos(baseAngleDegrees * Mathf.Deg2Rad);
            float smallMinDot = Mathf.Cos(smallPartAngleDegrees * Mathf.Deg2Rad);

            for (int start = 0; start < triCount; start++)
            {
                if (visited[start])
                {
                    continue;
                }

                queue.Clear();
                queue.Add(start);
                visited[start] = true;
                Vector3 min = positions[start * 3];
                Vector3 max = min;

                for (int head = 0; head < queue.Count; head++)
                {
                    int t = queue[head];
                    int baseV = t * 3;
                    for (int c = 0; c < 3; c++)
                    {
                        Vector3 p = positions[baseV + c];
                        min = Vector3.Min(min, p);
                        max = Vector3.Max(max, p);
                        if (buckets.TryGetValue(WeldKey(p), out List<int> incident))
                        {
                            for (int k = 0; k < incident.Count; k++)
                            {
                                int otherTri = incident[k] / 3;
                                if (!visited[otherTri])
                                {
                                    visited[otherTri] = true;
                                    queue.Add(otherTri);
                                }
                            }
                        }
                    }
                }

                Vector3 size = max - min;
                float minAxis = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
                float chosenMinDot = minAxis <= smallExtentThreshold ? smallMinDot : baseMinDot;
                for (int i = 0; i < queue.Count; i++)
                {
                    minDotByTri[queue[i]] = chosenMinDot;
                }
            }

            return minDotByTri;
        }

        private static float CornerAngle(Vector3 corner, Vector3 next, Vector3 prev)
        {
            Vector3 e1 = (next - corner).normalized;
            Vector3 e2 = (prev - corner).normalized;
            return Mathf.Max(1e-4f, Mathf.Acos(Mathf.Clamp(Vector3.Dot(e1, e2), -1f, 1f)));
        }

        private static Vector3Int WeldKey(Vector3 p)
        {
            return new Vector3Int(
                Mathf.RoundToInt(p.x * WeldScale),
                Mathf.RoundToInt(p.y * WeldScale),
                Mathf.RoundToInt(p.z * WeldScale));
        }
    }
}

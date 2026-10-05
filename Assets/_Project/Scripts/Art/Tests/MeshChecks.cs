using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Art.Tests
{
    /// <summary>Geometry assertions shared by the Art tests.</summary>
    internal static class MeshChecks
    {
        private const float NormalTolerance = 1e-4f;
        private const float MinCrossSqr = 1e-14f;
        private const float WeldScale = 1e4f;

        /// <summary>Flat-shading invariants: unit normals matching the winding, one palette cell per face.</summary>
        public static void AssertWellFormed(LowPolyMeshBuilder builder)
        {
            Assert.Greater(builder.TriangleCount, 0, "builder is empty");
            Assert.AreEqual(0, builder.VertexCount % 3);
            Assert.AreEqual(builder.VertexCount, builder.Normals.Count);
            Assert.AreEqual(builder.VertexCount, builder.Uvs.Count);
            for (int v = 0; v < builder.VertexCount; v += 3)
            {
                Vector3 a = builder.Positions[v];
                Vector3 b = builder.Positions[v + 1];
                Vector3 c = builder.Positions[v + 2];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                Assert.Greater(cross.sqrMagnitude, MinCrossSqr, $"degenerate triangle {v / 3}");

                Vector3 normal = builder.Normals[v];
                Assert.AreEqual(1f, normal.magnitude, NormalTolerance, $"normal of triangle {v / 3} is not unit");
                Vector3 windingNormal = cross / Mathf.Sqrt(cross.sqrMagnitude);
                Assert.Greater(Vector3.Dot(normal, windingNormal), 1f - NormalTolerance,
                    $"normal of triangle {v / 3} disagrees with its winding");
                Assert.AreEqual(normal, builder.Normals[v + 1]);
                Assert.AreEqual(normal, builder.Normals[v + 2]);

                Vector2 uv = builder.Uvs[v];
                Assert.AreEqual(uv, builder.Uvs[v + 1]);
                Assert.AreEqual(uv, builder.Uvs[v + 2]);
                Assert.IsTrue(IsPaletteCentre(uv), $"uv {uv} of triangle {v / 3} is not a palette cell centre");
            }
        }

        /// <summary>
        /// Every directed edge has exactly one opposite edge (closed, consistently wound surface) and the enclosed
        /// signed volume is positive (normals point outward).
        /// </summary>
        public static void AssertClosedAndOutward(LowPolyMeshBuilder builder)
        {
            var edges = new Dictionary<(Vector3Int, Vector3Int), int>();
            for (int v = 0; v < builder.VertexCount; v += 3)
            {
                for (int e = 0; e < 3; e++)
                {
                    var key = (Weld(builder.Positions[v + e]), Weld(builder.Positions[v + (e + 1) % 3]));
                    edges.TryGetValue(key, out int count);
                    edges[key] = count + 1;
                }
            }

            foreach (KeyValuePair<(Vector3Int, Vector3Int), int> edge in edges)
            {
                Assert.AreEqual(1, edge.Value, $"directed edge {edge.Key} is used {edge.Value} times");
                Assert.IsTrue(edges.ContainsKey((edge.Key.Item2, edge.Key.Item1)),
                    $"edge {edge.Key} has no opposite edge: surface is open or inconsistently wound");
            }

            Assert.Greater(SignedVolume(builder), 0f, "normals point inward");
        }

        public static float SignedVolume(LowPolyMeshBuilder builder)
        {
            double sum = 0d;
            for (int v = 0; v < builder.VertexCount; v += 3)
            {
                Vector3 cross = Vector3.Cross(builder.Positions[v + 1], builder.Positions[v + 2]);
                sum += Vector3.Dot(builder.Positions[v], cross);
            }

            return (float)(sum / 6d);
        }

        public static PaletteSwatch SwatchOf(LowPolyMeshBuilder builder, int triangle)
        {
            Vector2 uv = builder.Uvs[triangle * 3];
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                if (Palette.Uv(swatch) == uv)
                {
                    return swatch;
                }
            }

            throw new AssertionException($"uv {uv} is not a palette cell centre");
        }

        public static Vector3 FaceNormal(LowPolyMeshBuilder builder, int triangle)
        {
            return builder.Normals[triangle * 3];
        }

        private static bool IsPaletteCentre(Vector2 uv)
        {
            foreach (PaletteSwatch swatch in Enum.GetValues(typeof(PaletteSwatch)))
            {
                if (Palette.Uv(swatch) == uv)
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector3Int Weld(Vector3 position)
        {
            return new Vector3Int(
                (int)Math.Round(position.x * WeldScale),
                (int)Math.Round(position.y * WeldScale),
                (int)Math.Round(position.z * WeldScale));
        }
    }
}

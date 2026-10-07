using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Builds the low-resolution, collider-free ring of terrain around the chunk grid that carries the distant
    /// silhouettes out to the fog. It samples the same analytic surface, with rings spaced wider with distance,
    /// and is sunk below the surface wherever the detailed chunks cover it so it never pokes through them.
    /// </summary>
    public static class BackdropMesher
    {
        private const uint JitterSalt = 0xBB67AE85u;
        private const uint DiagonalSalt = 0x5BE0CD19u;
        private const uint PaintSalt = 0xCBBB9D5Du;
        private const float AngularJitter = 0.3f;
        private const float SinkFadeInside = 16f;
        private const float SinkFadeOutside = 48f;

        public static TerrainMeshData Build(MoonSurface surface, TerrainPainter painter, TerrainMeshSettings settings)
        {
            if (surface == null || painter == null || settings == null)
            {
                throw new ArgumentNullException(surface == null ? nameof(surface)
                    : painter == null ? nameof(painter) : nameof(settings));
            }

            int rings = settings.BackdropRings;
            int segments = settings.BackdropSegments;
            uint seed = (uint)surface.Seed;
            float growth = Mathf.Pow(settings.BackdropOuterRadius / settings.BackdropInnerRadius, 1f / rings);
            float segmentAngle = Mathf.PI * 2f / segments;
            var positions = new Vector3[(rings + 1) * segments];
            var regions = new SurfaceSample[positions.Length];
            for (int k = 0; k <= rings; k++)
            {
                float radius = settings.BackdropInnerRadius * Mathf.Pow(growth, k);
                for (int s = 0; s < segments; s++)
                {
                    float wobble = Hashing.ToSigned(Hashing.Hash(k, s, seed ^ JitterSalt)) * AngularJitter;
                    float angle = (s + wobble) * segmentAngle;
                    float x = Mathf.Sin(angle) * radius;
                    float z = Mathf.Cos(angle) * radius;
                    SurfaceSample sample = surface.Sample(x, z);
                    float square = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                    float sink = settings.BackdropSink * (1f - SmoothMath.Smootherstep(
                        settings.GridHalfExtent - SinkFadeInside, settings.GridHalfExtent + SinkFadeOutside, square));
                    positions[k * segments + s] = new Vector3(x, sample.Height - sink, z);
                    regions[k * segments + s] = sample;
                }
            }

            var groundColors = new Color32[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                groundColors[i] = painter.Ground(positions[i], regions[i]);
            }

            var vertices = new TerrainVertex[rings * segments * 6];
            int written = 0;
            for (int k = 0; k < rings; k++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int next = (s + 1) % segments;
                    int a = k * segments + s;
                    int b = k * segments + next;
                    int c = (k + 1) * segments + s;
                    int d = (k + 1) * segments + next;
                    uint paintHash = Hashing.Hash(k, s, seed ^ PaintSalt);
                    if ((Hashing.Hash(k, s, seed ^ DiagonalSalt) & 1u) == 0u)
                    {
                        Emit(positions, regions, groundColors, a, c, b, painter, paintHash, vertices, ref written);
                        Emit(positions, regions, groundColors, b, c, d, painter, Hashing.Mix(paintHash), vertices,
                            ref written);
                    }
                    else
                    {
                        Emit(positions, regions, groundColors, a, c, d, painter, paintHash, vertices, ref written);
                        Emit(positions, regions, groundColors, a, d, b, painter, Hashing.Mix(paintHash), vertices,
                            ref written);
                    }
                }
            }

            Vector3 min = positions[0];
            Vector3 max = min;
            for (int i = 0; i < positions.Length; i++)
            {
                min = Vector3.Min(min, positions[i]);
                max = Vector3.Max(max, positions[i]);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return new TerrainMeshData("TerrainBackdrop", Vector3.zero, vertices, null, null, bounds);
        }

        private static void Emit(Vector3[] positions, SurfaceSample[] regions, Color32[] groundColors, int i0,
            int i1, int i2, TerrainPainter painter, uint hash, TerrainVertex[] vertices, ref int written)
        {
            Vector3 p0 = positions[i0];
            Vector3 p1 = positions[i1];
            Vector3 p2 = positions[i2];
            Vector3 normal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            var region = new SurfaceSample(0f, 0f, 0f,
                (regions[i0].RimZone + regions[i1].RimZone + regions[i2].RimZone) / 3f);
            bool rock = painter.IsRock(normal, region);
            Color32 face = rock ? painter.Rock(normal, (p0 + p1 + p2) / 3f, hash) : default;
            vertices[written++] = new TerrainVertex(p0, normal, rock ? face : groundColors[i0]);
            vertices[written++] = new TerrainVertex(p1, normal, rock ? face : groundColors[i1]);
            vertices[written++] = new TerrainVertex(p2, normal, rock ? face : groundColors[i2]);
        }
    }
}

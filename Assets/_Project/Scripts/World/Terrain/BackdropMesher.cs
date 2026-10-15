using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Builds the smooth-shaded, indexed, collider-free ring of terrain around the chunk grid that carries the distant
    /// silhouettes out to the fog. It samples the same analytic surface, with rings spaced wider with distance,
    /// and is sunk below the surface wherever the detailed chunks cover it so it never pokes through them.
    /// </summary>
    public static class BackdropMesher
    {
        private const uint JitterSalt = 0xBB67AE85u;
        private const uint DiagonalSalt = 0x5BE0CD19u;
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
            var pocketNoise = new GradientNoise(Hashing.Mix(seed ^ 0x4B1D7E31u));
            float growth = Mathf.Pow(settings.BackdropOuterRadius / settings.BackdropInnerRadius, 1f / rings);
            float segmentAngle = Mathf.PI * 2f / segments;
            int vertexCount = (rings + 1) * segments;
            var vertices = new TerrainVertex[vertexCount];
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

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
                    float r = Mathf.Sqrt(x * x + z * z);
                    Vector3 normal;
                    if (r > 1f && sample.RimZone > 1e-4f)
                    {
                        float tRim = SmoothMath.Smootherstep(0f, 0.2f, sample.RimZone);
                        float pocket = pocketNoise.Fractal(x / 68f, z / 68f, 2, 2f, 0.5f);
                        float slumpPocket = SmoothMath.Smootherstep(-0.05f, 0.35f, pocket);
                        float stepR = Mathf.Lerp(0.25f, Mathf.Lerp(4.8f, 1.35f, slumpPocket), tRim);
                        float stepT = Mathf.Lerp(0.25f, 1.1f, tRim);
                        float urx = x / r;
                        float urz = z / r;
                        float utx = -urz;
                        float utz = urx;
                        float gr = (surface.SampleHeight(x + stepR * urx, z + stepR * urz)
                            - surface.SampleHeight(x - stepR * urx, z - stepR * urz)) / (2f * stepR);
                        float gt = (surface.SampleHeight(x + stepT * utx, z + stepT * utz)
                            - surface.SampleHeight(x - stepT * utx, z - stepT * utz)) / (2f * stepT);
                        normal = new Vector3(-(gr * urx + gt * utx), 1f, -(gr * urz + gt * utz)).normalized;
                    }
                    else
                    {
                        normal = surface.SampleNormal(x, z);
                    }
                    float square = Mathf.Max(Mathf.Abs(x), Mathf.Abs(z));
                    float sink = settings.BackdropSink * (1f - SmoothMath.Smootherstep(
                        settings.GridHalfExtent - SinkFadeInside, settings.GridHalfExtent + SinkFadeOutside, square));
                    var position = new Vector3(x, sample.Height - sink, z);
                    Color32 color = painter.PaintVertex(position, normal, sample);
                    vertices[k * segments + s] = new TerrainVertex(position, normal, color);
                    min = Vector3.Min(min, position);
                    max = Vector3.Max(max, position);
                }
            }

            var indices = new int[rings * segments * 6];
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
                    if ((Hashing.Hash(k, s, seed ^ DiagonalSalt) & 1u) == 0u)
                    {
                        indices[written++] = a;
                        indices[written++] = c;
                        indices[written++] = b;
                        indices[written++] = b;
                        indices[written++] = c;
                        indices[written++] = d;
                    }
                    else
                    {
                        indices[written++] = a;
                        indices[written++] = c;
                        indices[written++] = d;
                        indices[written++] = a;
                        indices[written++] = d;
                        indices[written++] = b;
                    }
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return new TerrainMeshData("TerrainBackdrop", Vector3.zero, vertices, null, null, bounds, indices);
        }
    }
}

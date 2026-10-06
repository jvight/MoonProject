using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World
{
    /// <summary>
    /// Builds Earth as a flat-shaded unit icosphere in its own frame (Y = spin axis) with one palette colour per
    /// face: ocean, seeded continents, a few clouds and the polar caps. LofiEarth places it in the sky. Vertex
    /// colours are linear because the shader uses them directly.
    /// </summary>
    public static class EarthMeshBuilder
    {
        private const uint LandSalt = 0x2545F491u;
        private const uint CloudSalt = 0x4F6CDD1Du;
        private const float PolarCap = 0.86f;
        private const float LandScale = 1.6f;
        private const float CloudScale = 3.1f;

        // The mesh is positioned by the shader around the camera, so CPU culling must never reject it.
        private const float NeverCulledExtent = 1e7f;

        public static Mesh Build(SkySettings sky, int seed)
        {
            if (sky == null)
            {
                throw new ArgumentNullException(nameof(sky));
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            Icosahedron(vertices, triangles);
            for (int i = 0; i < sky.EarthSubdivisions; i++)
            {
                Subdivide(vertices, triangles);
            }

            int faces = triangles.Count / 3;
            var centers = new Vector3[faces];
            var land = new float[faces];
            var cloud = new float[faces];
            var landNoise = new GradientNoise(Hashing.Mix((uint)seed ^ LandSalt));
            var cloudNoise = new GradientNoise(Hashing.Mix((uint)seed ^ CloudSalt));
            for (int f = 0; f < faces; f++)
            {
                Vector3 c = (vertices[triangles[f * 3]] + vertices[triangles[f * 3 + 1]]
                    + vertices[triangles[f * 3 + 2]]).normalized;
                centers[f] = c;
                land[f] = SphereNoise(landNoise, c * LandScale);
                cloud[f] = SphereNoise(cloudNoise, c * CloudScale);
            }

            float landThreshold = Quantile(land, 1f - sky.EarthLandFraction);
            float cloudThreshold = Quantile(cloud, 1f - sky.EarthCloudFraction);
            Color ocean = ((Color)Palette.Get(PaletteSwatch.EarthOcean)).linear;
            Color green = ((Color)Palette.Get(PaletteSwatch.EarthLand)).linear;
            Color white = ((Color)Palette.Get(PaletteSwatch.Cream)).linear;

            var positions = new Vector3[faces * 3];
            var normals = new Vector3[faces * 3];
            var colors = new Color[faces * 3];
            var indices = new int[faces * 3];
            for (int f = 0; f < faces; f++)
            {
                Vector3 a = vertices[triangles[f * 3]];
                Vector3 b = vertices[triangles[f * 3 + 1]];
                Vector3 c = vertices[triangles[f * 3 + 2]];
                Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                bool ice = Mathf.Abs(centers[f].y) > PolarCap || cloud[f] > cloudThreshold;
                Color color = ice ? white : land[f] > landThreshold ? green : ocean;
                for (int k = 0; k < 3; k++)
                {
                    int v = f * 3 + k;
                    positions[v] = k == 0 ? a : k == 1 ? b : c;
                    normals[v] = normal;
                    colors[v] = color;
                    indices[v] = v;
                }
            }

            var mesh = new Mesh { name = "Earth" };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetTriangles(indices, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * NeverCulledExtent);
            return mesh;
        }

        /// <summary>Seamless noise on the sphere: three orthogonal planar samples summed.</summary>
        private static float SphereNoise(GradientNoise noise, Vector3 p)
        {
            return noise.Fractal(p.x, p.y, 3, 2f, 0.5f) + noise.Fractal(p.y + 17.3f, p.z, 3, 2f, 0.5f)
                + noise.Fractal(p.z + 41.7f, p.x, 3, 2f, 0.5f);
        }

        private static float Quantile(float[] values, float fraction)
        {
            var sorted = (float[])values.Clone();
            Array.Sort(sorted);
            int index = Mathf.Clamp(Mathf.FloorToInt(fraction * sorted.Length), 0, sorted.Length - 1);
            return sorted[index];
        }

        private static void Icosahedron(List<Vector3> vertices, List<int> triangles)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            Vector3[] corners =
            {
                new Vector3(-1f, t, 0f), new Vector3(1f, t, 0f), new Vector3(-1f, -t, 0f), new Vector3(1f, -t, 0f),
                new Vector3(0f, -1f, t), new Vector3(0f, 1f, t), new Vector3(0f, -1f, -t), new Vector3(0f, 1f, -t),
                new Vector3(t, 0f, -1f), new Vector3(t, 0f, 1f), new Vector3(-t, 0f, -1f), new Vector3(-t, 0f, 1f),
            };
            foreach (Vector3 corner in corners)
            {
                vertices.Add(corner.normalized);
            }

            int[] faces =
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            triangles.AddRange(faces);
        }

        private static void Subdivide(List<Vector3> vertices, List<int> triangles)
        {
            var midpoints = new Dictionary<long, int>();
            var next = new List<int>(triangles.Count * 4);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                int ab = Midpoint(vertices, midpoints, a, b);
                int bc = Midpoint(vertices, midpoints, b, c);
                int ca = Midpoint(vertices, midpoints, c, a);
                next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }

            triangles.Clear();
            triangles.AddRange(next);
        }

        private static int Midpoint(List<Vector3> vertices, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (!cache.TryGetValue(key, out int index))
            {
                index = vertices.Count;
                vertices.Add(((vertices[a] + vertices[b]) * 0.5f).normalized);
                cache.Add(key, index);
            }

            return index;
        }
    }
}

using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Procedural meshes for the SoftGlow effects, built once at initialisation. uv.x runs along an effect and uv.y
    /// across it, matching the shader's conventions.
    /// </summary>
    public static class GlowMeshes
    {
        /// <summary>
        /// Open cone with its apex at the origin and a unit-radius base at z = 1 (scale z for length, x/y for the
        /// base radius). uv.x = 0 at the apex, 1 at the base; uv.y goes around.
        /// </summary>
        public static Mesh Cone(int sides)
        {
            return Tube(sides, 0f, true, "GlowCone");
        }

        /// <summary>
        /// Open unit cylinder from y = 0 to y = 1 with radius 1. uv.x = height (0 at the bottom), uv.y goes around.
        /// </summary>
        public static Mesh Pillar(int sides)
        {
            return Tube(sides, 1f, false, "GlowPillar");
        }

        /// <summary>Unit-radius UV sphere with outward normals (fresnel glows: flashes, beacons).</summary>
        public static Mesh Sphere(int rings, int segments)
        {
            rings = Mathf.Max(2, rings);
            segments = Mathf.Max(3, segments);
            int rowLength = segments + 1;
            var vertices = new Vector3[(rings + 1) * rowLength];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (int ring = 0; ring <= rings; ring++)
            {
                float polar = Mathf.PI * ring / rings;
                for (int segment = 0; segment <= segments; segment++)
                {
                    float azimuth = 2f * Mathf.PI * segment / segments;
                    var normal = new Vector3(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar),
                        Mathf.Sin(polar) * Mathf.Sin(azimuth));
                    int index = ring * rowLength + segment;
                    vertices[index] = normal;
                    normals[index] = normal;
                    uvs[index] = new Vector2((float)segment / segments, (float)ring / rings);
                }
            }

            var triangles = new int[rings * segments * 6];
            int t = 0;
            for (int ring = 0; ring < rings; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    int a = ring * rowLength + segment;
                    int b = a + rowLength;
                    triangles[t++] = a;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = b + 1;
                    triangles[t++] = b;
                }
            }

            return Assemble("GlowSphere", vertices, normals, uvs, triangles);
        }

        /// <summary>
        /// Quad in the XY plane with corners at (-1, -1) .. (1, 1) (scale for the radius), facing +Z; uv runs 0..1
        /// across it, so the SoftGlow radial mask turns it into a soft round sprite.
        /// </summary>
        public static Mesh Quad()
        {
            var vertices = new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(-1f, 1f, 0f), new Vector3(1f, 1f, 0f),
            };
            var normals = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward };
            var uvs = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            return Assemble("GlowQuad", vertices, normals, uvs, new[] { 0, 2, 1, 1, 2, 3 });
        }

        /// <summary>
        /// Open tube whose first ring has <paramref name="startRadius"/> and second ring radius 1, along +Z
        /// (<paramref name="alongZ"/>) or +Y.
        /// </summary>
        private static Mesh Tube(int sides, float startRadius, bool alongZ, string name)
        {
            sides = Mathf.Max(3, sides);
            int rowLength = sides + 1;
            var vertices = new Vector3[rowLength * 2];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (int row = 0; row < 2; row++)
            {
                float radius = row == 0 ? startRadius : 1f;
                for (int side = 0; side <= sides; side++)
                {
                    float angle = 2f * Mathf.PI * side / sides;
                    float a = Mathf.Cos(angle);
                    float b = Mathf.Sin(angle);
                    int index = row * rowLength + side;
                    vertices[index] = alongZ ? new Vector3(a * radius, b * radius, row)
                        : new Vector3(a * radius, row, b * radius);
                    normals[index] = alongZ ? new Vector3(a, b, 0f) : new Vector3(a, 0f, b);
                    uvs[index] = new Vector2(row, (float)side / sides);
                }
            }

            var triangles = new int[sides * 6];
            int t = 0;
            for (int side = 0; side < sides; side++)
            {
                int a = side;
                int b = side + rowLength;
                triangles[t++] = a;
                triangles[t++] = b;
                triangles[t++] = a + 1;
                triangles[t++] = a + 1;
                triangles[t++] = b;
                triangles[t++] = b + 1;
            }

            return Assemble(name, vertices, normals, uvs, triangles);
        }

        private static Mesh Assemble(string name, Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
            int[] triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// Look spike only, read-only: writes every visible opaque mesh of the running scene (generated terrain chunks,
    /// placed props, scatter, 07, the staged solar array) to one world-space OBJ with a group per object and a material
    /// library of base colours, for tools/art/blender/look_max_unreal_fbx.py to turn into an FBX for Unreal. Positions
    /// are written in Blender's frame (x = Unity z, y = -Unity x, z = Unity y, metres) so that Blender's default FBX
    /// export lands in Unreal at X = Unity z, Y = Unity x, Z = Unity y, the mapping poses.json uses; that frame change
    /// is a mirror, so triangle winding is reversed to keep faces pointing out.
    /// </summary>
    public static class LookMaxSceneExport
    {
        private const int SkyQueue = 2900;
        private static readonly string[] SkippedShaderWords = { "Sky", "Earth", "Glint", "Halo", "Beacon", "Hologram" };

        /// <summary>Writes scene.obj and scene.mtl into <paramref name="folder"/>; returns a one-line summary.</summary>
        public static string Write(string folder)
        {
            Directory.CreateDirectory(folder);
            var materials = new Dictionary<string, Color>();
            int objects = 0;
            var bases = new IndexBases();
            long triangles = 0;
            using (var obj = new StreamWriter(Path.Combine(folder, "scene.obj"), false, new UTF8Encoding(false)))
            {
                obj.WriteLine("mtllib scene.mtl");
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(false))
                    {
                        if (!TryGetMesh(renderer, out Mesh mesh, out bool baked))
                        {
                            continue;
                        }

                        triangles += WriteObject(obj, renderer, mesh, bases, materials);
                        objects++;
                        if (baked)
                        {
                            Object.Destroy(mesh);
                        }
                    }
                }
            }

            using (var mtl = new StreamWriter(Path.Combine(folder, "scene.mtl"), false, new UTF8Encoding(false)))
            {
                foreach (KeyValuePair<string, Color> material in materials)
                {
                    Color c = material.Value.linear;
                    mtl.WriteLine($"newmtl {material.Key}");
                    mtl.WriteLine($"Kd {F(c.r)} {F(c.g)} {F(c.b)}");
                }
            }

            return $"{objects} objects, {triangles} triangles, {materials.Count} materials";
        }

        private static bool TryGetMesh(Renderer renderer, out Mesh mesh, out bool baked)
        {
            mesh = null;
            baked = false;
            if (!renderer.enabled || renderer.sharedMaterial == null || !IsSolid(renderer.sharedMaterial))
            {
                return false;
            }

            if (renderer is SkinnedMeshRenderer skinned)
            {
                mesh = new Mesh();
                skinned.BakeMesh(mesh, true);
                baked = true;
                return true;
            }

            var filter = renderer.GetComponent<MeshFilter>();
            mesh = filter != null ? filter.sharedMesh : null;
            return renderer is MeshRenderer && mesh != null;
        }

        private static bool IsSolid(Material material)
        {
            if (material.renderQueue >= SkyQueue)
            {
                return false;
            }

            string shader = material.shader.name;
            foreach (string word in SkippedShaderWords)
            {
                if (shader.Contains(word))
                {
                    return false;
                }
            }

            return true;
        }

        private static long WriteObject(StreamWriter obj, Renderer renderer, Mesh mesh, IndexBases bases,
            Dictionary<string, Color> materials)
        {
            Matrix4x4 toWorld = renderer is SkinnedMeshRenderer
                ? Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one)
                : renderer.localToWorldMatrix;
            MeshArrays arrays = MeshArrays.Read(mesh);
            Vector3[] vertices = arrays.Vertices;
            Vector3[] normals = arrays.Normals;
            Vector2[] uvs = arrays.Uvs;
            bool hasNormals = normals.Length == vertices.Length;
            bool hasUvs = uvs.Length == vertices.Length;
            obj.WriteLine($"o {Clean(renderer.name)}_{renderer.GetInstanceID() & 0x7fffffff}");
            var line = new StringBuilder();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = toWorld.MultiplyPoint3x4(vertices[i]);
                obj.WriteLine($"v {F(p.z)} {F(-p.x)} {F(p.y)}");
            }

            for (int i = 0; hasNormals && i < normals.Length; i++)
            {
                Vector3 n = toWorld.MultiplyVector(normals[i]).normalized;
                obj.WriteLine($"vn {F(n.z)} {F(-n.x)} {F(n.y)}");
            }

            for (int i = 0; hasUvs && i < uvs.Length; i++)
            {
                obj.WriteLine($"vt {F(uvs[i].x)} {F(uvs[i].y)}");
            }

            long written = 0;
            Material[] shared = renderer.sharedMaterials;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                Material material = shared[Mathf.Min(sub, shared.Length - 1)];
                string name = material != null ? Clean(material.name) : "None";
                if (!materials.ContainsKey(name))
                {
                    materials[name] = material != null && material.HasProperty("_BaseColor")
                        ? material.GetColor("_BaseColor")
                        : Color.grey;
                }

                obj.WriteLine($"usemtl {name}");
                int[] tris = arrays.Triangles[sub];
                for (int t = 0; t < tris.Length; t += 3)
                {
                    line.Clear();
                    line.Append('f');
                    // Reversed: the frame change is a mirror
                    AppendCorner(line, bases, tris[t], hasUvs, hasNormals);
                    AppendCorner(line, bases, tris[t + 2], hasUvs, hasNormals);
                    AppendCorner(line, bases, tris[t + 1], hasUvs, hasNormals);
                    obj.WriteLine(line.ToString());
                    written++;
                }
            }

            bases.Vertex += vertices.Length;
            bases.Uv += hasUvs ? uvs.Length : 0;
            bases.Normal += hasNormals ? normals.Length : 0;
            return written;
        }

        private static void AppendCorner(StringBuilder line, IndexBases bases, int index, bool uv, bool normal)
        {
            line.Append(' ').Append(bases.Vertex + index);
            if (uv || normal)
            {
                line.Append('/');
                if (uv)
                {
                    line.Append(bases.Uv + index);
                }

                if (normal)
                {
                    line.Append('/').Append(bases.Normal + index);
                }
            }
        }

        /// <summary>OBJ numbers positions, UVs and normals separately, from 1, across the whole file.</summary>
        private sealed class IndexBases
        {
            public long Vertex = 1;
            public long Uv = 1;
            public long Normal = 1;
        }

        private static string Clean(string name)
        {
            var clean = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                clean.Append(char.IsLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
            }

            return clean.ToString();
        }

        private static string F(float value)
        {
            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }
    }
}

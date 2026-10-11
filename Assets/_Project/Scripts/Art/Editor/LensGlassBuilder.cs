using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's lens glass for the opening's pull-out (docs/design/cutscenes.md O10 and §1.8, Art's part):
    /// <c>Generated/Art/Rover/LensGlass.prefab</c>, a thin smooth glass ring just inside the eye's bezel on
    /// <c>M_LensGlass</c> (LofiLensGlass, additive: only Earth's reflection and a faint edge sheen show). Drawn in eye
    /// space (origin at the lens centre, +Z out of the lens, as <see cref="RoverMeshes.Eye"/>); Rover mounts it on the
    /// RoverModel's Eye node with identity, so it turns with the eye. Smooth normals, so the reflection slides round
    /// the ring as the camera moves. No shadows, no probes.
    /// </summary>
    public static class LensGlassBuilder
    {
        public const string GlassName = "LensGlass";

        /// <summary>The ring's radius: just inside the bezel (radius 0.162, tube 0.013), over the lens's rim.</summary>
        public const float RingRadius = 0.146f;

        /// <summary>The glass tube's radius.</summary>
        public const float TubeRadius = 0.01f;

        /// <summary>The ring's centre on the lens axis: a hair in front of the bezel's centre line (0.013).</summary>
        public const float RingForward = 0.016f;

        public const int RingSegments = 32;
        public const int TubeSegments = 8;

        [MoonBuilder("Art/LensGlass", 122)]
        public static void Build()
        {
            Material material = GeneratedAssets.CreateOrReplace(CreateMaterial(), ArtPaths.LensGlassMaterial);
            Mesh ring = GeneratedAssets.CreateOrReplace(CreateRing(), $"{ArtPaths.RoverFolder}/{GlassName}_Ring.asset");
            using (var scratch = new BuilderScratchScene())
            {
                GameObject glass = scratch.Create(GlassName);
                glass.AddComponent<MeshFilter>().sharedMesh = ring;
                var renderer = glass.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                GeneratedAssets.SavePrefab(glass, $"{ArtPaths.RoverFolder}/{GlassName}.prefab");
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>The glass's material (not saved), at the shader's authored reflection.</summary>
        public static Material CreateMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArtPaths.LensGlassShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"The lens glass shader is missing at {ArtPaths.LensGlassShader}.");
            }

            return new Material(shader) { enableInstancing = true };
        }

        /// <summary>A smooth torus round the lens axis, its normals pointing out of the tube.</summary>
        public static Mesh CreateRing()
        {
            int columns = RingSegments + 1;
            int rows = TubeSegments + 1;
            var vertices = new Vector3[columns * rows];
            var normals = new Vector3[vertices.Length];
            var triangles = new int[RingSegments * TubeSegments * 6];
            for (int i = 0; i < columns; i++)
            {
                float around = i * Mathf.PI * 2f / RingSegments;
                var radial = new Vector3(Mathf.Cos(around), Mathf.Sin(around), 0f);
                for (int j = 0; j < rows; j++)
                {
                    float tube = j * Mathf.PI * 2f / TubeSegments;
                    Vector3 normal = radial * Mathf.Cos(tube) + Vector3.forward * Mathf.Sin(tube);
                    int v = i * rows + j;
                    vertices[v] = radial * RingRadius + Vector3.forward * RingForward + normal * TubeRadius;
                    normals[v] = normal;
                }
            }

            int t = 0;
            for (int i = 0; i < RingSegments; i++)
            {
                for (int j = 0; j < TubeSegments; j++)
                {
                    int a = i * rows + j;
                    int b = (i + 1) * rows + j;
                    triangles[t++] = a;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = b + 1;
                    triangles[t++] = a + 1;
                }
            }

            var mesh = new Mesh { name = GlassName + "_Ring" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

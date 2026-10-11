using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The warm light pool for World's lamps (M3-18 V3): <c>Generated/Art/Props/LightPool.prefab</c>, one unit prism
    /// (radius 1 round the pool, <see cref="PoolBand"/> above and below the ground) on <c>M_LightPool</c>
    /// (LofiLightPool, additive), its root at the pool's centre on the ground, +Y up, no shadows. The shader projects
    /// the pool onto the surfaces inside the prism from the scene's depth, so it follows the ground instead of laying a
    /// flat disc that fights it. World scales it to each pool's radius and eases its strength per renderer with a
    /// MaterialPropertyBlock (SetVector _EmissionColor, linear; ARCHITECTURE.md "Glow modulation").
    /// </summary>
    public static class LightPoolBuilder
    {
        public const string PoolName = "LightPool";

        /// <summary>Sides of the prism: the falloff is per pixel, so a few make a round pool.</summary>
        public const int RimCorners = 16;

        /// <summary>
        /// The prism's half-height as a share of the pool's radius: the ground under a pool (the dock pad, the bay
        /// apron, a relay's pad) is flat, so this keeps the pool on the ground and off 07's deck and the props'
        /// tops (0.6 m for the dock's 5 m pool, 0.34 m for a 2.8 m charger pool).
        /// </summary>
        public const float PoolBand = 0.12f;

        /// <summary>
        /// How bright the pool's centre is at strength 1, as a share of the warm lamps' own surface colour: a faint
        /// warm floor under a lamp, far under the bloom threshold, so the lamp itself stays the brightest point.
        /// </summary>
        public const float PoolStrength = 0.22f;

        /// <summary>The pool's (1 - r^2) exponent: 2 is a soft pool with no visible rim.</summary>
        public const float PoolFalloff = 2f;

        [MoonBuilder("Art/LightPool", 176)]
        public static void Build()
        {
            Material material = GeneratedAssets.CreateOrReplace(CreateMaterial(), ArtPaths.LightPoolMaterial);
            // The mesh keeps its first asset name (it was a flat disc) so its GUID and the prefab's reference stay.
            Mesh volume = GeneratedAssets.CreateOrReplace(CreateVolume(),
                $"{ArtPaths.PropFolder}/{PoolName}_Disc.asset");
            using (var scratch = new BuilderScratchScene())
            {
                GameObject pool = scratch.Create(PoolName);
                pool.AddComponent<MeshFilter>().sharedMesh = volume;
                var renderer = pool.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                GeneratedAssets.SavePrefab(pool, $"{ArtPaths.PropFolder}/{PoolName}.prefab");
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>The pool's material (not saved): warm, additive, authored at strength 1.</summary>
        public static Material CreateMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArtPaths.LightPoolShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"The light pool shader is missing at {ArtPaths.LightPoolShader}.");
            }

            var material = new Material(shader);
            Color warm = ((Color)Palette.GetSurface(PaletteSwatch.WarmLamp)).linear * PoolStrength;
            material.SetColor("_BaseColor", warm.gamma);
            material.SetVector("_EmissionColor", Vector4.one);
            material.SetFloat("_Falloff", PoolFalloff);
            material.SetFloat("_Band", PoolBand);
            material.enableInstancing = true;
            return material;
        }

        /// <summary>
        /// A closed prism of <see cref="RimCorners"/> sides round the unit circle (its flat sides touch radius 1, so
        /// the whole pool is inside), from -<see cref="PoolBand"/> to +<see cref="PoolBand"/>, faces outward.
        /// </summary>
        public static Mesh CreateVolume()
        {
            float rim = 1f / Mathf.Cos(Mathf.PI / RimCorners);
            var vertices = new Vector3[RimCorners * 2 + 2];
            var triangles = new int[RimCorners * 12];
            int top = RimCorners * 2;
            int bottom = top + 1;
            vertices[top] = new Vector3(0f, PoolBand, 0f);
            vertices[bottom] = new Vector3(0f, -PoolBand, 0f);
            for (int i = 0; i < RimCorners; i++)
            {
                float angle = (i + 0.5f) * Mathf.PI * 2f / RimCorners;
                var corner = new Vector3(Mathf.Cos(angle) * rim, 0f, Mathf.Sin(angle) * rim);
                vertices[i * 2] = corner + Vector3.up * PoolBand;
                vertices[i * 2 + 1] = corner - Vector3.up * PoolBand;
            }

            for (int i = 0; i < RimCorners; i++)
            {
                int upper = i * 2;
                int lower = upper + 1;
                int nextUpper = (i + 1) % RimCorners * 2;
                int nextLower = nextUpper + 1;
                int t = i * 12;
                SetTriangle(triangles, t, top, nextUpper, upper);
                SetTriangle(triangles, t + 3, bottom, lower, nextLower);
                SetTriangle(triangles, t + 6, upper, nextUpper, nextLower);
                SetTriangle(triangles, t + 9, upper, nextLower, lower);
            }

            var mesh = new Mesh { name = PoolName + "_Volume" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SetTriangle(int[] triangles, int at, int a, int b, int c)
        {
            triangles[at] = a;
            triangles[at + 1] = b;
            triangles[at + 2] = c;
        }
    }
}

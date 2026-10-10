using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The warm light-decal disc for World's light pools (M3-18 V3): <c>Generated/Art/Props/LightPool.prefab</c>, one
    /// flat 1 m radius disc on <c>M_LightPool</c> (LofiLightPool, additive), its root at the disc's centre on the
    /// ground, +Y up, no shadows. World scales it to each pool's radius and eases its strength per renderer with a
    /// MaterialPropertyBlock (SetVector _EmissionColor, linear; ARCHITECTURE.md "Glow modulation").
    /// </summary>
    public static class LightPoolBuilder
    {
        public const string PoolName = "LightPool";

        /// <summary>Corners of the disc's rim: the falloff is per pixel, so a few make a round pool.</summary>
        public const int RimCorners = 16;

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
            Mesh disc = GeneratedAssets.CreateOrReplace(CreateDisc(), $"{ArtPaths.PropFolder}/{PoolName}_Disc.asset");
            using (var scratch = new BuilderScratchScene())
            {
                GameObject pool = scratch.Create(PoolName);
                pool.AddComponent<MeshFilter>().sharedMesh = disc;
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
            material.enableInstancing = true;
            return material;
        }

        /// <summary>A flat fan of <see cref="RimCorners"/> triangles, radius 1, facing up, uv = (x, z).</summary>
        public static Mesh CreateDisc()
        {
            var vertices = new Vector3[RimCorners + 1];
            var uvs = new Vector2[RimCorners + 1];
            var normals = new Vector3[RimCorners + 1];
            var triangles = new int[RimCorners * 3];
            normals[0] = Vector3.up;
            for (int i = 0; i < RimCorners; i++)
            {
                float angle = i * Mathf.PI * 2f / RimCorners;
                var rim = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vertices[i + 1] = new Vector3(rim.x, 0f, rim.y);
                uvs[i + 1] = rim;
                normals[i + 1] = Vector3.up;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = 1 + (i + 1) % RimCorners;
                triangles[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh { name = PoolName + "_Disc" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

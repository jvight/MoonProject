using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Six hand-picked <see cref="RockGenerator"/> rocks, pebble to boulder, as prefabs <c>Rock_00</c>..<c>Rock_05</c>
    /// (origin on the ground contact point, meshes only: the world scatter adds colliders/layers it needs).
    /// </summary>
    public static class RockModelBuilder
    {
        public const int VariantCount = 6;

        [MoonBuilder("Art/Rocks", 110)]
        public static void Build()
        {
            Build(0f, 0f);
        }

        /// <summary>Builds the rock prefabs with optional smooth-by-angle normals and vertex AO.</summary>
        public static void Build(float smoothAngleDegrees, float aoStrength)
        {
            Material material = PaletteAssetBuilder.LoadMaterial();
            for (int i = 0; i < VariantCount; i++)
            {
                ModelPrefabWriter.Write(CreateModel(i, smoothAngleDegrees, aoStrength), ArtPaths.RockFolder, material);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Variant <paramref name="index"/> (0 = smallest) as a single-node model.</summary>
        public static ModelNode CreateModel(int index)
        {
            return CreateModel(index, 0f, 0f);
        }

        /// <summary>Variant <paramref name="index"/> with optional smooth-by-angle normals and vertex AO.</summary>
        public static ModelNode CreateModel(int index, float smoothAngleDegrees, float aoStrength)
        {
            Variant(index, out int seed, out float size, out RockStyle style);
            var builder = new LowPolyMeshBuilder(style == RockStyle.Boulder ? 320 : 80);
            MeshRange range = RockGenerator.Build(builder, seed, size, style, Matrix4x4.identity);
            if (smoothAngleDegrees > 0f)
            {
                float angle = style == RockStyle.Slab || style == RockStyle.Jagged
                    ? Mathf.Min(smoothAngleDegrees, 38f)
                    : smoothAngleDegrees;
                MeshNormalSmoother.Smooth(builder, range, angle);
            }

            if (aoStrength > 0f)
            {
                MeshNormalSmoother.BakeRockOcclusion(builder, aoStrength);
            }

            string name = $"Rock_{index:00}";
            return new ModelNode(name, Vector3.zero, new ModelMesh(name, builder));
        }

        private static void Variant(int index, out int seed, out float size, out RockStyle style)
        {
            switch (index)
            {
                case 0:
                    seed = 11;
                    size = 0.35f;
                    style = RockStyle.Pebble;
                    return;
                case 1:
                    seed = 23;
                    size = 0.8f;
                    style = RockStyle.Rounded;
                    return;
                case 2:
                    seed = 41;
                    size = 1f;
                    style = RockStyle.Jagged;
                    return;
                case 3:
                    seed = 37;
                    size = 1.3f;
                    style = RockStyle.Slab;
                    return;
                case 4:
                    seed = 58;
                    size = 1.8f;
                    style = RockStyle.Rounded;
                    return;
                case 5:
                    seed = 73;
                    size = 3.4f;
                    style = RockStyle.Boulder;
                    return;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(index), index, "Rock variants are 0..5.");
            }
        }
    }
}

using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Art.Editor;
using MoonProject.Editor.Builders;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// The M1 beacon on The Peak, built with the art kit: a short mast on a squat base, a small dish turned toward
    /// Earth and a round AlertSoft lamp on top (its own renderer, so the glow contract can breathe it), plus the
    /// halo quad and material. Art replaces the model with the broken satellite dish in M4; the lamp node must keep
    /// the name <see cref="LampNodeName"/>.
    /// </summary>
    public static class PeakBeaconBuilder
    {
        public const string ModelName = "PeakBeaconModel";
        public const string LampNodeName = "Lamp";

        private const float BaseHeight = 0.5f;
        private const float BaseRadius = 0.9f;
        private const float MastHeight = 6f;
        private const float MastRadius = 0.1f;
        private const float DishHeight = 4.4f;
        private const float DishOffset = 0.35f;
        private const float DishRadius = 1.1f;
        private const float DishDepth = 0.35f;
        private const float FeedLength = 0.9f;
        private const float LampRadius = 0.22f;
        private const float HaloBoundsExtent = 8f;

        /// <summary>Height of the lamp centre above the model's ground-contact origin.</summary>
        public const float LampHeight = BaseHeight + MastHeight + LampRadius;

        [MoonBuilder("World/Peak Beacon", 230)]
        public static void Build()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ArtPaths.LowPolyMaterial);
            if (settings == null || material == null)
            {
                throw new InvalidOperationException("Run World/Settings and Art/Palette before World/Peak Beacon.");
            }

            ModelPrefabWriter.Write(CreateModel(settings.Sky.EarthDirection), WorldPaths.BeaconFolder, material);
            GeneratedAssets.CreateOrReplace(CreateHaloQuad(), WorldPaths.BeaconHaloMesh);
            Shader shader = Shader.Find(WorldPaths.BeaconHaloShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"Shader '{WorldPaths.BeaconHaloShader}' not found.");
            }

            GeneratedAssets.CreateOrReplace(new Material(shader), WorldPaths.BeaconHaloMaterial);
            AssetDatabase.SaveAssets();
        }

        private static ModelNode CreateModel(Vector3 towardEarth)
        {
            var mast = new LowPolyMeshBuilder();
            mast.Frustum(Place.At(0f, BaseHeight * 0.5f, 0f), BaseRadius, BaseRadius * 0.6f, BaseHeight, 6,
                PaletteSwatch.Charcoal);
            mast.Prism(Place.At(0f, BaseHeight + MastHeight * 0.5f, 0f), MastRadius, MastHeight, 6,
                PaletteSwatch.Metal);

            // The dish looks at Earth: the endgame broadcast goes there.
            Vector3 horizontal = new Vector3(towardEarth.x, 0f, towardEarth.z).normalized;
            Quaternion aim = Quaternion.FromToRotation(Vector3.up, towardEarth);
            Vector3 dishCenter = new Vector3(0f, DishHeight, 0f) + horizontal * DishOffset;
            mast.Frustum(Place.At(dishCenter, aim), DishRadius * 0.15f, DishRadius, DishDepth, 10,
                Paint.WithCaps(PaletteSwatch.Metal, PaletteSwatch.Cream));
            mast.Prism(Place.At(dishCenter + towardEarth * (FeedLength * 0.5f + DishDepth * 0.5f), aim), 0.05f,
                FeedLength, 5, PaletteSwatch.Charcoal);

            var lamp = new LowPolyMeshBuilder();
            lamp.Icosphere(Matrix4x4.identity, LampRadius, 1, PaletteSwatch.AlertSoft);

            var root = new ModelNode(ModelName, Vector3.zero);
            root.Add(new ModelNode("Mast", Vector3.zero, new ModelMesh("PeakBeacon_Mast", mast)));
            root.Add(new ModelNode(LampNodeName, new Vector3(0f, LampHeight, 0f),
                new ModelMesh("PeakBeacon_Lamp", lamp)));
            return root;
        }

        private static Mesh CreateHaloQuad()
        {
            var mesh = new Mesh();
            mesh.SetVertices(new[]
            {
                new Vector3(-1f, -1f, 0f), new Vector3(1f, -1f, 0f), new Vector3(1f, 1f, 0f), new Vector3(-1f, 1f, 0f),
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0, false);

            // The shader sizes the quad by distance, so the culling bounds must cover the largest close-up halo.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * HaloBoundsExtent * 2f);
            return mesh;
        }
    }
}

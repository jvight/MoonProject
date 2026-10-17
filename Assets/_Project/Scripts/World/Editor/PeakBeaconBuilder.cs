using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// The halo quad and material every World beacon glows with (The Peak's, the lander's). The Peak's model is Art's
    /// R1 PeakBeacon (<see cref="WorldPaths.BeaconModelPrefab"/>); its lamp node is <see cref="LampNodeName"/>.
    /// </summary>
    public static class PeakBeaconBuilder
    {
        public const string LampNodeName = "Lamp";

        /// <summary>Height of the R1 Peak beacon's lamp above its ground-contact origin (contract peak_beacon.json).</summary>
        public const float LampHeight = 6.72f;

        private const float HaloBoundsExtent = 8f;

        [MoonBuilder("World/Peak Beacon", 230)]
        public static void Build()
        {
            GeneratedAssets.CreateOrReplace(CreateHaloQuad(), WorldPaths.BeaconHaloMesh);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(WorldPaths.BeaconHaloShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"No shader at '{WorldPaths.BeaconHaloShader}'.");
            }

            GeneratedAssets.CreateOrReplace(new Material(shader), WorldPaths.BeaconHaloMaterial);
            AssetDatabase.SaveAssets();
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

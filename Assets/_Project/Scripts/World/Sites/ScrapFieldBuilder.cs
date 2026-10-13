using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Builds Kestrel-3's scrap field at boot (0.5.5 §2): rusted hull plates with worn faces, half-buried along the
    /// furrow, each a chamfered art-kit box with a box collider on <see cref="Layers.Ground"/> so 07 bumps them like
    /// rock. Scenery only: no glow, nothing to pick up.
    /// </summary>
    public sealed class ScrapFieldBuilder
    {
        private const float Chamfer = 0.03f;
        private readonly List<Mesh> _meshes = new List<Mesh>();

        public void Build(Transform parent, MoonSurface surface, ScrapFieldSettings settings, int seed,
            Material material, HideFlags hideFlags)
        {
            if (parent == null || surface == null || settings == null || material == null)
            {
                throw new ArgumentNullException(nameof(parent), "Scrap field build inputs must all be provided.");
            }

            var root = new GameObject("KestrelScrapField") { hideFlags = hideFlags };
            root.transform.SetParent(parent, false);
            List<ScrapPlate> plates = ScrapFieldPlanner.Plan(surface, settings, seed);
            for (int i = 0; i < plates.Count; i++)
            {
                ScrapPlate plate = plates[i];
                var builder = new LowPolyMeshBuilder();
                builder.Box(Matrix4x4.identity, plate.Size, Paint.WithCaps(PaletteSwatch.Rust, PaletteSwatch.WornPanel),
                    Chamfer);
                Mesh mesh = builder.ToMesh($"ScrapPlate_{i}");
                mesh.hideFlags = hideFlags;
                _meshes.Add(mesh);
                var piece = new GameObject($"ScrapPlate_{i}") { hideFlags = hideFlags, layer = Layers.Ground };
                piece.transform.SetParent(root.transform, false);
                piece.transform.SetPositionAndRotation(plate.Centre, plate.Rotation);
                piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = piece.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                piece.AddComponent<BoxCollider>().size = plate.Size;
            }
        }

        public void DestroyMeshes()
        {
            foreach (Mesh mesh in _meshes)
            {
                if (mesh == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(mesh);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(mesh);
                }
            }

            _meshes.Clear();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Builds Whispering Canyon's gate slabs and the exit's gate posts: chamfered art-kit rock boxes (dark sides,
    /// light top) with a box collider each on <see cref="Layers.Ground"/>, so the chasm's far face and the exit's step
    /// are truly vertical to the rover (design ruling 10: never climbable) while their tops are flat ground to land
    /// and drive on.
    /// </summary>
    public sealed class CanyonGateBuilder
    {
        private const float Chamfer = 0.25f;

        private readonly List<Mesh> _meshes = new List<Mesh>();

        public void Build(Transform parent, IReadOnlyList<CanyonSlab> slabs, Material material, HideFlags hideFlags)
        {
            if (parent == null || slabs == null || material == null)
            {
                throw new ArgumentNullException(nameof(parent), "Gate build inputs must all be provided.");
            }

            var root = new GameObject("CanyonGates") { hideFlags = hideFlags };
            root.transform.SetParent(parent, false);
            for (int i = 0; i < slabs.Count; i++)
            {
                CanyonSlab slab = slabs[i];
                var builder = new LowPolyMeshBuilder();
                Paint paint = Paint.WithCaps(PaletteSwatch.RockDark, PaletteSwatch.RockLight);
                builder.Box(Matrix4x4.identity, slab.Size, paint, Chamfer);
                Mesh mesh = builder.ToMesh($"CanyonGate_{i}");
                mesh.hideFlags = hideFlags;
                _meshes.Add(mesh);

                var piece = new GameObject($"CanyonGate_{i}") { hideFlags = hideFlags, layer = Layers.Ground };
                piece.transform.SetParent(root.transform, false);
                piece.transform.SetPositionAndRotation(slab.Centre, Quaternion.Euler(0f, slab.YawDegrees, 0f));
                piece.AddComponent<MeshFilter>().sharedMesh = mesh;
                var meshRenderer = piece.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = material;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                piece.AddComponent<BoxCollider>().size = slab.Size;
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

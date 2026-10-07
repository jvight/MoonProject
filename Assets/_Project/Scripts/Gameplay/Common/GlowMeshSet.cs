using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The procedural glow meshes every effect shares, created once and destroyed with the gameplay root.
    /// </summary>
    public sealed class GlowMeshSet : IDisposable
    {
        private const int ConeSides = 24;
        private const int PillarSides = 12;
        private const int SphereRings = 8;
        private const int SphereSegments = 12;

        public GlowMeshSet()
        {
            Cone = GlowMeshes.Cone(ConeSides);
            Pillar = GlowMeshes.Pillar(PillarSides);
            Sphere = GlowMeshes.Sphere(SphereRings, SphereSegments);
            Quad = GlowMeshes.Quad();
        }

        public Mesh Cone { get; }

        public Mesh Pillar { get; }

        public Mesh Sphere { get; }

        public Mesh Quad { get; }

        public void Dispose()
        {
            Destroy(Cone);
            Destroy(Pillar);
            Destroy(Sphere);
            Destroy(Quad);
        }

        private static void Destroy(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
